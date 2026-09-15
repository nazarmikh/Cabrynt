"""Evaluate initial trip-duration baselines without reading the test split."""

from __future__ import annotations

import json
from pathlib import Path

import pandas as pd

from cabrynt_trip_duration.evaluation import FIXED_SPEED_KMH, evaluate_baselines

PROJECT_ROOT = Path(__file__).resolve().parents[1]
DATA_DIRECTORY = PROJECT_ROOT / "artifacts" / "prepared-data"
RESULT_PATH = PROJECT_ROOT / "artifacts" / "baseline-metrics.json"


def main() -> None:
    train_path = DATA_DIRECTORY / "train.parquet"
    validation_path = DATA_DIRECTORY / "validation.parquet"
    missing_paths = [path for path in (train_path, validation_path) if not path.exists()]
    if missing_paths:
        raise FileNotFoundError(
            "Prepared data is missing. Run 'python scripts/build_dataset.py' first: "
            f"{missing_paths}"
        )

    train_data = pd.read_parquet(train_path)
    validation_data = pd.read_parquet(validation_path)
    results = evaluate_baselines(train_data, validation_data).round(3)

    RESULT_PATH.write_text(
        json.dumps(
            {
                "training_rows": len(train_data),
                "validation_rows": len(validation_data),
                "fixed_speed_kmh": FIXED_SPEED_KMH,
                "metrics": results.to_dict(orient="index"),
            },
            indent=2,
        ),
        encoding="utf-8",
    )

    print("Validation baseline metrics (minutes)")
    print(results.to_string())
    print(f"\nWrote metrics to {RESULT_PATH}")


if __name__ == "__main__":
    main()
