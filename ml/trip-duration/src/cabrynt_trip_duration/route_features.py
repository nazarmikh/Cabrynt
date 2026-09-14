"""Attach local OSRM route estimates to enriched trip rows."""

from __future__ import annotations

import pandas as pd

OSRM_FEATURE_COLUMNS = ["osrm_distance_km", "osrm_duration_minutes"]
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
