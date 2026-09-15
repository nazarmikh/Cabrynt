"""Baseline models and regression metrics for trip-duration experiments."""

from __future__ import annotations

import numpy as np
import pandas as pd
from sklearn.linear_model import LinearRegression

TARGET_COLUMN = "duration_minutes"
FIXED_SPEED_KMH = 30.0
DURATION_BINS = [0, 5, 10, 20, 30, np.inf]
DURATION_LABELS = ["0-5", "5-10", "10-20", "20-30", "30+"]
BASE_FEATURE_COLUMNS = [
    "pickup_longitude",
    "pickup_latitude",
    "destination_longitude",
    "destination_latitude",
    "straight_line_km",
]
QUOTE_TIME_FEATURES = [
    *BASE_FEATURE_COLUMNS,
    "hour",
    "weekday",
]


def regression_metrics(
    actual: pd.Series,
    predicted: np.ndarray,
) -> dict[str, float]:
    """Return duration metrics in minutes for one set of predictions."""
    actual_values = actual.to_numpy(dtype=float)
    predicted_values = np.asarray(predicted, dtype=float)
    errors = predicted_values - actual_values
    absolute_errors = np.abs(errors)

    return {
        "mae_minutes": float(absolute_errors.mean()),
        "rmse_minutes": float(np.sqrt(np.mean(errors**2))),
        "median_absolute_error_minutes": float(np.median(absolute_errors)),
        "p90_absolute_error_minutes": float(np.quantile(absolute_errors, 0.90)),
        "mean_signed_error_minutes": float(errors.mean()),
    }


def training_median_predictions(
    train_data: pd.DataFrame,
    evaluation_data: pd.DataFrame,
) -> np.ndarray:
    """Predict the training-target median for every evaluation row."""
    median_duration = train_data[TARGET_COLUMN].median()
    return np.full(len(evaluation_data), median_duration)


def fixed_speed_predictions(
    evaluation_data: pd.DataFrame,
    speed_kmh: float = FIXED_SPEED_KMH,
) -> np.ndarray:
    """Estimate duration from straight-line distance at a fixed average speed."""
    if speed_kmh <= 0:
        raise ValueError("speed_kmh must be positive")

    return evaluation_data["straight_line_km"].to_numpy(dtype=float) / speed_kmh * 60


def linear_regression_predictions(
    train_data: pd.DataFrame,
    evaluation_data: pd.DataFrame,
) -> np.ndarray:
    """Fit a simple quote-time linear regression and return non-negative estimates."""
    model = LinearRegression()
    model.fit(train_data[QUOTE_TIME_FEATURES], train_data[TARGET_COLUMN])
    return np.maximum(model.predict(evaluation_data[QUOTE_TIME_FEATURES]), 0)


def evaluate_baselines(
    train_data: pd.DataFrame,
    validation_data: pd.DataFrame,
) -> pd.DataFrame:
    """Evaluate the initial duration baselines on a validation split."""
    prediction_sets = {
        "training_median": training_median_predictions(train_data, validation_data),
        "fixed_30_kmh": fixed_speed_predictions(validation_data),
        "linear_regression": linear_regression_predictions(train_data, validation_data),
    }

    return evaluate_prediction_sets(validation_data[TARGET_COLUMN], prediction_sets)


def evaluate_prediction_sets(
    actual: pd.Series,
    prediction_sets: dict[str, np.ndarray],
) -> pd.DataFrame:
    """Calculate the same metrics for named prediction arrays."""
    return pd.DataFrame.from_dict(
        {
            name: regression_metrics(actual, predictions)
            for name, predictions in prediction_sets.items()
        },
        orient="index",
    )


def duration_segment_metrics(
    actual: pd.Series,
    prediction_sets: dict[str, np.ndarray],
) -> pd.DataFrame:
    """Calculate MAE for fixed actual-duration groups."""
    actual_values = actual.to_numpy(dtype=float)
    segments = pd.cut(
        actual,
        bins=DURATION_BINS,
        labels=DURATION_LABELS,
        include_lowest=True,
    )
    rows: list[dict[str, float | int | str]] = []

    for segment in DURATION_LABELS:
        mask = segments.eq(segment).to_numpy()
        row: dict[str, float | int | str] = {
            "actual_duration_minutes": segment,
            "rows": int(mask.sum()),
        }
        for name, predictions in prediction_sets.items():
            errors = np.abs(predictions[mask] - actual_values[mask])
            row[f"{name}_mae_minutes"] = float(errors.mean())
        rows.append(row)

    return pd.DataFrame(rows).set_index("actual_duration_minutes")
