"""Compare local OSRM route times with existing validation baselines."""

from __future__ import annotations

import argparse
import json
from pathlib import Path

import pandas as pd

from cabrynt_trip_duration.evaluation import (
    QUOTE_TIME_FEATURES,
    TARGET_COLUMN,
    evaluate_prediction_sets,
    fixed_speed_predictions,
    linear_regression_predictions,
    training_median_predictions,
)
from cabrynt_trip_duration.routing import (
    fetch_route_estimates,
    select_route_sample,
)

PROJECT_ROOT = Path(__file__).resolve().parents[1]
DATA_DIRECTORY = PROJECT_ROOT / "artifacts" / "prepared-data"
OSRM_DIRECTORY = PROJECT_ROOT / "artifacts" / "osrm"
ROUTE_CACHE_PATH = OSRM_DIRECTORY / "route-cache.sqlite3"
ROUTE_ESTIMATES_PATH = OSRM_DIRECTORY / "validation-route-estimates.parquet"
METRICS_PATH = OSRM_DIRECTORY / "validation-metrics.json"
DEFAULT_SAMPLE_SIZE = 5_000
DEFAULT_SAMPLE_SEED = 42


def parse_arguments() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Evaluate local OSRM against Cabrynt duration baselines."
    )
    parser.add_argument("--sample-size", type=int, default=DEFAULT_SAMPLE_SIZE)
    parser.add_argument("--sample-seed", type=int, default=DEFAULT_SAMPLE_SEED)
    parser.add_argument("--base-url", default="http://127.0.0.1:5000")
    return parser.parse_args()


def main() -> None:
    arguments = parse_arguments()
    train_path = DATA_DIRECTORY / "train.parquet"
    validation_path = DATA_DIRECTORY / "validation.parquet"
    missing_paths = [path for path in (train_path, validation_path) if not path.exists()]
    if missing_paths:
        raise FileNotFoundError(
            "Prepared data is missing. Run 'python scripts/build_dataset.py' first: "
            f"{missing_paths}"
        )

    train_data = pd.read_parquet(train_path)
    validation_columns = ["trip_id", TARGET_COLUMN, *QUOTE_TIME_FEATURES]
    validation_data = pd.read_parquet(validation_path, columns=validation_columns)
    sample = select_route_sample(
        validation_data,
        sample_size=arguments.sample_size,
        random_state=arguments.sample_seed,
    )

    OSRM_DIRECTORY.mkdir(parents=True, exist_ok=True)
    route_estimates, no_route_trip_ids = fetch_route_estimates(
        sample,
        cache_path=ROUTE_CACHE_PATH,
        base_url=arguments.base_url,
    )
    route_estimates.to_parquet(ROUTE_ESTIMATES_PATH, index=False)

    predictions = {
        "training_median": training_median_predictions(train_data, route_estimates),
        "fixed_30_kmh": fixed_speed_predictions(route_estimates),
        "linear_regression": linear_regression_predictions(train_data, route_estimates),
        "osrm": route_estimates["osrm_duration_minutes"].to_numpy(),
    }
    results = evaluate_prediction_sets(route_estimates[TARGET_COLUMN], predictions).round(3)
    METRICS_PATH.write_text(
        json.dumps(
            {
                "training_rows": len(train_data),
                "validation_rows": len(validation_data),
                "sample_rows": len(sample),
                "routable_rows": len(route_estimates),
                "osrm_no_route_rows": len(no_route_trip_ids),
                "osrm_no_route_trip_ids": no_route_trip_ids,
                "sample_seed": arguments.sample_seed,
                "osrm_base_url": arguments.base_url,
                "metrics": results.to_dict(orient="index"),
            },
            indent=2,
        ),
        encoding="utf-8",
    )

    print("Validation sample metrics (minutes)")
    print(results.to_string())
    print(
        f"\nOSRM routed {len(route_estimates)}/{len(sample)} sampled trips "
        f"({len(no_route_trip_ids)} had no route)."
    )
    print(f"\nWrote route estimates to {ROUTE_ESTIMATES_PATH}")
    print(f"Wrote metrics to {METRICS_PATH}")


if __name__ == "__main__":
    main()
