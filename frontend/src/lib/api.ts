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

export interface MapPoint {
  ipAddress: string
  latitude: number
  longitude: number
  country: string | null
  countryCode: string | null
  city: string | null
  threatScore: number
  requestCount: number
  attackCount: number
  lastSeenAt: string
}

export interface MapFilter {
  from?: Date
  to?: Date
  countryCode?: string
  minThreatScore?: number
  hasAttacks?: boolean
  isBlocked?: boolean
  limit?: number
}

export async function fetchMapPoints(filter: MapFilter = {}): Promise<MapPoint[]> {
  const params = new URLSearchParams()

  if (filter.from) params.set('from', filter.from.toISOString())
  if (filter.to) params.set('to', filter.to.toISOString())
  if (filter.countryCode) params.set('countryCode', filter.countryCode)
  if (filter.minThreatScore !== undefined) params.set('minThreatScore', filter.minThreatScore.toString())
  if (filter.hasAttacks !== undefined) params.set('hasAttacks', filter.hasAttacks.toString())
  if (filter.isBlocked !== undefined) params.set('isBlocked', filter.isBlocked.toString())
  if (filter.limit !== undefined) params.set('limit', filter.limit.toString())

  const response = await fetch(`${API_BASE_URL}/api/map/points?${params}`)

  if (!response.ok) {
    throw new Error(`Map points failed: ${response.status} ${response.statusText}`)
  }

  return response.json()
}

export async function fetchAttackPoints(filter: MapFilter = {}): Promise<MapPoint[]> {
  const params = new URLSearchParams()

  if (filter.from) params.set('from', filter.from.toISOString())
  if (filter.to) params.set('to', filter.to.toISOString())
  if (filter.countryCode) params.set('countryCode', filter.countryCode)
  if (filter.minThreatScore !== undefined) params.set('minThreatScore', filter.minThreatScore.toString())
  if (filter.isBlocked !== undefined) params.set('isBlocked', filter.isBlocked.toString())
  if (filter.limit !== undefined) params.set('limit', filter.limit.toString())

  const response = await fetch(`${API_BASE_URL}/api/map/attacks?${params}`)

  if (!response.ok) {
    throw new Error(`Attack points failed: ${response.status} ${response.statusText}`)
  }

  return response.json()
}

export interface IpDetails {
  ipAddress: string
  country: string | null
  countryCode: string | null
  region: string | null
  city: string | null
  latitude: number | null
  longitude: number | null
  isp: string | null
  organization: string | null
  asn: string | null
  isVpn: boolean
  isProxy: boolean
  isTor: boolean
  isDatacenter: boolean
  threatScore: number
  threatLevel: string | null
  requestCount: number
  attackCount: number
  blockCount: number
  firstSeenAt: string
  lastSeenAt: string
}

export async function fetchIpDetails(ipAddress: string): Promise<IpDetails> {
  const response = await fetch(`${API_BASE_URL}/api/map/ip/${encodeURIComponent(ipAddress)}`)

  if (!response.ok) {
    throw new Error(`IP details failed: ${response.status} ${response.statusText}`)
  }

  return response.json()
}

export async function fetchMapCountries(): Promise<string[]> {
  const response = await fetch(`${API_BASE_URL}/api/map/countries`)

  if (!response.ok) {
    throw new Error(`Countries failed: ${response.status} ${response.statusText}`)
  }

  return response.json()
}
