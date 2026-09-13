"""Build local enriched train and validation data without touching the test split."""

from __future__ import annotations

import json
import shutil
from pathlib import Path

import pandas as pd

from cabrynt_trip_duration.calendar_features import add_calendar_features
from cabrynt_trip_duration.congestion import (
    CONGESTION_FEATURE_COLUMNS,
    add_chronological_training_congestion_features,
    add_congestion_features,
    fit_congestion_profiles,
)
from cabrynt_trip_duration.enrichment import validate_enriched_data
from cabrynt_trip_duration.weather import WEATHER_FEATURE_COLUMNS, add_weather_features

PROJECT_ROOT = Path(__file__).resolve().parents[1]
PREPARED_DIRECTORY = PROJECT_ROOT / "artifacts" / "prepared-data"
WEATHER_PATH = PROJECT_ROOT / "data" / "weather" / "porto-hourly.parquet"
OUTPUT_DIRECTORY = PROJECT_ROOT / "artifacts" / "enriched-data"


def main() -> None:
    train_path = PREPARED_DIRECTORY / "train.parquet"
    validation_path = PREPARED_DIRECTORY / "validation.parquet"
    missing_paths = [path for path in (train_path, validation_path, WEATHER_PATH) if not path.exists()]
    if missing_paths:
        raise FileNotFoundError(
            "Missing prepared trip data or weather cache. Run build_dataset.py and download_weather.py: "
            f"{missing_paths}"
        )

    train_data = pd.read_parquet(train_path)
    validation_data = pd.read_parquet(validation_path)
    weather_data = pd.read_parquet(WEATHER_PATH)

    train_data = add_weather_features(add_calendar_features(train_data), weather_data)
    validation_data = add_weather_features(add_calendar_features(validation_data), weather_data)

    enriched_train = add_chronological_training_congestion_features(train_data)
    profiles = fit_congestion_profiles(train_data)
    enriched_validation = add_congestion_features(validation_data, profiles)
    validate_enriched_data(enriched_train, allow_unavailable_congestion=True)
    validate_enriched_data(enriched_validation, allow_unavailable_congestion=False)

    _reset_output_directory()
    enriched_train.to_parquet(OUTPUT_DIRECTORY / "train.parquet", index=False)
    enriched_validation.to_parquet(OUTPUT_DIRECTORY / "validation.parquet", index=False)
    (OUTPUT_DIRECTORY / "summary.json").write_text(
        json.dumps(
            {
                "train_rows": len(enriched_train),
                "validation_rows": len(enriched_validation),
                "weather_features": WEATHER_FEATURE_COLUMNS,
                "congestion_features": CONGESTION_FEATURE_COLUMNS,
                "train_rows_without_historical_congestion": int(
                    (enriched_train["historical_congestion_available"] == 0).sum()
                ),
            },
            indent=2,
        ),
        encoding="utf-8",
    )

    print(f"Wrote enriched train data to {OUTPUT_DIRECTORY / 'train.parquet'}")
    print(f"Wrote enriched validation data to {OUTPUT_DIRECTORY / 'validation.parquet'}")


def _reset_output_directory() -> None:
    if OUTPUT_DIRECTORY.exists():
        shutil.rmtree(OUTPUT_DIRECTORY)
    OUTPUT_DIRECTORY.mkdir(parents=True)


if __name__ == "__main__":
    main()
