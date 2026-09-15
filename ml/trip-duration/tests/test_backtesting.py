import numpy as np
import pandas as pd
import pytest

from cabrynt_trip_duration.backtesting import (
    paired_mae_difference_interval,
    two_fold_expanding_backtest,
)


def test_expanding_backtest_keeps_every_training_timestamp_before_validation() -> None:
    data = pd.DataFrame(
        {
            "timestamp": np.repeat(np.arange(10), 2),
            "trip_id": [f"trip-{index}" for index in range(20)],
        }
    )

    folds = two_fold_expanding_backtest(data)

    assert [fold.name for fold in folds] == ["early", "late"]
    assert [len(fold.train_data) for fold in folds] == [12, 16]
    assert [len(fold.validation_data) for fold in folds] == [4, 4]
    assert all(
        fold.train_data["timestamp"].max() < fold.validation_data["timestamp"].min()
        for fold in folds
    )


def test_paired_bootstrap_reports_a_stable_candidate_improvement() -> None:
    interval = paired_mae_difference_interval(
        pd.Series([10.0, 20.0, 30.0]),
        candidate_predictions=np.array([9.0, 19.0, 29.0]),
        reference_predictions=np.array([8.0, 18.0, 28.0]),
        resamples=100,
    )

    assert interval["candidate_minus_reference_mae_minutes"] == -1.0
    assert interval["confidence_interval_lower_minutes"] == -1.0
    assert interval["confidence_interval_upper_minutes"] == -1.0


def test_paired_bootstrap_rejects_mismatched_lengths() -> None:
    with pytest.raises(ValueError, match="matching lengths"):
        paired_mae_difference_interval(
            pd.Series([10.0, 20.0]),
            candidate_predictions=np.array([9.0]),
            reference_predictions=np.array([8.0, 18.0]),
        )
