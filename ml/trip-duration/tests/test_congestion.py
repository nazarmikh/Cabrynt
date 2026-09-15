import pandas as pd

from cabrynt_trip_duration.congestion import (
    add_chronological_training_congestion_features,
    add_congestion_features,
    fit_congestion_profiles,
)


def test_congestion_profiles_fall_back_to_the_historical_global_mean() -> None:
    history = _trip_data([10.0, 20.0], timestamps=[1, 2])
    future = _trip_data([30.0], timestamps=[3], longitude=-8.70)

    result = add_congestion_features(future, fit_congestion_profiles(history))

    assert result.loc[0, "historical_pickup_duration_minutes"] == 15.0
    assert result.loc[0, "historical_pickup_observations"] == 0
    assert result.loc[0, "historical_congestion_available"] == 1


def test_training_congestion_features_do_not_use_future_targets() -> None:
    train_data = _trip_data([10.0, 20.0, 100.0, 200.0], timestamps=[1, 2, 3, 4])

    result = add_chronological_training_congestion_features(train_data, folds=2)

    assert result.loc[result["timestamp"] < 3, "historical_congestion_available"].eq(0).all()
    later_rows = result.loc[result["timestamp"] >= 3]
    assert later_rows["historical_time_duration_minutes"].tolist() == [15.0, 15.0]
    assert later_rows["historical_pickup_duration_minutes"].tolist() == [15.0, 15.0]


def _trip_data(
    durations: list[float],
    timestamps: list[int],
    longitude: float = -8.61,
) -> pd.DataFrame:
    return pd.DataFrame(
        {
            "trip_id": [f"trip-{index}" for index in range(len(durations))],
            "timestamp": timestamps,
            "duration_minutes": durations,
            "pickup_longitude": [longitude] * len(durations),
            "pickup_latitude": [41.15] * len(durations),
            "hour": [8] * len(durations),
            "weekday": [1] * len(durations),
        }
    )
