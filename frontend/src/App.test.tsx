import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import App from './App'

const mockHealth = {
  status: 'Healthy',
  postgresConnected: true,
  redisConnected: true,
  timestamp: new Date().toISOString(),
}

const mockOverview = {
  totalRequests: 0,
  blockedRequests: 0,
  activeBlocks: 0,
  securityEventsToday: 0,
  wafEventsToday: 0,
  rateLimitHitsToday: 0,
  totalApplications: 0,
  totalDevices: 0,
  totalUsers: 0,
}

describe('App', () => {
  const mockUser = {
    id: '00000000-0000-0000-0000-000000000000',
    username: 'admin',
    email: 'admin@example.com',
    role: 1,
    emailVerified: true,
  }

  beforeEach(() => {
    localStorage.setItem('sg_access_token', 'fake-token')
    localStorage.setItem('sg_refresh_token', 'fake-refresh')
    localStorage.setItem('sg_expires_at', new Date(Date.now() + 3600000).toISOString())

    vi.spyOn(global, 'fetch').mockImplementation((url) => {
      const path = url.toString()

      if (path.includes('/api/auth/me')) {
        return Promise.resolve(new Response(JSON.stringify(mockUser), { status: 200 }))
      }

      if (path.includes('/api/health')) {
        return Promise.resolve(new Response(JSON.stringify(mockHealth), { status: 200 }))
      }

      if (path.includes('/api/dashboard/overview')) {
        return Promise.resolve(new Response(JSON.stringify(mockOverview), { status: 200 }))
      }

      return Promise.resolve(new Response(JSON.stringify([]), { status: 200 }))
    })
  })

  afterEach(() => {
    vi.restoreAllMocks()
  })

  it('renders the dashboard by default', async () => {
    render(
      <MemoryRouter initialEntries={['/']}>
        <App />
      </MemoryRouter>
    )

    expect(screen.getByText('Security Gateway')).toBeInTheDocument()
    await waitFor(() => expect(screen.getByText('Security Dashboard')).toBeInTheDocument())
  })

  it('renders the health page at /health', async () => {
    render(
      <MemoryRouter initialEntries={['/health']}>
        <App />
      </MemoryRouter>
    )

    await waitFor(() => expect(screen.getByText('Development Environment Status')).toBeInTheDocument())
  })

  it('renders the map page at /map', async () => {
    render(
      <MemoryRouter initialEntries={['/map']}>
        <App />
      </MemoryRouter>
    )

    await waitFor(() => expect(screen.getByText('Global Security Map')).toBeInTheDocument())
  })

  it('renders the IP explorer page at /ip-explorer', async () => {
    render(
      <MemoryRouter initialEntries={['/ip-explorer']}>
        <App />
      </MemoryRouter>
    )

    await waitFor(() => expect(screen.getByRole('heading', { name: 'IP Explorer' })).toBeInTheDocument())
  })
})
