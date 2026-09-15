import json

import pytest

from cabrynt_trip_duration.preprocessing import (
    MAX_DURATION_MINUTES,
    duration_minutes,
    haversine_km,
    parse_polyline,
    preprocessing_reason,
)


def test_parse_polyline_returns_longitude_latitude_pairs() -> None:
    points = parse_polyline(json.dumps([[-8.61, 41.15], [-8.62, 41.16]]))

    assert points == [(-8.61, 41.15), (-8.62, 41.16)]


@pytest.mark.parametrize("value", ["not json", "{}", "[[1]]", "[[true, 41.15]]"])
def test_parse_polyline_rejects_invalid_values(value: str) -> None:
    assert parse_polyline(value) is None


def test_duration_uses_intervals_between_gps_points() -> None:
    points = [(-8.61, 41.15), (-8.62, 41.16), (-8.63, 41.17)]

    assert duration_minutes(points) == 0.5


def test_haversine_distance_is_zero_for_the_same_point() -> None:
    assert haversine_km(-8.61, 41.15, -8.61, 41.15) == 0


@pytest.mark.parametrize(
    ("points", "reason"),
    [
        (None, "unparseable_or_invalid_polyline"),
        ([], "empty_polyline"),
        ([(-8.61, 41.15)], "too_few_points"),
        ([(-7.0, 41.15), (-8.61, 41.15)], "outside_porto_area"),
    ],
)
def test_preprocessing_rejects_invalid_trajectories(points, reason: str) -> None:
    assert preprocessing_reason(points) == reason


def test_preprocessing_rejects_a_four_hour_trajectory() -> None:
    points = [(-8.61, 41.15)] * (int(MAX_DURATION_MINUTES * 60 / 15) + 2)

    assert preprocessing_reason(points) == "duration_above_four_hours"


def test_preprocessing_rejects_a_gps_teleport() -> None:
    points = [(-8.61, 41.15), (-8.9, 41.5)]

    assert preprocessing_reason(points) == "gps_speed_teleport"


def test_preprocessing_keeps_a_short_return_trip() -> None:
    points = [(-8.61, 41.15), (-8.611, 41.151), (-8.61, 41.15)]

    assert preprocessing_reason(points) is None
