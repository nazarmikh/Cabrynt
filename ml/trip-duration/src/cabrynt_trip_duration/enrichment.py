"""Validation for datasets enriched with quote-time features."""

from __future__ import annotations

import pandas as pd

from cabrynt_trip_duration.congestion import CONGESTION_FEATURE_COLUMNS
from cabrynt_trip_duration.weather import WEATHER_FEATURE_COLUMNS

CALENDAR_FEATURE_COLUMNS = [
    "hour",
    "weekday",
    "month",
    "is_weekend",
    "is_public_holiday",
    "hour_sin",
    "hour_cos",
    "weekday_sin",
    "weekday_cos",
    "month_sin",
    "month_cos",
]
ENRICHED_FEATURE_COLUMNS = [
    *CALENDAR_FEATURE_COLUMNS,
    *WEATHER_FEATURE_COLUMNS,
    *CONGESTION_FEATURE_COLUMNS,
]


def validate_enriched_data(
    data: pd.DataFrame,
    allow_unavailable_congestion: bool,
) -> None:
    """Reject incomplete feature data before a model can consume it."""
    missing_columns = set(ENRICHED_FEATURE_COLUMNS) - set(data)
    if missing_columns:
        raise ValueError(f"enriched data is missing columns: {sorted(missing_columns)}")

    required_features = [*CALENDAR_FEATURE_COLUMNS, *WEATHER_FEATURE_COLUMNS]
    if data[required_features].isna().any().any():
        raise ValueError("calendar or weather features contain missing values")

    availability = data["historical_congestion_available"]
    if not availability.isin([0, 1]).all():
        raise ValueError("historical_congestion_available must contain only 0 or 1")
    if not allow_unavailable_congestion and not availability.eq(1).all():
        raise ValueError("all rows must have historical congestion features")

    available_rows = data.loc[availability.eq(1)]
    required_congestion = [
        "historical_time_duration_minutes",
        "historical_pickup_duration_minutes",
        "historical_pickup_observations",
    ]
    if available_rows[required_congestion].isna().any().any():
        raise ValueError("available congestion features contain missing values")
