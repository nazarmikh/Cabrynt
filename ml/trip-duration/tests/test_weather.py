import pandas as pd
import pytest

from cabrynt_trip_duration.weather import add_weather_features, weather_from_payload


def test_weather_from_payload_creates_model_features() -> None:
    weather_data = weather_from_payload(
        {
            "hourly": {
                "time": ["2014-01-01T00:00", "2014-01-01T01:00"],
                "temperature_2m": [11.5, 12.0],
                "precipitation": [0.0, 0.3],
                "cloud_cover": [25, 75],
                "wind_speed_10m": [8.0, 10.0],
            }
        }
    )

    assert weather_data["weather_timestamp"].tolist() == [1_388_534_400, 1_388_538_000]
    assert weather_data["is_precipitating"].tolist() == [0, 1]
    assert weather_data["temperature_c"].tolist() == [11.5, 12.0]


def test_add_weather_features_joins_the_timestamp_hour() -> None:
    trip_data = pd.DataFrame({"timestamp": [1_388_534_451]})
    weather_data = pd.DataFrame(
        {
            "weather_timestamp": [1_388_534_400],
            "temperature_c": [11.5],
            "precipitation_mm": [0.3],
            "cloud_cover_percent": [75],
            "wind_speed_kmh": [10.0],
            "is_precipitating": [1],
        }
    )

    result = add_weather_features(trip_data, weather_data)

    assert result.loc[0, "temperature_c"] == 11.5
    assert "weather_timestamp" not in result


def test_add_weather_features_rejects_missing_hours() -> None:
    trip_data = pd.DataFrame({"timestamp": [1_388_534_400]})
    weather_data = pd.DataFrame(
        columns=[
            "weather_timestamp",
            "temperature_c",
            "precipitation_mm",
            "cloud_cover_percent",
            "wind_speed_kmh",
            "is_precipitating",
        ]
    )

    with pytest.raises(ValueError, match="missing the hour"):
        add_weather_features(trip_data, weather_data)
