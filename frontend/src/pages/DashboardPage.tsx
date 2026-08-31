import { useEffect, useState } from 'react'
import {
  Bar,
  BarChart,
  CartesianGrid,
  Cell,
  Legend,
  Pie,
  PieChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts'
import {
  fetchDashboardOverview,
  fetchRecentEvents,
  fetchSecurityEventSeries,
  fetchTimeline,
  fetchTopAttacks,
  fetchTopThreats,
  type AttackTypeSummary,
  type DashboardOverview,
  type RecentEvent,
  type SecurityEventSeries,
  type TopThreat,
} from '../lib/api'

const severityColors: Record<string, string> = {
  Critical: '#ef4444',
  High: '#f97316',
  Medium: '#eab308',
  Low: '#22c55e',
  Info: '#3b82f6',
}

const eventColors = ['#ef4444', '#f97316', '#eab308', '#22c55e', '#3b82f6', '#a855f7']

function formatNumber(value: number): string {
  return new Intl.NumberFormat().format(value)
}

function formatTime(value: string): string {
  return new Date(value).toLocaleTimeString()
}

export function DashboardPage() {
  const [overview, setOverview] = useState<DashboardOverview | null>(null)
  const [series, setSeries] = useState<SecurityEventSeries[]>([])
  const [threats, setThreats] = useState<TopThreat[]>([])
  const [attacks, setAttacks] = useState<AttackTypeSummary[]>([])
  const [recentEvents, setRecentEvents] = useState<RecentEvent[]>([])
  const [timeline, setTimeline] = useState<RecentEvent[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    const load = async () => {
      try {
        const from = new Date(Date.now() - 7 * 24 * 60 * 60 * 1000)
        const to = new Date()

        const [overviewData, seriesData, threatsData, attacksData, recentData, timelineData] =
          await Promise.all([
            fetchDashboardOverview(),
            fetchSecurityEventSeries(from, to),
            fetchTopThreats(10),
            fetchTopAttacks(10),
            fetchRecentEvents(20),
            fetchTimeline(from, to, 50),
          ])

        setOverview(overviewData)
        setSeries(seriesData)
        setThreats(threatsData)
        setAttacks(attacksData)
        setRecentEvents(recentData)
        setTimeline(timelineData)
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to load dashboard')
      } finally {
        setLoading(false)
      }
    }

    load()
  }, [])

  if (loading) return <p>Loading dashboard...</p>
  if (error) return <div className="status error">{error}</div>
  if (!overview) return null

  const chartData = series.flatMap((s) =>
    s.points.map((p) => ({
      date: new Date(p.timestamp).toLocaleDateString(),
      [s.severity]: p.count,
    }))
  )

  const mergedChartData = chartData.reduce<Record<string, Record<string, number>>>((acc, row) => {
    const key = row.date
    if (!acc[key]) acc[key] = {}

    Object.entries(row).forEach(([k, v]) => {
      if (k !== 'date') {
        acc[key][k] = (acc[key][k] ?? 0) + (v as number)
      }
    })

    return acc
  }, {})

  const timelineChartData = Object.entries(
    timeline.reduce<Record<string, number>>((acc, event) => {
      const hour = new Date(event.timestamp).toLocaleString(undefined, {
        month: 'short',
        day: 'numeric',
        hour: '2-digit',
      })
      acc[hour] = (acc[hour] ?? 0) + 1
      return acc
    }, {})
  ).map(([hour, count]) => ({ hour, count }))

  return (
    <div className="dashboard">
      <h2>Security Dashboard</h2>

      <section className="stats-grid">
        <div className="stat-card">
          <div className="stat-value">{formatNumber(overview.totalRequests)}</div>
          <div className="stat-label">Total Requests</div>
        </div>
        <div className="stat-card">
          <div className="stat-value">{formatNumber(overview.blockedRequests)}</div>
          <div className="stat-label">Blocked Requests</div>
        </div>
        <div className="stat-card">
          <div className="stat-value">{formatNumber(overview.activeBlocks)}</div>
          <div className="stat-label">Active Blocks</div>
        </div>
        <div className="stat-card">
          <div className="stat-value">{formatNumber(overview.securityEventsToday)}</div>
          <div className="stat-label">Security Events Today</div>
        </div>
        <div className="stat-card">
          <div className="stat-value">{formatNumber(overview.wafEventsToday)}</div>
          <div className="stat-label">WAF Events Today</div>
        </div>
        <div className="stat-card">
          <div className="stat-value">{formatNumber(overview.rateLimitHitsToday)}</div>
          <div className="stat-label">Rate Limit Hits Today</div>
        </div>
      </section>

      <section className="charts-grid">
        <div className="chart-card">
          <h3>Security Events (Last 7 Days)</h3>
          <ResponsiveContainer width="100%" height={300}>
            <BarChart data={Object.entries(mergedChartData).map(([date, values]) => ({ date, ...values }))}>
              <CartesianGrid strokeDasharray="3 3" />
              <XAxis dataKey="date" />
              <YAxis />
              <Tooltip />
              <Legend />
              {series.map((s) => (
                <Bar key={s.severity} dataKey={s.severity} fill={severityColors[s.severity] ?? '#94a3b8'} />
              ))}
            </BarChart>
          </ResponsiveContainer>
        </div>

        <div className="chart-card">
          <h3>Top Attack Types</h3>
          <ResponsiveContainer width="100%" height={300}>
            <PieChart>
              <Pie
                data={attacks}
                dataKey="count"
                nameKey="type"
                cx="50%"
                cy="50%"
                outerRadius={100}
                label
              >
                {attacks.map((_, index) => (
                  <Cell key={`cell-${index}`} fill={eventColors[index % eventColors.length]} />
                ))}
              </Pie>
              <Tooltip />
              <Legend />
            </PieChart>
          </ResponsiveContainer>
        </div>
      </section>

      <section className="charts-grid">
        <div className="chart-card">
          <h3>Security Timeline (Last 24 Hours)</h3>
          <ResponsiveContainer width="100%" height={250}>
            <BarChart data={timelineChartData}>
              <CartesianGrid strokeDasharray="3 3" />
              <XAxis dataKey="hour" />
              <YAxis />
              <Tooltip />
              <Bar dataKey="count" fill="#3b82f6" />
            </BarChart>
          </ResponsiveContainer>
        </div>

        <div className="chart-card">
          <h3>Top Threats</h3>
          <table className="data-table">
            <thead>
              <tr>
                <th>IP Address</th>
                <th>Threat Score</th>
                <th>Requests</th>
                <th>Attacks</th>
              </tr>
            </thead>
            <tbody>
              {threats.map((threat) => (
                <tr key={threat.ipAddress}>
                  <td>{threat.ipAddress}</td>
                  <td>{threat.threatScore}</td>
                  <td>{formatNumber(threat.requestCount)}</td>
                  <td>{formatNumber(threat.attackCount)}</td>
                </tr>
              ))}
              {threats.length === 0 && (
                <tr>
                  <td colSpan={4}>No threats detected</td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </section>

      <section className="chart-card">
        <h3>Real-Time Event Feed</h3>
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
            {recentEvents.map((event) => (
              <tr key={event.id}>
                <td>{formatTime(event.timestamp)}</td>
                <td>{event.eventType}</td>
                <td>
                  <span
                    className="severity-badge"
                    style={{ backgroundColor: severityColors[event.severity] ?? '#94a3b8' }}
                  >
                    {event.severity}
                  </span>
                </td>
                <td>{event.sourceIp}</td>
                <td>{event.description ?? '-'}</td>
              </tr>
            ))}
            {recentEvents.length === 0 && (
              <tr>
                <td colSpan={5}>No recent events</td>
              </tr>
            )}
          </tbody>
        </table>
      </section>
    </div>
  )
}
