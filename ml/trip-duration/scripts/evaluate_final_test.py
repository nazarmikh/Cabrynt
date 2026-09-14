"""Evaluate the frozen route-aware candidate once on the untouched test cohort."""

from __future__ import annotations

import json
from pathlib import Path

import pandas as pd

from cabrynt_trip_duration.backtesting import paired_mae_difference_interval
from cabrynt_trip_duration.calendar_features import add_calendar_features
from cabrynt_trip_duration.evaluation import (
    TARGET_COLUMN,
    duration_segment_metrics,
    evaluate_prediction_sets,
)
from cabrynt_trip_duration.final_evaluation import final_prediction_sets
from cabrynt_trip_duration.route_features import attach_route_estimates
from cabrynt_trip_duration.weather import add_weather_features

PROJECT_ROOT = Path(__file__).resolve().parents[1]
PREPARED_DIRECTORY = PROJECT_ROOT / "artifacts" / "prepared-data"
ENRICHED_DIRECTORY = PROJECT_ROOT / "artifacts" / "enriched-data"
OSRM_DIRECTORY = PROJECT_ROOT / "artifacts" / "osrm"
WEATHER_PATH = PROJECT_ROOT / "data" / "weather" / "porto-hourly.parquet"
TRAINING_ROUTE_ESTIMATES_PATH = OSRM_DIRECTORY / "training-route-estimates.parquet"
TEST_ROUTE_ESTIMATES_PATH = OSRM_DIRECTORY / "test-route-estimates.parquet"
RESULT_PATH = OSRM_DIRECTORY / "final-test-metrics.json"


def main() -> None:
    enriched_train_data, route_train_data, route_test_data = _load_evaluation_data()
    predictions = final_prediction_sets(
        enriched_train_data,
        route_train_data,
        route_test_data,
    )
    metrics = evaluate_prediction_sets(route_test_data[TARGET_COLUMN], predictions).round(3)
    segment_metrics = duration_segment_metrics(route_test_data[TARGET_COLUMN], predictions).round(3)
    candidate_comparisons = _candidate_comparisons(route_test_data[TARGET_COLUMN], predictions)

    RESULT_PATH.write_text(
        json.dumps(
            {
                "evaluation": "final_untouched_test_cohort",
                "full_training_rows": len(enriched_train_data),
                "route_aware_training_rows": len(route_train_data),
                "routable_test_cohort_rows": len(route_test_data),
                "metrics": metrics.to_dict(orient="index"),
                "duration_segment_metrics": segment_metrics.to_dict(orient="index"),
                "candidate_comparisons": candidate_comparisons,
            },
            indent=2,
        ),
        encoding="utf-8",
    )

    print("Final unseen test cohort metrics (minutes)")
    print(metrics.to_string())
    print("\nResidual-model MAE differences (95% bootstrap CI)")
    for name, comparison in candidate_comparisons.items():
        print(
            f"{name}: {comparison['candidate_minus_reference_mae_minutes']:.3f} "
            f"({comparison['confidence_interval_lower_minutes']:.3f} to "
            f"{comparison['confidence_interval_upper_minutes']:.3f})"
        )
    print("\nMAE by actual trip duration (minutes)")
    print(segment_metrics.to_string())
    print(f"\nWrote final metrics to {RESULT_PATH}")


def _load_evaluation_data() -> tuple[pd.DataFrame, pd.DataFrame, pd.DataFrame]:
    required_paths = [
        ENRICHED_DIRECTORY / "train.parquet",
        PREPARED_DIRECTORY / "test.parquet",
        WEATHER_PATH,
        TRAINING_ROUTE_ESTIMATES_PATH,
        TEST_ROUTE_ESTIMATES_PATH,
    ]
    missing_paths = [path for path in required_paths if not path.exists()]
    if missing_paths:
        raise FileNotFoundError(
            "Final evaluation inputs are missing. Build enriched training data, the OSRM "
            f"training cohort, and the final OSRM test cohort first: {missing_paths}"
        )

    enriched_train_data = pd.read_parquet(ENRICHED_DIRECTORY / "train.parquet")
    route_train_data = attach_route_estimates(
        enriched_train_data,
        pd.read_parquet(TRAINING_ROUTE_ESTIMATES_PATH),
    )
    test_data = pd.read_parquet(PREPARED_DIRECTORY / "test.parquet")
    route_test_data = attach_route_estimates(
        test_data,
        pd.read_parquet(TEST_ROUTE_ESTIMATES_PATH),
    )
    weather_data = pd.read_parquet(WEATHER_PATH)
    route_test_data = add_weather_features(
        add_calendar_features(route_test_data),
        weather_data,
    )

    return enriched_train_data, route_train_data, route_test_data


def _candidate_comparisons(
    actual: pd.Series,
    predictions: dict[str, object],
) -> dict[str, dict[str, float]]:
    candidate = predictions["osrm_residual_gradient_boosting"]
    return {
        "residual_vs_direct_osrm": paired_mae_difference_interval(
            actual,
            candidate,
            predictions["direct_osrm"],
        ),
        "residual_vs_full_calendar_weather": paired_mae_difference_interval(
            actual,
            candidate,
            predictions["full_calendar_weather_gradient_boosting"],
        ),
        "residual_vs_route_cohort_calendar_weather": paired_mae_difference_interval(
            actual,
            candidate,
            predictions["route_cohort_calendar_weather_gradient_boosting"],
        ),
    }


if __name__ == "__main__":
    main()
