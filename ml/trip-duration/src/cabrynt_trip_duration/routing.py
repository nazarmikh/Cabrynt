"""Local OSRM route estimates and deterministic evaluation sampling."""

from __future__ import annotations

from dataclasses import dataclass
from pathlib import Path
import json
import sqlite3
from urllib import error, parse, request

import pandas as pd


class OsrmRequestError(RuntimeError):
    """Raised when the local OSRM service cannot return a driving route."""


class OsrmNoRouteError(OsrmRequestError):
    """Raised when OSRM cannot connect the snapped start and end roads."""


@dataclass(frozen=True)
class RouteEstimate:
    """A driving route estimate returned by OSRM."""

    distance_km: float
    duration_minutes: float


class RouteCache:
    """Persist local OSRM route responses so benchmark reruns do not repeat work."""

    def __init__(self, database_path: Path) -> None:
        database_path.parent.mkdir(parents=True, exist_ok=True)
        self._connection = sqlite3.connect(database_path)
        self._connection.execute(
            """
            CREATE TABLE IF NOT EXISTS route_estimates (
                pickup_longitude TEXT NOT NULL,
                pickup_latitude TEXT NOT NULL,
                destination_longitude TEXT NOT NULL,
                destination_latitude TEXT NOT NULL,
                distance_km REAL NOT NULL,
                duration_minutes REAL NOT NULL,
                PRIMARY KEY (
                    pickup_longitude,
                    pickup_latitude,
                    destination_longitude,
                    destination_latitude
                )
            )
            """
        )
        self._connection.commit()

    def close(self) -> None:
        self._connection.close()

    def get(
        self,
        pickup_longitude: float,
        pickup_latitude: float,
        destination_longitude: float,
        destination_latitude: float,
    ) -> RouteEstimate | None:
        row = self._connection.execute(
            """
            SELECT distance_km, duration_minutes
            FROM route_estimates
            WHERE pickup_longitude = ?
              AND pickup_latitude = ?
              AND destination_longitude = ?
              AND destination_latitude = ?
            """,
            _coordinate_key(
                pickup_longitude,
                pickup_latitude,
                destination_longitude,
                destination_latitude,
            ),
        ).fetchone()

        if row is None:
            return None

        return RouteEstimate(distance_km=row[0], duration_minutes=row[1])

    def put(
        self,
        pickup_longitude: float,
        pickup_latitude: float,
        destination_longitude: float,
        destination_latitude: float,
        estimate: RouteEstimate,
    ) -> None:
        self._connection.execute(
            """
            INSERT OR REPLACE INTO route_estimates (
                pickup_longitude,
                pickup_latitude,
                destination_longitude,
                destination_latitude,
                distance_km,
                duration_minutes
            ) VALUES (?, ?, ?, ?, ?, ?)
            """,
            (
                *_coordinate_key(
                    pickup_longitude,
                    pickup_latitude,
                    destination_longitude,
                    destination_latitude,
                ),
                estimate.distance_km,
                estimate.duration_minutes,
            ),
        )
        self._connection.commit()

    def __enter__(self) -> RouteCache:
        return self

    def __exit__(self, *_: object) -> None:
        self.close()


class OsrmClient:
    """Small client for a locally hosted OSRM route service."""

    def __init__(
        self,
        base_url: str = "http://127.0.0.1:5000",
        timeout_seconds: float = 10.0,
    ) -> None:
        self.base_url = base_url.rstrip("/")
        self.timeout_seconds = timeout_seconds

    def route(
        self,
        pickup_longitude: float,
        pickup_latitude: float,
        destination_longitude: float,
        destination_latitude: float,
    ) -> RouteEstimate:
        coordinates = (
            f"{pickup_longitude:.6f},{pickup_latitude:.6f};"
            f"{destination_longitude:.6f},{destination_latitude:.6f}"
        )
        query = parse.urlencode({"overview": "false"})
        url = f"{self.base_url}/route/v1/driving/{coordinates}?{query}"

        try:
            with request.urlopen(url, timeout=self.timeout_seconds) as response:
                payload = json.loads(response.read())
        except error.HTTPError as exception:
            response_text = exception.read().decode("utf-8", errors="replace")
            try:
                payload = json.loads(response_text)
            except json.JSONDecodeError:
                payload = {}

            if payload.get("code") == "NoRoute":
                raise OsrmNoRouteError("OSRM found no driving route between the points.") from exception

            raise OsrmRequestError(
                f"OSRM returned HTTP {exception.code}: {response_text}"
            ) from exception
        except (error.URLError, TimeoutError) as exception:
            raise OsrmRequestError(
                f"Could not reach OSRM at {self.base_url}. Start the local OSRM service first."
            ) from exception
        except json.JSONDecodeError as exception:
            raise OsrmRequestError("OSRM returned invalid JSON.") from exception

        if payload.get("code") == "NoRoute":
            raise OsrmNoRouteError("OSRM found no driving route between the points.")
        if payload.get("code") != "Ok" or not payload.get("routes"):
            raise OsrmRequestError(f"OSRM could not route the coordinates: {payload}")

        route = payload["routes"][0]
        try:
            return RouteEstimate(
                distance_km=float(route["distance"]) / 1_000,
                duration_minutes=float(route["duration"]) / 60,
            )
        except (KeyError, TypeError, ValueError) as exception:
            raise OsrmRequestError("OSRM response did not contain a valid route estimate.") from exception


def select_validation_sample(
    validation_data: pd.DataFrame,
    sample_size: int,
    random_state: int,
) -> pd.DataFrame:
    """Return a stable validation sample for an affordable OSRM benchmark."""
    if sample_size <= 0:
        raise ValueError("sample_size must be positive")
    if sample_size > len(validation_data):
        raise ValueError("sample_size cannot exceed the validation row count")

    return (
        validation_data.sample(n=sample_size, random_state=random_state)
        .sort_values("trip_id", kind="stable")
        .reset_index(drop=True)
    )


def _coordinate_key(
    pickup_longitude: float,
    pickup_latitude: float,
    destination_longitude: float,
    destination_latitude: float,
) -> tuple[str, str, str, str]:
    return (
        f"{pickup_longitude:.6f}",
        f"{pickup_latitude:.6f}",
        f"{destination_longitude:.6f}",
        f"{destination_latitude:.6f}",
    )
