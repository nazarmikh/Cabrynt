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

## Feature Audit

Before training a new model, inspect the enriched candidate features with:

```powershell
python scripts/audit_features.py
```

The command reads only the enriched training and validation files and writes an ignored JSON report to `artifacts/feature-audit/summary.json`. It checks missing values, constant fields, large train-to-validation mean shifts, and highly correlated feature pairs. It does not fit a model or read the reserved test split.

The latest audit found no constant fields, no missing validation features, and no feature pairs with an absolute correlation of 0.80 or higher. Historical profile fields are missing for the training cold-start rows by design. The chronological split shifts the month distribution in raw and cyclical forms because training ends before validation's April-to-June period. The cyclical encodings still represent December-to-January continuity correctly. No feature is removed automatically: future ablation experiments will decide which fields improve validation performance.

## Selected Feature Contract

The current candidate model predicts trip duration from information available at quote time:

| Group | Inputs |
| --- | --- |
| Route endpoints | Pickup and destination longitude/latitude, plus straight-line distance |
| Calendar | Porto-local hour, weekday, month, weekend flag, Portuguese public-holiday flag, and cyclic hour/weekday/month encodings |
| Weather | Temperature, precipitation, cloud cover, wind speed, and a precipitation flag |

It does not use completed-trip GPS points, observed travel distance, realised route shape, or actual duration. Those values are known only after a trip and would leak the answer into training.

The experiment also evaluated historical travel-time profile features based only on earlier trips. They did not improve validation MAE, so they are not part of the selected candidate. Historical weather is valid for this offline evaluation; a deployed quote endpoint will need current observations or a forecast provider that supplies the same weather contract.

## First Enriched Model Experiment

Run the first fixed model comparison with:

```powershell
python scripts/evaluate_enriched_models.py
```

The script reads only enriched training and validation data. It writes ignored metrics and duration-segment results to `artifacts/model-metrics/enriched-models.json`; it does not train on or read the reserved test split.

The comparison includes the existing baselines, an enriched linear regression, and three fixed `HistGradientBoostingRegressor` configurations. The calendar/weather model intentionally excludes target-derived historical profiles. The full model uses native missing-value handling for cold-start profile fields, while the warm-start model trains only on rows where profiles were available.

| Model | Validation MAE (minutes) |
| --- | ---: |
| Linear regression baseline | 4.348 |
| Enriched linear regression | 4.292 |
| Calendar/weather gradient boosting | 3.686 |
| Full enriched gradient boosting | 3.793 |
| Warm-start gradient boosting | 3.812 |

Calendar/weather gradient boosting is the best initial model. The historical-profile variants do not improve it, so they are not selected for the next experiment. These are validation results, not final test results, and are not directly comparable with the separate 5,000-row OSRM benchmark.

## OSRM Model Comparison

Compare the selected model against direct OSRM on the exact cached routable cohort:

```powershell
python scripts/compare_model_with_osrm.py
```

The script reads the enriched training and validation data plus the cached OSRM route estimates. It does not make routing requests, so Docker is not required once `validation-route-estimates.parquet` exists. To rebuild or extend that route cohort, start local OSRM and run `evaluate_osrm_baseline.py` first.

| Model | Cohort MAE (minutes) | Cohort P90 absolute error (minutes) |
| --- | ---: | ---: |
| Linear regression | 4.342 | 7.558 |
| Direct OSRM | 5.438 | 11.330 |
| Calendar/weather gradient boosting | **3.660** | **7.042** |

The selected model beats direct OSRM by 1.778 MAE minutes on the same 4,999 routable validation trips. This is not a claim that OSRM is useless: OSRM has lower MAE for the 610 trips lasting up to five minutes, while the model is better for every longer duration group. The reserved test split remains untouched.

## Route-Aware Training Cohort

The next experiment needs OSRM distance and duration during training, not only validation. With local OSRM running, build a deterministic 50,000-row sample from the chronological training split:

```powershell
docker compose -f compose.osrm.yaml up -d
python scripts/build_osrm_training_cohort.py
```

The script stores ignored route estimates and metadata in `artifacts/osrm/`. It reuses `route-cache.sqlite3`, so rerunning with `--force` does not request routes already cached. The cohort contains only `trip_id`, OSRM distance, and OSRM duration; the later model experiment will join those values to the enriched training features.

The 50,000-row sample is for an initial route-aware ablation, not a final claim that a model trained on that subset is better than the full-data candidate. The future comparison will train calendar/weather-only and OSRM-aware models on this same cohort, then evaluate both on the fixed OSRM validation cohort.

## Route-Aware Model Experiment

Evaluate direct and residual OSRM-aware models after the training cohort has been built:

```powershell
python scripts/evaluate_route_aware_models.py
```

The script trains every learned model on the same 49,998-route training cohort and evaluates them on the fixed 4,999-trip OSRM validation cohort. It makes no routing requests and writes ignored results to `artifacts/osrm/route-aware-model-metrics.json`.

| Model | Cohort MAE (minutes) | Cohort P90 absolute error (minutes) |
| --- | ---: | ---: |
| Direct OSRM | 5.438 | 11.330 |
| Calendar/weather gradient boosting | 3.777 | 7.356 |
| OSRM-aware gradient boosting | 3.714 | 7.159 |
| OSRM residual gradient boosting | **3.690** | **7.073** |

The residual model predicts a correction to OSRM duration, rather than duration from scratch. It improves the same-cohort calendar/weather model, but does not beat the existing full-data calendar/weather candidate (`3.660` MAE). Direct OSRM remains best for trips lasting up to five minutes. The full-data calendar/weather model therefore remains the current candidate.

## Route-Aware Chronological Backtest

Check whether the residual model's improvement is stable across earlier time periods:

```powershell
python scripts/backtest_route_aware_models.py
```

The script creates two expanding folds from the route-ready training cohort: an early 60%/20% train-validation split and a later 80%/20% split. It also reports paired bootstrap confidence intervals for residual-model MAE minus route-free-model MAE. No Docker, routing requests, project validation rows, or test rows are used.

| Fold | Calendar/weather MAE | OSRM residual MAE | Residual minus route-free MAE (95% CI) |
| --- | ---: | ---: | --- |
| Early | 4.290 | 4.202 | -0.088 (-0.118 to -0.058) |
| Late | 4.043 | 3.898 | -0.145 (-0.176 to -0.117) |

The residual improvement is stable in both chronological folds. That justifies expanding the OSRM training cohort before deciding whether a route-aware model can beat the full-data calendar/weather candidate.

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
- audits candidate features for missing values, distribution shifts, and redundancy;
- evaluates median, fixed-speed, and linear-regression baselines on validation data;
- compares fixed enriched linear and gradient-boosting models on validation data;
- prepares a deterministic local-OSRM training cohort for the next route-aware experiment;
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
- `src/` contains reusable data preparation, enrichment, audit, evaluation, and routing code.
- `tests/` verifies data preparation, evaluation, and routing behaviour.

Raw data, virtual environments, caches and generated model files are not committed.
