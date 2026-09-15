"""Train the selected trip-duration model and export a verified ONNX artifact."""

from __future__ import annotations

import argparse
from pathlib import Path

import pandas as pd

from cabrynt_trip_duration.model_export import export_final_model
from cabrynt_trip_duration.route_features import attach_route_estimates

PROJECT_ROOT = Path(__file__).resolve().parents[1]
ENRICHED_TRAIN_PATH = PROJECT_ROOT / "artifacts" / "enriched-data" / "train.parquet"
TRAINING_ROUTE_ESTIMATES_PATH = PROJECT_ROOT / "artifacts" / "osrm" / "training-route-estimates.parquet"
MODEL_OUTPUT_DIRECTORY = PROJECT_ROOT / "artifacts" / "models"


def parse_arguments() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Train and export the selected route-aware trip-duration model."
    )
    parser.add_argument(
        "--run",
        action="store_true",
        help="Fit the final model and write ignored ONNX artifacts.",
    )
    arguments = parser.parse_args()
    if not arguments.run:
        parser.error("pass --run to train and export the final model")
    return arguments


def main() -> None:
    parse_arguments()
    train_data = _load_route_train_data()
    result = export_final_model(train_data, MODEL_OUTPUT_DIRECTORY)

    print(f"Exported ONNX model to {result.onnx_path}")
    print(f"Wrote metadata to {result.metadata_path}")
    print(
        "Maximum scikit-learn versus ONNX residual difference: "
        f"{result.max_parity_difference_minutes:.9f} minutes"
    )


def _load_route_train_data() -> pd.DataFrame:
    missing_paths = [
        path
        for path in (ENRICHED_TRAIN_PATH, TRAINING_ROUTE_ESTIMATES_PATH)
        if not path.exists()
    ]
    if missing_paths:
        raise FileNotFoundError(
            "Route-aware training data is missing. Build enriched data and the OSRM training "
            f"cohort first: {missing_paths}"
        )

    return attach_route_estimates(
        pd.read_parquet(ENRICHED_TRAIN_PATH),
        pd.read_parquet(TRAINING_ROUTE_ESTIMATES_PATH),
    )


if __name__ == "__main__":
    main()
