import pandas as pd

from cabrynt_trip_duration.calendar_features import add_calendar_features, porto_datetime


def test_porto_datetime_uses_local_summer_time() -> None:
    started_at = porto_datetime(1_372_636_800)

    assert started_at.hour == 1
    assert started_at.tzname() == "WEST"


def test_calendar_features_include_portuguese_public_holidays() -> None:
    data = pd.DataFrame({"timestamp": [1_388_534_400]})

    result = add_calendar_features(data)

    assert result.loc[0, "hour"] == 0
    assert result.loc[0, "weekday"] == 2
    assert result.loc[0, "is_public_holiday"] == 1
    assert result.loc[0, "is_weekend"] == 0
    assert result.loc[0, "month"] == 1


def test_calendar_features_are_cyclical() -> None:
    data = pd.DataFrame({"timestamp": [1_388_534_400, 1_388_620_800]})

    result = add_calendar_features(data)

    assert result["hour_sin"].between(-1, 1).all()
    assert result["hour_cos"].between(-1, 1).all()
    assert result["weekday_sin"].between(-1, 1).all()
    assert result["weekday_cos"].between(-1, 1).all()
