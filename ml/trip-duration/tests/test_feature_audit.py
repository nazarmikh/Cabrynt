import pandas as pd
import pytest

from cabrynt_trip_duration.feature_audit import audit_features


def test_feature_audit_reports_missing_values_and_correlation() -> None:
    train_data = _sample_data()
    validation_data = _sample_data().assign(historical_pickup_duration_minutes=[4.0, 5.0, 6.0])

    report = audit_features(train_data, validation_data)

    assert report["feature_count"] == 25
    assert report["features_with_missing_training_values"] == [
        "historical_pickup_duration_minutes"
    ]
    assert report["features_with_missing_validation_values"] == []
    assert report["constant_features"] == ["is_weekend", "is_public_holiday"]
    assert report["features_with_large_mean_shift"]
    assert report["high_correlation_pairs"]


def test_feature_audit_rejects_missing_model_columns() -> None:
    data = _sample_data().drop(columns="temperature_c")

    with pytest.raises(ValueError, match="temperature_c"):
        audit_features(data, data)


def _sample_data() -> pd.DataFrame:
    return pd.DataFrame(
        {
            "duration_minutes": [10.0, 20.0, 30.0],
            "pickup_longitude": [-8.61, -8.60, -8.59],
            "pickup_latitude": [41.15, 41.16, 41.17],
            "destination_longitude": [-8.60, -8.59, -8.58],
            "destination_latitude": [41.16, 41.17, 41.18],
            "straight_line_km": [1.0, 2.0, 3.0],
            "hour": [8, 9, 10],
            "weekday": [1, 2, 3],
            "month": [7, 8, 9],
            "is_weekend": [0, 0, 0],
            "is_public_holiday": [0, 0, 0],
            "hour_sin": [0.8, 0.7, 0.5],
            "hour_cos": [-0.5, -0.7, -0.8],
            "weekday_sin": [0.8, 0.9, 0.4],
            "weekday_cos": [0.6, -0.2, -0.9],
            "month_sin": [-0.5, -0.87, -1.0],
            "month_cos": [-0.87, -0.5, 0.0],
            "temperature_c": [20.0, 21.0, 22.0],
            "precipitation_mm": [0.0, 0.0, 1.0],
            "cloud_cover_percent": [20.0, 30.0, 40.0],
            "wind_speed_kmh": [10.0, 11.0, 12.0],
            "is_precipitating": [0, 0, 1],
            "historical_time_duration_minutes": [12.0, 13.0, 14.0],
            "historical_pickup_duration_minutes": [None, 13.0, 14.0],
            "historical_pickup_observations": [0, 3, 4],
            "historical_congestion_available": [0, 1, 1],
        }
    )
