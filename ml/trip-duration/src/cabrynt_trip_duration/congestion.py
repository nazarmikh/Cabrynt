"""Leakage-safe historical congestion features for trip-duration data."""

from __future__ import annotations

from dataclasses import dataclass

import numpy as np
import pandas as pd

TARGET_COLUMN = "duration_minutes"
GRID_SIZE_DEGREES = 0.01
SMOOTHING_COUNT = 20
CONGESTION_FEATURE_COLUMNS = [
    "historical_time_duration_minutes",
    "historical_pickup_duration_minutes",
    "historical_pickup_observations",
    "historical_congestion_available",
]


@dataclass(frozen=True)
class CongestionProfiles:
    """Target-derived profiles fitted strictly on historical completed trips."""

    global_duration_minutes: float
    time_profile: pd.DataFrame
    pickup_profile: pd.DataFrame


def fit_congestion_profiles(history: pd.DataFrame) -> CongestionProfiles:
    """Fit smoothed duration profiles from completed historical trips."""
    _require_columns(history, [TARGET_COLUMN, "pickup_longitude", "pickup_latitude", "hour", "weekday"])
    if history.empty:
        raise ValueError("cannot fit congestion profiles on empty history")

    prepared = _with_pickup_grid(history)
    global_duration = float(prepared[TARGET_COLUMN].mean())
    time_profile = _smoothed_profile(prepared, ["hour", "weekday"], global_duration)
    pickup_profile = _smoothed_profile(
        prepared,
        ["pickup_grid_longitude", "pickup_grid_latitude", "hour"],
        global_duration,
    )
    return CongestionProfiles(global_duration, time_profile, pickup_profile)


def add_congestion_features(
    data: pd.DataFrame,
    profiles: CongestionProfiles,
) -> pd.DataFrame:
    """Add profiles to future rows, with smoothed global fallbacks for unseen zones."""
    _require_columns(data, ["pickup_longitude", "pickup_latitude", "hour", "weekday"])
    result = _with_pickup_grid(data)
    result = result.merge(
        profiles.time_profile,
        how="left",
        on=["hour", "weekday"],
        validate="many_to_one",
    )
    result = result.merge(
        profiles.pickup_profile,
        how="left",
        on=["pickup_grid_longitude", "pickup_grid_latitude", "hour"],
        validate="many_to_one",
    )
    result["historical_time_duration_minutes"] = result[
        "historical_time_duration_minutes"
    ].fillna(profiles.global_duration_minutes)
    result["historical_pickup_duration_minutes"] = result[
        "historical_pickup_duration_minutes"
    ].fillna(profiles.global_duration_minutes)
    result["historical_pickup_observations"] = result[
        "historical_pickup_observations"
    ].fillna(0).astype("int32")
    result["historical_congestion_available"] = 1
    return result.drop(columns=["pickup_grid_longitude", "pickup_grid_latitude"])


def add_chronological_training_congestion_features(
    train_data: pd.DataFrame,
    folds: int = 5,
) -> pd.DataFrame:
    """Create train features from past folds only, preventing target leakage."""
    _require_columns(train_data, ["timestamp"])
    if folds < 2:
        raise ValueError("folds must be at least 2")

    ordered = train_data.sort_values(["timestamp", "trip_id"], kind="stable").copy()
    unique_timestamps = ordered["timestamp"].drop_duplicates().to_numpy()
    timestamp_folds = np.array_split(unique_timestamps, folds)
    result_folds: list[pd.DataFrame] = []

    for timestamp_fold in timestamp_folds:
        current = ordered.loc[ordered["timestamp"].isin(timestamp_fold)].copy()
        history = ordered.loc[ordered["timestamp"] < timestamp_fold[0]]
        if history.empty:
            result_folds.append(_add_unavailable_congestion_features(current))
        else:
            result_folds.append(add_congestion_features(current, fit_congestion_profiles(history)))

    return pd.concat(result_folds, ignore_index=True).sort_values(
        ["timestamp", "trip_id"], kind="stable"
    )


def _smoothed_profile(
    data: pd.DataFrame,
    keys: list[str],
    global_duration: float,
) -> pd.DataFrame:
    profile = data.groupby(keys, as_index=False)[TARGET_COLUMN].agg(["sum", "count"]).reset_index()
    profile["smoothed_duration"] = (
        profile["sum"] + SMOOTHING_COUNT * global_duration
    ) / (profile["count"] + SMOOTHING_COUNT)

    if keys == ["hour", "weekday"]:
        return profile[keys + ["smoothed_duration"]].rename(
            columns={"smoothed_duration": "historical_time_duration_minutes"}
        )

    return profile[keys + ["smoothed_duration", "count"]].rename(
        columns={
            "smoothed_duration": "historical_pickup_duration_minutes",
            "count": "historical_pickup_observations",
        }
    )


def _with_pickup_grid(data: pd.DataFrame) -> pd.DataFrame:
    result = data.copy()
    result["pickup_grid_longitude"] = np.floor(
        result["pickup_longitude"] / GRID_SIZE_DEGREES
    ).astype("int16")
    result["pickup_grid_latitude"] = np.floor(
        result["pickup_latitude"] / GRID_SIZE_DEGREES
    ).astype("int16")
    return result


def _add_unavailable_congestion_features(data: pd.DataFrame) -> pd.DataFrame:
    result = data.copy()
    result["historical_time_duration_minutes"] = np.nan
    result["historical_pickup_duration_minutes"] = np.nan
    result["historical_pickup_observations"] = 0
    result["historical_congestion_available"] = 0
    return result


def _require_columns(data: pd.DataFrame, columns: list[str]) -> None:
    missing_columns = set(columns) - set(data)
    if missing_columns:
        raise ValueError(f"data is missing columns: {sorted(missing_columns)}")
