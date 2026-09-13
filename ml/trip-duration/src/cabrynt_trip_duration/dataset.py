"""Build model-ready rows and chronological splits from taxi trip records."""

from __future__ import annotations

from collections import Counter
from collections.abc import Iterable
from datetime import UTC, datetime
import math
from numbers import Real

import pandas as pd

from cabrynt_trip_duration.preprocessing import (
    duration_minutes,
    haversine_km,
    parse_polyline,
    preprocessing_reason,
)

CLEAN_COLUMNS = [
    "trip_id",
    "timestamp",
    "pickup_longitude",
    "pickup_latitude",
    "destination_longitude",
    "destination_latitude",
    "duration_minutes",
    "straight_line_km",
    "hour",
    "weekday",
]


def prepare_trip(
    trip_id: object,
    timestamp: object,
    polyline: object,
) -> tuple[dict[str, object] | None, str | None]:
    """Create one model-ready row or return its exclusion reason."""
    if (
        not isinstance(timestamp, Real)
        or isinstance(timestamp, bool)
        or not math.isfinite(timestamp)
        or int(timestamp) != timestamp
    ):
        return None, "invalid_timestamp"

    points = parse_polyline(polyline)
    reason = preprocessing_reason(points)
    if reason is not None:
        return None, reason

    started_at = datetime.fromtimestamp(int(timestamp), tz=UTC)
    pickup_longitude, pickup_latitude = points[0]
    destination_longitude, destination_latitude = points[-1]

    return {
        "trip_id": str(trip_id),
        "timestamp": int(timestamp),
        "pickup_longitude": pickup_longitude,
        "pickup_latitude": pickup_latitude,
        "destination_longitude": destination_longitude,
        "destination_latitude": destination_latitude,
        "duration_minutes": duration_minutes(points),
        "straight_line_km": haversine_km(
            pickup_longitude,
            pickup_latitude,
            destination_longitude,
            destination_latitude,
        ),
        "hour": started_at.hour,
        "weekday": started_at.weekday(),
    }, None


def prepare_chunk(
    rows: Iterable[tuple[object, object, object]],
    seen_trip_ids: set[str],
) -> tuple[list[dict[str, object]], Counter[str]]:
    """Prepare one input chunk and keep only the first row for each trip ID."""
    prepared_rows: list[dict[str, object]] = []
    rejected_by_reason: Counter[str] = Counter()

    for trip_id, timestamp, polyline in rows:
        normalized_trip_id = str(trip_id)
        if normalized_trip_id in seen_trip_ids:
            rejected_by_reason["duplicate_trip_id"] += 1
            continue
        seen_trip_ids.add(normalized_trip_id)

        prepared_row, reason = prepare_trip(trip_id, timestamp, polyline)
        if reason is not None:
            rejected_by_reason[reason] += 1
            continue

        prepared_rows.append(prepared_row)

    return prepared_rows, rejected_by_reason


def chronological_splits(
    data: pd.DataFrame,
    train_fraction: float = 0.70,
    validation_fraction: float = 0.15,
) -> tuple[pd.DataFrame, pd.DataFrame, pd.DataFrame]:
    """Split data by time while keeping identical timestamps in one split."""
    if not 0 < train_fraction < 1:
        raise ValueError("train_fraction must be between 0 and 1")
    if not 0 < validation_fraction < 1 - train_fraction:
        raise ValueError("validation_fraction must leave room for a test split")
    if data.empty:
        raise ValueError("cannot split an empty dataset")

    ordered = data.sort_values(["timestamp", "trip_id"], kind="stable").reset_index(drop=True)
    train_index = math.ceil(len(ordered) * train_fraction) - 1
    validation_index = math.ceil(len(ordered) * (train_fraction + validation_fraction)) - 1
    train_cutoff = ordered.loc[train_index, "timestamp"]
    validation_cutoff = ordered.loc[validation_index, "timestamp"]

    train_data = ordered.loc[ordered["timestamp"] <= train_cutoff].copy()
    validation_data = ordered.loc[
        (ordered["timestamp"] > train_cutoff)
        & (ordered["timestamp"] <= validation_cutoff)
    ].copy()
    test_data = ordered.loc[ordered["timestamp"] > validation_cutoff].copy()

    if validation_data.empty or test_data.empty:
        raise ValueError("timestamp distribution cannot produce three chronological splits")

    return train_data, validation_data, test_data
