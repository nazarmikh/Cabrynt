"""Attach local OSRM route estimates to enriched trip rows."""

from __future__ import annotations

import numpy as np
import pandas as pd

OSRM_FEATURE_COLUMNS = ["osrm_distance_km", "osrm_duration_minutes"]
DERIVED_ROUTE_FEATURE_COLUMNS = [
    "osrm_average_speed_kmh",
    "osrm_distance_gap_km",
    "osrm_detour_ratio",
]
ROUTE_ESTIMATE_COLUMNS = ["trip_id", *OSRM_FEATURE_COLUMNS]


def attach_route_estimates(
    data: pd.DataFrame,
    route_estimates: pd.DataFrame,
) -> pd.DataFrame:
    """Return rows with matching OSRM estimates and reject ambiguous route data."""
    missing_route_columns = set(ROUTE_ESTIMATE_COLUMNS) - set(route_estimates)
    if missing_route_columns:
        raise ValueError(f"route estimates are missing columns: {sorted(missing_route_columns)}")
    if route_estimates["trip_id"].duplicated().any():
        raise ValueError("route estimates contain duplicate trip IDs")

    cohort = data.merge(
        route_estimates[ROUTE_ESTIMATE_COLUMNS],
        how="inner",
        on="trip_id",
        validate="one_to_one",
    )
    if len(cohort) != len(route_estimates):
        raise ValueError("some route estimates do not belong to the supplied trip data")

    return cohort.sort_values("trip_id", kind="stable").reset_index(drop=True)


def add_derived_route_features(data: pd.DataFrame) -> pd.DataFrame:
    """Add quote-time geometry features derived from OSRM and endpoint distance."""
    required_columns = {"straight_line_km", *OSRM_FEATURE_COLUMNS}
    missing_columns = required_columns - set(data)
    if missing_columns:
        raise ValueError(f"route data is missing columns: {sorted(missing_columns)}")
    if data["osrm_duration_minutes"].lt(0).any():
        raise ValueError("osrm_duration_minutes cannot be negative")

    result = data.copy()
    result["osrm_average_speed_kmh"] = (
        result["osrm_distance_km"] / result["osrm_duration_minutes"].replace(0, np.nan) * 60
    )
    result["osrm_distance_gap_km"] = result["osrm_distance_km"] - result["straight_line_km"]
    result["osrm_detour_ratio"] = (
        result["osrm_distance_km"] / result["straight_line_km"]
    ).replace([np.inf, -np.inf], np.nan)

    return result
