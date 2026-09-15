"""Check whether OSRM residual improvements are stable across earlier time periods."""

from __future__ import annotations

import json
from pathlib import Path

import pandas as pd

from cabrynt_trip_duration.backtesting import (
    paired_mae_difference_interval,
    two_fold_expanding_backtest,
)
from cabrynt_trip_duration.evaluation import (
    TARGET_COLUMN,
    duration_segment_metrics,
    evaluate_prediction_sets,
)
from cabrynt_trip_duration.modeling import (
    CALENDAR_WEATHER_FEATURE_COLUMNS,
    gradient_boosting_predictions,
    osrm_residual_gradient_boosting_predictions,
)
from cabrynt_trip_duration.route_features import attach_route_estimates

PROJECT_ROOT = Path(__file__).resolve().parents[1]
ENRICHED_TRAIN_PATH = PROJECT_ROOT / "artifacts" / "enriched-data" / "train.parquet"
TRAINING_ROUTE_ESTIMATES_PATH = PROJECT_ROOT / "artifacts" / "osrm" / "training-route-estimates.parquet"
RESULT_PATH = PROJECT_ROOT / "artifacts" / "osrm" / "route-aware-backtest-metrics.json"


def main() -> None:
    route_train_data = _load_route_train_data()
    fold_results: dict[str, object] = {}

    for fold in two_fold_expanding_backtest(route_train_data):
        predictions = _prediction_sets(fold.train_data, fold.validation_data)
        metrics = evaluate_prediction_sets(
            fold.validation_data[TARGET_COLUMN],
            predictions,
        ).round(3)
        fold_results[fold.name] = {
            "training_rows": len(fold.train_data),
            "validation_rows": len(fold.validation_data),
            "training_end_timestamp": int(fold.train_data["timestamp"].max()),
            "validation_start_timestamp": int(fold.validation_data["timestamp"].min()),
            "metrics": metrics.to_dict(orient="index"),
            "duration_segment_metrics": duration_segment_metrics(
                fold.validation_data[TARGET_COLUMN],
                predictions,
            )
            .round(3)
            .to_dict(orient="index"),
            "residual_vs_calendar_weather_interval": paired_mae_difference_interval(
                fold.validation_data[TARGET_COLUMN],
                predictions["osrm_residual_gradient_boosting"],
                predictions["calendar_weather_gradient_boosting"],
            ),
        }

    RESULT_PATH.write_text(
        json.dumps(
            {
                "route_ready_rows": len(route_train_data),
                "folds": fold_results,
            },
            indent=2,
        ),
        encoding="utf-8",
    )

    _print_results(fold_results)
    print(f"\nWrote metrics to {RESULT_PATH}")


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

    train_data = pd.read_parquet(ENRICHED_TRAIN_PATH)
    route_estimates = pd.read_parquet(TRAINING_ROUTE_ESTIMATES_PATH)
    return attach_route_estimates(train_data, route_estimates)


def _prediction_sets(
    train_data: pd.DataFrame,
    validation_data: pd.DataFrame,
) -> dict[str, object]:
    return {
        "direct_osrm": validation_data["osrm_duration_minutes"].to_numpy(),
        "calendar_weather_gradient_boosting": gradient_boosting_predictions(
            train_data,
            validation_data,
            CALENDAR_WEATHER_FEATURE_COLUMNS,
        ),
        "osrm_residual_gradient_boosting": osrm_residual_gradient_boosting_predictions(
            train_data,
            validation_data,
        ),
    }


def _print_results(fold_results: dict[str, object]) -> None:
    for name, result in fold_results.items():
        typed_result = dict(result)
        metrics = pd.DataFrame.from_dict(typed_result["metrics"], orient="index")
        interval = typed_result["residual_vs_calendar_weather_interval"]
        print(f"\n{name.capitalize()} chronological fold (minutes)")
        print(metrics.to_string())
        print(
            "Residual minus calendar/weather MAE: "
            f"{interval['candidate_minus_reference_mae_minutes']:.3f} "
            f"(95% bootstrap CI {interval['confidence_interval_lower_minutes']:.3f} "
            f"to {interval['confidence_interval_upper_minutes']:.3f})"
        )


if __name__ == "__main__":
    main()
