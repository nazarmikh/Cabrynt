"""Calendar features derived from the trip start time in Porto local time."""

from __future__ import annotations

from datetime import datetime
from zoneinfo import ZoneInfo

import holidays
import numpy as np
import pandas as pd

PORTO_TIMEZONE = ZoneInfo("Europe/Lisbon")


def porto_datetime(timestamp: int) -> datetime:
    """Convert a Unix timestamp to Porto local time."""
    return datetime.fromtimestamp(timestamp, tz=PORTO_TIMEZONE)


def add_calendar_features(data: pd.DataFrame) -> pd.DataFrame:
    """Add quote-time calendar features without changing the source rows."""
    if "timestamp" not in data:
        raise ValueError("data must contain a timestamp column")

    result = data.copy()
    local_time = pd.to_datetime(result["timestamp"], unit="s", utc=True).dt.tz_convert(
        PORTO_TIMEZONE
    )
    holiday_dates = set(holidays.country_holidays("PT", years=local_time.dt.year.unique()))

    result["hour"] = local_time.dt.hour.astype("int8")
    result["weekday"] = local_time.dt.weekday.astype("int8")
    result["month"] = local_time.dt.month.astype("int8")
    result["is_weekend"] = (local_time.dt.weekday >= 5).astype("int8")
    result["is_public_holiday"] = local_time.dt.date.isin(holiday_dates).astype("int8")
    result["hour_sin"] = np.sin(2 * np.pi * result["hour"] / 24)
    result["hour_cos"] = np.cos(2 * np.pi * result["hour"] / 24)
    result["weekday_sin"] = np.sin(2 * np.pi * result["weekday"] / 7)
    result["weekday_cos"] = np.cos(2 * np.pi * result["weekday"] / 7)

    return result
