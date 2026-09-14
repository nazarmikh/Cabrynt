"""First reproducible models for enriched trip-duration features."""

from __future__ import annotations

import numpy as np
import pandas as pd
from sklearn.ensemble import HistGradientBoostingRegressor
from sklearn.impute import SimpleImputer
from sklearn.linear_model import LinearRegression
from sklearn.pipeline import make_pipeline

from cabrynt_trip_duration.enrichment import (
    CALENDAR_FEATURE_COLUMNS,
    ENRICHED_FEATURE_COLUMNS,
)
from cabrynt_trip_duration.evaluation import BASE_FEATURE_COLUMNS, TARGET_COLUMN
from cabrynt_trip_duration.weather import WEATHER_FEATURE_COLUMNS

CALENDAR_WEATHER_FEATURE_COLUMNS = [
    *BASE_FEATURE_COLUMNS,
    *CALENDAR_FEATURE_COLUMNS,
    *WEATHER_FEATURE_COLUMNS,
]
FULL_ENRICHED_FEATURE_COLUMNS = [*BASE_FEATURE_COLUMNS, *ENRICHED_FEATURE_COLUMNS]
GRADIENT_BOOSTING_PARAMETERS = {
    "learning_rate": 0.08,
    "max_iter": 150,
    "max_leaf_nodes": 31,
    "min_samples_leaf": 50,
    "l2_regularization": 1.0,
    "early_stopping": False,
    "random_state": 42,
}


def enriched_linear_regression_predictions(
    train_data: pd.DataFrame,
    validation_data: pd.DataFrame,
) -> np.ndarray:
    """Fit a linear model with median handling for cold-start profile values."""
    _validate_model_input(train_data, validation_data, FULL_ENRICHED_FEATURE_COLUMNS)

    model = make_pipeline(SimpleImputer(strategy="median"), LinearRegression())
    model.fit(train_data[FULL_ENRICHED_FEATURE_COLUMNS], train_data[TARGET_COLUMN])
    return _non_negative_predictions(model.predict(validation_data[FULL_ENRICHED_FEATURE_COLUMNS]))


def gradient_boosting_predictions(
    train_data: pd.DataFrame,
    validation_data: pd.DataFrame,
    feature_columns: list[str],
) -> np.ndarray:
    """Fit a fixed, non-linear tabular model without validation-set tuning."""
    _validate_model_input(train_data, validation_data, feature_columns)

    model = HistGradientBoostingRegressor(**GRADIENT_BOOSTING_PARAMETERS)
    model.fit(train_data[feature_columns], train_data[TARGET_COLUMN])
    return _non_negative_predictions(model.predict(validation_data[feature_columns]))


def warm_start_gradient_boosting_predictions(
    train_data: pd.DataFrame,
    validation_data: pd.DataFrame,
) -> np.ndarray:
    """Train only on rows with historical profiles and score profile-ready validation rows."""
    _validate_model_input(train_data, validation_data, FULL_ENRICHED_FEATURE_COLUMNS)
    if not validation_data["historical_congestion_available"].eq(1).all():
        raise ValueError("warm-start evaluation requires available validation congestion features")

    warm_start_train_data = train_data.loc[
        train_data["historical_congestion_available"].eq(1)
    ]
    if warm_start_train_data.empty:
        raise ValueError("warm-start training data is empty")

    return gradient_boosting_predictions(
        warm_start_train_data,
        validation_data,
        FULL_ENRICHED_FEATURE_COLUMNS,
    )


def _validate_model_input(
    train_data: pd.DataFrame,
    validation_data: pd.DataFrame,
    feature_columns: list[str],
) -> None:
    required_columns = set([TARGET_COLUMN, *feature_columns])
    for name, data in (("training", train_data), ("validation", validation_data)):
        missing_columns = required_columns - set(data)
        if missing_columns:
            raise ValueError(f"{name} data is missing columns: {sorted(missing_columns)}")


def _non_negative_predictions(predictions: np.ndarray) -> np.ndarray:
    return np.maximum(np.asarray(predictions, dtype=float), 0)
