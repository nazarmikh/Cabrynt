"""Chronological backtesting helpers for route-aware experiments."""

from __future__ import annotations

from dataclasses import dataclass

import numpy as np
import pandas as pd


@dataclass(frozen=True)
class ExpandingTimeFold:
    """One train-before-validation fold from chronologically ordered data."""

    name: str
    train_data: pd.DataFrame
    validation_data: pd.DataFrame


def two_fold_expanding_backtest(data: pd.DataFrame) -> list[ExpandingTimeFold]:
    """Split data into 60/20 and 80/20 expanding chronological folds."""
    required_columns = {"timestamp", "trip_id"}
    missing_columns = required_columns - set(data)
    if missing_columns:
        raise ValueError(f"backtest data is missing columns: {sorted(missing_columns)}")

    ordered = data.sort_values(["timestamp", "trip_id"], kind="stable").copy()
    timestamps = ordered["timestamp"].drop_duplicates().to_numpy()
    if len(timestamps) < 5:
        raise ValueError("backtest data needs at least five distinct timestamps")

    chunks = np.array_split(timestamps, 5)
    return [
        ExpandingTimeFold(
            name="early",
            train_data=_rows_for_timestamps(ordered, chunks[:3]),
            validation_data=_rows_for_timestamps(ordered, chunks[3:4]),
        ),
        ExpandingTimeFold(
            name="late",
            train_data=_rows_for_timestamps(ordered, chunks[:4]),
            validation_data=_rows_for_timestamps(ordered, chunks[4:]),
        ),
    ]


def paired_mae_difference_interval(
    actual: pd.Series,
    candidate_predictions: np.ndarray,
    reference_predictions: np.ndarray,
    resamples: int = 1_000,
    random_state: int = 42,
) -> dict[str, float]:
    """Return a paired bootstrap interval for candidate MAE minus reference MAE."""
    actual_values = actual.to_numpy(dtype=float)
    candidate_values = np.asarray(candidate_predictions, dtype=float)
    reference_values = np.asarray(reference_predictions, dtype=float)
    if len(candidate_values) != len(reference_values) or len(candidate_values) != len(actual_values):
        raise ValueError("actual and prediction arrays must have matching lengths")
    if len(actual_values) == 0:
        raise ValueError("bootstrap data cannot be empty")
    if resamples <= 0:
        raise ValueError("resamples must be positive")

    candidate_errors = np.abs(candidate_values - actual_values)
    reference_errors = np.abs(reference_values - actual_values)
    differences = candidate_errors - reference_errors
    random = np.random.default_rng(random_state)
    bootstrap_differences = np.empty(resamples)
    for index in range(resamples):
        sample_indices = random.integers(0, len(differences), size=len(differences))
        bootstrap_differences[index] = differences[sample_indices].mean()

    return {
        "candidate_minus_reference_mae_minutes": float(differences.mean()),
        "confidence_interval_lower_minutes": float(np.quantile(bootstrap_differences, 0.025)),
        "confidence_interval_upper_minutes": float(np.quantile(bootstrap_differences, 0.975)),
    }


def _rows_for_timestamps(data: pd.DataFrame, timestamp_chunks: list[np.ndarray]) -> pd.DataFrame:
    selected_timestamps = np.concatenate(timestamp_chunks)
    return data.loc[data["timestamp"].isin(selected_timestamps)].copy()
