"""Lock a disjoint OSRM-routable confirmation cohort without reading targets."""

from __future__ import annotations

import argparse
import json
from pathlib import Path

import pandas as pd

from cabrynt_trip_duration.route_features import ROUTE_ESTIMATE_COLUMNS
from cabrynt_trip_duration.routing import (
    ROUTE_INPUT_COLUMNS,
    fetch_route_estimates,
    select_route_sample,
)

PROJECT_ROOT = Path(__file__).resolve().parents[1]
DATA_DIRECTORY = PROJECT_ROOT / "artifacts" / "prepared-data"
OSRM_DIRECTORY = PROJECT_ROOT / "artifacts" / "osrm"
ROUTE_CACHE_PATH = OSRM_DIRECTORY / "route-cache.sqlite3"
INITIAL_ROUTE_ESTIMATES_PATH = OSRM_DIRECTORY / "initial-test-route-estimates.parquet"
ROUTE_ESTIMATES_PATH = OSRM_DIRECTORY / "confirmation-route-estimates.parquet"
METADATA_PATH = OSRM_DIRECTORY / "confirmation-route-metadata.json"
DEFAULT_SAMPLE_SIZE = 5_000
DEFAULT_SAMPLE_SEED = 45


def parse_arguments() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Lock Cabrynt's final OSRM confirmation cohort without reading targets."
    )
    parser.add_argument("--sample-size", type=int, default=DEFAULT_SAMPLE_SIZE)
    parser.add_argument("--sample-seed", type=int, default=DEFAULT_SAMPLE_SEED)
    parser.add_argument("--base-url", default="http://127.0.0.1:5000")
    return parser.parse_args()


def main() -> None:
    arguments = parse_arguments()
    _reject_existing_output()
    initial_trip_ids = _load_initial_trip_ids()
    test_data = _load_test_route_inputs()
    unknown_trip_ids = initial_trip_ids - set(test_data["trip_id"].astype(str))
    if unknown_trip_ids:
        raise ValueError("initial test route estimates contain IDs outside the test split")
    sample = select_route_sample(
        test_data,
        sample_size=arguments.sample_size,
        random_state=arguments.sample_seed,
        excluded_trip_ids=initial_trip_ids,
    )

    OSRM_DIRECTORY.mkdir(parents=True, exist_ok=True)
    route_estimates, no_route_trip_ids = fetch_route_estimates(
        sample,
        cache_path=ROUTE_CACHE_PATH,
        base_url=arguments.base_url,
    )
    route_estimates[ROUTE_ESTIMATE_COLUMNS].to_parquet(ROUTE_ESTIMATES_PATH, index=False)
    METADATA_PATH.write_text(
        json.dumps(
            {
                "test_rows": len(test_data),
                "excluded_initial_cohort_rows": len(initial_trip_ids),
                "eligible_test_rows": len(test_data) - len(initial_trip_ids),
                "sample_rows": len(sample),
                "routable_rows": len(route_estimates),
                "osrm_no_route_rows": len(no_route_trip_ids),
                "osrm_no_route_trip_ids": no_route_trip_ids,
                "sample_seed": arguments.sample_seed,
                "osrm_base_url": arguments.base_url,
            },
            indent=2,
        ),
        encoding="utf-8",
    )

    print(
        f"OSRM routed {len(route_estimates)}/{len(sample)} confirmation trips "
        f"({len(no_route_trip_ids)} had no route)."
    )
    print(f"Wrote route estimates to {ROUTE_ESTIMATES_PATH}")
    print(f"Wrote metadata to {METADATA_PATH}")


def _load_initial_trip_ids() -> set[str]:
    if not INITIAL_ROUTE_ESTIMATES_PATH.exists():
        raise FileNotFoundError(
            "Initial test route estimates are missing. Build the initial test cohort first."
        )

    initial_data = pd.read_parquet(INITIAL_ROUTE_ESTIMATES_PATH, columns=["trip_id"])
    if initial_data["trip_id"].duplicated().any():
        raise ValueError("initial test route estimates contain duplicate trip IDs")
    return set(initial_data["trip_id"].astype(str))


def _load_test_route_inputs() -> pd.DataFrame:
    test_path = DATA_DIRECTORY / "test.parquet"
    if not test_path.exists():
        raise FileNotFoundError(
            "Prepared test data is missing. Run 'python scripts/build_dataset.py' first."
        )

    return pd.read_parquet(test_path, columns=ROUTE_INPUT_COLUMNS)


def _reject_existing_output() -> None:
    existing_paths = [path for path in (ROUTE_ESTIMATES_PATH, METADATA_PATH) if path.exists()]
    if existing_paths:
        raise FileExistsError(
            "The confirmation cohort already exists and must not be replaced: "
            f"{existing_paths}"
        )


if __name__ == "__main__":
    main()
