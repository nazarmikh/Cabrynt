import numpy as np
import pandas as pd
import pytest

from cabrynt_trip_duration.inference_contract import (
    FEATURE_COLUMNS,
    MODEL_INPUT_NAME,
    final_duration_predictions,
    feature_matrix,
    model_metadata,
)


def test_feature_matrix_uses_the_declared_float32_feature_order() -> None:
    data = pd.DataFrame(
        {
            column: [float(index), float(index + 1)]
            for index, column in enumerate(FEATURE_COLUMNS)
        }
    ).loc[:, list(reversed(FEATURE_COLUMNS))]

    result = feature_matrix(data)

    assert result.dtype == np.float32
    assert result.shape == (2, len(FEATURE_COLUMNS))
    assert result[0].tolist() == list(range(len(FEATURE_COLUMNS)))


def test_feature_matrix_rejects_missing_contract_fields() -> None:
    with pytest.raises(ValueError, match="missing columns"):
        feature_matrix(pd.DataFrame({FEATURE_COLUMNS[0]: [1.0]}))


def test_final_duration_adds_residual_and_clamps_negative_values() -> None:
    result = final_duration_predictions([5.0, 1.0], [-2.0, -3.0])

    assert result.tolist() == [3.0, 0.0]


def test_model_metadata_contains_runtime_input_and_output_rules() -> None:
    metadata = model_metadata(
        onnx_output_name="variable",
        training_rows=200,
        model_sha256="abc",
        max_parity_difference_minutes=0.0001,
    )

    assert metadata["input"]["name"] == MODEL_INPUT_NAME
    assert metadata["input"]["feature_columns"] == list(FEATURE_COLUMNS)
    assert metadata["output"]["meaning"] == "residual_correction_minutes"
