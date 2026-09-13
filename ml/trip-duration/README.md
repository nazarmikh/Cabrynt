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

To build the local model-ready datasets:

```powershell
python scripts/build_dataset.py
```

This creates ignored Parquet files in `artifacts/prepared-data/`: a clean full dataset and chronological `train`, `validation`, and `test` splits.

To evaluate the first validation baselines:

```powershell
python scripts/evaluate_baselines.py
```

This reads only `train.parquet` and `validation.parquet`. It writes ignored metrics to `artifacts/baseline-metrics.json` and leaves `test.parquet` untouched.

## Data Enrichment

The model-ready data is enriched only with information that would be available when a quote is requested: Porto-local calendar fields, Portuguese public holidays, and hourly historical weather. Download the local weather cache, then build enriched train and validation data:

```powershell
python scripts/download_weather.py
python scripts/build_enriched_datasets.py
```

Weather is retrieved from the [Open-Meteo Historical Weather API](https://open-meteo.com/en/docs/historical-weather-api) and cached locally. The enriched build also adds smoothed historical congestion profiles fitted only on earlier training trips. It does not read or transform the reserved test split.

## Local OSRM Baseline

OSRM is run locally so the project does not send thousands of routing requests to a public demo service. It estimates a driving route using the Portugal OpenStreetMap road network.

With Docker Desktop running, prepare the local routing graph. The download is about 400 MB and preprocessing can take several minutes:

```powershell
.\scripts\setup_osrm.ps1
docker compose -f compose.osrm.yaml up -d
```

Then compare OSRM against every existing baseline on the same deterministic 5,000-row validation sample:

```powershell
python scripts/evaluate_osrm_baseline.py
```

The command reads `train.parquet` and `validation.parquet`, never `test.parquet`. It caches local route results in `artifacts/osrm/route-cache.sqlite3`, so a rerun does not request routes that were already evaluated. OSRM `NoRoute` cases are recorded, and all metrics use the same routable subset for every baseline. Use `--sample-size` and `--sample-seed` only when deliberately creating a new benchmark sample.

To rebuild the routing graph from a newer map extract, run `setup_osrm.ps1 -ForceDownload`. This replaces both the downloaded extract and its derived graph.

## Current Experiment

The first implementation:

- inspects the dataset and trajectory format;
- derives duration from the 15-second GPS sampling interval;
- excludes malformed or empty traces, traces with fewer than two points, coordinates outside the Porto area, trips above four hours, and GPS jumps above 150 km/h;
- reports `MISSING_DATA`, repeated coordinates and near-zero endpoint distance as diagnostics rather than automatically deleting them;
- builds clean full-data Parquet files and chronological train, validation and test splits;
- uses Porto-local time for calendar features, with hourly weather and public-holiday enrichment;
- builds leakage-safe historical congestion profiles for train and validation data;
- evaluates median, fixed-speed, and linear-regression baselines on validation data;
- adds a local OSRM road-routing benchmark on a fixed validation sample;
- examines where validation errors are largest.

The builder removes duplicate trip IDs while keeping the first occurrence. The test split and official challenge holdout are reserved for final evaluation.

## Latest Local Build

The latest build processed 1,710,670 input rows and retained 1,498,634 rows. It produced 1,049,044 training rows, 224,795 validation rows, and 224,795 test rows. Generated data is local and is not committed.

## Latest Enrichment Build

The local weather cache contains 8,760 hourly Porto observations from 2013-07-01 through 2014-06-30. Enriched train and validation datasets contain all weather and calendar fields. The first chronological 209,247 training rows have no historical-congestion profile because no earlier trips are available; this is explicitly marked rather than filled with future target data.

## Latest Validation Baselines

The first validation run uses 1,049,044 training rows and 224,795 validation rows. The reserved test split was not read.

| Baseline | MAE (minutes) | RMSE (minutes) | P90 absolute error (minutes) |
| --- | ---: | ---: | ---: |
| Training median | 5.138 | 9.138 | 9.750 |
| Fixed 30 km/h | 5.930 | 10.064 | 11.423 |
| Linear regression | 4.348 | 8.255 | 7.405 |

These full-validation values are reference points for later experiments, not final test results. The OSRM benchmark below uses a separate fixed validation cohort, so its values should only be compared within that table.

## OSRM Validation Benchmark

The local Portugal OSRM graph was evaluated on a deterministic 5,000-row validation sample. It routed 4,999 rows; one row had no route, so every baseline below uses the same 4,999-row routable cohort.

| Baseline | MAE (minutes) | RMSE (minutes) | P90 absolute error (minutes) |
| --- | ---: | ---: | ---: |
| Training median | 5.143 | 8.776 | 10.000 |
| Fixed 30 km/h | 5.937 | 9.611 | 11.765 |
| Linear regression | 4.342 | 7.754 | 7.558 |
| OSRM driving route | 5.438 | 9.398 | 11.330 |

Direct OSRM is weaker than the simple linear model on this historical dataset. Its default car profile has no access to the actual taxi route or historical traffic, but its road-network distance and duration remain useful candidates for a later ML correction model.

## Structure

- `notebooks/` contains exploration and early experiments.
- `scripts/` contains full-data preparation and evaluation tools.
- `src/` contains reusable data, evaluation, and routing code.
- `tests/` verifies data preparation, evaluation, and routing behaviour.

Raw data, virtual environments, caches and generated model files are not committed.
