import type { PaymentMethod } from "@/lib/backend-types"

export const paymentMethodOptions: Array<{ value: PaymentMethod; label: string }> = [
  { value: "Card", label: "Card" },
  { value: "Cash", label: "Cash" },
  { value: "ApplePay", label: "Apple Pay" },
]

export function formatPaymentMethod(paymentMethod?: PaymentMethod | null) {
  return paymentMethodOptions.find((option) => option.value === paymentMethod)?.label ?? "Not set"
}
