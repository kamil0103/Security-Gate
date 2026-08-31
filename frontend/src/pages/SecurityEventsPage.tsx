import { useEffect, useState } from 'react'
import {
  type SecurityEvent,
  type SecurityEventType,
  type SecurityEventSeverity,
  fetchSecurityEvents,
} from '../lib/api'

const EVENT_TYPES: SecurityEventType[] = [
  'AuthenticationFailure',
  'AccountLocked',
  'RateLimitExceeded',
  'WafEvent',
  'AccessBlocked',
  'UnknownDevice',
  'NewDeviceFromUntrustedNetwork',
  'IpReputationChanged',
  'PolicyViolation',
  'Custom',
]

const SEVERITIES: SecurityEventSeverity[] = ['Info', 'Low', 'Medium', 'High', 'Critical']

const SEVERITY_CLASS: Record<SecurityEventSeverity, string> = {
  Info: 'muted',
  Low: 'info',
  Medium: 'warning',
  High: 'danger',
  Critical: 'danger',
}

export function SecurityEventsPage() {
  const [events, setEvents] = useState<SecurityEvent[]>([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [type, setType] = useState<SecurityEventType | ''>('')
  const [severity, setSeverity] = useState<SecurityEventSeverity | ''>('')
  const [sourceIp, setSourceIp] = useState('')

  const loadEvents = async () => {
    setLoading(true)
    setError(null)
    try {
      const data = await fetchSecurityEvents({
        type: type || undefined,
        severity: severity || undefined,
        sourceIp: sourceIp || undefined,
      })
      setEvents(data)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load security events')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    loadEvents()
  }, [])

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault()
    loadEvents()
  }

  return (
    <div className="page">
      <h2>Security events</h2>

      {error && <div className="alert error">{error}</div>}

      <section className="card">
        <form onSubmit={handleSearch} className="form-row">
          <select value={type} onChange={(e) => setType(e.target.value as SecurityEventType | '')}>
            <option value="">All types</option>
            {EVENT_TYPES.map((t) => (
              <option key={t} value={t}>
                {t}
              </option>
            ))}
          </select>
          <select
            value={severity}
            onChange={(e) => setSeverity(e.target.value as SecurityEventSeverity | '')}
          >
            <option value="">All severities</option>
            {SEVERITIES.map((s) => (
              <option key={s} value={s}>
                {s}
              </option>
            ))}
          </select>
          <input
            type="text"
            placeholder="Source IP"
            value={sourceIp}
            onChange={(e) => setSourceIp(e.target.value)}
          />
          <button type="submit" className="button primary">
            Search
          </button>
        </form>
      </section>

      <section className="card">
        {loading && <p>Loading...</p>}
        {!loading && events.length === 0 && <p>No security events found.</p>}
        {!loading && events.length > 0 && (
          <table className="data-table">
            <thead>
              <tr>
                <th>Time</th>
                <th>Type</th>
                <th>Severity</th>
                <th>Source IP</th>
                <th>Description</th>
              </tr>
            </thead>
            <tbody>
              {events.map((event) => (
                <tr key={event.id}>
                  <td>{new Date(event.timestamp).toLocaleString()}</td>
                  <td>{event.type}</td>
                  <td>
                    <span className={`badge ${SEVERITY_CLASS[event.severity]}`}>
                      {event.severity}
                    </span>
                  </td>
                  <td>{event.sourceIp}</td>
                  <td>{event.description || '-'}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </section>
    </div>
  )
}
