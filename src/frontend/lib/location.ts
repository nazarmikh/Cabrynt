export interface Coordinates {
  latitude: number
  longitude: number
}

function randomInRange(min: number, max: number) {
  return min + Math.random() * (max - min)
}

export function randomDemoCoordinates(): Coordinates {
  return {
    latitude: Number(randomInRange(50.95, 51.25).toFixed(6)),
    longitude: Number(randomInRange(3.05, 4.15).toFixed(6)),
  }
}
