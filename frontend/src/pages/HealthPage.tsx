import { useEffect, useState } from 'react'
import { fetchHealth, type HealthCheckResult } from '../lib/api'

export function HealthPage() {
  const [health, setHealth] = useState<HealthCheckResult | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    fetchHealth()
      .then(setHealth)
      .catch((err) => setError(err instanceof Error ? err.message : 'Unknown error'))
      .finally(() => setLoading(false))
  }, [])

  return (
    <main className="card">
      <h2>Development Environment Status</h2>

      {loading && <p>Checking backend health...</p>}

      {error && (
        <div className="status error">
          <strong>Error:</strong> {error}
        </div>
      )}

      {health && (
        <>
          <div className={`status ${health.status.toLowerCase()}`}>
            <strong>Status:</strong> {health.status}
          </div>
          <ul className="service-list">
            <li>
              <span className="label">PostgreSQL:</span>
              <span className={health.postgresConnected ? 'ok' : 'fail'}>
                {health.postgresConnected ? 'Connected' : 'Disconnected'}
              </span>
            </li>
            <li>
              <span className="label">Redis:</span>
              <span className={health.redisConnected ? 'ok' : 'fail'}>
                {health.redisConnected ? 'Connected' : 'Disconnected'}
              </span>
            </li>
            <li>
              <span className="label">Timestamp:</span>
              <span>{new Date(health.timestamp).toLocaleString()}</span>
            </li>
          </ul>
        </>
      )}
    </main>
  )
}
