from scripts.ablate_route_features import FEATURE_CONFIGURATIONS


def test_route_feature_ablation_includes_baseline_and_all_derived_features() -> None:
    configurations = dict(FEATURE_CONFIGURATIONS)

    assert FEATURE_CONFIGURATIONS[0][0] == "baseline"
    assert "osrm_average_speed_kmh" in configurations["route_speed"]
    assert "osrm_detour_ratio" in configurations["route_geometry"]
    assert "osrm_distance_gap_km" in configurations["all_derived_route_features"]
