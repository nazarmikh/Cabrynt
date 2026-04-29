// Mock data for NovaDrive platform

export interface User {
  id: string
  name: string
  email: string
  role: "passenger" | "admin"
  loyaltyPoints: number
  address: string
  paymentMethod: string
  createdAt: string
}

export interface Vehicle {
  id: string
  vin: string
  licensePlate: string
  model: string
  type: "standard" | "van" | "luxury"
  status: "active" | "inactive" | "maintenance"
  battery: number
  location: { lat: number; lng: number }
  currentSpeed: number
  temperature: number
  mileage: number
}

export interface Ride {
  id: string
  passengerId: string
  vehicleId: string
  pickup: string
  destination: string
  status: "requested" | "en_route" | "completed" | "canceled"
  price: number
  distance: number
  duration: number
  vehicleType: "standard" | "van" | "luxury"
  createdAt: string
  completedAt?: string
}

export interface Ticket {
  id: string
  userId: string
  subject: string
  description: string
  priority: "low" | "medium" | "high" | "critical"
  status: "open" | "in_progress" | "resolved"
  createdAt: string
  updatedAt: string
}

export interface TelemetryData {
  vehicleId: string
  timestamp: string
  gps: { lat: number; lng: number }
  speed: number
  battery: number
  temperature: number
  heading: number
}

export interface SensorDiagnostic {
  id: string
  vehicleId: string
  sensorType: "lidar" | "radar" | "camera" | "ultrasonic"
  errorCode: string
  severity: "info" | "warning" | "error" | "critical"
  message: string
  timestamp: string
  rawData: object
}

export interface MaintenanceLog {
  id: string
  vehicleId: string
  serviceDate: string
  description: string
  technician: string
  cost: number
  nextServiceMileage: number
}

export interface PricingConfig {
  baseFare: number
  distanceRate: number
  durationRate: number
  vehicleMultipliers: Record<string, number>
  nightRateMultiplier: number
  surgeMultiplier: number
}

// Current user mock
export const currentUser: User = {
  id: "user-1",
  name: "Alex Johnson",
  email: "alex@example.com",
  role: "passenger",
  loyaltyPoints: 2450,
  address: "123 Main Street, San Francisco, CA 94102",
  paymentMethod: "Visa •••• 4242",
  createdAt: "2024-01-15T10:00:00Z",
}

export const adminUser: User = {
  id: "admin-1",
  name: "Sarah Chen",
  email: "admin@novadrive.com",
  role: "admin",
  loyaltyPoints: 0,
  address: "NovaDrive HQ, San Francisco, CA",
  paymentMethod: "Corporate",
  createdAt: "2023-06-01T08:00:00Z",
}

// Vehicles mock data
export const vehicles: Vehicle[] = [
  {
    id: "v-001",
    vin: "5YJ3E1EA1NF123456",
    licensePlate: "ND-001",
    model: "NovaPod X1",
    type: "standard",
    status: "active",
    battery: 87,
    location: { lat: 37.7749, lng: -122.4194 },
    currentSpeed: 28,
    temperature: 22,
    mileage: 45230,
  },
  {
    id: "v-002",
    vin: "5YJ3E1EA2NF234567",
    licensePlate: "ND-002",
    model: "NovaPod V2",
    type: "van",
    status: "active",
    battery: 92,
    location: { lat: 37.7849, lng: -122.4094 },
    currentSpeed: 0,
    temperature: 21,
    mileage: 32150,
  },
  {
    id: "v-003",
    vin: "5YJ3E1EA3NF345678",
    licensePlate: "ND-003",
    model: "NovaPod L1",
    type: "luxury",
    status: "active",
    battery: 76,
    location: { lat: 37.7649, lng: -122.4294 },
    currentSpeed: 45,
    temperature: 23,
    mileage: 28900,
  },
  {
    id: "v-004",
    vin: "5YJ3E1EA4NF456789",
    licensePlate: "ND-004",
    model: "NovaPod X1",
    type: "standard",
    status: "maintenance",
    battery: 34,
    location: { lat: 37.7549, lng: -122.4394 },
    currentSpeed: 0,
    temperature: 19,
    mileage: 67800,
  },
  {
    id: "v-005",
    vin: "5YJ3E1EA5NF567890",
    licensePlate: "ND-005",
    model: "NovaPod X1",
    type: "standard",
    status: "active",
    battery: 95,
    location: { lat: 37.7949, lng: -122.3994 },
    currentSpeed: 32,
    temperature: 22,
    mileage: 12450,
  },
]

// Rides mock data
export const rides: Ride[] = [
  {
    id: "ride-001",
    passengerId: "user-1",
    vehicleId: "v-001",
    pickup: "123 Main St, San Francisco",
    destination: "456 Market St, San Francisco",
    status: "completed",
    price: 18.50,
    distance: 4.2,
    duration: 15,
    vehicleType: "standard",
    createdAt: "2024-03-15T14:30:00Z",
    completedAt: "2024-03-15T14:45:00Z",
  },
  {
    id: "ride-002",
    passengerId: "user-1",
    vehicleId: "v-003",
    pickup: "789 Mission St, San Francisco",
    destination: "321 Howard St, San Francisco",
    status: "completed",
    price: 32.00,
    distance: 5.8,
    duration: 22,
    vehicleType: "luxury",
    createdAt: "2024-03-14T09:15:00Z",
    completedAt: "2024-03-14T09:37:00Z",
  },
  {
    id: "ride-003",
    passengerId: "user-1",
    vehicleId: "v-002",
    pickup: "555 California St, San Francisco",
    destination: "SFO Airport",
    status: "en_route",
    price: 45.00,
    distance: 14.5,
    duration: 35,
    vehicleType: "van",
    createdAt: "2024-03-16T08:00:00Z",
  },
  {
    id: "ride-004",
    passengerId: "user-2",
    vehicleId: "v-005",
    pickup: "Pier 39, San Francisco",
    destination: "Golden Gate Park",
    status: "requested",
    price: 24.50,
    distance: 7.2,
    duration: 25,
    vehicleType: "standard",
    createdAt: "2024-03-16T10:30:00Z",
  },
  {
    id: "ride-005",
    passengerId: "user-1",
    vehicleId: "v-001",
    pickup: "Union Square",
    destination: "Fisherman's Wharf",
    status: "canceled",
    price: 15.00,
    distance: 3.5,
    duration: 12,
    vehicleType: "standard",
    createdAt: "2024-03-13T16:45:00Z",
  },
]

// Tickets mock data
export const tickets: Ticket[] = [
  {
    id: "ticket-001",
    userId: "user-1",
    subject: "Ride charged incorrectly",
    description: "I was charged €45 but the app showed €32 for my ride on March 14th.",
    priority: "high",
    status: "in_progress",
    createdAt: "2024-03-15T10:00:00Z",
    updatedAt: "2024-03-15T14:30:00Z",
  },
  {
    id: "ticket-002",
    userId: "user-2",
    subject: "Vehicle arrived late",
    description: "The vehicle arrived 15 minutes after the estimated time.",
    priority: "medium",
    status: "open",
    createdAt: "2024-03-16T09:00:00Z",
    updatedAt: "2024-03-16T09:00:00Z",
  },
  {
    id: "ticket-003",
    userId: "user-3",
    subject: "App crashes on booking",
    description: "The app crashes every time I try to book a ride from the home screen.",
    priority: "critical",
    status: "open",
    createdAt: "2024-03-16T11:30:00Z",
    updatedAt: "2024-03-16T11:30:00Z",
  },
  {
    id: "ticket-004",
    userId: "user-1",
    subject: "Lost item in vehicle",
    description: "I left my umbrella in vehicle ND-003 yesterday.",
    priority: "low",
    status: "resolved",
    createdAt: "2024-03-14T18:00:00Z",
    updatedAt: "2024-03-15T09:00:00Z",
  },
]

// Sensor diagnostics mock data
export const sensorDiagnostics: SensorDiagnostic[] = [
  {
    id: "diag-001",
    vehicleId: "v-004",
    sensorType: "lidar",
    errorCode: "LID-E001",
    severity: "error",
    message: "Front LIDAR sensor calibration required",
    timestamp: "2024-03-16T08:45:00Z",
    rawData: { sensorId: "lidar-front-01", calibrationOffset: 0.023, lastCalibration: "2024-02-15" },
  },
  {
    id: "diag-002",
    vehicleId: "v-001",
    sensorType: "camera",
    errorCode: "CAM-W002",
    severity: "warning",
    message: "Rear camera lens cleaning recommended",
    timestamp: "2024-03-16T07:30:00Z",
    rawData: { sensorId: "cam-rear-01", clarity: 0.78, threshold: 0.85 },
  },
  {
    id: "diag-003",
    vehicleId: "v-003",
    sensorType: "radar",
    errorCode: "RAD-I001",
    severity: "info",
    message: "Radar firmware update available",
    timestamp: "2024-03-15T23:00:00Z",
    rawData: { currentVersion: "2.3.1", availableVersion: "2.4.0" },
  },
  {
    id: "diag-004",
    vehicleId: "v-004",
    sensorType: "ultrasonic",
    errorCode: "ULT-C001",
    severity: "critical",
    message: "Left side ultrasonic sensor failure",
    timestamp: "2024-03-16T06:15:00Z",
    rawData: { sensorId: "ultra-left-02", responseTime: null, status: "no_response" },
  },
]

// Maintenance logs mock data
export const maintenanceLogs: MaintenanceLog[] = [
  {
    id: "maint-001",
    vehicleId: "v-004",
    serviceDate: "2024-03-16",
    description: "Full sensor recalibration and battery health check",
    technician: "Mike Torres",
    cost: 850,
    nextServiceMileage: 75000,
  },
  {
    id: "maint-002",
    vehicleId: "v-001",
    serviceDate: "2024-03-10",
    description: "Tire rotation and brake pad replacement",
    technician: "Lisa Wang",
    cost: 420,
    nextServiceMileage: 50000,
  },
  {
    id: "maint-003",
    vehicleId: "v-002",
    serviceDate: "2024-03-05",
    description: "Software update and interior cleaning",
    technician: "James Miller",
    cost: 150,
    nextServiceMileage: 40000,
  },
  {
    id: "maint-004",
    vehicleId: "v-003",
    serviceDate: "2024-02-28",
    description: "Battery module replacement",
    technician: "Mike Torres",
    cost: 2400,
    nextServiceMileage: 35000,
  },
]

// Pricing config
export const pricingConfig: PricingConfig = {
  baseFare: 2.50,
  distanceRate: 1.10,
  durationRate: 0.30,
  vehicleMultipliers: {
    standard: 1.0,
    van: 1.3,
    luxury: 1.8,
  },
  nightRateMultiplier: 1.25,
  surgeMultiplier: 1.0,
}

// Dashboard stats for admin
export const adminStats = {
  totalUsers: 12847,
  activeVehicles: 147,
  activeRides: 23,
  openTickets: 156,
  revenue: {
    today: 15420,
    week: 98750,
    month: 412500,
  },
  ridesByDay: [
    { day: "Mon", rides: 1240 },
    { day: "Tue", rides: 1380 },
    { day: "Wed", rides: 1520 },
    { day: "Thu", rides: 1450 },
    { day: "Fri", rides: 1780 },
    { day: "Sat", rides: 2100 },
    { day: "Sun", rides: 1650 },
  ],
  fleetStatus: {
    active: 147,
    idle: 45,
    charging: 32,
    maintenance: 8,
  },
}

// Helper function to calculate price
export function calculateRidePrice(
  distance: number,
  duration: number,
  vehicleType: "standard" | "van" | "luxury",
  isNightRate: boolean = false,
  discountCode?: string,
  loyaltyPoints: number = 0
): {
  baseFare: number
  distanceCost: number
  durationCost: number
  vehicleMultiplier: number
  nightSurcharge: number
  loyaltyDiscount: number
  codeDiscount: number
  total: number
} {
  const baseFare = pricingConfig.baseFare
  const distanceCost = distance * pricingConfig.distanceRate
  const durationCost = duration * pricingConfig.durationRate
  const vehicleMultiplier = pricingConfig.vehicleMultipliers[vehicleType]
  
  let subtotal = (baseFare + distanceCost + durationCost) * vehicleMultiplier
  
  const nightSurcharge = isNightRate ? subtotal * (pricingConfig.nightRateMultiplier - 1) : 0
  subtotal += nightSurcharge
  
  // Loyalty discount: 1 point = €0.01, max 10% discount
  const maxLoyaltyDiscount = subtotal * 0.1
  const loyaltyDiscount = Math.min(loyaltyPoints * 0.01, maxLoyaltyDiscount)
  
  // Code discount
  let codeDiscount = 0
  if (discountCode === "NOVA10") {
    codeDiscount = subtotal * 0.1
  } else if (discountCode === "FIRST25") {
    codeDiscount = subtotal * 0.25
  }
  
  const total = Math.max(subtotal - loyaltyDiscount - codeDiscount, baseFare)
  
  return {
    baseFare,
    distanceCost,
    durationCost,
    vehicleMultiplier,
    nightSurcharge,
    loyaltyDiscount,
    codeDiscount,
    total,
  }
}
