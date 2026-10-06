import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { ApprovalsPage } from './ApprovalsPage'
import { fetchPendingAccessRequests, resolveAccessRequest, type AccessRequest } from '../lib/api'

vi.mock('../lib/api', () => ({
  fetchPendingAccessRequests: vi.fn(),
  resolveAccessRequest: vi.fn(),
}))

const authenticated = {
  id: 'authenticated-request',
  publicId: 'AUTH123',
  applicationName: 'Sonarr',
  applicationDomain: 'sonarr.example.test',
  clientIp: '198.51.100.10',
  userId: 'user-123',
  username: 'tester',
  threatScore: 0,
  requestCount: 1,
  reasonForChallenge: 'Device pending approval',
} as AccessRequest

const anonymous = {
  ...authenticated,
  id: 'anonymous-request',
  publicId: 'ANON123',
  applicationName: 'Radarr',
  applicationDomain: 'radarr.example.test',
  userId: undefined,
  username: undefined,
  reasonForChallenge: 'Authentication required',
} as AccessRequest

describe('ApprovalsPage', () => {
  beforeEach(() => {
    vi.mocked(fetchPendingAccessRequests).mockResolvedValue([authenticated, anonymous])
    vi.mocked(resolveAccessRequest).mockResolvedValue(authenticated)
  })

  afterEach(() => vi.clearAllMocks())

  it('separates authenticated approvals from anonymous challenges', async () => {
    render(<ApprovalsPage />)
    await waitFor(() => expect(screen.getByText('AUTH123')).toBeInTheDocument())
    expect(screen.queryByText('ANON123')).not.toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Needs approval (1)' })).toBeInTheDocument()

    fireEvent.click(screen.getByRole('button', { name: 'Authentication required (1)' }))
    expect(screen.getByText('ANON123')).toBeInTheDocument()
    expect(screen.queryByText('AUTH123')).not.toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Approve' })).toBeDisabled()
  })

  it('approves only authenticated requests using the selected scope', async () => {
    render(<ApprovalsPage />)
    await waitFor(() => expect(screen.getByText('AUTH123')).toBeInTheDocument())
    const card = screen.getByText('AUTH123').closest('.approval-card')
    expect(card).not.toBeNull()
    fireEvent.click(within(card as HTMLElement).getByRole('button', { name: 'Approve' }))
    await waitFor(() => expect(resolveAccessRequest).toHaveBeenCalledWith(
      authenticated.id,
      expect.objectContaining({ decision: 'Approve', approvalScope: 'Session' }),
    ))
  })

  it('does not send an approval request for anonymous challenges', async () => {
    render(<ApprovalsPage />)
    await waitFor(() => expect(screen.getByText('AUTH123')).toBeInTheDocument())
    fireEvent.click(screen.getByRole('button', { name: 'Authentication required (1)' }))
    fireEvent.click(screen.getByRole('button', { name: 'Approve' }))
    expect(resolveAccessRequest).not.toHaveBeenCalled()
  })
})
