import { useState } from 'react'
import { blockIp, unblockIp, isIpBlocked } from '../lib/api'

export function BlockIpPage() {
  const [ipAddress, setIpAddress] = useState('')
  const [duration, setDuration] = useState('')
  const [reason, setReason] = useState('')
  const [checkResult, setCheckResult] = useState<{ ipAddress: string; isBlocked: boolean } | null>(null)
  const [result, setResult] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)

  const handleBlock = async (e: React.FormEvent) => {
    e.preventDefault()
    setError(null)
    setResult(null)
    try {
      const res = await blockIp(
        ipAddress,
        duration ? parseInt(duration, 10) : undefined,
        reason || undefined
      )
      setResult(`${res.ipAddress} blocked${res.expiresAt ? ` until ${new Date(res.expiresAt).toLocaleString()}` : ''}`)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Block failed')
    }
  }

  const handleUnblock = async () => {
    setError(null)
    setResult(null)
    try {
      await unblockIp(ipAddress)
      setResult(`${ipAddress} unblocked`)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Unblock failed')
    }
  }

  const handleCheck = async () => {
    setError(null)
    setCheckResult(null)
    try {
      const res = await isIpBlocked(ipAddress)
      setCheckResult(res)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Check failed')
    }
  }

  return (
    <div className="page">
      <h2>Block / unblock IP</h2>

      {error && <div className="alert error">{error}</div>}
      {result && <div className="status healthy">{result}</div>}

      <section className="card">
        <form onSubmit={handleBlock} className="form-stack">
          <label>IP address</label>
          <input
            type="text"
            value={ipAddress}
            onChange={(e) => setIpAddress(e.target.value)}
            placeholder="192.0.2.1"
            required
          />
          <label>Duration (minutes, leave empty for permanent)</label>
          <input
            type="number"
            value={duration}
            onChange={(e) => setDuration(e.target.value)}
            placeholder="60"
          />
          <label>Reason</label>
          <input
            type="text"
            value={reason}
            onChange={(e) => setReason(e.target.value)}
            placeholder="Manual block"
          />
          <div className="actions">
            <button type="submit" className="button danger">
              Block
            </button>
            <button type="button" onClick={handleUnblock} className="button secondary">
              Unblock
            </button>
            <button type="button" onClick={handleCheck} className="button secondary">
              Check
            </button>
          </div>
        </form>
      </section>

      {checkResult && (
        <section className="card">
          <p>
            <strong>{checkResult.ipAddress}</strong> is{' '}
            {checkResult.isBlocked ? (
              <span className="badge danger">blocked</span>
            ) : (
              <span className="badge success">not blocked</span>
            )}
          </p>
        </section>
      )}
    </div>
  )
}
