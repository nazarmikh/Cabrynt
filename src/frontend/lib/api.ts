import { getAccessToken } from "@/lib/auth"

const API_BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5000"
const GRAPHQL_URL = process.env.NEXT_PUBLIC_GRAPHQL_URL ?? `${API_BASE_URL}/graphql`

export class ApiError extends Error {
  status: number
  data?: unknown

  constructor(message: string, status: number, data?: unknown) {
    super(message)
    this.name = "ApiError"
    this.status = status
    this.data = data
  }
}

async function parseResponse(response: Response) {
  const contentType = response.headers.get("content-type") ?? ""

  if (contentType.includes("application/json")) {
    return response.json()
  }

  const text = await response.text()
  return text || null
}

export async function apiRequest<T>(
  path: string,
  init: RequestInit = {},
  authenticated = false
): Promise<T> {
  const headers = new Headers(init.headers)

  if (init.body && !headers.has("Content-Type")) {
    headers.set("Content-Type", "application/json")
  }

  if (!headers.has("Accept")) {
    headers.set("Accept", "application/json")
  }

  if (authenticated) {
    const token = getAccessToken()
    if (token) {
      headers.set("Authorization", `Bearer ${token}`)
    }
  }

  const response = await fetch(`${API_BASE_URL}${path}`, {
    ...init,
    headers,
  })

  const data = await parseResponse(response)

  if (!response.ok) {
    const message =
      typeof data === "object" && data !== null && "message" in data
        ? String((data as { message: unknown }).message)
        : response.statusText || "Request failed"

    throw new ApiError(message, response.status, data)
  }

  return data as T
}

export async function graphqlRequest<T>(
  query: string,
  variables?: Record<string, unknown>
): Promise<T> {
  const token = getAccessToken()
  const headers = new Headers({
    "Content-Type": "application/json",
    Accept: "application/json",
  })

  if (token) {
    headers.set("Authorization", `Bearer ${token}`)
  }

  const response = await fetch(GRAPHQL_URL, {
    method: "POST",
    headers,
    body: JSON.stringify({ query, variables }),
  })

  const payload = (await response.json()) as {
    data?: T
    errors?: Array<{ message: string }>
  }

  if (!response.ok || payload.errors?.length || !payload.data) {
    throw new ApiError(payload.errors?.[0]?.message ?? "GraphQL request failed", response.status, payload)
  }

  return payload.data
}
