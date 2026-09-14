"""Compare the selected model with direct OSRM on the same routable cohort."""

from __future__ import annotations

import json
from pathlib import Path

import pandas as pd

from cabrynt_trip_duration.evaluation import (
    TARGET_COLUMN,
    duration_segment_metrics,
    evaluate_prediction_sets,
    linear_regression_predictions,
)
from cabrynt_trip_duration.modeling import (
    CALENDAR_WEATHER_FEATURE_COLUMNS,
    gradient_boosting_predictions,
)
from cabrynt_trip_duration.route_features import attach_route_estimates

PROJECT_ROOT = Path(__file__).resolve().parents[1]
ENRICHED_DATA_DIRECTORY = PROJECT_ROOT / "artifacts" / "enriched-data"
OSRM_DIRECTORY = PROJECT_ROOT / "artifacts" / "osrm"
ROUTE_ESTIMATES_PATH = OSRM_DIRECTORY / "validation-route-estimates.parquet"
RESULT_PATH = OSRM_DIRECTORY / "model-comparison-metrics.json"


def main() -> None:
    train_data, validation_data = _load_enriched_data()
    route_estimates = _load_route_estimates()
    cohort = attach_route_estimates(validation_data, route_estimates)

    predictions = {
        "linear_regression": linear_regression_predictions(train_data, cohort),
        "osrm": cohort["osrm_duration_minutes"].to_numpy(),
        "calendar_weather_gradient_boosting": gradient_boosting_predictions(
            train_data,
            cohort,
            CALENDAR_WEATHER_FEATURE_COLUMNS,
        ),
    }
    metrics = evaluate_prediction_sets(cohort[TARGET_COLUMN], predictions).round(3)
    segment_metrics = duration_segment_metrics(cohort[TARGET_COLUMN], predictions).round(3)

    RESULT_PATH.write_text(
        json.dumps(
            {
                "training_rows": len(train_data),
                "validation_rows": len(validation_data),
                "routable_cohort_rows": len(cohort),
                "route_estimates_path": str(ROUTE_ESTIMATES_PATH),
                "metrics": metrics.to_dict(orient="index"),
                "duration_segment_metrics": segment_metrics.to_dict(orient="index"),
            },
            indent=2,
        ),
        encoding="utf-8",
    )

    print("OSRM cohort comparison metrics (minutes)")
    print(metrics.to_string())
    print("\nMAE by actual trip duration (minutes)")
    print(segment_metrics.to_string())
    print(f"\nWrote metrics to {RESULT_PATH}")


def _load_enriched_data() -> tuple[pd.DataFrame, pd.DataFrame]:
    train_path = ENRICHED_DATA_DIRECTORY / "train.parquet"
    validation_path = ENRICHED_DATA_DIRECTORY / "validation.parquet"
    missing_paths = [path for path in (train_path, validation_path) if not path.exists()]
    if missing_paths:
        raise FileNotFoundError(
            "Enriched data is missing. Run 'python scripts/build_enriched_datasets.py' first: "
            f"{missing_paths}"
        )

    return pd.read_parquet(train_path), pd.read_parquet(validation_path)


def _load_route_estimates() -> pd.DataFrame:
    if not ROUTE_ESTIMATES_PATH.exists():
        raise FileNotFoundError(
            "OSRM route estimates are missing. Start the local OSRM service and run "
            "'python scripts/evaluate_osrm_baseline.py' first."
        )
    return pd.read_parquet(ROUTE_ESTIMATES_PATH)


if __name__ == "__main__":
    main()
