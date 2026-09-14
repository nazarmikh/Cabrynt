import numpy as np
import pandas as pd
import pytest

from scripts.evaluate_enriched_models import _experiment_metadata, _prediction_sets
from cabrynt_trip_duration.modeling import (
    CALENDAR_WEATHER_FEATURE_COLUMNS,
    FINAL_RESIDUAL_FEATURE_COLUMNS,
    FINAL_RESIDUAL_PARAMETERS,
    FULL_ENRICHED_FEATURE_COLUMNS,
    OSRM_AWARE_FEATURE_COLUMNS,
    enriched_linear_regression_predictions,
    gradient_boosting_predictions,
    osrm_residual_gradient_boosting_predictions,
    warm_start_gradient_boosting_predictions,
)


def test_enriched_linear_regression_handles_cold_start_missing_values() -> None:
    train_data = _sample_data(40)
    train_data.loc[0, "historical_time_duration_minutes"] = np.nan
    train_data.loc[0, "historical_pickup_duration_minutes"] = np.nan
    validation_data = _sample_data(4)

    predictions = enriched_linear_regression_predictions(train_data, validation_data)

    assert len(predictions) == len(validation_data)
    assert (predictions >= 0).all()


def test_gradient_boosting_returns_non_negative_predictions() -> None:
    train_data = _sample_data(40)
    validation_data = _sample_data(4)

    predictions = gradient_boosting_predictions(
        train_data,
        validation_data,
        CALENDAR_WEATHER_FEATURE_COLUMNS,
    )

    assert len(predictions) == len(validation_data)
    assert (predictions >= 0).all()


def test_warm_start_model_requires_available_validation_profiles() -> None:
    train_data = _sample_data(40)
    validation_data = _sample_data(4)
    validation_data.loc[0, "historical_congestion_available"] = 0

    with pytest.raises(ValueError, match="requires available"):
        warm_start_gradient_boosting_predictions(train_data, validation_data)


def test_full_feature_set_contains_calendar_weather_and_profiles() -> None:
    assert set(CALENDAR_WEATHER_FEATURE_COLUMNS) < set(FULL_ENRICHED_FEATURE_COLUMNS)
    assert "historical_pickup_duration_minutes" in FULL_ENRICHED_FEATURE_COLUMNS


def test_final_residual_contract_uses_tuned_osrm_aware_model() -> None:
    assert FINAL_RESIDUAL_FEATURE_COLUMNS == OSRM_AWARE_FEATURE_COLUMNS
    assert FINAL_RESIDUAL_PARAMETERS["loss"] == "absolute_error"


def test_osrm_residual_model_returns_non_negative_predictions() -> None:
    train_data = _sample_data(60).assign(
        osrm_distance_km=lambda data: data["straight_line_km"] * 1.2,
        osrm_duration_minutes=lambda data: data["duration_minutes"] - 0.5,
    )
    validation_data = _sample_data(6).assign(
        osrm_distance_km=lambda data: data["straight_line_km"] * 1.2,
        osrm_duration_minutes=lambda data: data["duration_minutes"] - 0.5,
    )

    predictions = osrm_residual_gradient_boosting_predictions(train_data, validation_data)

    assert len(predictions) == len(validation_data)
    assert (predictions >= 0).all()
    assert "osrm_duration_minutes" in OSRM_AWARE_FEATURE_COLUMNS


def test_evaluation_script_builds_predictions_and_metadata() -> None:
    train_data = _sample_data(60)
    validation_data = _sample_data(6)

    prediction_sets = _prediction_sets(train_data, validation_data)
    metadata = _experiment_metadata(train_data)

    assert set(prediction_sets) == {
        "training_median",
        "fixed_30_kmh",
        "linear_regression",
        "enriched_linear_regression",
        "calendar_weather_gradient_boosting",
        "full_enriched_gradient_boosting",
        "warm_start_gradient_boosting",
    }
    assert all(len(predictions) == len(validation_data) for predictions in prediction_sets.values())
    assert metadata["warm_start_gradient_boosting"]["training_rows"] == len(train_data)


def _sample_data(rows: int) -> pd.DataFrame:
    index = np.arange(rows, dtype=float)
    return pd.DataFrame(
        {
            "duration_minutes": 5 + index * 0.1,
            "pickup_longitude": -8.62 + index * 0.0001,
            "pickup_latitude": 41.14 + index * 0.0001,
            "destination_longitude": -8.60 + index * 0.0001,
            "destination_latitude": 41.16 + index * 0.0001,
            "straight_line_km": 1 + index * 0.01,
            "hour": index.astype(int) % 24,
            "weekday": index.astype(int) % 7,
            "month": 7,
            "is_weekend": 0,
            "is_public_holiday": 0,
            "hour_sin": np.sin(index),
            "hour_cos": np.cos(index),
            "weekday_sin": np.sin(index),
            "weekday_cos": np.cos(index),
            "month_sin": -0.5,
            "month_cos": -0.87,
            "temperature_c": 20 + index * 0.01,
            "precipitation_mm": 0.0,
            "cloud_cover_percent": 20 + index * 0.1,
            "wind_speed_kmh": 10 + index * 0.01,
            "is_precipitating": 0,
            "historical_time_duration_minutes": 8 + index * 0.1,
            "historical_pickup_duration_minutes": 8 + index * 0.1,
            "historical_pickup_observations": 10,
            "historical_congestion_available": 1,
        }
    )
