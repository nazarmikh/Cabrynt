"""Shared validation and trajectory helpers for the Porto taxi dataset."""

from __future__ import annotations

import json
import math
from collections.abc import Sequence
from numbers import Real

SECONDS_PER_GPS_POINT = 15
MAX_SEGMENT_SPEED_KMH = 150.0
MAX_DURATION_MINUTES = 240.0

PORTO_LONGITUDE_RANGE = (-9.0, -8.0)
PORTO_LATITUDE_RANGE = (40.5, 41.7)

Point = tuple[float, float]


def parse_polyline(value: object) -> list[Point] | None:
    """Parse a dataset POLYLINE value into longitude/latitude pairs."""
    if not isinstance(value, str):
        return None

    try:
        raw_points = json.loads(value)
    except json.JSONDecodeError:
        return None

    if not isinstance(raw_points, list):
        return None

    points: list[Point] = []
    for point in raw_points:
        if not isinstance(point, list) or len(point) != 2:
            return None

        longitude, latitude = point
        if not _is_coordinate(longitude, latitude):
            return None

        points.append((float(longitude), float(latitude)))

    return points


def duration_minutes(points: Sequence[Point]) -> float:
    """Calculate duration from 15-second GPS intervals."""
    return max(len(points) - 1, 0) * SECONDS_PER_GPS_POINT / 60


def preprocessing_reason(points: list[Point] | None) -> str | None:
    """Return the first reason a trajectory is excluded, or None when usable."""
    if points is None:
        return "unparseable_or_invalid_polyline"
    if not points:
        return "empty_polyline"
    if len(points) < 2:
        return "too_few_points"
    if not all(_is_in_porto(longitude, latitude) for longitude, latitude in points):
        return "outside_porto_area"
    if duration_minutes(points) > MAX_DURATION_MINUTES:
        return "duration_above_four_hours"
    if max_segment_speed_kmh(points) > MAX_SEGMENT_SPEED_KMH:
        return "gps_speed_teleport"
    return None


def trajectory_statistics(points: Sequence[Point]) -> dict[str, float]:
    """Return GPS-derived diagnostics. These are never quote-time model features."""
    distances = [
        haversine_km(start[0], start[1], end[0], end[1])
        for start, end in zip(points, points[1:], strict=False)
    ]
    observed_distance_km = sum(distances)

    return {
        "observed_distance_km": observed_distance_km,
        "max_segment_speed_kmh": max(distances, default=0.0) * 240,
    }


def max_segment_speed_kmh(points: Sequence[Point]) -> float:
    return trajectory_statistics(points)["max_segment_speed_kmh"]


def haversine_km(
    longitude_1: float,
    latitude_1: float,
    longitude_2: float,
    latitude_2: float,
) -> float:
    """Calculate the great-circle distance between two longitude/latitude points."""
    earth_radius_km = 6_371.0
    latitude_1_radians = math.radians(latitude_1)
    latitude_2_radians = math.radians(latitude_2)
    latitude_difference = latitude_2_radians - latitude_1_radians
    longitude_difference = math.radians(longitude_2 - longitude_1)

    a = (
        math.sin(latitude_difference / 2) ** 2
        + math.cos(latitude_1_radians)
        * math.cos(latitude_2_radians)
        * math.sin(longitude_difference / 2) ** 2
    )
    return 2 * earth_radius_km * math.asin(math.sqrt(min(max(a, 0.0), 1.0)))


def _is_coordinate(longitude: object, latitude: object) -> bool:
    return (
        isinstance(longitude, Real)
        and not isinstance(longitude, bool)
        and isinstance(latitude, Real)
        and not isinstance(latitude, bool)
        and math.isfinite(longitude)
        and math.isfinite(latitude)
        and -180 <= longitude <= 180
        and -90 <= latitude <= 90
    )


def _is_in_porto(longitude: float, latitude: float) -> bool:
    return (
        PORTO_LONGITUDE_RANGE[0] <= longitude <= PORTO_LONGITUDE_RANGE[1]
        and PORTO_LATITUDE_RANGE[0] <= latitude <= PORTO_LATITUDE_RANGE[1]
    )
