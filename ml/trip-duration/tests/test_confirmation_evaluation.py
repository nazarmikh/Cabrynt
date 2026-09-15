import numpy as np
import pandas as pd

from cabrynt_trip_duration.confirmation_evaluation import confirmation_prediction_sets


def test_confirmation_evaluation_includes_selected_model_and_baselines() -> None:
    train_data = _sample_data(80)
    route_train_data = train_data.assign(
        osrm_distance_km=lambda data: data["straight_line_km"] * 1.2,
        osrm_duration_minutes=lambda data: data["duration_minutes"] - 0.5,
        osrm_average_speed_kmh=30.0,
    )
    confirmation_data = _sample_data(8).assign(
        osrm_distance_km=lambda data: data["straight_line_km"] * 1.2,
        osrm_duration_minutes=lambda data: data["duration_minutes"] - 0.5,
        osrm_average_speed_kmh=30.0,
    )

    predictions = confirmation_prediction_sets(train_data, route_train_data, confirmation_data)

    assert set(predictions) == {
        "training_median",
        "fixed_30_kmh",
        "linear_regression",
        "direct_osrm",
        "full_calendar_weather_gradient_boosting",
        "final_osrm_residual_gradient_boosting",
        "selected_route_speed_residual_gradient_boosting",
    }
    assert all(len(prediction) == len(confirmation_data) for prediction in predictions.values())
    assert all((prediction >= 0).all() for prediction in predictions.values())


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
        }
    )
