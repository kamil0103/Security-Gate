import { useEffect, useState } from 'react'
import { fetchPendingAccessRequests, resolveAccessRequest, type AccessRequest, type ResolveAccessRequestRequest } from '../lib/api'

export function ApprovalsPage() {
  const [requests, setRequests] = useState<AccessRequest[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [actionReason, setActionReason] = useState('')
  const [showAuthenticationRequired, setShowAuthenticationRequired] = useState(false)
  const [isResolving, setIsResolving] = useState<string | null>(null)
  const [selectedScope, setSelectedScope] = useState<string>('Session')

  const load = async (showSpinner = false) => {
    try {
      if (showSpinner) setIsLoading(true)
      setError(null)
      const data = await fetchPendingAccessRequests()
      setRequests(data)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load')
    } finally {
      setIsLoading(false)
    }
  }

  useEffect(() => {
    void load(true)
    const interval = setInterval(() => {
      if (!document.hidden) void load()
    }, 30000)
    return () => clearInterval(interval)
  }, [])

  const handleAction = async (
    request: AccessRequest,
    decision: 'Approve' | 'Deny' | 'BlockIp' | 'BlockDevice'
  ) => {
    if (decision === 'Approve' && !request.userId) {
      setError('This visitor must sign in before their request can be approved.')
      return
    }
    setError(null)
    setIsResolving(request.id)
    try {
      await resolveAccessRequest(request.id, {
        decision,
        approvalScope: decision === 'Approve' ? (selectedScope as ResolveAccessRequestRequest['approvalScope']) : undefined,
        reason: actionReason || undefined,
      })
      await load()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Action failed')
    } finally {
      setIsResolving(null)
    }
  }

  const renderDeviceSummary = (r: AccessRequest) => {
    const parts = [r.browser, r.operatingSystem].filter(Boolean)
    return parts.length > 0 ? parts.join(' / ') : r.userAgent?.slice(0, 60) ?? 'Unknown'
  }

  const ready = requests.filter((r) => Boolean(r.userId))
  const authenticationRequired = requests.filter((r) => !r.userId)
  const visibleRequests = showAuthenticationRequired ? authenticationRequired : ready

  return (
    <div className="approvals-page">
      <h2>Pending Access Approvals</h2>
      <div className="approval-actions">
        <button className={!showAuthenticationRequired ? 'button primary' : 'button secondary'} onClick={() => setShowAuthenticationRequired(false)}>Needs approval ({ready.length})</button>
        <button className={showAuthenticationRequired ? 'button primary' : 'button secondary'} onClick={() => setShowAuthenticationRequired(true)}>Authentication required ({authenticationRequired.length})</button>
        <button className="button secondary" onClick={() => void load(true)}>Refresh</button>
      </div>
      {error && <div className="status error">{error}</div>}

      <div className="approval-scope">
        <label htmlFor="scope">Default approval scope:</label>
        <select
          id="scope"
          value={selectedScope}
          onChange={(e) => setSelectedScope(e.target.value)}
        >
          <option value="Once">Once</option>
          <option value="Session">Session</option>
          <option value="Device">Device</option>
          <option value="IpAndDevice">IP + Device</option>
          <option value="Ip">IP</option>
          <option value="Permanent">Permanent</option>
        </select>
      </div>

      <div className="form-group">
        <label htmlFor="reason">Reason (optional):</label>
        <input
          id="reason"
          type="text"
          value={actionReason}
          onChange={(e) => setActionReason(e.target.value)}
          placeholder="Reason for decision"
        />
      </div>

      {isLoading && visibleRequests.length === 0 ? (
        <p>Loading...</p>
      ) : visibleRequests.length === 0 ? (
        <p>{showAuthenticationRequired ? 'No visitors awaiting authentication.' : 'No authenticated requests awaiting approval.'}</p>
      ) : (
        <div className="approvals-list">
          {visibleRequests.map((request) => (
            <div key={request.id} className="approval-card">
              <div className="approval-header">
                <strong>{request.applicationName}</strong>
                <span className="muted">{request.applicationDomain}</span>
                <span className="request-id">{request.publicId}</span>
              </div>
              <div className="approval-body">
                <p>
                  <span className="label">IP:</span> {request.clientIp}
                </p>
                <p>
                  <span className="label">Location:</span>{' '}
                  {[request.city, request.region, request.country].filter(Boolean).join(', ') || 'Unknown'}
                </p>
                <p>
                  <span className="label">ISP/ASN:</span> {request.isp ?? 'Unknown'} {request.asn ? `(${request.asn})` : ''}
                </p>
                <p>
                  <span className="label">Device:</span> {renderDeviceSummary(request)}
                </p>
                {request.username && (
                  <p>
                    <span className="label">User:</span> {request.username}
                  </p>
                )}
                <p>
                  <span className="label">Threat:</span>{' '}
                  {request.threatLevel ?? 'Unknown'} ({request.threatScore})
                </p>
                <p>
                  <span className="label">Reason for challenge:</span> {request.reasonForChallenge}
                </p>
                <p>
                  <span className="label">Requests:</span> {request.requestCount}
                </p>
              </div>
              <div className="approval-actions">
                <button
                  className="button primary"
                  onClick={() => handleAction(request, 'Approve')}
                  disabled={!request.userId || isResolving !== null}
                  title={!request.userId ? 'Visitor must authenticate first' : undefined}
                >
                  Approve
                </button>
                <button
                  className="button secondary"
                  onClick={() => handleAction(request, 'Deny')}
                  disabled={isResolving !== null}
                >
                  Deny
                </button>
                <button
                  className="button secondary"
                  onClick={() => handleAction(request, 'BlockIp')}
                  disabled={isResolving !== null}
                >
                  Block IP
                </button>
                <button
                  className="button secondary"
                  onClick={() => handleAction(request, 'BlockDevice')}
                  disabled={isResolving !== null}
                >
                  Block Device
                </button>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}
