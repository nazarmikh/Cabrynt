export type AppRole = "Passenger" | "Admin" | "Vehicle"
export type UiRole = "passenger" | "admin" | "vehicle"
export type PaymentMethod = "Cash" | "Card" | "ApplePay"

export interface LoginResponse {
  id: number
  email: string
  role: AppRole
}

export interface MeResponse {
  name: string
  email: string
  role: AppRole
  homeAddress?: string
  points?: number
  preferredPaymentMethod?: PaymentMethod
}

export interface CurrentUser {
  name: string
  email: string
  role: UiRole
  homeAddress?: string
  points: number
  preferredPaymentMethod?: PaymentMethod
}

export interface RideResponse {
  rideId: number
  rideStatus: string
  requestTime: string
  estimatedPrice: number
  vehicleId?: number | null
  departureLocation?: string | null
  destinationLocation?: string | null
  distance: number
  duration: number
  preferredVehicleType: string
}

export interface CompleteRideResponse {
  rideId: number
  rideStatus: string
  completedAt: string
  vehicleId?: number | null
  paymentId?: number | null
  paymentAmount?: number | null
}

export interface CreateRideRequest {
  departureLocation: string
  destinationLocation: string
  departureLatitude: number
  departureLongitude: number
  destinationLatitude: number
  destinationLongitude: number
  preferredVehicleType: string
  discountCode?: string
}

export interface UpdateMeRequest {
  name: string
  homeAddress: string
  preferredPaymentMethod: PaymentMethod
}

export interface RideQuoteResponse {
  distance: number
  duration: number
  baseFare: number
  distanceCost: number
  durationCost: number
  vehicleMultiplier: number
  nightSurcharge: number
  isNightRateApplied: boolean
  loyaltyDiscount: number
  codeDiscount: number
  vatAmount: number
  estimatedPrice: number
}

export type TicketPriority = "Low" | "Medium" | "High" | "Critical"
export type TicketStatus = "Open" | "InProgress" | "Resolved"

export interface TicketResponse {
  id: number
  subject: string
  description: string
  ticketPriority: TicketPriority
  ticketStatus: TicketStatus
  reportTime: string
}

export interface TicketsResponse {
  tickets: TicketResponse[]
}

export interface AdminTicketResponse extends TicketResponse {
  passengerUserId: number
  passengerEmail: string
}

export interface AdminDashboardSummary {
  totalUsers: number
  activeVehicles: number
  activeRides: number
  openTickets: number
  todayRevenue: number
  weekRevenue: number
  monthRevenue: number
}

export interface GraphQlVehicle {
  id: number
  vin: string
  licencePlate: string
  model: string
  vehicleType: string
  vehicleStatus: string
  year: number
  battery?: number | null
  latitude?: number | null
  longitude?: number | null
  currentSpeed?: number | null
  hardwareTemperature?: number | null
  lastTelemetryAt?: string | null
}

export interface GraphQlRide {
  id: number
  departureLocation: string
  destinationLocation: string
  distance: number
  duration: number
  preferredVehicleType: string
  estimatedPrice: number
  rideStatus: string
  requestTime: string
  vehicleId?: number | null
  vehicleLicencePlate?: string | null
  passengerEmail: string
}

export interface GraphQlTicket {
  id: number
  subject: string
  description: string
  ticketPriority: string
  ticketStatus: string
  reportTime: string
  passengerUserId: number
  passengerEmail: string
}

export interface AdminDashboardQueryResult {
  adminDashboardSummary: AdminDashboardSummary
  vehicles: GraphQlVehicle[]
  rides: GraphQlRide[]
  tickets: GraphQlTicket[]
}

export interface GraphQlTelemetry {
  id: string
  vehicleId: number
  latitude: number
  longitude: number
  currentSpeed: number
  remainingBatteryPercentage: number
  hardwareTemperature: number
  timeStamp: string
}

export interface GraphQlSensorDiagnostic {
  id?: string | null
  vehicleId: number
  vehicleTelemetryId?: string | null
  sensorType: string
  errorCode: number
  deviationSeverity: string
  rawSensorValue?: string | null
  timeStamp: string
}

export interface GraphQlMaintenance {
  id: number
  vehicleId: number
  vehicleLicencePlate: string
  serviceDate: string
  description: string
  technicianName: string
  cost: number
  nextInspectionMileage: number
}

export interface GraphQlUser {
  id: number
  name: string
  email: string
  role: string
  accountCreated: string
  lastLogin: string
  points: number
  preferredPaymentMethod?: PaymentMethod | null
}

export interface CreateMaintenanceRequest {
  serviceDate: string
  description: string
  technicianName: string
  cost: number
  nextInspectionMileage: number
}
