import json

import numpy as np
import pandas as pd

from cabrynt_trip_duration.inference_contract import FEATURE_COLUMNS
from cabrynt_trip_duration.model_export import export_final_model


def test_exported_model_matches_the_scikit_learn_residual_model(tmp_path) -> None:
    train_data = _training_data(300)

    result = export_final_model(train_data, tmp_path, parity_rows=40)

    assert result.onnx_path.exists()
    assert result.max_parity_difference_minutes < 0.001

    metadata = json.loads(result.metadata_path.read_text(encoding="utf-8"))
    assert metadata["training_rows"] == len(train_data)
    assert metadata["input"]["feature_columns"] == list(FEATURE_COLUMNS)
    assert metadata["output"]["meaning"] == "residual_correction_minutes"


def _training_data(rows: int) -> pd.DataFrame:
    random = np.random.default_rng(42)
    data = pd.DataFrame(
        random.normal(size=(rows, len(FEATURE_COLUMNS))),
        columns=FEATURE_COLUMNS,
    )
    data["osrm_duration_minutes"] = random.uniform(3.0, 20.0, size=rows)
    data["duration_minutes"] = (
        data["osrm_duration_minutes"]
        + data["pickup_longitude"] ** 2
        - data["destination_latitude"]
    )
    return data
