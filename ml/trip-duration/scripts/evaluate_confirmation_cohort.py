"""Evaluate the final selected route-aware model on the locked confirmation cohort."""

from __future__ import annotations

import argparse
import json
from pathlib import Path

import pandas as pd

from cabrynt_trip_duration.backtesting import paired_mae_difference_interval
from cabrynt_trip_duration.calendar_features import add_calendar_features
from cabrynt_trip_duration.confirmation_evaluation import confirmation_prediction_sets
from cabrynt_trip_duration.evaluation import (
    TARGET_COLUMN,
    duration_segment_metrics,
    evaluate_prediction_sets,
)
from cabrynt_trip_duration.route_features import (
    add_derived_route_features,
    attach_route_estimates,
)
from cabrynt_trip_duration.weather import add_weather_features

PROJECT_ROOT = Path(__file__).resolve().parents[1]
PREPARED_DIRECTORY = PROJECT_ROOT / "artifacts" / "prepared-data"
ENRICHED_DIRECTORY = PROJECT_ROOT / "artifacts" / "enriched-data"
OSRM_DIRECTORY = PROJECT_ROOT / "artifacts" / "osrm"
WEATHER_PATH = PROJECT_ROOT / "data" / "weather" / "porto-hourly.parquet"
TRAINING_ROUTE_ESTIMATES_PATH = OSRM_DIRECTORY / "training-route-estimates.parquet"
CONFIRMATION_ROUTE_ESTIMATES_PATH = OSRM_DIRECTORY / "confirmation-route-estimates.parquet"
RESULT_PATH = OSRM_DIRECTORY / "confirmation-metrics.json"


def parse_arguments() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Evaluate Cabrynt's selected duration model on the locked confirmation cohort."
    )
    parser.add_argument(
        "--run",
        action="store_true",
        help="Run the one-time confirmation evaluation after model selection.",
    )
    arguments = parser.parse_args()
    if not arguments.run:
        parser.error("pass --run to start the confirmation evaluation")
    return arguments


def main() -> None:
    parse_arguments()
    enriched_train_data, route_train_data, route_confirmation_data = _load_evaluation_data()
    predictions = confirmation_prediction_sets(
        enriched_train_data,
        route_train_data,
        route_confirmation_data,
    )
    metrics = evaluate_prediction_sets(route_confirmation_data[TARGET_COLUMN], predictions).round(3)
    segment_metrics = duration_segment_metrics(
        route_confirmation_data[TARGET_COLUMN],
        predictions,
    ).round(3)
    comparisons = _candidate_comparisons(route_confirmation_data[TARGET_COLUMN], predictions)

    RESULT_PATH.write_text(
        json.dumps(
            {
                "evaluation": "final_locked_confirmation_cohort",
                "full_training_rows": len(enriched_train_data),
                "route_aware_training_rows": len(route_train_data),
                "routable_confirmation_rows": len(route_confirmation_data),
                "metrics": metrics.to_dict(orient="index"),
                "duration_segment_metrics": segment_metrics.to_dict(orient="index"),
                "selected_model_comparisons": comparisons,
            },
            indent=2,
        ),
        encoding="utf-8",
    )

    print("Final locked confirmation cohort metrics (minutes)")
    print(metrics.to_string())
    print("\nSelected-model MAE differences (95% bootstrap CI)")
    for name, comparison in comparisons.items():
        print(
            f"{name}: {comparison['candidate_minus_reference_mae_minutes']:.3f} "
            f"({comparison['confidence_interval_lower_minutes']:.3f} to "
            f"{comparison['confidence_interval_upper_minutes']:.3f})"
        )
    print("\nMAE by actual trip duration (minutes)")
    print(segment_metrics.to_string())
    print(f"\nWrote confirmation metrics to {RESULT_PATH}")


def _load_evaluation_data() -> tuple[pd.DataFrame, pd.DataFrame, pd.DataFrame]:
    required_paths = [
        ENRICHED_DIRECTORY / "train.parquet",
        PREPARED_DIRECTORY / "test.parquet",
        WEATHER_PATH,
        TRAINING_ROUTE_ESTIMATES_PATH,
        CONFIRMATION_ROUTE_ESTIMATES_PATH,
    ]
    missing_paths = [path for path in required_paths if not path.exists()]
    if missing_paths:
        raise FileNotFoundError(
            "Confirmation evaluation inputs are missing. Build enriched training data, the OSRM "
            f"training cohort, and the confirmation cohort first: {missing_paths}"
        )

    enriched_train_data = pd.read_parquet(ENRICHED_DIRECTORY / "train.parquet")
    route_train_data = add_derived_route_features(
        attach_route_estimates(
            enriched_train_data,
            pd.read_parquet(TRAINING_ROUTE_ESTIMATES_PATH),
        )
    )
    route_confirmation_data = attach_route_estimates(
        pd.read_parquet(PREPARED_DIRECTORY / "test.parquet"),
        pd.read_parquet(CONFIRMATION_ROUTE_ESTIMATES_PATH),
    )
    route_confirmation_data = add_derived_route_features(
        add_weather_features(
            add_calendar_features(route_confirmation_data),
            pd.read_parquet(WEATHER_PATH),
        )
    )

    return enriched_train_data, route_train_data, route_confirmation_data


def _candidate_comparisons(
    actual: pd.Series,
    predictions: dict[str, object],
) -> dict[str, dict[str, float]]:
    final_predictions = predictions["final_osrm_residual_gradient_boosting"]
    return {
        "final_vs_direct_osrm": paired_mae_difference_interval(
            actual,
            final_predictions,
            predictions["direct_osrm"],
        ),
        "final_vs_full_calendar_weather": paired_mae_difference_interval(
            actual,
            final_predictions,
            predictions["full_calendar_weather_gradient_boosting"],
        ),
        "route_speed_vs_final": paired_mae_difference_interval(
            actual,
            predictions["selected_route_speed_residual_gradient_boosting"],
            final_predictions,
        ),
    }


if __name__ == "__main__":
    main()
