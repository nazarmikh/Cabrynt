"""Measure whether more route-ready training data improves the selected model."""

from __future__ import annotations

import argparse
import json
from pathlib import Path

import pandas as pd

from cabrynt_trip_duration.backtesting import two_fold_expanding_backtest
from cabrynt_trip_duration.evaluation import TARGET_COLUMN, regression_metrics
from cabrynt_trip_duration.learning_curve import nested_training_samples
from cabrynt_trip_duration.modeling import (
    FINAL_RESIDUAL_FEATURE_COLUMNS,
    FINAL_RESIDUAL_PARAMETERS,
    osrm_residual_gradient_boosting_predictions,
)
from cabrynt_trip_duration.route_features import attach_route_estimates

PROJECT_ROOT = Path(__file__).resolve().parents[1]
ENRICHED_TRAIN_PATH = PROJECT_ROOT / "artifacts" / "enriched-data" / "train.parquet"
TRAINING_ROUTE_ESTIMATES_PATH = PROJECT_ROOT / "artifacts" / "osrm" / "training-route-estimates.parquet"
RESULT_PATH = PROJECT_ROOT / "artifacts" / "osrm" / "learning-curve-metrics.json"
TRAINING_SIZES = (25_000, 50_000, 100_000)
RANDOM_STATE = 42


def parse_arguments() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Evaluate the final OSRM residual model at fixed training-data sizes."
    )
    parser.add_argument(
        "--run",
        action="store_true",
        help="Run the development-only chronological learning curve.",
    )
    arguments = parser.parse_args()
    if not arguments.run:
        parser.error("pass --run to start the learning-curve evaluation")
    return arguments


def main() -> None:
    parse_arguments()
    route_train_data = _load_route_train_data()
    fold_metrics = evaluate_learning_curve(route_train_data)
    summary = summarize_learning_curve(fold_metrics)

    RESULT_PATH.parent.mkdir(parents=True, exist_ok=True)
    RESULT_PATH.write_text(
        json.dumps(
            {
                "model": {
                    "feature_columns": FINAL_RESIDUAL_FEATURE_COLUMNS,
                    "parameters": FINAL_RESIDUAL_PARAMETERS,
                },
                "sample_sizes": list(TRAINING_SIZES),
                "random_state": RANDOM_STATE,
                "route_ready_training_rows": len(route_train_data),
                "fold_metrics": fold_metrics.to_dict(orient="records"),
                "summary": summary.to_dict(orient="records"),
            },
            indent=2,
        ),
        encoding="utf-8",
    )

    print("Chronological training-data learning curve (minutes)")
    print(summary.to_string(index=False))
    print(f"\nWrote learning-curve metrics to {RESULT_PATH}")


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


def evaluate_learning_curve(route_train_data: pd.DataFrame) -> pd.DataFrame:
    """Evaluate fixed nested training sizes without reading validation or test splits."""
    rows: list[dict[str, object]] = []
    for fold in two_fold_expanding_backtest(route_train_data):
        samples = nested_training_samples(
            fold.train_data,
            TRAINING_SIZES,
            random_state=RANDOM_STATE,
        )
        for training_rows, train_sample in samples.items():
            predictions = osrm_residual_gradient_boosting_predictions(
                train_sample,
                fold.validation_data,
                model_parameters=FINAL_RESIDUAL_PARAMETERS,
                feature_columns=FINAL_RESIDUAL_FEATURE_COLUMNS,
            )
            metrics = regression_metrics(fold.validation_data[TARGET_COLUMN], predictions)
            rows.append(
                {
                    "fold": fold.name,
                    "training_rows": training_rows,
                    "available_training_rows": len(fold.train_data),
                    "validation_rows": len(fold.validation_data),
                    "mae_minutes": metrics["mae_minutes"],
                    "p90_absolute_error_minutes": metrics["p90_absolute_error_minutes"],
                }
            )

    return pd.DataFrame(rows)


def summarize_learning_curve(fold_metrics: pd.DataFrame) -> pd.DataFrame:
    """Summarize average and worst chronological-fold accuracy per sample size."""
    required_columns = {
        "fold",
        "training_rows",
        "mae_minutes",
        "p90_absolute_error_minutes",
    }
    missing_columns = required_columns - set(fold_metrics)
    if missing_columns:
        raise ValueError(f"learning-curve metrics are missing columns: {sorted(missing_columns)}")

    return (
        fold_metrics.groupby("training_rows", as_index=False)
        .agg(
            mean_mae_minutes=("mae_minutes", "mean"),
            worst_fold_mae_minutes=("mae_minutes", "max"),
            mean_p90_absolute_error_minutes=("p90_absolute_error_minutes", "mean"),
        )
        .sort_values("training_rows", kind="stable")
        .reset_index(drop=True)
    )


if __name__ == "__main__":
    main()
