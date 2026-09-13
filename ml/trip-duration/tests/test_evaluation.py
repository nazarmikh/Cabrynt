import numpy as np
import pandas as pd
import pytest

from cabrynt_trip_duration.evaluation import (
    QUOTE_TIME_FEATURES,
    evaluate_baselines,
    evaluate_prediction_sets,
    fixed_speed_predictions,
    regression_metrics,
    training_median_predictions,
)


def test_regression_metrics_are_reported_in_minutes() -> None:
    metrics = regression_metrics(
        pd.Series([10.0, 20.0]),
        np.array([12.0, 16.0]),
    )

    assert metrics["mae_minutes"] == 3.0
    assert metrics["rmse_minutes"] == pytest.approx(np.sqrt(10))
    assert metrics["median_absolute_error_minutes"] == 3.0
    assert metrics["p90_absolute_error_minutes"] == pytest.approx(3.8)
    assert metrics["mean_signed_error_minutes"] == -1.0


def test_training_median_predictions_do_not_use_validation_targets() -> None:
    train_data = pd.DataFrame({"duration_minutes": [4.0, 8.0, 12.0]})
    validation_data = pd.DataFrame({"duration_minutes": [100.0, 200.0]})

    predictions = training_median_predictions(train_data, validation_data)

    assert predictions.tolist() == [8.0, 8.0]


def test_fixed_speed_predictions_convert_distance_to_minutes() -> None:
    validation_data = pd.DataFrame({"straight_line_km": [5.0, 10.0]})

    predictions = fixed_speed_predictions(validation_data)

    assert predictions.tolist() == [10.0, 20.0]


def test_fixed_speed_predictions_reject_non_positive_speeds() -> None:
    validation_data = pd.DataFrame({"straight_line_km": [5.0]})

    with pytest.raises(ValueError):
        fixed_speed_predictions(validation_data, speed_kmh=0)


def test_evaluate_baselines_returns_all_initial_models() -> None:
    train_data = _sample_data([5.0, 10.0, 15.0, 20.0, 25.0])
    validation_data = _sample_data([7.0, 12.0])

    results = evaluate_baselines(train_data, validation_data)

    assert results.index.tolist() == [
        "training_median",
        "fixed_30_kmh",
        "linear_regression",
    ]
    assert set(results.columns) == {
        "mae_minutes",
        "rmse_minutes",
        "median_absolute_error_minutes",
        "p90_absolute_error_minutes",
        "mean_signed_error_minutes",
    }
    assert (results["mae_minutes"] >= 0).all()


def test_evaluate_prediction_sets_supports_a_routing_baseline() -> None:
    results = evaluate_prediction_sets(
        pd.Series([10.0, 20.0]),
        {"osrm": np.array([12.0, 16.0])},
    )

    assert results.loc["osrm", "mae_minutes"] == 3.0


def _sample_data(durations: list[float]) -> pd.DataFrame:
    rows = len(durations)
    data = pd.DataFrame(
        {
            "duration_minutes": durations,
            "pickup_longitude": np.linspace(-8.62, -8.60, rows),
            "pickup_latitude": np.linspace(41.14, 41.16, rows),
            "destination_longitude": np.linspace(-8.60, -8.58, rows),
            "destination_latitude": np.linspace(41.16, 41.18, rows),
            "straight_line_km": np.linspace(1, 5, rows),
            "hour": np.arange(rows) % 24,
            "weekday": np.arange(rows) % 7,
        }
    )
    return data[["duration_minutes", *QUOTE_TIME_FEATURES]]
