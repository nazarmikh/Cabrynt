"""Create a local audit report for enriched candidate model features."""

from __future__ import annotations

import json
from pathlib import Path

import pandas as pd

from cabrynt_trip_duration.feature_audit import audit_features

PROJECT_ROOT = Path(__file__).resolve().parents[1]
DATA_DIRECTORY = PROJECT_ROOT / "artifacts" / "enriched-data"
REPORT_PATH = PROJECT_ROOT / "artifacts" / "feature-audit" / "summary.json"


def main() -> None:
    train_path = DATA_DIRECTORY / "train.parquet"
    validation_path = DATA_DIRECTORY / "validation.parquet"
    missing_paths = [path for path in (train_path, validation_path) if not path.exists()]
    if missing_paths:
        raise FileNotFoundError(
            "Enriched data is missing. Run 'python scripts/build_enriched_datasets.py' first: "
            f"{missing_paths}"
        )

    train_data = pd.read_parquet(train_path)
    validation_data = pd.read_parquet(validation_path)
    report = audit_features(train_data, validation_data)

    REPORT_PATH.parent.mkdir(parents=True, exist_ok=True)
    REPORT_PATH.write_text(json.dumps(report, indent=2), encoding="utf-8")

    print(f"Audited {report['feature_count']} candidate features")
    print(f"Training rows: {report['training_rows']:,}")
    print(f"Validation rows: {report['validation_rows']:,}")
    print(f"Constant features: {report['constant_features']}")
    print(f"Training features with missing values: {report['features_with_missing_training_values']}")
    print(f"Features with large mean shift: {report['features_with_large_mean_shift']}")
    print(f"High correlation pairs: {len(report['high_correlation_pairs'])}")
    print(f"Wrote report to {REPORT_PATH}")


if __name__ == "__main__":
    main()
