import pandas as pd
import pytest

from scripts.compare_model_with_osrm import _build_routable_cohort


def test_routable_cohort_keeps_only_osrm_estimated_validation_rows() -> None:
    validation_data = pd.DataFrame(
        {
            "trip_id": ["trip-1", "trip-2", "trip-3"],
            "duration_minutes": [5.0, 10.0, 15.0],
        }
    )
    route_estimates = pd.DataFrame(
        {
            "trip_id": ["trip-3", "trip-1"],
            "osrm_distance_km": [3.0, 1.0],
            "osrm_duration_minutes": [8.0, 4.0],
        }
    )

    cohort = _build_routable_cohort(validation_data, route_estimates)

    assert cohort["trip_id"].tolist() == ["trip-1", "trip-3"]
    assert cohort["osrm_duration_minutes"].tolist() == [4.0, 8.0]


def test_routable_cohort_rejects_unknown_or_duplicate_route_ids() -> None:
    validation_data = pd.DataFrame(
        {
            "trip_id": ["trip-1"],
            "duration_minutes": [5.0],
        }
    )
    unknown_route = pd.DataFrame(
        {
            "trip_id": ["unknown"],
            "osrm_distance_km": [1.0],
            "osrm_duration_minutes": [4.0],
        }
    )
    duplicate_route = pd.DataFrame(
        {
            "trip_id": ["trip-1", "trip-1"],
            "osrm_distance_km": [1.0, 1.0],
            "osrm_duration_minutes": [4.0, 4.0],
        }
    )

    with pytest.raises(ValueError, match="do not belong"):
        _build_routable_cohort(validation_data, unknown_route)
    with pytest.raises(ValueError, match="duplicate"):
        _build_routable_cohort(validation_data, duplicate_route)
