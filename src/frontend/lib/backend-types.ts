export type AppRole = "Passenger" | "Admin"
export type UiRole = "passenger" | "admin"
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
  departureLatitude: number
  departureLongitude: number
  destinationLatitude: number
  destinationLongitude: number
  distance: number
  duration: number
  estimatedTripDuration?: number | null
  estimatedTripDurationSource?: TripDurationEstimateSource | null
  tripDurationModelVersion?: string | null
  preferredServiceTier: string
}

export interface CreateRideRequest {
  departureLatitude: number
  departureLongitude: number
  destinationLatitude: number
  destinationLongitude: number
  preferredServiceTier: string
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

export interface TripDurationModelStatus {
  isEnabled: boolean
  isAvailable: boolean
  state: "Disabled" | "Initializing" | "Ready" | "Unavailable"
  configuredVersion?: string | null
}

export type RouteEstimateSource = "Osrm" | "StraightLineFallback"

export interface ModelDemoEstimate {
  routeDistance: number
  routeDuration: number
  routeEstimateSource: RouteEstimateSource
  estimatedTripDuration: number
  estimatedTripDurationSource: TripDurationEstimateSource
  modelCorrection?: number | null
  modelVersion?: string | null
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
