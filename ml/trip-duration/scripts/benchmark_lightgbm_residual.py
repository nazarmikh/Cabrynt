"""Benchmark one fixed LightGBM residual model on chronological development folds."""

from __future__ import annotations

import argparse
import json
from pathlib import Path

import pandas as pd

from cabrynt_trip_duration.backtesting import (
    paired_mae_difference_interval,
    two_fold_expanding_backtest,
)
from cabrynt_trip_duration.evaluation import TARGET_COLUMN, regression_metrics
from cabrynt_trip_duration.lightgbm_modeling import (
    LIGHTGBM_RESIDUAL_PARAMETERS,
    lightgbm_osrm_residual_predictions,
)
from cabrynt_trip_duration.modeling import (
    TUNED_RESIDUAL_PARAMETERS,
    osrm_residual_gradient_boosting_predictions,
)
from cabrynt_trip_duration.route_features import attach_route_estimates

PROJECT_ROOT = Path(__file__).resolve().parents[1]
ENRICHED_TRAIN_PATH = PROJECT_ROOT / "artifacts" / "enriched-data" / "train.parquet"
TRAINING_ROUTE_ESTIMATES_PATH = PROJECT_ROOT / "artifacts" / "osrm" / "training-route-estimates.parquet"
RESULT_PATH = PROJECT_ROOT / "artifacts" / "osrm" / "lightgbm-benchmark-metrics.json"


def parse_arguments() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Benchmark a fixed LightGBM OSRM residual model chronologically."
    )
    parser.add_argument(
        "--run",
        action="store_true",
        help="Run the development-only LightGBM comparison.",
    )
    arguments = parser.parse_args()
    if not arguments.run:
        parser.error("pass --run to start the LightGBM benchmark")
    return arguments


def main() -> None:
    parse_arguments()
    route_train_data = _load_route_train_data()
    fold_results = _evaluate_folds(route_train_data)

    RESULT_PATH.write_text(
        json.dumps(
            {
                "route_aware_training_rows": len(route_train_data),
                "hist_gradient_boosting_parameters": TUNED_RESIDUAL_PARAMETERS,
                "lightgbm_parameters": LIGHTGBM_RESIDUAL_PARAMETERS,
                "folds": fold_results,
            },
            indent=2,
        ),
        encoding="utf-8",
    )

    _print_results(fold_results)
    print(f"\nWrote LightGBM benchmark metrics to {RESULT_PATH}")


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


def _evaluate_folds(route_train_data: pd.DataFrame) -> dict[str, object]:
    results: dict[str, object] = {}
    for fold in two_fold_expanding_backtest(route_train_data):
        predictions = {
            "tuned_hist_gradient_boosting": osrm_residual_gradient_boosting_predictions(
                fold.train_data,
                fold.validation_data,
                model_parameters=TUNED_RESIDUAL_PARAMETERS,
            ),
            "lightgbm": lightgbm_osrm_residual_predictions(
                fold.train_data,
                fold.validation_data,
            ),
        }
        results[fold.name] = {
            "training_rows": len(fold.train_data),
            "validation_rows": len(fold.validation_data),
            "metrics": {
                name: regression_metrics(fold.validation_data[TARGET_COLUMN], prediction)
                for name, prediction in predictions.items()
            },
            "lightgbm_minus_hist_gradient_boosting_interval": paired_mae_difference_interval(
                fold.validation_data[TARGET_COLUMN],
                predictions["lightgbm"],
                predictions["tuned_hist_gradient_boosting"],
            ),
        }

    return results


def _print_results(fold_results: dict[str, object]) -> None:
    for name, result in fold_results.items():
        typed_result = dict(result)
        metrics = pd.DataFrame.from_dict(typed_result["metrics"], orient="index").round(3)
        interval = typed_result["lightgbm_minus_hist_gradient_boosting_interval"]
        print(f"\n{name.capitalize()} chronological fold (minutes)")
        print(metrics.to_string())
        print(
            "LightGBM minus tuned HistGradientBoosting MAE: "
            f"{interval['candidate_minus_reference_mae_minutes']:.3f} "
            f"(95% bootstrap CI {interval['confidence_interval_lower_minutes']:.3f} "
            f"to {interval['confidence_interval_upper_minutes']:.3f})"
        )


if __name__ == "__main__":
    main()
