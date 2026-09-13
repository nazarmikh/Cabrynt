"""
Duration tail investigation for the UCI Porto Taxi dataset.

Collects every trip with computed duration above 60 minutes and
computes detailed per-trip statistics. Groups them into buckets
so we can decide where the real long trips end and corrupt
recordings begin.
"""

import json
import math
import sys
import zipfile
from pathlib import Path

import numpy as np
import pandas as pd

sys.stdout.reconfigure(encoding="utf-8")

PROJECT_ROOT = Path(__file__).resolve().parents[1]
ZIP_PATH = PROJECT_ROOT / "data" / "uci" / "train.csv.zip"
CHUNK = 50_000
SECONDS_PER_POINT = 15
MIN_DURATION_MIN = 60   # collect everything above this


# ── helpers ──────────────────────────────────────────────────────────────────

def haversine_km(lon1, lat1, lon2, lat2):
    R = 6371.0
    phi1, phi2 = math.radians(lat1), math.radians(lat2)
    dphi = math.radians(lat2 - lat1)
    dlam = math.radians(lon2 - lon1)
    a = math.sin(dphi / 2) ** 2 + math.cos(phi1) * math.cos(phi2) * math.sin(dlam / 2) ** 2
    return 2 * R * math.asin(math.sqrt(a))


def trip_stats(pts, duration_s):
    """
    Compute movement stats for one trip's GPS trace.
    Returns a dict or None if the trace is too short.
    """
    if len(pts) < 2:
        return None

    total_dist_km = 0.0
    max_seg_speed = 0.0
    max_stuck_run = 1
    current_stuck = 1

    for i in range(1, len(pts)):
        try:
            d = haversine_km(pts[i-1][0], pts[i-1][1], pts[i][0], pts[i][1])
        except Exception:
            continue
        total_dist_km += d
        seg_speed = d / (SECONDS_PER_POINT / 3600)
        if seg_speed > max_seg_speed:
            max_seg_speed = seg_speed

        # stuck-run tracking (for diagnostic only — not a filter)
        if pts[i] == pts[i - 1]:
            current_stuck += 1
            max_stuck_run = max(max_stuck_run, current_stuck)
        else:
            current_stuck = 1

    duration_h = duration_s / 3600
    avg_speed = total_dist_km / duration_h if duration_h > 0 else 0.0

    straight_km = haversine_km(pts[0][0], pts[0][1], pts[-1][0], pts[-1][1])

    return {
        "total_gps_km": round(total_dist_km, 2),
        "straight_km": round(straight_km, 2),
        "avg_speed_kmh": round(avg_speed, 1),
        "max_seg_speed_kmh": round(max_seg_speed, 1),
        "max_stuck_run": max_stuck_run,   # longest run of identical coords
        "start_lon": pts[0][0],
        "start_lat": pts[0][1],
        "end_lon": pts[-1][0],
        "end_lat": pts[-1][1],
    }


# ── collect long trips ────────────────────────────────────────────────────────

print(f"Scanning for trips with duration > {MIN_DURATION_MIN} min ...\n")

records = []
total_scanned = 0

with zipfile.ZipFile(ZIP_PATH) as zf:
    csv_name = zf.namelist()[0]
    with zf.open(csv_name) as f:
        reader = pd.read_csv(
            f,
            chunksize=CHUNK,
            dtype={"TRIP_ID": str, "CALL_TYPE": str, "POLYLINE": str},
            usecols=["TRIP_ID", "CALL_TYPE", "TIMESTAMP", "POLYLINE"],
        )

        for chunk in reader:
            total_scanned += len(chunk)

            for _, row in chunk.iterrows():
                raw = row.get("POLYLINE", "")
                if not isinstance(raw, str):
                    continue
                try:
                    pts = json.loads(raw)
                except Exception:
                    continue
                if not isinstance(pts, list) or len(pts) < 2:
                    continue

                duration_s = (len(pts) - 1) * SECONDS_PER_POINT
                duration_min = duration_s / 60

                if duration_min < MIN_DURATION_MIN:
                    continue

                stats = trip_stats(pts, duration_s)
                if stats is None:
                    continue

                records.append({
                    "trip_id": row["TRIP_ID"],
                    "call_type": row.get("CALL_TYPE", "?"),
                    "timestamp": row.get("TIMESTAMP"),
                    "duration_min": round(duration_min, 1),
                    "num_points": len(pts),
                    **stats,
                })

print(f"Scanned {total_scanned:,} rows total.")
print(f"Found {len(records):,} trips with duration > {MIN_DURATION_MIN} min.\n")

df = pd.DataFrame(records)
df = df.sort_values("duration_min")


# ── bucket analysis ───────────────────────────────────────────────────────────

buckets = [
    (60,   90,  "60–90 min  (unusual but plausible)"),
    (90,  120,  "90–120 min (very unusual)"),
    (120, 180,  "120–180 min (suspicious)"),
    (180, 360,  "180–360 min (very suspicious)"),
    (360, 9999, "> 360 min  (almost certainly corrupt)"),
]

print("=" * 70)
print("DURATION BUCKET ANALYSIS")
print("=" * 70)

for lo, hi, label in buckets:
    sub = df[(df.duration_min >= lo) & (df.duration_min < hi)]
    if sub.empty:
        print(f"\n{label}: 0 trips\n")
        continue

    print(f"\n{label}: {len(sub):,} trips")
    print(f"  avg GPS distance:   {sub.total_gps_km.mean():.1f} km")
    print(f"  avg straight dist:  {sub.straight_km.mean():.1f} km")
    print(f"  avg speed (GPS):    {sub.avg_speed_kmh.mean():.1f} km/h")
    print(f"  max seg speed p50:  {sub.max_seg_speed_kmh.median():.1f} km/h")
    print(f"  trips with avg speed < 5 km/h:  {(sub.avg_speed_kmh < 5).sum():,}  ({(sub.avg_speed_kmh < 5).mean()*100:.1f}%)")
    print(f"  trips with avg speed < 1 km/h:  {(sub.avg_speed_kmh < 1).sum():,}  ({(sub.avg_speed_kmh < 1).mean()*100:.1f}%)")
    print(f"  trips with max stuck run >= 10: {(sub.max_stuck_run >= 10).sum():,}  ({(sub.max_stuck_run >= 10).mean()*100:.1f}%)")
    print(f"  trips with straight dist < 0.1 km: {(sub.straight_km < 0.1).sum():,}")

    # show 5 representative examples
    sample = sub.sample(min(5, len(sub)), random_state=42).sort_values("duration_min")
    print(f"\n  Sample rows (trip_id | dur_min | gps_km | straight_km | avg_spd | max_stuck_run):")
    for _, r in sample.iterrows():
        print(f"    {r.trip_id} | {r.duration_min:>6.1f} min | {r.total_gps_km:>6.1f} km gps"
              f" | {r.straight_km:>5.1f} km straight | {r.avg_speed_kmh:>5.1f} km/h avg"
              f" | stuck_run={r.max_stuck_run}")

print("\n" + "=" * 70)
print("OVERALL STATS FOR ALL LONG TRIPS")
print("=" * 70)
print(f"\nTotal long trips (> {MIN_DURATION_MIN} min): {len(df):,}")
print(f"  with avg speed < 5 km/h (likely not moving): {(df.avg_speed_kmh < 5).sum():,}  ({(df.avg_speed_kmh < 5).mean()*100:.1f}%)")
print(f"  with avg speed < 1 km/h (essentially stationary): {(df.avg_speed_kmh < 1).sum():,}  ({(df.avg_speed_kmh < 1).mean()*100:.1f}%)")
print(f"  with straight dist < 0.1 km (went nowhere): {(df.straight_km < 0.1).sum():,}  ({(df.straight_km < 0.1).mean()*100:.1f}%)")
print(f"  with max stuck run >= 20 (GPS froze for 5+ min): {(df.max_stuck_run >= 20).sum():,}  ({(df.max_stuck_run >= 20).mean()*100:.1f}%)")

print("\nDuration percentiles among long trips:")
for p in [0, 10, 25, 50, 75, 90, 95, 100]:
    print(f"  p{p:3d}: {np.percentile(df.duration_min, p):.1f} min")

print("\nDone.")
