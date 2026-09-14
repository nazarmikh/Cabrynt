"""Create a deterministic, route-aware training cohort from local OSRM estimates."""

from __future__ import annotations

import argparse
import json
from pathlib import Path

import pandas as pd

from cabrynt_trip_duration.routing import (
    ROUTE_INPUT_COLUMNS,
    fetch_route_estimates,
    select_route_sample,
)

PROJECT_ROOT = Path(__file__).resolve().parents[1]
DATA_DIRECTORY = PROJECT_ROOT / "artifacts" / "prepared-data"
OSRM_DIRECTORY = PROJECT_ROOT / "artifacts" / "osrm"
ROUTE_CACHE_PATH = OSRM_DIRECTORY / "route-cache.sqlite3"
ROUTE_ESTIMATES_PATH = OSRM_DIRECTORY / "training-route-estimates.parquet"
METADATA_PATH = OSRM_DIRECTORY / "training-route-metadata.json"
DEFAULT_SAMPLE_SIZE = 50_000
DEFAULT_SAMPLE_SEED = 43


def parse_arguments() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Build a deterministic OSRM-enriched Cabrynt training cohort."
    )
    parser.add_argument("--sample-size", type=int, default=DEFAULT_SAMPLE_SIZE)
    parser.add_argument("--sample-seed", type=int, default=DEFAULT_SAMPLE_SEED)
    parser.add_argument("--base-url", default="http://127.0.0.1:5000")
    parser.add_argument(
        "--force",
        action="store_true",
        help="Replace existing cohort files while reusing the local route cache.",
    )
    return parser.parse_args()


def main() -> None:
    arguments = parse_arguments()
    _reject_existing_output(arguments.force)
    train_data = _load_training_data()
    sample = select_route_sample(
        train_data,
        sample_size=arguments.sample_size,
        random_state=arguments.sample_seed,
    )

    OSRM_DIRECTORY.mkdir(parents=True, exist_ok=True)
    route_estimates, no_route_trip_ids = fetch_route_estimates(
        sample,
        cache_path=ROUTE_CACHE_PATH,
        base_url=arguments.base_url,
    )
    route_estimates = route_estimates[
        ["trip_id", "osrm_distance_km", "osrm_duration_minutes"]
    ]
    route_estimates.to_parquet(ROUTE_ESTIMATES_PATH, index=False)

    METADATA_PATH.write_text(
        json.dumps(
            {
                "training_rows": len(train_data),
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
        f"OSRM routed {len(route_estimates)}/{len(sample)} sampled training trips "
        f"({len(no_route_trip_ids)} had no route)."
    )
    print(f"Wrote route estimates to {ROUTE_ESTIMATES_PATH}")
    print(f"Wrote metadata to {METADATA_PATH}")


def _load_training_data() -> pd.DataFrame:
    train_path = DATA_DIRECTORY / "train.parquet"
    if not train_path.exists():
        raise FileNotFoundError(
            "Prepared training data is missing. Run 'python scripts/build_dataset.py' first."
        )

    return pd.read_parquet(train_path, columns=ROUTE_INPUT_COLUMNS)


def _reject_existing_output(force: bool) -> None:
    existing_paths = [path for path in (ROUTE_ESTIMATES_PATH, METADATA_PATH) if path.exists()]
    if existing_paths and not force:
        raise FileExistsError(
            "OSRM training cohort already exists. Use --force to rebuild it with the "
            f"same route cache: {existing_paths}"
        )


if __name__ == "__main__":
    main()
