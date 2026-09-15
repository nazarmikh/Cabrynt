"""Build clean chronological Parquet datasets from the Porto taxi archive."""

from __future__ import annotations

import json
import shutil
import zipfile
from collections import Counter
from pathlib import Path

import pandas as pd
import pyarrow as pa
import pyarrow.parquet as pq

from cabrynt_trip_duration.dataset import (
    CLEAN_COLUMNS,
    chronological_splits,
    prepare_chunk,
)

PROJECT_ROOT = Path(__file__).resolve().parents[1]
TRAIN_PATH = PROJECT_ROOT / "data" / "uci" / "train.csv.zip"
OUTPUT_DIRECTORY = PROJECT_ROOT / "artifacts" / "prepared-data"
CHUNK_ROWS = 50_000


def main() -> None:
    if not TRAIN_PATH.exists():
        raise FileNotFoundError(f"Training data not found at {TRAIN_PATH}")

    _reset_output_directory()

    unsorted_path = OUTPUT_DIRECTORY / "clean-unsorted.parquet"
    seen_trip_ids: set[str] = set()
    rejected_by_reason: Counter[str] = Counter()
    total_rows = 0
    kept_rows = 0
    writer: pq.ParquetWriter | None = None

    print(f"Building clean data from {TRAIN_PATH}...")

    try:
        with zipfile.ZipFile(TRAIN_PATH) as archive:
            with archive.open(archive.namelist()[0]) as csv_file:
                chunks = pd.read_csv(
                    csv_file,
                    chunksize=CHUNK_ROWS,
                    usecols=["TRIP_ID", "TIMESTAMP", "POLYLINE"],
                    dtype={"TRIP_ID": str, "TIMESTAMP": "Int64", "POLYLINE": str},
                )

                for chunk in chunks:
                    total_rows += len(chunk)
                    rows = chunk[["TRIP_ID", "TIMESTAMP", "POLYLINE"]].itertuples(
                        index=False,
                        name=None,
                    )
                    prepared_rows, rejected = prepare_chunk(rows, seen_trip_ids)
                    rejected_by_reason.update(rejected)

                    if prepared_rows:
                        prepared_chunk = pd.DataFrame(prepared_rows, columns=CLEAN_COLUMNS)
                        table = pa.Table.from_pandas(prepared_chunk, preserve_index=False)
                        if writer is None:
                            writer = pq.ParquetWriter(unsorted_path, table.schema)
                        writer.write_table(table)
                        kept_rows += len(prepared_chunk)

                    print(f"  processed {total_rows:,} rows", flush=True)
    finally:
        if writer is not None:
            writer.close()

    if kept_rows == 0:
        raise RuntimeError("no rows passed preprocessing")

    clean_data = pd.read_parquet(unsorted_path)
    train_data, validation_data, test_data = chronological_splits(clean_data)

    clean_path = OUTPUT_DIRECTORY / "clean.parquet"
    train_data.to_parquet(OUTPUT_DIRECTORY / "train.parquet", index=False)
    validation_data.to_parquet(OUTPUT_DIRECTORY / "validation.parquet", index=False)
    test_data.to_parquet(OUTPUT_DIRECTORY / "test.parquet", index=False)
    clean_data.to_parquet(clean_path, index=False)
    unsorted_path.unlink()

    summary = {
        "input_rows": total_rows,
        "kept_rows": kept_rows,
        "rejected_by_reason": dict(sorted(rejected_by_reason.items())),
        "splits": {
            "train": len(train_data),
            "validation": len(validation_data),
            "test": len(test_data),
        },
        "timestamp_ranges": {
            "train": _timestamp_range(train_data),
            "validation": _timestamp_range(validation_data),
            "test": _timestamp_range(test_data),
        },
    }
    summary_path = OUTPUT_DIRECTORY / "summary.json"
    summary_path.write_text(json.dumps(summary, indent=2), encoding="utf-8")

    print("\nDataset build complete")
    print(f"  kept: {kept_rows:,} of {total_rows:,} rows")
    for reason, count in summary["rejected_by_reason"].items():
        print(f"  rejected {reason}: {count:,}")
    for name, size in summary["splits"].items():
        print(f"  {name}: {size:,} rows")
    print(f"\nWrote prepared datasets to {OUTPUT_DIRECTORY}")


def _reset_output_directory() -> None:
    if OUTPUT_DIRECTORY.exists():
        shutil.rmtree(OUTPUT_DIRECTORY)
    OUTPUT_DIRECTORY.mkdir(parents=True)


def _timestamp_range(data: pd.DataFrame) -> dict[str, int]:
    return {
        "first": int(data["timestamp"].min()),
        "last": int(data["timestamp"].max()),
    }


if __name__ == "__main__":
    main()
