import { useEffect, useState } from 'react'
import {
  type Application,
  type ApplicationPolicy,
  fetchApplications,
  createApplication,
  updateApplication,
  deleteApplication,
  fetchApplicationPolicy,
  updateApplicationPolicy,
} from '../lib/api'

const emptyForm = {
  name: '',
  domain: '',
  upstreamUrl: '',
}

const emptyPolicy: Omit<ApplicationPolicy, 'id' | 'applicationId'> = {
  requireAuthentication: true,
  allowAnonymousFromTrustedNetworks: false,
  allowedCountries: '',
  blockedCountries: '',
  allowedIpAddresses: '',
  blockedIpAddresses: '',
  allowedCloudflareCountries: '',
  blockedCloudflareCountries: '',
  bypassAuthenticationPaths: '',
}

export function ApplicationsPage() {
  const [applications, setApplications] = useState<Application[]>([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [form, setForm] = useState(emptyForm)
  const [editingId, setEditingId] = useState<string | null>(null)
  const [editingPolicyId, setEditingPolicyId] = useState<string | null>(null)
  const [policyForm, setPolicyForm] = useState(emptyPolicy)

  const loadApplications = async () => {
    setLoading(true)
    setError(null)
    try {
      const data = await fetchApplications()
      setApplications(data)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load applications')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    loadApplications()
  }, [])

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault()
    setError(null)
    try {
      await createApplication(form)
      setForm(emptyForm)
      await loadApplications()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to create application')
    }
  }

  const handleUpdate = async (id: string) => {
    const app = applications.find((a) => a.id === id)
    if (!app) return
    setError(null)
    try {
      await updateApplication(id, {
        name: app.name,
        domain: app.domain,
        upstreamUrl: app.upstreamUrl,
        isEnabled: app.isEnabled,
      })
      setEditingId(null)
      await loadApplications()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to update application')
    }
  }

  const handleDelete = async (id: string) => {
    if (!confirm('Are you sure you want to delete this application?')) return
    setError(null)
    try {
      await deleteApplication(id)
      await loadApplications()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to delete application')
    }
  }

  const openPolicy = async (app: Application) => {
    setError(null)
    setEditingPolicyId(app.id)
    try {
      const policy = await fetchApplicationPolicy(app.id)
      setPolicyForm(policy ?? emptyPolicy)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load policy')
    }
  }

  const handlePolicySave = async () => {
    if (!editingPolicyId) return
    setError(null)
    try {
      await updateApplicationPolicy(editingPolicyId, policyForm)
      setEditingPolicyId(null)
      await loadApplications()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to update policy')
    }
  }

  const updateAppField = (id: string, field: keyof Application, value: string | boolean) => {
    setApplications((prev) =>
      prev.map((a) => (a.id === id ? { ...a, [field]: value } : a))
    )
  }

  return (
    <div className="page">
      <h2>Applications</h2>

      {error && <div className="alert error">{error}</div>}

      <section className="card">
        <h3>Add application</h3>
        <form onSubmit={handleCreate} className="form-row">
          <input
            type="text"
            placeholder="Name"
            value={form.name}
            onChange={(e) => setForm({ ...form, name: e.target.value })}
            required
          />
          <input
            type="text"
            placeholder="Domain (e.g. app.example.com)"
            value={form.domain}
            onChange={(e) => setForm({ ...form, domain: e.target.value })}
            required
          />
          <input
            type="text"
            placeholder="Upstream URL (e.g. http://localhost:5000)"
            value={form.upstreamUrl}
            onChange={(e) => setForm({ ...form, upstreamUrl: e.target.value })}
            required
          />
          <button type="submit" className="button primary">
            Create
          </button>
        </form>
      </section>

      <section className="card">
        <h3>Configured applications</h3>
        {loading && <p>Loading...</p>}
        {!loading && applications.length === 0 && <p>No applications configured.</p>}
        <ul className="list">
          {applications.map((app) => (
            <li key={app.id} className="list-item">
              {editingId === app.id ? (
                <div className="form-row">
                  <input
                    type="text"
                    value={app.name}
                    onChange={(e) => updateAppField(app.id, 'name', e.target.value)}
                  />
                  <input
                    type="text"
                    value={app.domain}
                    onChange={(e) => updateAppField(app.id, 'domain', e.target.value)}
                  />
                  <input
                    type="text"
                    value={app.upstreamUrl}
                    onChange={(e) => updateAppField(app.id, 'upstreamUrl', e.target.value)}
                  />
                  <label className="checkbox">
                    <input
                      type="checkbox"
                      checked={app.isEnabled}
                      onChange={(e) => updateAppField(app.id, 'isEnabled', e.target.checked)}
                    />
                    Enabled
                  </label>
                  <button onClick={() => handleUpdate(app.id)} className="button primary">
                    Save
                  </button>
                  <button onClick={() => setEditingId(null)} className="button secondary">
                    Cancel
                  </button>
                </div>
              ) : (
                <div className="row">
                  <div className="details">
                    <strong>{app.name}</strong>
                    <span className="meta">
                      {app.domain} &rarr; {app.upstreamUrl}
                    </span>
                    <span className={`badge ${app.isEnabled ? 'success' : 'muted'}`}>
                      {app.isEnabled ? 'Enabled' : 'Disabled'}
                    </span>
                    {app.policy && (
                      <span className="badge info">
                        Auth: {app.policy.requireAuthentication ? 'required' : 'optional'}
                      </span>
                    )}
                  </div>
                  <div className="actions">
                    <button onClick={() => setEditingId(app.id)} className="button secondary">
                      Edit
                    </button>
                    <button onClick={() => openPolicy(app)} className="button secondary">
                      Policy
                    </button>
                    <button onClick={() => handleDelete(app.id)} className="button danger">
                      Delete
                    </button>
                  </div>
                </div>
              )}
            </li>
          ))}
        </ul>
      </section>

      {editingPolicyId && (
        <div className="modal-backdrop" onClick={() => setEditingPolicyId(null)}>
          <div className="modal" onClick={(e) => e.stopPropagation()}>
            <h3>Application policy</h3>
            <div className="form-stack">
              <label className="checkbox">
                <input
                  type="checkbox"
                  checked={policyForm.requireAuthentication}
                  onChange={(e) =>
                    setPolicyForm({ ...policyForm, requireAuthentication: e.target.checked })
                  }
                />
                Require authentication
              </label>
              <label className="checkbox">
                <input
                  type="checkbox"
                  checked={policyForm.allowAnonymousFromTrustedNetworks}
                  onChange={(e) =>
                    setPolicyForm({ ...policyForm, allowAnonymousFromTrustedNetworks: e.target.checked })
                  }
                />
                Allow anonymous access from trusted networks
              </label>

              <label>Allowed countries (comma-separated ISO codes)</label>
              <input
                type="text"
                value={policyForm.allowedCountries}
                onChange={(e) => setPolicyForm({ ...policyForm, allowedCountries: e.target.value })}
              />
              <label>Blocked countries (comma-separated ISO codes)</label>
              <input
                type="text"
                value={policyForm.blockedCountries}
                onChange={(e) => setPolicyForm({ ...policyForm, blockedCountries: e.target.value })}
              />
              <label>Allowed IP addresses (comma-separated)</label>
              <input
                type="text"
                value={policyForm.allowedIpAddresses}
                onChange={(e) => setPolicyForm({ ...policyForm, allowedIpAddresses: e.target.value })}
              />
              <label>Blocked IP addresses (comma-separated)</label>
              <input
                type="text"
                value={policyForm.blockedIpAddresses}
                onChange={(e) => setPolicyForm({ ...policyForm, blockedIpAddresses: e.target.value })}
              />
              <label>Bypass authentication paths (comma-separated)</label>
              <input
                type="text"
                value={policyForm.bypassAuthenticationPaths}
                onChange={(e) =>
                  setPolicyForm({ ...policyForm, bypassAuthenticationPaths: e.target.value })
                }
              />

              <div className="actions">
                <button onClick={handlePolicySave} className="button primary">
                  Save policy
                </button>
                <button onClick={() => setEditingPolicyId(null)} className="button secondary">
                  Cancel
                </button>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
