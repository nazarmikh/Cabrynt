"""Stable input and output rules for the deployed trip-duration model."""

from __future__ import annotations

from collections.abc import Sequence

import numpy as np
import pandas as pd

from cabrynt_trip_duration.modeling import (
    FINAL_RESIDUAL_FEATURE_COLUMNS,
    FINAL_RESIDUAL_PARAMETERS,
)

MODEL_NAME = "cabrynt-trip-duration-osrm-residual"
MODEL_VERSION = "1.0.0"
MODEL_INPUT_NAME = "features"
ONNX_TARGET_OPSET = 17
PARITY_ABSOLUTE_TOLERANCE_MINUTES = 0.001
SUPPORTED_CITY = "Porto, Portugal"
FEATURE_COLUMNS = tuple(FINAL_RESIDUAL_FEATURE_COLUMNS)


def feature_matrix(data: pd.DataFrame) -> np.ndarray:
    """Return quote-time features in the exact float32 ONNX input order."""
    missing_columns = set(FEATURE_COLUMNS) - set(data)
    if missing_columns:
        raise ValueError(f"inference data is missing columns: {sorted(missing_columns)}")

    return data.loc[:, FEATURE_COLUMNS].to_numpy(dtype=np.float32)


def final_duration_predictions(
    osrm_duration_minutes: Sequence[float] | np.ndarray,
    residual_corrections_minutes: Sequence[float] | np.ndarray,
) -> np.ndarray:
    """Apply the selected residual-model post-processing rule."""
    osrm_values = np.asarray(osrm_duration_minutes, dtype=float)
    correction_values = np.asarray(residual_corrections_minutes, dtype=float)
    if osrm_values.shape != correction_values.shape:
        raise ValueError("OSRM durations and residual corrections must have matching shapes")

    return np.maximum(osrm_values + correction_values, 0.0)


def model_metadata(
    *,
    onnx_output_name: str,
    training_rows: int,
    model_sha256: str,
    max_parity_difference_minutes: float,
) -> dict[str, object]:
    """Describe the artifact so another runtime can construct inputs safely."""
    return {
        "model_name": MODEL_NAME,
        "model_version": MODEL_VERSION,
        "supported_city": SUPPORTED_CITY,
        "model_sha256": model_sha256,
        "training_rows": training_rows,
        "input": {
            "name": MODEL_INPUT_NAME,
            "data_type": "float32",
            "feature_columns": list(FEATURE_COLUMNS),
        },
        "output": {
            "name": onnx_output_name,
            "meaning": "residual_correction_minutes",
            "post_processing": "max(osrm_duration_minutes + residual_correction_minutes, 0)",
            "unit": "minutes",
        },
        "model_parameters": FINAL_RESIDUAL_PARAMETERS,
        "onnx_target_opset": ONNX_TARGET_OPSET,
        "max_parity_difference_minutes": max_parity_difference_minutes,
    }
