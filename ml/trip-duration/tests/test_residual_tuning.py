import pandas as pd

from scripts.tune_residual_model import (
    TUNING_CONFIGURATIONS,
    _select_configuration,
    _summarize_metrics,
)


def test_tuning_configurations_include_the_existing_baseline() -> None:
    assert TUNING_CONFIGURATIONS[0] == ("baseline", {})
    assert len(TUNING_CONFIGURATIONS) == 12


def test_tuning_selects_lowest_stable_configuration() -> None:
    fold_metrics = pd.DataFrame(
        {
            "configuration": [
                "baseline",
                "baseline",
                "stable_better",
                "stable_better",
                "unstable_better",
                "unstable_better",
            ],
            "configuration_index": [0, 0, 1, 1, 2, 2],
            "fold": ["early", "late", "early", "late", "early", "late"],
            "mae_minutes": [4.0, 3.0, 3.8, 2.9, 3.0, 3.2],
            "p90_absolute_error_minutes": [8.0, 7.0, 7.8, 6.9, 7.0, 7.2],
        }
    )

    selection = _select_configuration(fold_metrics, _summarize_metrics(fold_metrics))

    assert selection["configuration"] == "stable_better"


def test_tuning_keeps_baseline_when_other_configuration_regresses() -> None:
    fold_metrics = pd.DataFrame(
        {
            "configuration": ["baseline", "baseline", "unstable", "unstable"],
            "configuration_index": [0, 0, 1, 1],
            "fold": ["early", "late", "early", "late"],
            "mae_minutes": [4.0, 3.0, 3.0, 3.1],
            "p90_absolute_error_minutes": [8.0, 7.0, 7.0, 7.1],
        }
    )

    selection = _select_configuration(fold_metrics, _summarize_metrics(fold_metrics))

    assert selection["configuration"] == "baseline"
