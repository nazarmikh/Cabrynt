"""Evaluate direct and residual OSRM-aware models on the fixed routing cohort."""

from __future__ import annotations

import json
from pathlib import Path

import pandas as pd

from cabrynt_trip_duration.evaluation import (
    TARGET_COLUMN,
    duration_segment_metrics,
    evaluate_prediction_sets,
)
from cabrynt_trip_duration.modeling import (
    CALENDAR_WEATHER_FEATURE_COLUMNS,
    OSRM_AWARE_FEATURE_COLUMNS,
    gradient_boosting_predictions,
    osrm_residual_gradient_boosting_predictions,
)
from cabrynt_trip_duration.route_features import attach_route_estimates

PROJECT_ROOT = Path(__file__).resolve().parents[1]
ENRICHED_DATA_DIRECTORY = PROJECT_ROOT / "artifacts" / "enriched-data"
OSRM_DIRECTORY = PROJECT_ROOT / "artifacts" / "osrm"
TRAINING_ROUTE_ESTIMATES_PATH = OSRM_DIRECTORY / "training-route-estimates.parquet"
VALIDATION_ROUTE_ESTIMATES_PATH = OSRM_DIRECTORY / "validation-route-estimates.parquet"
RESULT_PATH = OSRM_DIRECTORY / "route-aware-model-metrics.json"


def main() -> None:
    train_data, validation_data = _load_enriched_data()
    route_train_data = attach_route_estimates(train_data, _load_route_estimates(TRAINING_ROUTE_ESTIMATES_PATH))
    route_validation_data = attach_route_estimates(
        validation_data,
        _load_route_estimates(VALIDATION_ROUTE_ESTIMATES_PATH),
    )
    predictions = _prediction_sets(route_train_data, route_validation_data)
    metrics = evaluate_prediction_sets(
        route_validation_data[TARGET_COLUMN],
        predictions,
    ).round(3)
    segment_metrics = duration_segment_metrics(
        route_validation_data[TARGET_COLUMN],
        predictions,
    ).round(3)

    RESULT_PATH.write_text(
        json.dumps(
            {
                "route_aware_training_rows": len(route_train_data),
                "routable_validation_rows": len(route_validation_data),
                "feature_sets": {
                    "calendar_weather_gradient_boosting": CALENDAR_WEATHER_FEATURE_COLUMNS,
                    "osrm_aware_gradient_boosting": OSRM_AWARE_FEATURE_COLUMNS,
                    "osrm_residual_gradient_boosting": OSRM_AWARE_FEATURE_COLUMNS,
                },
                "metrics": metrics.to_dict(orient="index"),
                "duration_segment_metrics": segment_metrics.to_dict(orient="index"),
            },
            indent=2,
        ),
        encoding="utf-8",
    )

    print("Route-aware OSRM cohort metrics (minutes)")
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


def _load_route_estimates(path: Path) -> pd.DataFrame:
    if not path.exists():
        raise FileNotFoundError(
            f"Route estimates are missing at {path}. Build the required OSRM cohort first."
        )
    return pd.read_parquet(path)


def _prediction_sets(
    route_train_data: pd.DataFrame,
    route_validation_data: pd.DataFrame,
) -> dict[str, object]:
    return {
        "direct_osrm": route_validation_data["osrm_duration_minutes"].to_numpy(),
        "calendar_weather_gradient_boosting": gradient_boosting_predictions(
            route_train_data,
            route_validation_data,
            CALENDAR_WEATHER_FEATURE_COLUMNS,
        ),
        "osrm_aware_gradient_boosting": gradient_boosting_predictions(
            route_train_data,
            route_validation_data,
            OSRM_AWARE_FEATURE_COLUMNS,
        ),
        "osrm_residual_gradient_boosting": osrm_residual_gradient_boosting_predictions(
            route_train_data,
            route_validation_data,
        ),
    }


if __name__ == "__main__":
    main()
