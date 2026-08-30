import { describe, it, expect } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import App from './App'

describe('App', () => {
  it('renders the application title', async () => {
    render(<App />)
    expect(screen.getByText('Security Gateway')).toBeInTheDocument()
    await waitFor(() => expect(screen.getByText('Development Environment Status')).toBeInTheDocument())
  })
})
