import pandas as pd
import pytest

from cabrynt_trip_duration.enrichment import validate_enriched_data


def test_enriched_validation_requires_complete_features() -> None:
    data = _enriched_data()

    validate_enriched_data(data, allow_unavailable_congestion=False)


def test_enriched_data_rejects_missing_weather() -> None:
    data = _enriched_data()
    data.loc[0, "temperature_c"] = None

    with pytest.raises(ValueError, match="weather features"):
        validate_enriched_data(data, allow_unavailable_congestion=False)


def test_train_data_can_mark_cold_start_congestion_unavailable() -> None:
    data = _enriched_data()
    data.loc[0, "historical_congestion_available"] = 0
    data.loc[0, "historical_time_duration_minutes"] = None
    data.loc[0, "historical_pickup_duration_minutes"] = None

    validate_enriched_data(data, allow_unavailable_congestion=True)


def _enriched_data() -> pd.DataFrame:
    return pd.DataFrame(
        {
            "hour": [8],
            "weekday": [1],
            "month": [7],
            "is_weekend": [0],
            "is_public_holiday": [0],
            "hour_sin": [0.5],
            "hour_cos": [-0.5],
            "weekday_sin": [0.78],
            "weekday_cos": [0.62],
            "month_sin": [-0.5],
            "month_cos": [-0.87],
            "temperature_c": [20.0],
            "precipitation_mm": [0.0],
            "cloud_cover_percent": [25],
            "wind_speed_kmh": [10.0],
            "is_precipitating": [0],
            "historical_time_duration_minutes": [12.0],
            "historical_pickup_duration_minutes": [13.0],
            "historical_pickup_observations": [42],
            "historical_congestion_available": [1],
        }
    )
