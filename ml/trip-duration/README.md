# Cabrynt Trip Duration

This workspace contains Cabrynt's trip-duration experiment. It estimates the time from pickup to destination for trips in Porto.

## Prediction Contract

Information available when a quote is requested:

- pickup and destination coordinates;
- request time;
- features calculated from those values.

The output is trip duration in minutes. The intermediate GPS points describe what happened during the trip, so they are used only to inspect data quality and are not model features.

## Setup

From this directory:

```powershell
python -m venv .venv
.\.venv\Scripts\Activate.ps1
python -m pip install -r requirements.txt
```

Download the [Porto taxi trajectory dataset](https://archive.ics.uci.edu/dataset/339/taxi+service+trajectory+prediction+challenge+ecml+pkdd+2015) and place the training archive at:

```text
data/uci/train.csv.zip
```

Open `notebooks/01_dataset_inspection.ipynb` with the virtual environment as its Jupyter kernel. It uses a 10,000-row sample so exploration runs quickly.

To audit the complete archive without loading it into memory at once:

```powershell
python scripts/audit_quality.py
```

## Current Experiment

The first implementation:

- inspects the dataset and trajectory format;
- derives duration from the 15-second GPS sampling interval;
- excludes malformed or empty traces, traces with fewer than two points, coordinates outside the Porto area, trips above four hours, and GPS jumps above 150 km/h;
- reports `MISSING_DATA`, repeated coordinates and near-zero endpoint distance as diagnostics rather than automatically deleting them;
- creates a chronological train, validation and test split;
- compares simple baselines with a gradient-boosting model;
- examines where validation errors are largest.

The full-data audit reports duplicate trip IDs. They will be removed when the next pipeline step creates a reproducible clean dataset. The test split and official challenge holdout are reserved for final evaluation.

## Structure

- `notebooks/` contains exploration and early experiments.
- `scripts/` contains full-data audit tools.
- `src/` contains reusable preprocessing functions.
- `tests/` verifies preprocessing behaviour.

Raw data, virtual environments, caches and generated model files are not committed.
