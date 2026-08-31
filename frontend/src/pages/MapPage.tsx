import { useEffect, useRef, useState } from 'react'
import L from 'leaflet'
import 'leaflet/dist/leaflet.css'
import {
  fetchAttackPoints,
  fetchMapCountries,
  fetchMapPoints,
  type MapFilter,
  type MapPoint,
} from '../lib/api'

const attackIcon = new L.Icon({
  iconUrl: 'data:image/svg+xml;base64,PHN2ZyB3aWR0aD0iMjQiIGhlaWdodD0iMjQiIHZpZXdCb3g9IjAgMCAyNCAyNCIgZmlsbD0ibm9uZSIgeG1sbnM9Imh0dHA6Ly93d3cudzMub3JnLzIwMDAvc3ZnIj48Y2lyY2xlIGN4PSIxMiIgY3k9IjEyIiByPSIxMCIgZmlsbD0iI2VmNDQ0NCIgc3Ryb2tlPSIjZmZmIiBzdHJva2Utd2lkdGg9IjIiLz48L3N2Zz4=',
  iconSize: [20, 20],
  iconAnchor: [10, 10],
})

function getColorByThreat(score: number): string {
  if (score >= 80) return '#ef4444'
  if (score >= 50) return '#f97316'
  if (score >= 30) return '#eab308'
  return '#3b82f6'
}

export function MapPage() {
  const mapRef = useRef<HTMLDivElement>(null)
  const mapInstanceRef = useRef<L.Map | null>(null)
  const [points, setPoints] = useState<MapPoint[]>([])
  const [attacks, setAttacks] = useState<MapPoint[]>([])
  const [countries, setCountries] = useState<string[]>([])
  const [showAttacks, setShowAttacks] = useState(true)
  const [showThreats, setShowThreats] = useState(true)
  const [filters, setFilters] = useState<MapFilter>({
    from: new Date(Date.now() - 7 * 24 * 60 * 60 * 1000),
    to: new Date(),
  })
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    const load = async () => {
      try {
        const [pointData, attackData, countryData] = await Promise.all([
          fetchMapPoints({ ...filters, limit: 1000 }),
          fetchAttackPoints({ ...filters, limit: 1000 }),
          fetchMapCountries(),
        ])

        setPoints(pointData)
        setAttacks(attackData)
        setCountries(countryData)
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to load map data')
      } finally {
        setLoading(false)
      }
    }

    load()
  }, [filters])

  useEffect(() => {
    if (!mapRef.current || mapInstanceRef.current) return

    const map = L.map(mapRef.current).setView([20, 0], 2)
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      attribution: '&copy; OpenStreetMap contributors',
    }).addTo(map)

    mapInstanceRef.current = map

    return () => {
      map.remove()
      mapInstanceRef.current = null
    }
  }, [])

  useEffect(() => {
    const map = mapInstanceRef.current
    if (!map) return

    map.eachLayer((layer) => {
      if (layer instanceof L.Marker || layer instanceof L.CircleMarker) {
        map.removeLayer(layer)
      }
    })

    const markers: L.Layer[] = []

    if (showThreats) {
      points.forEach((point) => {
        const marker = L.circleMarker([point.latitude, point.longitude], {
          radius: 6,
          fillColor: getColorByThreat(point.threatScore),
          color: '#fff',
          weight: 1,
          opacity: 1,
          fillOpacity: 0.8,
        })
          .bindPopup(
            `<strong>${point.ipAddress}</strong><br/>` +
              `Threat score: ${point.threatScore}<br/>` +
              `Requests: ${point.requestCount}<br/>` +
              `Attacks: ${point.attackCount}<br/>` +
              `${[point.city, point.country].filter(Boolean).join(', ')}`
          )
          .addTo(map)

        markers.push(marker)
      })
    }

    if (showAttacks) {
      attacks.forEach((point) => {
        const marker = L.marker([point.latitude, point.longitude], { icon: attackIcon })
          .bindPopup(
            `<strong>${point.ipAddress}</strong><br/>` +
              `Attacks: ${point.attackCount}<br/>` +
              `Threat score: ${point.threatScore}<br/>` +
              `${[point.city, point.country].filter(Boolean).join(', ')}`
          )
          .addTo(map)

        markers.push(marker)
      })
    }

    return () => {
      markers.forEach((marker) => map.removeLayer(marker))
    }
  }, [points, attacks, showAttacks, showThreats])

  if (loading) return <p>Loading map...</p>
  if (error) return <div className="status error">{error}</div>

  return (
    <div className="map-page">
      <h2>Global Security Map</h2>

      <div className="map-controls">
        <label>
          Country
          <select
            value={filters.countryCode ?? ''}
            onChange={(e) =>
              setFilters((f) => ({
                ...f,
                countryCode: e.target.value || undefined,
              }))
            }
          >
            <option value="">All countries</option>
            {countries.map((country) => (
              <option key={country} value={country}>
                {country}
              </option>
            ))}
          </select>
        </label>

        <label>
          Min threat score
          <input
            type="number"
            min={0}
            max={100}
            value={filters.minThreatScore ?? ''}
            onChange={(e) =>
              setFilters((f) => ({
                ...f,
                minThreatScore: e.target.value ? parseInt(e.target.value, 10) : undefined,
              }))
            }
          />
        </label>

        <label className="checkbox-label">
          <input
            type="checkbox"
            checked={showThreats}
            onChange={(e) => setShowThreats(e.target.checked)}
          />
          Show threats
        </label>

        <label className="checkbox-label">
          <input
            type="checkbox"
            checked={showAttacks}
            onChange={(e) => setShowAttacks(e.target.checked)}
          />
          Show attacks
        </label>
      </div>

      <div className="map-stats">
        <div className="stat-card">
          <div className="stat-value">{points.length}</div>
          <div className="stat-label">Threats on map</div>
        </div>
        <div className="stat-card">
          <div className="stat-value">{attacks.length}</div>
          <div className="stat-label">Attacks on map</div>
        </div>
      </div>

      <div ref={mapRef} className="map-container" />
    </div>
  )
}
