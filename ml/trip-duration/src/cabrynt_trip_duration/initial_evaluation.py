"""Prediction sets for the initial held-out route-aware test evaluation."""

from __future__ import annotations

import numpy as np
import pandas as pd

from cabrynt_trip_duration.evaluation import (
    fixed_speed_predictions,
    linear_regression_predictions,
    training_median_predictions,
)
from cabrynt_trip_duration.modeling import (
    CALENDAR_WEATHER_FEATURE_COLUMNS,
    gradient_boosting_predictions,
    osrm_residual_gradient_boosting_predictions,
)


def initial_prediction_sets(
    enriched_train_data: pd.DataFrame,
    route_train_data: pd.DataFrame,
    route_test_data: pd.DataFrame,
) -> dict[str, np.ndarray]:
    """Evaluate the frozen candidate against fixed baselines on held-out test rows."""
    return {
        "training_median": training_median_predictions(enriched_train_data, route_test_data),
        "fixed_30_kmh": fixed_speed_predictions(route_test_data),
        "linear_regression": linear_regression_predictions(enriched_train_data, route_test_data),
        "direct_osrm": route_test_data["osrm_duration_minutes"].to_numpy(dtype=float),
        "full_calendar_weather_gradient_boosting": gradient_boosting_predictions(
            enriched_train_data,
            route_test_data,
            CALENDAR_WEATHER_FEATURE_COLUMNS,
        ),
        "route_cohort_calendar_weather_gradient_boosting": gradient_boosting_predictions(
            route_train_data,
            route_test_data,
            CALENDAR_WEATHER_FEATURE_COLUMNS,
        ),
        "osrm_residual_gradient_boosting": osrm_residual_gradient_boosting_predictions(
            route_train_data,
            route_test_data,
        ),
    }
