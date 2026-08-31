import { useEffect, useState } from 'react'
import { fetchPendingAccessRequests, resolveAccessRequest, type AccessRequest, type ResolveAccessRequestRequest } from '../lib/api'

export function ApprovalsPage() {
  const [requests, setRequests] = useState<AccessRequest[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [actionReason, setActionReason] = useState('')
  const [selectedScope, setSelectedScope] = useState<string>('Session')

  const load = async () => {
    try {
      setIsLoading(true)
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
    load()
    const interval = setInterval(load, 5000)
    return () => clearInterval(interval)
  }, [])

  const handleAction = async (
    request: AccessRequest,
    decision: 'Approve' | 'Deny' | 'BlockIp' | 'BlockDevice'
  ) => {
    setError(null)
    try {
      await resolveAccessRequest(request.id, {
        decision,
        approvalScope: decision === 'Approve' ? (selectedScope as ResolveAccessRequestRequest['approvalScope']) : undefined,
        reason: actionReason || undefined,
      })
      await load()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Action failed')
    }
  }

  const renderDeviceSummary = (r: AccessRequest) => {
    const parts = [r.browser, r.operatingSystem].filter(Boolean)
    return parts.length > 0 ? parts.join(' / ') : r.userAgent?.slice(0, 60) ?? 'Unknown'
  }

  return (
    <div className="approvals-page">
      <h2>Pending Access Approvals</h2>
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

      {isLoading && requests.length === 0 ? (
        <p>Loading...</p>
      ) : requests.length === 0 ? (
        <p>No pending access requests.</p>
      ) : (
        <div className="approvals-list">
          {requests.map((request) => (
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
                >
                  Approve
                </button>
                <button
                  className="button secondary"
                  onClick={() => handleAction(request, 'Deny')}
                >
                  Deny
                </button>
                <button
                  className="button secondary"
                  onClick={() => handleAction(request, 'BlockIp')}
                >
                  Block IP
                </button>
                <button
                  className="button secondary"
                  onClick={() => handleAction(request, 'BlockDevice')}
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
