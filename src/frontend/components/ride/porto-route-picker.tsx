"use client"

import { useEffect, useRef } from "react"
import { Button } from "@/components/ui/button"
import type { Coordinates } from "@/lib/location"

const portoBounds: [[number, number], [number, number]] = [
  [41.09, -8.71],
  [41.21, -8.53],
]

const portoCenter: [number, number] = [41.1579, -8.6291]

export type RoutePoint = "pickup" | "destination"

interface PortoRoutePickerProps {
  activePoint: RoutePoint
  pickup: Coordinates | null
  destination: Coordinates | null
  onActivePointChange: (point: RoutePoint) => void
  onPointSelect: (point: RoutePoint, coordinates: Coordinates) => void
}

export function PortoRoutePicker({
  activePoint,
  pickup,
  destination,
  onActivePointChange,
  onPointSelect,
}: PortoRoutePickerProps) {
  const mapElement = useRef<HTMLDivElement>(null)
  const map = useRef<import("leaflet").Map | null>(null)
  const leaflet = useRef<typeof import("leaflet") | null>(null)
  const markers = useRef<import("leaflet").LayerGroup | null>(null)
  const activePointRef = useRef(activePoint)
  const onPointSelectRef = useRef(onPointSelect)

  useEffect(() => {
    activePointRef.current = activePoint
    onPointSelectRef.current = onPointSelect
  }, [activePoint, onPointSelect])

  useEffect(() => {
    let disposed = false

    void import("leaflet").then((loadedLeaflet) => {
      if (disposed || !mapElement.current || map.current) {
        return
      }

      leaflet.current = loadedLeaflet
      const mapInstance = loadedLeaflet.map(mapElement.current, {
        center: portoCenter,
        zoom: 13,
        maxBounds: portoBounds,
        maxBoundsViscosity: 1,
        minZoom: 12,
      })

      loadedLeaflet.tileLayer("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", {
        attribution: "&copy; OpenStreetMap contributors",
        maxZoom: 19,
      }).addTo(mapInstance)

      markers.current = loadedLeaflet.layerGroup().addTo(mapInstance)
      mapInstance.on("click", (event) => {
        const coordinates = {
          latitude: Number(event.latlng.lat.toFixed(6)),
          longitude: Number(event.latlng.lng.toFixed(6)),
        }
        onPointSelectRef.current(activePointRef.current, coordinates)
      })
      map.current = mapInstance
    })

    return () => {
      disposed = true
      map.current?.remove()
      map.current = null
      markers.current = null
      leaflet.current = null
    }
  }, [])

  useEffect(() => {
    if (!leaflet.current || !markers.current) {
      return
    }

    const loadedLeaflet = leaflet.current
    markers.current.clearLayers()

    if (pickup) {
      loadedLeaflet.circleMarker([pickup.latitude, pickup.longitude], {
        color: "#2563eb",
        fillColor: "#2563eb",
        fillOpacity: 1,
        radius: 8,
      }).bindTooltip("Pickup").addTo(markers.current)
    }

    if (destination) {
      loadedLeaflet.circleMarker([destination.latitude, destination.longitude], {
        color: "#0f766e",
        fillColor: "#0f766e",
        fillOpacity: 1,
        radius: 8,
      }).bindTooltip("Destination").addTo(markers.current)
    }

    if (pickup && destination) {
      loadedLeaflet.polyline(
        [
          [pickup.latitude, pickup.longitude],
          [destination.latitude, destination.longitude],
        ],
        { color: "#475569", dashArray: "6 6", weight: 2 }
      ).addTo(markers.current)
    }
  }, [pickup, destination])

  return (
    <div className="space-y-3">
      <div className="grid grid-cols-2 gap-2" aria-label="Route point selection">
        <Button
          type="button"
          variant={activePoint === "pickup" ? "default" : "outline"}
          onClick={() => onActivePointChange("pickup")}
        >
          Pickup
        </Button>
        <Button
          type="button"
          variant={activePoint === "destination" ? "default" : "outline"}
          onClick={() => onActivePointChange("destination")}
        >
          Destination
        </Button>
      </div>
      <div ref={mapElement} className="h-80 overflow-hidden rounded-md border" aria-label="Porto route map" />
    </div>
  )
}
