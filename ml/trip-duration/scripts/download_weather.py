"""Download and cache the hourly weather range required by prepared trip data."""

from __future__ import annotations

import argparse
from datetime import UTC, datetime
import json
from pathlib import Path

import pandas as pd

from cabrynt_trip_duration.weather import fetch_porto_weather, write_weather_data

PROJECT_ROOT = Path(__file__).resolve().parents[1]
PREPARED_DATA_PATH = PROJECT_ROOT / "artifacts" / "prepared-data" / "clean.parquet"
WEATHER_PATH = PROJECT_ROOT / "data" / "weather" / "porto-hourly.parquet"
WEATHER_METADATA_PATH = PROJECT_ROOT / "data" / "weather" / "porto-hourly.metadata.json"


def parse_arguments() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Download historical weather for prepared Porto trips.")
    parser.add_argument("--refresh", action="store_true")
    return parser.parse_args()


def main() -> None:
    arguments = parse_arguments()
    if WEATHER_PATH.exists() and not arguments.refresh:
        print(f"Using existing weather cache at {WEATHER_PATH}")
        return
    if not PREPARED_DATA_PATH.exists():
        raise FileNotFoundError(
            "Prepared data is missing. Run 'python scripts/build_dataset.py' first."
        )

    timestamps = pd.read_parquet(PREPARED_DATA_PATH, columns=["timestamp"])["timestamp"]
    start_date = _utc_date(timestamps.min())
    end_date = _utc_date(timestamps.max())
    print(f"Downloading Porto weather from {start_date} through {end_date}...")
    weather_data = fetch_porto_weather(start_date, end_date)
    write_weather_data(weather_data, WEATHER_PATH)
    WEATHER_METADATA_PATH.write_text(
        json.dumps(
            {
                "source": "Open-Meteo Historical Weather API",
                "start_date": start_date,
                "end_date": end_date,
                "hourly_rows": len(weather_data),
                "downloaded_at_utc": datetime.now(tz=UTC).isoformat(),
            },
            indent=2,
        ),
        encoding="utf-8",
    )
    print(f"Cached {len(weather_data):,} hourly weather rows at {WEATHER_PATH}")


def _utc_date(timestamp: int) -> str:
    return datetime.fromtimestamp(int(timestamp), tz=UTC).date().isoformat()


if __name__ == "__main__":
    main()
