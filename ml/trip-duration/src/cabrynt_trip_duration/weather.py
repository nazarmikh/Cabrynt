"""Historical Porto weather retrieval and quote-time weather feature joins."""

from __future__ import annotations

import json
from pathlib import Path
import ssl
from urllib import parse, request

import certifi
import pandas as pd

ARCHIVE_URL = "https://archive-api.open-meteo.com/v1/archive"
PORTO_LATITUDE = 41.1579
PORTO_LONGITUDE = -8.6291
WEATHER_SOURCE_COLUMNS = [
    "temperature_2m",
    "precipitation",
    "cloud_cover",
    "wind_speed_10m",
]
WEATHER_FEATURE_COLUMNS = [
    "temperature_c",
    "precipitation_mm",
    "cloud_cover_percent",
    "wind_speed_kmh",
    "is_precipitating",
]


def fetch_porto_weather(start_date: str, end_date: str) -> pd.DataFrame:
    """Fetch hourly UTC weather for Porto from Open-Meteo's archive API."""
    parameters = {
        "latitude": PORTO_LATITUDE,
        "longitude": PORTO_LONGITUDE,
        "hourly": ",".join(WEATHER_SOURCE_COLUMNS),
        "timezone": "UTC",
        "start_date": start_date,
        "end_date": end_date,
    }
    url = f"{ARCHIVE_URL}?{parse.urlencode(parameters)}"
    certificate_context = ssl.create_default_context(cafile=certifi.where())
    with request.urlopen(url, timeout=30, context=certificate_context) as response:
        payload = json.loads(response.read())

    return weather_from_payload(payload)


def weather_from_payload(payload: dict[str, object]) -> pd.DataFrame:
    """Validate an Open-Meteo response and convert it to feature rows."""
    hourly = payload.get("hourly")
    if not isinstance(hourly, dict):
        raise ValueError("weather response does not contain hourly data")

    timestamps = hourly.get("time")
    if not isinstance(timestamps, list) or not timestamps:
        raise ValueError("weather response does not contain hourly timestamps")

    weather_data: dict[str, object] = {"weather_timestamp": timestamps}
    for source_column in WEATHER_SOURCE_COLUMNS:
        values = hourly.get(source_column)
        if not isinstance(values, list) or len(values) != len(timestamps):
            raise ValueError(f"weather response has invalid {source_column} values")
        weather_data[source_column] = values

    result = pd.DataFrame(weather_data)
    parsed_timestamps = pd.to_datetime(result["weather_timestamp"], utc=True)
    result["weather_timestamp"] = parsed_timestamps.map(lambda value: int(value.timestamp()))
    result = result.rename(
        columns={
            "temperature_2m": "temperature_c",
            "precipitation": "precipitation_mm",
            "cloud_cover": "cloud_cover_percent",
            "wind_speed_10m": "wind_speed_kmh",
        }
    )
    result["is_precipitating"] = (result["precipitation_mm"] > 0).astype("int8")
    return result[["weather_timestamp", *WEATHER_FEATURE_COLUMNS]]


def write_weather_data(weather_data: pd.DataFrame, output_path: Path) -> None:
    """Write a validated local weather cache."""
    missing_columns = set(["weather_timestamp", *WEATHER_FEATURE_COLUMNS]) - set(weather_data)
    if missing_columns:
        raise ValueError(f"weather data is missing columns: {sorted(missing_columns)}")

    output_path.parent.mkdir(parents=True, exist_ok=True)
    weather_data.to_parquet(output_path, index=False)


def add_weather_features(data: pd.DataFrame, weather_data: pd.DataFrame) -> pd.DataFrame:
    """Join hourly weather using the trip's UTC timestamp rounded down to an hour."""
    if "timestamp" not in data:
        raise ValueError("data must contain a timestamp column")

    result = data.copy()
    result["weather_timestamp"] = (result["timestamp"] // 3_600) * 3_600
    result = result.merge(weather_data, how="left", on="weather_timestamp", validate="many_to_one")

    missing_rows = result[WEATHER_FEATURE_COLUMNS].isna().any(axis=1)
    if missing_rows.any():
        missing_timestamp = int(result.loc[missing_rows, "timestamp"].iloc[0])
        raise ValueError(f"weather data is missing the hour for timestamp {missing_timestamp}")

    return result.drop(columns="weather_timestamp")
