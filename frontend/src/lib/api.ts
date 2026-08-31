import { authFetch } from './auth'

const API_BASE_URL = import.meta.env.VITE_API_URL ?? ''

export interface HealthCheckResult {
  status: string
  postgresConnected: boolean
  redisConnected: boolean
  timestamp: string
}

export async function fetchHealth(): Promise<HealthCheckResult> {
  const response = await authFetch(`${API_BASE_URL}/api/health`)

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
  const response = await authFetch(`${API_BASE_URL}/api/dashboard/overview`)

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

  const response = await authFetch(`${API_BASE_URL}/api/dashboard/security-events-series?${params}`)

  if (!response.ok) {
    throw new Error(`Security event series failed: ${response.status} ${response.statusText}`)
  }

  return response.json()
}

export async function fetchTopThreats(limit = 10): Promise<TopThreat[]> {
  const response = await authFetch(`${API_BASE_URL}/api/dashboard/top-threats?limit=${limit}`)

  if (!response.ok) {
    throw new Error(`Top threats failed: ${response.status} ${response.statusText}`)
  }

  return response.json()
}

export async function fetchTopAttacks(limit = 10): Promise<AttackTypeSummary[]> {
  const response = await authFetch(`${API_BASE_URL}/api/dashboard/top-attacks?limit=${limit}`)

  if (!response.ok) {
    throw new Error(`Top attacks failed: ${response.status} ${response.statusText}`)
  }

  return response.json()
}

export async function fetchRecentEvents(limit = 20): Promise<RecentEvent[]> {
  const response = await authFetch(`${API_BASE_URL}/api/dashboard/recent-events?limit=${limit}`)

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

  const response = await authFetch(`${API_BASE_URL}/api/dashboard/timeline?${params}`)

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

  const response = await authFetch(`${API_BASE_URL}/api/map/points?${params}`)

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

  const response = await authFetch(`${API_BASE_URL}/api/map/attacks?${params}`)

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
  const response = await authFetch(`${API_BASE_URL}/api/map/ip/${encodeURIComponent(ipAddress)}`)

  if (!response.ok) {
    throw new Error(`IP details failed: ${response.status} ${response.statusText}`)
  }

  return response.json()
}

export async function fetchMapCountries(): Promise<string[]> {
  const response = await authFetch(`${API_BASE_URL}/api/map/countries`)

  if (!response.ok) {
    throw new Error(`Countries failed: ${response.status} ${response.statusText}`)
  }

  return response.json()
}

export interface AccessRequest {
  id: string
  publicId: string
  status: string
  createdAt: string
  expiresAt: string
  resolvedAt?: string
  resolutionReason?: string
  applicationId: string
  applicationName: string
  applicationDomain: string
  httpMethod: string
  requestedPath: string
  clientIp: string
  country?: string
  countryCode?: string
  region?: string
  city?: string
  isp?: string
  asn?: string
  isVpn: boolean
  isProxy: boolean
  isTor: boolean
  isDatacenter: boolean
  threatScore: number
  threatLevel?: string
  requestCount: number
  deviceFingerprint?: string
  deviceName?: string
  deviceId?: string
  sessionId?: string
  userAgent?: string
  browser?: string
  operatingSystem?: string
  userId?: string
  username?: string
  reasonForChallenge: string
  reviewedByUserId?: string
  reviewedByUsername?: string
  decision?: string
  approvalScope?: string
}

export async function fetchPendingAccessRequests(): Promise<AccessRequest[]> {
  const response = await authFetch(`${API_BASE_URL}/api/access-requests/pending`)

  if (!response.ok) {
    throw new Error(`Failed to load pending access requests: ${response.status}`)
  }

  return response.json()
}

export interface ResolveAccessRequestRequest {
  decision: 'Approve' | 'Deny' | 'BlockIp' | 'BlockDevice'
  approvalScope?: 'Once' | 'Session' | 'Device' | 'IpAndDevice' | 'Ip' | 'Permanent'
  reason?: string
}

export async function resolveAccessRequest(
  id: string,
  request: ResolveAccessRequestRequest
): Promise<AccessRequest> {
  const response = await authFetch(`${API_BASE_URL}/api/access-requests/${id}/resolve`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request),
  })

  if (!response.ok) {
    throw new Error(`Failed to resolve access request: ${response.status}`)
  }

  return response.json()
}

export interface Application {
  id: string
  name: string
  domain: string
  upstreamUrl: string
  isEnabled: boolean
  createdAt: string
  policy?: ApplicationPolicy
}

export interface ApplicationPolicy {
  id: string
  applicationId: string
  requireAuthentication: boolean
  allowAnonymousFromTrustedNetworks: boolean
  allowedCountries: string
  blockedCountries: string
  allowedIpAddresses: string
  blockedIpAddresses: string
  allowedCloudflareCountries: string
  blockedCloudflareCountries: string
  bypassAuthenticationPaths: string
}

export interface CreateApplicationRequest {
  name: string
  domain: string
  upstreamUrl: string
}

export interface UpdateApplicationRequest {
  name: string
  domain: string
  upstreamUrl: string
  isEnabled: boolean
}

export async function fetchApplications(): Promise<Application[]> {
  const response = await authFetch(`${API_BASE_URL}/api/applications`)

  if (!response.ok) {
    throw new Error(`Failed to load applications: ${response.status}`)
  }

  return response.json()
}

export async function createApplication(request: CreateApplicationRequest): Promise<Application> {
  const response = await authFetch(`${API_BASE_URL}/api/applications`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request),
  })

  if (!response.ok) {
    throw new Error(`Failed to create application: ${response.status}`)
  }

  return response.json()
}

export async function updateApplication(
  id: string,
  request: UpdateApplicationRequest
): Promise<Application> {
  const response = await authFetch(`${API_BASE_URL}/api/applications/${id}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request),
  })

  if (!response.ok) {
    throw new Error(`Failed to update application: ${response.status}`)
  }

  return response.json()
}

export async function deleteApplication(id: string): Promise<void> {
  const response = await authFetch(`${API_BASE_URL}/api/applications/${id}`, {
    method: 'DELETE',
  })

  if (!response.ok) {
    throw new Error(`Failed to delete application: ${response.status}`)
  }
}

export async function fetchApplicationPolicy(applicationId: string): Promise<ApplicationPolicy | null> {
  const response = await authFetch(`${API_BASE_URL}/api/applications/${applicationId}/policy`)

  if (response.status === 404) {
    return null
  }

  if (!response.ok) {
    throw new Error(`Failed to load policy: ${response.status}`)
  }

  return response.json()
}

export async function updateApplicationPolicy(
  applicationId: string,
  request: Omit<ApplicationPolicy, 'id' | 'applicationId'>
): Promise<ApplicationPolicy> {
  const response = await authFetch(`${API_BASE_URL}/api/applications/${applicationId}/policy`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request),
  })

  if (!response.ok) {
    throw new Error(`Failed to update policy: ${response.status}`)
  }

  return response.json()
}

export interface TrustedNetwork {
  id: string
  name: string
  cidr: string
  description?: string
  isEnabled: boolean
  createdAt: string
}

export interface CreateTrustedNetworkRequest {
  name: string
  cidr: string
  description?: string
}

export async function fetchTrustedNetworks(): Promise<TrustedNetwork[]> {
  const response = await authFetch(`${API_BASE_URL}/api/access-control/trusted-networks`)

  if (!response.ok) {
    throw new Error(`Failed to load trusted networks: ${response.status}`)
  }

  return response.json()
}

export async function createTrustedNetwork(request: CreateTrustedNetworkRequest): Promise<TrustedNetwork> {
  const response = await authFetch(`${API_BASE_URL}/api/access-control/trusted-networks`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request),
  })

  if (!response.ok) {
    throw new Error(`Failed to create trusted network: ${response.status}`)
  }

  return response.json()
}

export async function updateTrustedNetwork(
  id: string,
  request: CreateTrustedNetworkRequest & { isEnabled: boolean }
): Promise<TrustedNetwork> {
  const response = await authFetch(`${API_BASE_URL}/api/access-control/trusted-networks/${id}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request),
  })

  if (!response.ok) {
    throw new Error(`Failed to update trusted network: ${response.status}`)
  }

  return response.json()
}

export async function deleteTrustedNetwork(id: string): Promise<void> {
  const response = await authFetch(`${API_BASE_URL}/api/access-control/trusted-networks/${id}`, {
    method: 'DELETE',
  })

  if (!response.ok) {
    throw new Error(`Failed to delete trusted network: ${response.status}`)
  }
}

export interface Device {
  id: string
  userId: string
  name: string
  fingerprint: string
  userAgent?: string
  operatingSystem?: string
  browser?: string
  trustStatus: 'Pending' | 'Trusted' | 'Untrusted' | 'Blocked'
  createdAt: string
  lastSeenAt: string
}

export async function fetchMyDevices(): Promise<Device[]> {
  const response = await authFetch(`${API_BASE_URL}/api/devices`)

  if (!response.ok) {
    throw new Error(`Failed to load devices: ${response.status}`)
  }

  return response.json()
}

export async function trustDevice(deviceId: string): Promise<void> {
  const response = await authFetch(`${API_BASE_URL}/api/devices/${deviceId}/trust`, {
    method: 'POST',
  })

  if (!response.ok) {
    throw new Error(`Failed to trust device: ${response.status}`)
  }
}

export async function untrustDevice(deviceId: string): Promise<void> {
  const response = await authFetch(`${API_BASE_URL}/api/devices/${deviceId}/untrust`, {
    method: 'POST',
  })

  if (!response.ok) {
    throw new Error(`Failed to untrust device: ${response.status}`)
  }
}

export async function blockDevice(deviceId: string): Promise<void> {
  const response = await authFetch(`${API_BASE_URL}/api/devices/${deviceId}/block`, {
    method: 'POST',
  })

  if (!response.ok) {
    throw new Error(`Failed to block device: ${response.status}`)
  }
}

export async function removeDevice(deviceId: string): Promise<void> {
  const response = await authFetch(`${API_BASE_URL}/api/devices/${deviceId}`, {
    method: 'DELETE',
  })

  if (!response.ok) {
    throw new Error(`Failed to remove device: ${response.status}`)
  }
}

export type NotificationChannelType = 'Email' | 'Telegram' | 'Discord' | 'Ntfy' | 'WebPush'

export interface NotificationChannel {
  id: string
  name: string
  type: NotificationChannelType
  isEnabled: boolean
  configuration: string
}

export interface CreateNotificationChannelRequest {
  name: string
  type: NotificationChannelType
  isEnabled: boolean
  configuration: string
}

export async function fetchNotificationChannels(): Promise<NotificationChannel[]> {
  const response = await authFetch(`${API_BASE_URL}/api/notifications/channels`)

  if (!response.ok) {
    throw new Error(`Failed to load notification channels: ${response.status}`)
  }

  return response.json()
}

export async function createNotificationChannel(
  request: CreateNotificationChannelRequest
): Promise<NotificationChannel> {
  const response = await authFetch(`${API_BASE_URL}/api/notifications/channels`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request),
  })

  if (!response.ok) {
    throw new Error(`Failed to create notification channel: ${response.status}`)
  }

  return response.json()
}

export async function updateNotificationChannel(
  id: string,
  request: CreateNotificationChannelRequest
): Promise<NotificationChannel> {
  const response = await authFetch(`${API_BASE_URL}/api/notifications/channels/${id}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request),
  })

  if (!response.ok) {
    throw new Error(`Failed to update notification channel: ${response.status}`)
  }

  return response.json()
}

export async function deleteNotificationChannel(id: string): Promise<void> {
  const response = await authFetch(`${API_BASE_URL}/api/notifications/channels/${id}`, {
    method: 'DELETE',
  })

  if (!response.ok) {
    throw new Error(`Failed to delete notification channel: ${response.status}`)
  }
}

export async function testNotificationChannel(
  id: string,
  subject: string,
  body: string
): Promise<void> {
  const response = await authFetch(`${API_BASE_URL}/api/notifications/channels/${id}/test`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ subject, body }),
  })

  if (!response.ok) {
    throw new Error(`Failed to send test notification: ${response.status}`)
  }
}

export type SecurityEventType =
  | 'AuthenticationFailure'
  | 'AccountLocked'
  | 'RateLimitExceeded'
  | 'WafEvent'
  | 'AccessBlocked'
  | 'UnknownDevice'
  | 'NewDeviceFromUntrustedNetwork'
  | 'IpReputationChanged'
  | 'PolicyViolation'
  | 'Custom'

export type SecurityEventSeverity = 'Info' | 'Low' | 'Medium' | 'High' | 'Critical'

export interface SecurityEvent {
  id: string
  timestamp: string
  type: SecurityEventType
  severity: SecurityEventSeverity
  sourceIp: string
  userId?: string
  deviceId?: string
  description?: string
  relatedEntityType?: string
  relatedEntityId?: string
  createdAt: string
}

export interface SecurityEventFilter {
  type?: SecurityEventType
  severity?: SecurityEventSeverity
  sourceIp?: string
  from?: string
  to?: string
  skip?: number
  take?: number
}

export async function fetchSecurityEvents(filter: SecurityEventFilter = {}): Promise<SecurityEvent[]> {
  const params = new URLSearchParams()
  if (filter.type) params.set('Type', filter.type)
  if (filter.severity) params.set('Severity', filter.severity)
  if (filter.sourceIp) params.set('SourceIp', filter.sourceIp)
  if (filter.from) params.set('From', filter.from)
  if (filter.to) params.set('To', filter.to)
  if (filter.skip !== undefined) params.set('Skip', filter.skip.toString())
  params.set('Take', (filter.take ?? 50).toString())

  const response = await authFetch(`${API_BASE_URL}/api/securityevents?${params}`)

  if (!response.ok) {
    throw new Error(`Failed to load security events: ${response.status}`)
  }

  return response.json()
}

export interface BlockResult {
  blocked: boolean
  ipAddress: string
  expiresAt?: string
  reason?: string
}

export async function blockIp(
  ipAddress: string,
  durationMinutes?: number,
  reason?: string
): Promise<BlockResult> {
  const response = await authFetch(`${API_BASE_URL}/api/blocking/block`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ ipAddress, durationMinutes, reason }),
  })

  if (!response.ok) {
    throw new Error(`Failed to block IP: ${response.status}`)
  }

  return response.json()
}

export async function unblockIp(ipAddress: string): Promise<void> {
  const response = await authFetch(`${API_BASE_URL}/api/blocking/unblock`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ ipAddress }),
  })

  if (!response.ok) {
    throw new Error(`Failed to unblock IP: ${response.status}`)
  }
}

export async function isIpBlocked(ipAddress: string): Promise<{ ipAddress: string; isBlocked: boolean }> {
  const response = await authFetch(
    `${API_BASE_URL}/api/blocking/is-blocked?ipAddress=${encodeURIComponent(ipAddress)}`
  )

  if (!response.ok) {
    throw new Error(`Failed to check block status: ${response.status}`)
  }

  return response.json()
}
