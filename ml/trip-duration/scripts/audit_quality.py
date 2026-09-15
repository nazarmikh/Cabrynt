"""Report data-quality findings for the full Porto taxi training archive."""

from __future__ import annotations

import zipfile
from collections import Counter
from pathlib import Path

import pandas as pd

from cabrynt_trip_duration.preprocessing import (
    MAX_DURATION_MINUTES,
    MAX_SEGMENT_SPEED_KMH,
    haversine_km,
    parse_polyline,
    preprocessing_reason,
)

PROJECT_ROOT = Path(__file__).resolve().parents[1]
TRAIN_PATH = PROJECT_ROOT / "data" / "uci" / "train.csv.zip"
CHUNK_ROWS = 50_000


def has_repeated_coordinates(points: list[tuple[float, float]], run_length: int = 3) -> bool:
    repeated = 1
    for point, previous_point in zip(points[1:], points, strict=False):
        repeated = repeated + 1 if point == previous_point else 1
        if repeated >= run_length:
            return True
    return False


if not TRAIN_PATH.exists():
    raise FileNotFoundError(f"Training data not found at {TRAIN_PATH}")

rejected_by_reason: Counter[str] = Counter()
call_types: Counter[str] = Counter()
day_types: Counter[str] = Counter()
all_trip_ids: set[str] = set()

total_rows = 0
kept_rows = 0
duplicate_trip_ids = 0
missing_data_rows = 0
repeated_coordinate_rows = 0
near_zero_endpoint_rows = 0

print(f"Scanning {TRAIN_PATH} in {CHUNK_ROWS:,}-row chunks...")

with zipfile.ZipFile(TRAIN_PATH) as archive:
    with archive.open(archive.namelist()[0]) as csv_file:
        chunks = pd.read_csv(
            csv_file,
            chunksize=CHUNK_ROWS,
            dtype={"TRIP_ID": str, "CALL_TYPE": str, "DAY_TYPE": str, "POLYLINE": str},
        )

        for chunk in chunks:
            total_rows += len(chunk)

            for row in chunk.itertuples(index=False):
                call_types[row.CALL_TYPE] += 1
                day_types[row.DAY_TYPE] += 1

                if row.TRIP_ID in all_trip_ids:
                    duplicate_trip_ids += 1
                else:
                    all_trip_ids.add(row.TRIP_ID)

                points = parse_polyline(row.POLYLINE)
                reason = preprocessing_reason(points)
                if reason is not None:
                    rejected_by_reason[reason] += 1
                    continue

                kept_rows += 1
                if row.MISSING_DATA:
                    missing_data_rows += 1
                if has_repeated_coordinates(points):
                    repeated_coordinate_rows += 1
                if haversine_km(*points[0], *points[-1]) < 0.05:
                    near_zero_endpoint_rows += 1

print("\nQUALITY AUDIT")
print(f"Total rows: {total_rows:,}")
print(f"Rows passing trajectory rules: {kept_rows:,}")
print(f"Duplicate TRIP_ID values: {duplicate_trip_ids:,}")

print("\nExcluded by first failed rule:")
for reason, count in rejected_by_reason.most_common():
    print(f"  {reason}: {count:,} ({count / total_rows:.2%})")

print("\nDiagnostic signals retained for now:")
print(f"  MISSING_DATA after required checks: {missing_data_rows:,}")
print(f"  3+ repeated coordinates: {repeated_coordinate_rows:,}")
print(f"  pickup and destination within 50 m: {near_zero_endpoint_rows:,}")

print("\nSelected thresholds:")
print(f"  max segment speed: {MAX_SEGMENT_SPEED_KMH:g} km/h")
print(f"  max duration: {MAX_DURATION_MINUTES / 60:g} hours")

print("\nCALL_TYPE distribution:")
for value, count in sorted(call_types.items()):
    print(f"  {value}: {count:,} ({count / total_rows:.1%})")
print("\nDAY_TYPE distribution:")
for value, count in sorted(day_types.items()):
    print(f"  {value}: {count:,} ({count / total_rows:.1%})")
