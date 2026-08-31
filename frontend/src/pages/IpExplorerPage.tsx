import { useState } from 'react'
import { fetchIpDetails, type IpDetails } from '../lib/api'

export function IpExplorerPage() {
  const [ip, setIp] = useState('')
  const [details, setDetails] = useState<IpDetails | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const handleSearch = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!ip.trim()) return

    setLoading(true)
    setError(null)
    setDetails(null)

    try {
      const data = await fetchIpDetails(ip.trim())
      setDetails(data)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load IP details')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="ip-explorer">
      <h2>IP Explorer</h2>

      <form onSubmit={handleSearch} className="ip-search-form">
        <input
          type="text"
          placeholder="Enter IP address (e.g., 8.8.8.8)"
          value={ip}
          onChange={(e) => setIp(e.target.value)}
        />
        <button type="submit" disabled={loading}>
          {loading ? 'Searching...' : 'Search'}
        </button>
      </form>

      {error && <div className="status error">{error}</div>}

      {details && (
        <div className="ip-details card">
          <h3>{details.ipAddress}</h3>

          <div className="details-grid">
            <div>
              <span className="label">Location</span>
              <p>
                {[details.city, details.region, details.country].filter(Boolean).join(', ') || '-'}
              </p>
            </div>
            <div>
              <span className="label">Coordinates</span>
              <p>
                {details.latitude != null && details.longitude != null
                  ? `${details.latitude.toFixed(4)}, ${details.longitude.toFixed(4)}`
                  : '-'}
              </p>
            </div>
            <div>
              <span className="label">ISP</span>
              <p>{details.isp ?? '-'}</p>
            </div>
            <div>
              <span className="label">Organization</span>
              <p>{details.organization ?? '-'}</p>
            </div>
            <div>
              <span className="label">ASN</span>
              <p>{details.asn ?? '-'}</p>
            </div>
            <div>
              <span className="label">Threat level</span>
              <p>{details.threatLevel ?? 'None'}</p>
            </div>
            <div>
              <span className="label">Threat score</span>
              <p>{details.threatScore}</p>
            </div>
            <div>
              <span className="label">Requests</span>
              <p>{details.requestCount}</p>
            </div>
            <div>
              <span className="label">Attacks</span>
              <p>{details.attackCount}</p>
            </div>
            <div>
              <span className="label">Blocks</span>
              <p>{details.blockCount}</p>
            </div>
          </div>

          <div className="flags">
            <span className={details.isVpn ? 'flag active' : 'flag'}>VPN</span>
            <span className={details.isProxy ? 'flag active' : 'flag'}>Proxy</span>
            <span className={details.isTor ? 'flag active' : 'flag'}>Tor</span>
            <span className={details.isDatacenter ? 'flag active' : 'flag'}>Datacenter</span>
          </div>
        </div>
      )}
    </div>
  )
}
