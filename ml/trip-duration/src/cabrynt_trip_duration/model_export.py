"""Export the selected residual model and verify its ONNX predictions."""

from __future__ import annotations

from dataclasses import dataclass
import hashlib
import json
from pathlib import Path

import numpy as np
import onnx
import onnxruntime as ort
import pandas as pd
from skl2onnx import convert_sklearn
from skl2onnx.common.data_types import FloatTensorType

from cabrynt_trip_duration.inference_contract import (
    FEATURE_COLUMNS,
    MODEL_INPUT_NAME,
    ONNX_TARGET_OPSET,
    PARITY_ABSOLUTE_TOLERANCE_MINUTES,
    feature_matrix,
    model_metadata,
)
from cabrynt_trip_duration.modeling import (
    FINAL_RESIDUAL_PARAMETERS,
    fit_osrm_residual_gradient_boosting_model,
)


@dataclass(frozen=True)
class ModelExportResult:
    """Paths and parity result for one exported model artifact."""

    onnx_path: Path
    metadata_path: Path
    max_parity_difference_minutes: float


def export_final_model(
    train_data: pd.DataFrame,
    output_directory: Path,
    parity_rows: int = 128,
) -> ModelExportResult:
    """Fit the selected model, export ONNX, and verify raw residual parity."""
    if parity_rows <= 0:
        raise ValueError("parity_rows must be positive")
    if train_data.empty:
        raise ValueError("training data is empty")

    model = fit_osrm_residual_gradient_boosting_model(
        train_data,
        model_parameters=FINAL_RESIDUAL_PARAMETERS,
        feature_columns=list(FEATURE_COLUMNS),
    )
    onnx_model = convert_sklearn(
        model,
        initial_types=[(MODEL_INPUT_NAME, FloatTensorType([None, len(FEATURE_COLUMNS)]))],
        target_opset=ONNX_TARGET_OPSET,
    )
    onnx.checker.check_model(onnx_model)

    output_directory.mkdir(parents=True, exist_ok=True)
    onnx_path = output_directory / "trip-duration-residual.onnx"
    onnx_path.write_bytes(onnx_model.SerializeToString())

    parity_data = train_data.iloc[: min(parity_rows, len(train_data))].loc[:, FEATURE_COLUMNS]
    parity_data = parity_data.astype(np.float32)
    parity_input = feature_matrix(parity_data)
    sklearn_predictions = model.predict(parity_data)
    session = ort.InferenceSession(
        onnx_path.as_posix(),
        providers=["CPUExecutionProvider"],
    )
    onnx_output_name = session.get_outputs()[0].name
    onnx_predictions = session.run(
        [onnx_output_name],
        {MODEL_INPUT_NAME: parity_input},
    )[0].reshape(-1)
    max_difference = float(np.max(np.abs(sklearn_predictions - onnx_predictions)))
    if max_difference > PARITY_ABSOLUTE_TOLERANCE_MINUTES:
        raise ValueError(
            "ONNX export differs from the scikit-learn residual model by "
            f"{max_difference:.6f} minutes"
        )

    model_sha256 = hashlib.sha256(onnx_path.read_bytes()).hexdigest()
    metadata_path = output_directory / "trip-duration-residual.metadata.json"
    metadata_path.write_text(
        json.dumps(
            model_metadata(
                onnx_output_name=onnx_output_name,
                training_rows=len(train_data),
                model_sha256=model_sha256,
                max_parity_difference_minutes=max_difference,
            ),
            indent=2,
        ),
        encoding="utf-8",
    )

    return ModelExportResult(
        onnx_path=onnx_path,
        metadata_path=metadata_path,
        max_parity_difference_minutes=max_difference,
    )
