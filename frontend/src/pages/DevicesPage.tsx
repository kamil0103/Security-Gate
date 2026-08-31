import { useEffect, useState } from 'react'
import { type Device, fetchMyDevices, trustDevice, untrustDevice, blockDevice, removeDevice } from '../lib/api'

const STATUS_COLORS: Record<string, string> = {
  Pending: 'warning',
  Trusted: 'success',
  Untrusted: 'muted',
  Blocked: 'danger',
}

export function DevicesPage() {
  const [devices, setDevices] = useState<Device[]>([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const loadDevices = async () => {
    setLoading(true)
    setError(null)
    try {
      const data = await fetchMyDevices()
      setDevices(data)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load devices')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    loadDevices()
  }, [])

  const act = async (action: (id: string) => Promise<void>, deviceId: string) => {
    setError(null)
    try {
      await action(deviceId)
      await loadDevices()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Action failed')
    }
  }

  return (
    <div className="page">
      <h2>My devices</h2>

      {error && <div className="alert error">{error}</div>}

      <section className="card">
        {loading && <p>Loading...</p>}
        {!loading && devices.length === 0 && <p>No devices registered for your account.</p>}
        <ul className="list">
          {devices.map((device) => (
            <li key={device.id} className="list-item">
              <div className="row">
                <div className="details">
                  <strong>{device.name}</strong>
                  <span className="meta">
                    {device.browser} {device.operatingSystem}
                  </span>
                  <span className="meta">Last seen: {new Date(device.lastSeenAt).toLocaleString()}</span>
                  <span className={`badge ${STATUS_COLORS[device.trustStatus] ?? 'muted'}`}>
                    {device.trustStatus}
                  </span>
                </div>
                <div className="actions">
                  {device.trustStatus !== 'Trusted' && (
                    <button onClick={() => act(trustDevice, device.id)} className="button primary">
                      Trust
                    </button>
                  )}
                  {device.trustStatus !== 'Untrusted' && (
                    <button onClick={() => act(untrustDevice, device.id)} className="button secondary">
                      Untrust
                    </button>
                  )}
                  {device.trustStatus !== 'Blocked' && (
                    <button onClick={() => act(blockDevice, device.id)} className="button danger">
                      Block
                    </button>
                  )}
                  <button onClick={() => act(removeDevice, device.id)} className="button secondary">
                    Remove
                  </button>
                </div>
              </div>
            </li>
          ))}
        </ul>
      </section>
    </div>
  )
}
