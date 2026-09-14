"""Evaluate first enriched trip-duration models without reading the test split."""

from __future__ import annotations

import json
from pathlib import Path

import numpy as np
import pandas as pd

from cabrynt_trip_duration.evaluation import (
    TARGET_COLUMN,
    evaluate_prediction_sets,
    fixed_speed_predictions,
    linear_regression_predictions,
    training_median_predictions,
)
from cabrynt_trip_duration.modeling import (
    CALENDAR_WEATHER_FEATURE_COLUMNS,
    FULL_ENRICHED_FEATURE_COLUMNS,
    GRADIENT_BOOSTING_PARAMETERS,
    enriched_linear_regression_predictions,
    gradient_boosting_predictions,
    warm_start_gradient_boosting_predictions,
)

PROJECT_ROOT = Path(__file__).resolve().parents[1]
DATA_DIRECTORY = PROJECT_ROOT / "artifacts" / "enriched-data"
RESULT_PATH = PROJECT_ROOT / "artifacts" / "model-metrics" / "enriched-models.json"
DURATION_BINS = [0, 5, 10, 20, 30, np.inf]
DURATION_LABELS = ["0-5", "5-10", "10-20", "20-30", "30+"]


def main() -> None:
    train_data, validation_data = _load_enriched_data()
    prediction_sets = _prediction_sets(train_data, validation_data)
    metrics = evaluate_prediction_sets(
        validation_data[TARGET_COLUMN],
        prediction_sets,
    ).round(3)
    segment_metrics = _duration_segment_metrics(
        validation_data[TARGET_COLUMN],
        prediction_sets,
    ).round(3)

    RESULT_PATH.parent.mkdir(parents=True, exist_ok=True)
    RESULT_PATH.write_text(
        json.dumps(
            {
                "training_rows": len(train_data),
                "validation_rows": len(validation_data),
                "experiments": _experiment_metadata(train_data),
                "metrics": metrics.to_dict(orient="index"),
                "duration_segment_metrics": segment_metrics.to_dict(orient="index"),
            },
            indent=2,
        ),
        encoding="utf-8",
    )

    print("Enriched validation metrics (minutes)")
    print(metrics.to_string())
    print("\nMAE by actual trip duration (minutes)")
    print(segment_metrics.to_string())
    print(f"\nWrote metrics to {RESULT_PATH}")


def _load_enriched_data() -> tuple[pd.DataFrame, pd.DataFrame]:
    train_path = DATA_DIRECTORY / "train.parquet"
    validation_path = DATA_DIRECTORY / "validation.parquet"
    missing_paths = [path for path in (train_path, validation_path) if not path.exists()]
    if missing_paths:
        raise FileNotFoundError(
            "Enriched data is missing. Run 'python scripts/build_enriched_datasets.py' first: "
            f"{missing_paths}"
        )

    return pd.read_parquet(train_path), pd.read_parquet(validation_path)


def _prediction_sets(
    train_data: pd.DataFrame,
    validation_data: pd.DataFrame,
) -> dict[str, np.ndarray]:
    baseline_predictions = {
        name: predictions
        for name, predictions in _baseline_prediction_sets(train_data, validation_data).items()
    }

    return {
        **baseline_predictions,
        "enriched_linear_regression": enriched_linear_regression_predictions(
            train_data,
            validation_data,
        ),
        "calendar_weather_gradient_boosting": gradient_boosting_predictions(
            train_data,
            validation_data,
            CALENDAR_WEATHER_FEATURE_COLUMNS,
        ),
        "full_enriched_gradient_boosting": gradient_boosting_predictions(
            train_data,
            validation_data,
            FULL_ENRICHED_FEATURE_COLUMNS,
        ),
        "warm_start_gradient_boosting": warm_start_gradient_boosting_predictions(
            train_data,
            validation_data,
        ),
    }


def _experiment_metadata(train_data: pd.DataFrame) -> dict[str, object]:
    warm_start_rows = int(train_data["historical_congestion_available"].eq(1).sum())
    return {
        "enriched_linear_regression": {
            "model": "LinearRegression",
            "feature_columns": FULL_ENRICHED_FEATURE_COLUMNS,
            "training_rows": len(train_data),
            "missing_value_policy": "median imputation",
        },
        "calendar_weather_gradient_boosting": {
            "model": "HistGradientBoostingRegressor",
            "feature_columns": CALENDAR_WEATHER_FEATURE_COLUMNS,
            "training_rows": len(train_data),
            "parameters": GRADIENT_BOOSTING_PARAMETERS,
        },
        "full_enriched_gradient_boosting": {
            "model": "HistGradientBoostingRegressor",
            "feature_columns": FULL_ENRICHED_FEATURE_COLUMNS,
            "training_rows": len(train_data),
            "parameters": GRADIENT_BOOSTING_PARAMETERS,
            "missing_value_policy": "native missing-value handling",
        },
        "warm_start_gradient_boosting": {
            "model": "HistGradientBoostingRegressor",
            "feature_columns": FULL_ENRICHED_FEATURE_COLUMNS,
            "training_rows": warm_start_rows,
            "parameters": GRADIENT_BOOSTING_PARAMETERS,
            "training_cohort": "historical_congestion_available == 1",
        },
    }

def _baseline_prediction_sets(
    train_data: pd.DataFrame,
    validation_data: pd.DataFrame,
) -> dict[str, np.ndarray]:
    return {
        "training_median": training_median_predictions(train_data, validation_data),
        "fixed_30_kmh": fixed_speed_predictions(validation_data),
        "linear_regression": linear_regression_predictions(train_data, validation_data),
    }


def _duration_segment_metrics(
    actual: pd.Series,
    prediction_sets: dict[str, np.ndarray],
) -> pd.DataFrame:
    segments = pd.cut(
        actual,
        bins=DURATION_BINS,
        labels=DURATION_LABELS,
        include_lowest=True,
    )
    rows: list[dict[str, float | int | str]] = []

    for segment in DURATION_LABELS:
        mask = segments.eq(segment).to_numpy()
        row: dict[str, float | int | str] = {
            "actual_duration_minutes": segment,
            "rows": int(mask.sum()),
        }
        for name, predictions in prediction_sets.items():
            errors = np.abs(predictions[mask] - actual.to_numpy(dtype=float)[mask])
            row[f"{name}_mae_minutes"] = float(errors.mean())
        rows.append(row)

    return pd.DataFrame(rows).set_index("actual_duration_minutes")


if __name__ == "__main__":
    main()
