"""Feature quality and distribution checks before model experiments."""

from __future__ import annotations

import math

import pandas as pd

from cabrynt_trip_duration.enrichment import ENRICHED_FEATURE_COLUMNS
from cabrynt_trip_duration.evaluation import BASE_FEATURE_COLUMNS, TARGET_COLUMN

MODEL_FEATURE_COLUMNS = [*BASE_FEATURE_COLUMNS, *ENRICHED_FEATURE_COLUMNS]
HIGH_CORRELATION_THRESHOLD = 0.80
LARGE_MEAN_SHIFT_THRESHOLD = 0.50


def audit_features(train_data: pd.DataFrame, validation_data: pd.DataFrame) -> dict[str, object]:
    """Summarize candidate model features without fitting a predictive model."""
    _validate_input_columns(train_data)
    _validate_input_columns(validation_data)

    feature_summaries = [
        _feature_summary(feature, train_data[feature], validation_data[feature], train_data[TARGET_COLUMN])
        for feature in MODEL_FEATURE_COLUMNS
    ]
    high_correlation_pairs = _high_correlation_pairs(train_data[MODEL_FEATURE_COLUMNS])

    return {
        "training_rows": len(train_data),
        "validation_rows": len(validation_data),
        "feature_count": len(MODEL_FEATURE_COLUMNS),
        "feature_summaries": feature_summaries,
        "constant_features": [
            summary["feature"] for summary in feature_summaries if summary["train_unique_values"] <= 1
        ],
        "features_with_missing_training_values": [
            summary["feature"] for summary in feature_summaries if summary["train_missing_rate"] > 0
        ],
        "features_with_missing_validation_values": [
            summary["feature"]
            for summary in feature_summaries
            if summary["validation_missing_rate"] > 0
        ],
        "features_with_large_mean_shift": [
            {
                "feature": summary["feature"],
                "standardized_mean_shift": summary["standardized_mean_shift"],
            }
            for summary in feature_summaries
            if summary["standardized_mean_shift"] is not None
            and abs(float(summary["standardized_mean_shift"])) >= LARGE_MEAN_SHIFT_THRESHOLD
        ],
        "high_correlation_pairs": high_correlation_pairs,
    }


def _feature_summary(
    feature: str,
    train_values: pd.Series,
    validation_values: pd.Series,
    target: pd.Series,
) -> dict[str, object]:
    train_mean = float(train_values.mean())
    train_standard_deviation = float(train_values.std())
    validation_mean = float(validation_values.mean())
    target_correlation = (
        train_values.corr(target) if train_values.nunique(dropna=True) > 1 else None
    )
    standardized_mean_shift = _standardized_mean_shift(
        train_mean,
        validation_mean,
        train_standard_deviation,
    )

    return {
        "feature": feature,
        "train_missing_rate": float(train_values.isna().mean()),
        "validation_missing_rate": float(validation_values.isna().mean()),
        "train_unique_values": int(train_values.nunique(dropna=True)),
        "validation_unique_values": int(validation_values.nunique(dropna=True)),
        "train_mean": train_mean,
        "validation_mean": validation_mean,
        "train_standard_deviation": train_standard_deviation,
        "standardized_mean_shift": standardized_mean_shift,
        "target_correlation": _finite_float(target_correlation),
    }


def _high_correlation_pairs(data: pd.DataFrame) -> list[dict[str, object]]:
    non_constant_features = [
        feature for feature in MODEL_FEATURE_COLUMNS if data[feature].nunique(dropna=True) > 1
    ]
    correlation_data = data[non_constant_features].dropna()
    usable_features = [
        feature for feature in non_constant_features if correlation_data[feature].nunique(dropna=True) > 1
    ]
    if len(usable_features) < 2:
        return []

    correlations = correlation_data[usable_features].corr()
    pairs: list[dict[str, object]] = []

    for index, left_feature in enumerate(MODEL_FEATURE_COLUMNS):
        for right_feature in MODEL_FEATURE_COLUMNS[index + 1 :]:
            if left_feature not in correlations or right_feature not in correlations:
                continue
            correlation = correlations.loc[left_feature, right_feature]
            if pd.notna(correlation) and abs(correlation) >= HIGH_CORRELATION_THRESHOLD:
                pairs.append(
                    {
                        "left_feature": left_feature,
                        "right_feature": right_feature,
                        "correlation": float(correlation),
                    }
                )

    return sorted(pairs, key=lambda pair: abs(float(pair["correlation"])), reverse=True)


def _validate_input_columns(data: pd.DataFrame) -> None:
    required_columns = set([TARGET_COLUMN, *MODEL_FEATURE_COLUMNS])
    missing_columns = required_columns - set(data)
    if missing_columns:
        raise ValueError(f"data is missing columns: {sorted(missing_columns)}")


def _standardized_mean_shift(
    train_mean: float,
    validation_mean: float,
    train_standard_deviation: float,
) -> float | None:
    if train_standard_deviation == 0 or not math.isfinite(train_standard_deviation):
        return None
    return (validation_mean - train_mean) / train_standard_deviation


def _finite_float(value: float | None) -> float | None:
    if value is None or not math.isfinite(value):
        return None
    return float(value)
