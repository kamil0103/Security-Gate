import { useEffect, useState } from 'react'
import { type AuditLog, type AuditCategory, searchAuditLogs } from '../lib/api'

const CATEGORIES: AuditCategory[] = [
  'Authentication',
  'Authorization',
  'AccessControl',
  'Blocking',
  'Application',
  'RateLimiting',
  'Waf',
  'ThreatDetection',
  'Notification',
  'System',
]

export function AuditPage() {
  const [result, setResult] = useState<{ total: number; logs: AuditLog[] } | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [category, setCategory] = useState<AuditCategory | ''>('')
  const [action, setAction] = useState('')
  const [username, setUsername] = useState('')
  const [skip, setSkip] = useState(0)
  const take = 50

  const loadLogs = async () => {
    setLoading(true)
    setError(null)
    try {
      const data = await searchAuditLogs({
        category: category || undefined,
        action: action || undefined,
        username: username || undefined,
        skip,
        take,
      })
      setResult({ total: data.total, logs: data.logs })
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load audit logs')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    loadLogs()
  }, [skip])

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault()
    setSkip(0)
    loadLogs()
  }

  return (
    <div className="page">
      <h2>Audit log</h2>

      {error && <div className="alert error">{error}</div>}

      <section className="card">
        <form onSubmit={handleSearch} className="form-row">
          <select
            value={category}
            onChange={(e) => setCategory(e.target.value as AuditCategory | '')}
          >
            <option value="">All categories</option>
            {CATEGORIES.map((c) => (
              <option key={c} value={c}>
                {c}
              </option>
            ))}
          </select>
          <input
            type="text"
            placeholder="Action"
            value={action}
            onChange={(e) => setAction(e.target.value)}
          />
          <input
            type="text"
            placeholder="Username"
            value={username}
            onChange={(e) => setUsername(e.target.value)}
          />
          <button type="submit" className="button primary">
            Search
          </button>
        </form>
      </section>

      <section className="card">
        {loading && <p>Loading...</p>}
        {!loading && (!result || result.logs.length === 0) && <p>No audit logs found.</p>}
        {!loading && result && result.logs.length > 0 && (
          <>
            <p className="subtitle">
              Showing {skip + 1}-{Math.min(skip + take, result.total)} of {result.total}
            </p>
            <table className="data-table">
              <thead>
                <tr>
                  <th>Time</th>
                  <th>Category</th>
                  <th>Action</th>
                  <th>User</th>
                  <th>IP</th>
                  <th>Success</th>
                  <th>Details</th>
                </tr>
              </thead>
              <tbody>
                {result.logs.map((log) => (
                  <tr key={log.id}>
                    <td>{new Date(log.timestamp).toLocaleString()}</td>
                    <td>{log.category}</td>
                    <td>{log.action}</td>
                    <td>{log.username || '-'}</td>
                    <td>{log.ipAddress || '-'}</td>
                    <td>
                      <span className={`badge ${log.success ? 'success' : 'danger'}`}>
                        {log.success ? 'Yes' : 'No'}
                      </span>
                    </td>
                    <td>{log.details || '-'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
            <div className="actions" style={{ marginTop: '1rem' }}>
              <button
                onClick={() => setSkip((s) => Math.max(0, s - take))}
                disabled={skip === 0}
                className="button secondary"
              >
                Previous
              </button>
              <button
                onClick={() => setSkip((s) => (result && s + take < result.total ? s + take : s))}
                disabled={!result || skip + take >= result.total}
                className="button secondary"
              >
                Next
              </button>
            </div>
          </>
        )}
      </section>
    </div>
  )
}
