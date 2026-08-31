const API_BASE_URL = import.meta.env.VITE_API_URL ?? ''

export interface HealthCheckResult {
  status: string
  postgresConnected: boolean
  redisConnected: boolean
  timestamp: string
}

export async function fetchHealth(): Promise<HealthCheckResult> {
  const response = await fetch(`${API_BASE_URL}/api/health`)

  if (!response.ok) {
    throw new Error(`Health check failed: ${response.status} ${response.statusText}`)
  }

  return response.json()
}

export interface DashboardOverview {
  totalRequests: number
  blockedRequests: number
  activeBlocks: number
  securityEventsToday: number
  wafEventsToday: number
  rateLimitHitsToday: number
  totalApplications: number
  totalDevices: number
  totalUsers: number
}

export interface TimeSeriesPoint {
  timestamp: string
  count: number
}

export interface SecurityEventSeries {
  severity: string
  points: TimeSeriesPoint[]
}

export interface TopThreat {
  ipAddress: string
  threatScore: number
  requestCount: number
  attackCount: number
}

export interface AttackTypeSummary {
  type: string
  count: number
}

export interface RecentEvent {
  id: string
  eventType: string
  severity: string
  sourceIp: string
  description: string | null
  timestamp: string
}

export async function fetchDashboardOverview(): Promise<DashboardOverview> {
  const response = await fetch(`${API_BASE_URL}/api/dashboard/overview`)

  if (!response.ok) {
    throw new Error(`Dashboard overview failed: ${response.status} ${response.statusText}`)
  }

  return response.json()
}

export async function fetchSecurityEventSeries(
  from: Date,
  to: Date
): Promise<SecurityEventSeries[]> {
  const params = new URLSearchParams({
    from: from.toISOString(),
    to: to.toISOString(),
  })

  const response = await fetch(`${API_BASE_URL}/api/dashboard/security-events-series?${params}`)

  if (!response.ok) {
    throw new Error(`Security event series failed: ${response.status} ${response.statusText}`)
  }

  return response.json()
}

export async function fetchTopThreats(limit = 10): Promise<TopThreat[]> {
  const response = await fetch(`${API_BASE_URL}/api/dashboard/top-threats?limit=${limit}`)

  if (!response.ok) {
    throw new Error(`Top threats failed: ${response.status} ${response.statusText}`)
  }

  return response.json()
}

export async function fetchTopAttacks(limit = 10): Promise<AttackTypeSummary[]> {
  const response = await fetch(`${API_BASE_URL}/api/dashboard/top-attacks?limit=${limit}`)

  if (!response.ok) {
    throw new Error(`Top attacks failed: ${response.status} ${response.statusText}`)
  }

  return response.json()
}

export async function fetchRecentEvents(limit = 20): Promise<RecentEvent[]> {
  const response = await fetch(`${API_BASE_URL}/api/dashboard/recent-events?limit=${limit}`)

  if (!response.ok) {
    throw new Error(`Recent events failed: ${response.status} ${response.statusText}`)
  }

  return response.json()
}

export async function fetchTimeline(from: Date, to: Date, limit = 50): Promise<RecentEvent[]> {
  const params = new URLSearchParams({
    from: from.toISOString(),
    to: to.toISOString(),
    limit: limit.toString(),
  })

  const response = await fetch(`${API_BASE_URL}/api/dashboard/timeline?${params}`)

  if (!response.ok) {
    throw new Error(`Timeline failed: ${response.status} ${response.statusText}`)
  }

  return response.json()
}
