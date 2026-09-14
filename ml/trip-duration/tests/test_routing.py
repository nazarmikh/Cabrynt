from __future__ import annotations

import json
from io import BytesIO
from urllib.error import HTTPError

import pandas as pd
import pytest

from cabrynt_trip_duration.routing import (
    OsrmClient,
    OsrmNoRouteError,
    RouteCache,
    RouteEstimate,
    fetch_route_estimates,
    select_route_sample,
)


class FakeResponse:
    def __init__(self, payload: dict[str, object]) -> None:
        self._payload = json.dumps(payload).encode("utf-8")

    def read(self) -> bytes:
        return self._payload

    def __enter__(self) -> FakeResponse:
        return self

    def __exit__(self, *_: object) -> None:
        return None


def test_osrm_client_converts_metres_and_seconds(monkeypatch: pytest.MonkeyPatch) -> None:
    requested_urls: list[str] = []

    def fake_urlopen(url: str, timeout: float) -> FakeResponse:
        requested_urls.append(url)
        assert timeout == 10.0
        return FakeResponse(
            {"code": "Ok", "routes": [{"distance": 2_500, "duration": 540}]}
        )

    monkeypatch.setattr("cabrynt_trip_duration.routing.request.urlopen", fake_urlopen)

    estimate = OsrmClient().route(-8.61, 41.15, -8.60, 41.16)

    assert estimate == RouteEstimate(distance_km=2.5, duration_minutes=9.0)
    assert requested_urls == [
        "http://127.0.0.1:5000/route/v1/driving/-8.610000,41.150000;"
        "-8.600000,41.160000?overview=false"
    ]


def test_osrm_client_rejects_response_without_a_route(monkeypatch: pytest.MonkeyPatch) -> None:
    monkeypatch.setattr(
        "cabrynt_trip_duration.routing.request.urlopen",
        lambda *_, **__: FakeResponse({"code": "NoRoute", "routes": []}),
    )

    with pytest.raises(OsrmNoRouteError, match="no driving route"):
        OsrmClient().route(-8.61, 41.15, -8.60, 41.16)


def test_osrm_client_identifies_no_route_http_responses(monkeypatch: pytest.MonkeyPatch) -> None:
    no_route_response = HTTPError(
        "http://127.0.0.1:5000/route/v1/driving/example",
        400,
        "Bad Request",
        hdrs=None,
        fp=BytesIO(b'{"code":"NoRoute","message":"No route found between points"}'),
    )

    def fake_urlopen(*_: object, **__: object) -> FakeResponse:
        raise no_route_response

    monkeypatch.setattr("cabrynt_trip_duration.routing.request.urlopen", fake_urlopen)

    with pytest.raises(OsrmNoRouteError, match="no driving route"):
        OsrmClient().route(-8.61, 41.15, -8.60, 41.16)


def test_route_cache_uses_normalized_coordinate_keys(tmp_path) -> None:
    estimate = RouteEstimate(distance_km=3.2, duration_minutes=8.5)

    with RouteCache(tmp_path / "routes.sqlite3") as cache:
        cache.put(-8.6100001, 41.1500001, -8.6000001, 41.1600001, estimate)

        cached = cache.get(-8.6100004, 41.1500004, -8.6000004, 41.1600004)

    assert cached == estimate


def test_select_route_sample_is_stable_and_ordered() -> None:
    validation_data = pd.DataFrame(
        {
            "trip_id": ["trip-c", "trip-a", "trip-b", "trip-d"],
            "duration_minutes": [1.0, 2.0, 3.0, 4.0],
        }
    )

    first = select_route_sample(validation_data, sample_size=3, random_state=42)
    second = select_route_sample(validation_data, sample_size=3, random_state=42)

    assert first.equals(second)
    assert first["trip_id"].tolist() == sorted(first["trip_id"])


def test_select_route_sample_rejects_an_invalid_size() -> None:
    validation_data = pd.DataFrame({"trip_id": ["trip-a"]})

    with pytest.raises(ValueError, match="eligible"):
        select_route_sample(validation_data, sample_size=2, random_state=42)


def test_select_route_sample_excludes_existing_trip_ids() -> None:
    test_data = pd.DataFrame(
        {
            "trip_id": ["trip-a", "trip-b", "trip-c", "trip-d"],
            "duration_minutes": [1.0, 2.0, 3.0, 4.0],
        }
    )

    sample = select_route_sample(
        test_data,
        sample_size=2,
        random_state=45,
        excluded_trip_ids={"trip-a", "trip-c"},
    )

    assert set(sample["trip_id"]) == {"trip-b", "trip-d"}


def test_fetch_route_estimates_reuses_cached_routes(
    monkeypatch: pytest.MonkeyPatch,
    tmp_path,
) -> None:
    calls: list[tuple[float, float, float, float]] = []

    class FakeOsrmClient:
        def __init__(self, base_url: str) -> None:
            assert base_url == "http://example.test"

        def route(
            self,
            pickup_longitude: float,
            pickup_latitude: float,
            destination_longitude: float,
            destination_latitude: float,
        ) -> RouteEstimate:
            calls.append(
                (
                    pickup_longitude,
                    pickup_latitude,
                    destination_longitude,
                    destination_latitude,
                )
            )
            return RouteEstimate(distance_km=2.5, duration_minutes=8.0)

    monkeypatch.setattr("cabrynt_trip_duration.routing.OsrmClient", FakeOsrmClient)
    sample = pd.DataFrame(
        {
            "trip_id": ["trip-1"],
            "pickup_longitude": [-8.61],
            "pickup_latitude": [41.15],
            "destination_longitude": [-8.60],
            "destination_latitude": [41.16],
        }
    )

    first, first_no_routes = fetch_route_estimates(
        sample,
        cache_path=tmp_path / "routes.sqlite3",
        base_url="http://example.test",
    )
    second, second_no_routes = fetch_route_estimates(
        sample,
        cache_path=tmp_path / "routes.sqlite3",
        base_url="http://example.test",
    )

    assert calls == [(-8.61, 41.15, -8.60, 41.16)]
    assert first["osrm_duration_minutes"].tolist() == [8.0]
    assert second["osrm_distance_km"].tolist() == [2.5]
    assert first_no_routes == []
    assert second_no_routes == []
