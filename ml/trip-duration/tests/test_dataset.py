import json

import pandas as pd
import pytest

from cabrynt_trip_duration.dataset import (
    chronological_splits,
    prepare_chunk,
    prepare_trip,
)


def test_prepare_trip_derives_only_quote_time_features() -> None:
    row, reason = prepare_trip(
        "trip-1",
        1_370_000_000,
        json.dumps([[-8.61, 41.15], [-8.611, 41.151], [-8.612, 41.152]]),
    )

    assert reason is None
    assert row is not None
    assert row["trip_id"] == "trip-1"
    assert row["duration_minutes"] == 0.5
    assert row["straight_line_km"] > 0
    assert "observed_distance_km" not in row


def test_prepare_trip_rejects_an_invalid_timestamp() -> None:
    row, reason = prepare_trip("trip-1", "not-a-time", "[[-8.61, 41.15], [-8.62, 41.16]]")

    assert row is None
    assert reason == "invalid_timestamp"


def test_prepare_chunk_keeps_the_first_duplicate_trip_id() -> None:
    first_polyline = "[[-8.61, 41.15], [-8.611, 41.151]]"
    second_polyline = "[[-8.61, 41.15], [-8.612, 41.152]]"

    rows, rejected = prepare_chunk(
        [("same-trip", 1_370_000_000, first_polyline), ("same-trip", 1_370_000_100, second_polyline)],
        set(),
    )

    assert len(rows) == 1
    assert rows[0]["destination_longitude"] == -8.611
    assert rejected == {"duplicate_trip_id": 1}


def test_chronological_splits_keep_timestamp_boundaries_together() -> None:
    data = pd.DataFrame(
        {
            "trip_id": ["a", "b", "c", "d", "e", "f", "g", "h", "i", "j"],
            "timestamp": [1, 2, 3, 4, 5, 6, 7, 7, 8, 9],
        }
    )

    train_data, validation_data, test_data = chronological_splits(data)

    assert train_data["timestamp"].max() < validation_data["timestamp"].min()
    assert validation_data["timestamp"].max() < test_data["timestamp"].min()
    assert len(train_data) + len(validation_data) + len(test_data) == len(data)


@pytest.mark.parametrize(
    ("train_fraction", "validation_fraction"),
    [(0, 0.15), (0.70, 0), (0.9, 0.2)],
)
def test_chronological_splits_reject_invalid_fractions(
    train_fraction: float,
    validation_fraction: float,
) -> None:
    data = pd.DataFrame({"trip_id": ["a", "b", "c"], "timestamp": [1, 2, 3]})

    with pytest.raises(ValueError):
        chronological_splits(data, train_fraction, validation_fraction)
