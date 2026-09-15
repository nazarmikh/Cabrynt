"""Deterministic nested training samples for chronological learning curves."""

from __future__ import annotations

import numpy as np
import pandas as pd


def nested_training_samples(
    train_data: pd.DataFrame,
    sample_sizes: tuple[int, ...],
    random_state: int = 42,
) -> dict[int, pd.DataFrame]:
    """Return reproducible nested random samples, ordered before model fitting.

    Callers must supply data that is already entirely earlier than their validation
    period. Random sampling within that training period measures data-volume effects
    without allowing future rows into the model.
    """
    required_columns = {"trip_id", "timestamp"}
    missing_columns = required_columns - set(train_data)
    if missing_columns:
        raise ValueError(f"training data is missing columns: {sorted(missing_columns)}")
    if train_data["trip_id"].duplicated().any():
        raise ValueError("training data contains duplicate trip IDs")
    if not sample_sizes:
        raise ValueError("at least one sample size is required")
    if any(size <= 0 for size in sample_sizes):
        raise ValueError("sample sizes must be positive")
    if tuple(sorted(sample_sizes)) != sample_sizes or len(set(sample_sizes)) != len(sample_sizes):
        raise ValueError("sample sizes must be unique and sorted in ascending order")
    if sample_sizes[-1] > len(train_data):
        raise ValueError("sample size cannot exceed available training rows")

    random = np.random.default_rng(random_state)
    row_order = random.permutation(len(train_data))
    samples: dict[int, pd.DataFrame] = {}
    for size in sample_sizes:
        samples[size] = (
            train_data.iloc[row_order[:size]]
            .sort_values(["timestamp", "trip_id"], kind="stable")
            .copy()
        )

    return samples
