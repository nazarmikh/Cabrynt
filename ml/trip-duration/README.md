# Cabrynt Trip Duration Model

Machine-learning workspace for estimating ride duration during the Cabrynt quote flow.

## Current Scope

- Validate the Porto taxi trajectory dataset.
- Establish Cabrynt's fixed-speed calculation as the baseline.
- Train and evaluate a regression model without data leakage.
- Export a validated model for later ONNX integration with the backend.

## Structure

- `notebooks/` contains exploratory analysis.
- `src/` contains reproducible preparation, training, and evaluation code.
- `tests/` contains automated tests for the ML pipeline.

Raw and processed datasets, local environments, caches, and generated model artifacts are excluded from Git.
