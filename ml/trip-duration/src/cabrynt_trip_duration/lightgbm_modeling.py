"""Fixed LightGBM comparison model for route-aware residual prediction."""

from __future__ import annotations

import numpy as np
import pandas as pd
from lightgbm import LGBMRegressor

from cabrynt_trip_duration.evaluation import TARGET_COLUMN
from cabrynt_trip_duration.modeling import OSRM_AWARE_FEATURE_COLUMNS, _non_negative_predictions

LIGHTGBM_RESIDUAL_PARAMETERS = {
    "objective": "regression_l1",
    "learning_rate": 0.06,
    "n_estimators": 200,
    "num_leaves": 63,
    "min_child_samples": 50,
    "reg_lambda": 1.0,
    "random_state": 42,
    "n_jobs": -1,
    "verbosity": -1,
}


def lightgbm_osrm_residual_predictions(
    train_data: pd.DataFrame,
    validation_data: pd.DataFrame,
) -> np.ndarray:
    """Fit a fixed LightGBM correction to the direct OSRM duration estimate."""
    _validate_model_input(train_data, validation_data)

    model = LGBMRegressor(**LIGHTGBM_RESIDUAL_PARAMETERS)
    residual_target = train_data[TARGET_COLUMN] - train_data["osrm_duration_minutes"]
    model.fit(train_data[OSRM_AWARE_FEATURE_COLUMNS], residual_target)
    corrections = model.predict(validation_data[OSRM_AWARE_FEATURE_COLUMNS])
    return _non_negative_predictions(
        validation_data["osrm_duration_minutes"].to_numpy(dtype=float) + corrections
    )


def _validate_model_input(
    train_data: pd.DataFrame,
    validation_data: pd.DataFrame,
) -> None:
    required_columns = {TARGET_COLUMN, *OSRM_AWARE_FEATURE_COLUMNS}
    for name, data in (("training", train_data), ("validation", validation_data)):
        missing_columns = required_columns - set(data)
        if missing_columns:
            raise ValueError(f"{name} data is missing columns: {sorted(missing_columns)}")
