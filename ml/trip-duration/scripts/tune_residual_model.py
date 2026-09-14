"""Tune fixed OSRM residual-model configurations using chronological folds only."""

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
from cabrynt_trip_duration.modeling import (
    TUNED_RESIDUAL_PARAMETERS,
    osrm_residual_gradient_boosting_predictions,
)
from cabrynt_trip_duration.route_features import attach_route_estimates

PROJECT_ROOT = Path(__file__).resolve().parents[1]
ENRICHED_TRAIN_PATH = PROJECT_ROOT / "artifacts" / "enriched-data" / "train.parquet"
TRAINING_ROUTE_ESTIMATES_PATH = PROJECT_ROOT / "artifacts" / "osrm" / "training-route-estimates.parquet"
RESULT_PATH = PROJECT_ROOT / "artifacts" / "osrm" / "residual-tuning-metrics.json"

TUNING_CONFIGURATIONS: tuple[tuple[str, dict[str, object]], ...] = (
    ("baseline", {}),
    ("slower_learning", {"learning_rate": 0.05, "max_iter": 240}),
    ("faster_learning", {"learning_rate": 0.10, "max_iter": 120}),
    ("fewer_leaves", {"learning_rate": 0.06, "max_iter": 200, "max_leaf_nodes": 15}),
    ("more_leaves", {"learning_rate": 0.06, "max_iter": 200, "max_leaf_nodes": 63}),
    (
        "more_leaves_regularized",
        {
            "learning_rate": 0.06,
            "max_iter": 200,
            "max_leaf_nodes": 63,
            "min_samples_leaf": 100,
            "l2_regularization": 3.0,
        },
    ),
    (
        "more_leaves_smaller_leaf",
        {
            "learning_rate": 0.06,
            "max_iter": 200,
            "max_leaf_nodes": 63,
            "min_samples_leaf": 25,
        },
    ),
    ("low_regularization", {"learning_rate": 0.06, "max_iter": 200, "l2_regularization": 0.1}),
    ("high_regularization", {"learning_rate": 0.06, "max_iter": 200, "l2_regularization": 3.0}),
    ("absolute_error", {"loss": "absolute_error", "learning_rate": 0.06, "max_iter": 200}),
    (
        "absolute_error_more_leaves",
        TUNED_RESIDUAL_PARAMETERS,
    ),
    (
        "absolute_error_regularized",
        {
            "loss": "absolute_error",
            "learning_rate": 0.06,
            "max_iter": 200,
            "min_samples_leaf": 100,
            "l2_regularization": 3.0,
        },
    ),
)


def parse_arguments() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Tune fixed OSRM residual-model configurations chronologically."
    )
    parser.add_argument(
        "--run",
        action="store_true",
        help="Run the fixed 12-configuration development-only tuning experiment.",
    )
    arguments = parser.parse_args()
    if not arguments.run:
        parser.error("pass --run to start the tuning experiment")
    return arguments


def main() -> None:
    parse_arguments()
    route_train_data = _load_route_train_data()
    fold_metrics = _evaluate_configurations(route_train_data)
    summary = _summarize_metrics(fold_metrics)
    selection = _select_configuration(fold_metrics, summary)
    selection["fold_intervals_vs_baseline"] = _selected_fold_intervals(
        route_train_data,
        str(selection["configuration"]),
    )

    RESULT_PATH.write_text(
        json.dumps(
            {
                "route_aware_training_rows": len(route_train_data),
                "configurations": {
                    name: parameters for name, parameters in TUNING_CONFIGURATIONS
                },
                "fold_metrics": fold_metrics.to_dict(orient="records"),
                "summary": summary.to_dict(orient="records"),
                "selection": selection,
            },
            indent=2,
        ),
        encoding="utf-8",
    )

    print("Chronological residual-model tuning (minutes)")
    print(summary.to_string(index=False))
    print(
        "\nSelected configuration: "
        f"{selection['configuration']} ({selection['selection_reason']})"
    )
    for fold, interval in selection["fold_intervals_vs_baseline"].items():
        print(
            f"{fold.capitalize()} selected minus baseline MAE: "
            f"{interval['candidate_minus_reference_mae_minutes']:.3f} "
            f"(95% bootstrap CI {interval['confidence_interval_lower_minutes']:.3f} "
            f"to {interval['confidence_interval_upper_minutes']:.3f})"
        )
    print(f"\nWrote tuning results to {RESULT_PATH}")


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


def _evaluate_configurations(route_train_data: pd.DataFrame) -> pd.DataFrame:
    rows: list[dict[str, object]] = []
    for configuration_index, (name, parameters) in enumerate(TUNING_CONFIGURATIONS):
        for fold in two_fold_expanding_backtest(route_train_data):
            predictions = osrm_residual_gradient_boosting_predictions(
                fold.train_data,
                fold.validation_data,
                model_parameters=parameters,
            )
            metrics = regression_metrics(fold.validation_data[TARGET_COLUMN], predictions)
            rows.append(
                {
                    "configuration": name,
                    "configuration_index": configuration_index,
                    "fold": fold.name,
                    "mae_minutes": metrics["mae_minutes"],
                    "p90_absolute_error_minutes": metrics["p90_absolute_error_minutes"],
                }
            )

    return pd.DataFrame(rows)


def _summarize_metrics(fold_metrics: pd.DataFrame) -> pd.DataFrame:
    summary = (
        fold_metrics.groupby(["configuration", "configuration_index"], as_index=False)
        .agg(
            mean_mae_minutes=("mae_minutes", "mean"),
            worst_fold_mae_minutes=("mae_minutes", "max"),
            mean_p90_absolute_error_minutes=("p90_absolute_error_minutes", "mean"),
        )
        .sort_values(
            ["mean_mae_minutes", "worst_fold_mae_minutes", "configuration_index"],
            kind="stable",
        )
        .reset_index(drop=True)
    )
    return summary


def _select_configuration(
    fold_metrics: pd.DataFrame,
    summary: pd.DataFrame,
) -> dict[str, object]:
    baseline = fold_metrics.loc[fold_metrics["configuration"].eq("baseline")].set_index("fold")
    if set(baseline.index) != {"early", "late"}:
        raise ValueError("tuning results must include baseline results for both folds")

    stable_names = {"baseline"}
    for configuration, candidate in fold_metrics.groupby("configuration"):
        candidate_by_fold = candidate.set_index("fold")
        if set(candidate_by_fold.index) != set(baseline.index):
            raise ValueError(f"configuration {configuration} is missing a chronological fold")
        if candidate_by_fold.loc[baseline.index, "mae_minutes"].le(
            baseline["mae_minutes"]
        ).all():
            stable_names.add(configuration)

    selected = summary.loc[summary["configuration"].isin(stable_names)].iloc[0]
    return {
        "configuration": str(selected["configuration"]),
        "selection_reason": "lowest mean MAE among configurations that do not regress on either chronological fold",
        "mean_mae_minutes": float(selected["mean_mae_minutes"]),
        "worst_fold_mae_minutes": float(selected["worst_fold_mae_minutes"]),
    }


def _selected_fold_intervals(
    route_train_data: pd.DataFrame,
    configuration_name: str,
) -> dict[str, dict[str, float]]:
    parameters = dict(TUNING_CONFIGURATIONS)[configuration_name]
    intervals: dict[str, dict[str, float]] = {}
    for fold in two_fold_expanding_backtest(route_train_data):
        baseline_predictions = osrm_residual_gradient_boosting_predictions(
            fold.train_data,
            fold.validation_data,
        )
        selected_predictions = osrm_residual_gradient_boosting_predictions(
            fold.train_data,
            fold.validation_data,
            model_parameters=parameters,
        )
        intervals[fold.name] = paired_mae_difference_interval(
            fold.validation_data[TARGET_COLUMN],
            selected_predictions,
            baseline_predictions,
        )

    return intervals


if __name__ == "__main__":
    main()
