"""Prediction sets for the final locked route-aware confirmation cohort."""

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
    FINAL_RESIDUAL_FEATURE_COLUMNS,
    FINAL_RESIDUAL_PARAMETERS,
    SELECTED_ROUTE_FEATURE_COLUMNS,
    gradient_boosting_predictions,
    osrm_residual_gradient_boosting_predictions,
)


def confirmation_prediction_sets(
    enriched_train_data: pd.DataFrame,
    route_train_data: pd.DataFrame,
    route_confirmation_data: pd.DataFrame,
) -> dict[str, np.ndarray]:
    """Compare the selected model with fixed baselines on confirmation rows."""
    return {
        "training_median": training_median_predictions(
            enriched_train_data,
            route_confirmation_data,
        ),
        "fixed_30_kmh": fixed_speed_predictions(route_confirmation_data),
        "linear_regression": linear_regression_predictions(
            enriched_train_data,
            route_confirmation_data,
        ),
        "direct_osrm": route_confirmation_data["osrm_duration_minutes"].to_numpy(
            dtype=float
        ),
        "full_calendar_weather_gradient_boosting": gradient_boosting_predictions(
            enriched_train_data,
            route_confirmation_data,
            CALENDAR_WEATHER_FEATURE_COLUMNS,
        ),
        "final_osrm_residual_gradient_boosting": osrm_residual_gradient_boosting_predictions(
            route_train_data,
            route_confirmation_data,
            model_parameters=FINAL_RESIDUAL_PARAMETERS,
            feature_columns=FINAL_RESIDUAL_FEATURE_COLUMNS,
        ),
        "selected_route_speed_residual_gradient_boosting": (
            osrm_residual_gradient_boosting_predictions(
                route_train_data,
                route_confirmation_data,
                model_parameters=FINAL_RESIDUAL_PARAMETERS,
                feature_columns=SELECTED_ROUTE_FEATURE_COLUMNS,
            )
        ),
    }
