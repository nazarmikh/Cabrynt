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
  departureLocation?: string | null
  destinationLocation?: string | null
  distance: number
  duration: number
  preferredVehicleType: string
}

export interface CreateRideRequest {
  departureLocation: string
  destinationLocation: string
  departureLatitude: number
  departureLongitude: number
  destinationLatitude: number
  destinationLongitude: number
  preferredVehicleType: string
}

export interface UpdateMeRequest {
  name: string
  homeAddress: string
  preferredPaymentMethod: PaymentMethod
}

export interface RideQuoteResponse {
  distance: number
  duration: number
  estimatedTripDuration: number
  estimatedTripDurationSource: TripDurationEstimateSource
  baseFare: number
  distanceCost: number
  durationCost: number
  vehicleMultiplier: number
  nightSurcharge: number
  isNightRateApplied: boolean
  vatAmount: number
  estimatedPrice: number
}

export type TripDurationEstimateSource = "MachineLearning" | "Osrm" | "StraightLineFallback"

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

export interface CreateMaintenanceRequest {
  serviceDate: string
  description: string
  technicianName: string
  cost: number
  nextInspectionMileage: number
}
