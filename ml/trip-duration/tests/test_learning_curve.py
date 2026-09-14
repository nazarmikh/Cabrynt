import pandas as pd
import pytest

from cabrynt_trip_duration.learning_curve import nested_training_samples
from scripts.evaluate_learning_curve import TRAINING_SIZES, summarize_learning_curve


def test_nested_training_samples_are_reproducible_and_nested() -> None:
    data = pd.DataFrame(
        {
            "trip_id": [f"trip-{index}" for index in range(12)],
            "timestamp": list(range(12)),
        }
    )

    samples = nested_training_samples(data, (3, 6, 9))
    repeated_samples = nested_training_samples(data, (3, 6, 9))

    assert set(samples[3]["trip_id"]).issubset(set(samples[6]["trip_id"]))
    assert set(samples[6]["trip_id"]).issubset(set(samples[9]["trip_id"]))
    assert samples[6]["trip_id"].tolist() == repeated_samples[6]["trip_id"].tolist()
    assert samples[9]["timestamp"].is_monotonic_increasing


def test_nested_training_samples_reject_invalid_sizes() -> None:
    data = pd.DataFrame({"trip_id": ["a", "b"], "timestamp": [1, 2]})

    with pytest.raises(ValueError, match="ascending"):
        nested_training_samples(data, (2, 1))


def test_learning_curve_uses_sizes_available_to_both_chronological_folds() -> None:
    assert TRAINING_SIZES == (25_000, 50_000, 100_000)


def test_learning_curve_summary_groups_metrics_by_training_size() -> None:
    metrics = pd.DataFrame(
        {
            "fold": ["early", "late", "early", "late"],
            "training_rows": [25_000, 25_000, 50_000, 50_000],
            "mae_minutes": [4.0, 3.0, 3.8, 2.8],
            "p90_absolute_error_minutes": [8.0, 7.0, 7.5, 6.5],
        }
    )

    summary = summarize_learning_curve(metrics)

    assert summary["training_rows"].tolist() == [25_000, 50_000]
    assert summary.loc[0, "mean_mae_minutes"] == 3.5
    assert summary.loc[1, "worst_fold_mae_minutes"] == 3.8
