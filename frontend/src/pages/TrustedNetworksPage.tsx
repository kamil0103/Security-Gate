import { useEffect, useState } from 'react'
import {
  type TrustedNetwork,
  type CreateTrustedNetworkRequest,
  fetchTrustedNetworks,
  createTrustedNetwork,
  updateTrustedNetwork,
  deleteTrustedNetwork,
} from '../lib/api'

const emptyNetwork: CreateTrustedNetworkRequest & { isEnabled: boolean } = {
  name: '',
  cidr: '',
  description: '',
  isEnabled: true,
}

export function TrustedNetworksPage() {
  const [networks, setNetworks] = useState<TrustedNetwork[]>([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [form, setForm] = useState(emptyNetwork)
  const [editingId, setEditingId] = useState<string | null>(null)

  const loadNetworks = async () => {
    setLoading(true)
    setError(null)
    try {
      const data = await fetchTrustedNetworks()
      setNetworks(data)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load trusted networks')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    loadNetworks()
  }, [])

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault()
    setError(null)
    try {
      await createTrustedNetwork(form)
      setForm(emptyNetwork)
      await loadNetworks()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to create network')
    }
  }

  const handleUpdate = async (id: string) => {
    const network = networks.find((n) => n.id === id)
    if (!network) return
    setError(null)
    try {
      await updateTrustedNetwork(id, {
        name: network.name,
        cidr: network.cidr,
        description: network.description,
        isEnabled: network.isEnabled,
      })
      setEditingId(null)
      await loadNetworks()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to update network')
    }
  }

  const handleDelete = async (id: string) => {
    if (!confirm('Delete this trusted network?')) return
    setError(null)
    try {
      await deleteTrustedNetwork(id)
      await loadNetworks()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to delete network')
    }
  }

  const updateField = (id: string, field: keyof TrustedNetwork, value: string | boolean) => {
    setNetworks((prev) => prev.map((n) => (n.id === id ? { ...n, [field]: value } : n)))
  }

  return (
    <div className="page">
      <h2>Trusted networks</h2>
      <p className="subtitle">
        Admin approvals can only be performed from these networks. Leave empty to allow any network
        during initial setup.
      </p>

      {error && <div className="alert error">{error}</div>}

      <section className="card">
        <h3>Add trusted network</h3>
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
            placeholder="CIDR (e.g. 192.168.5.0/24)"
            value={form.cidr}
            onChange={(e) => setForm({ ...form, cidr: e.target.value })}
            required
          />
          <input
            type="text"
            placeholder="Description"
            value={form.description}
            onChange={(e) => setForm({ ...form, description: e.target.value })}
          />
          <button type="submit" className="button primary">
            Add
          </button>
        </form>
      </section>

      <section className="card">
        <h3>Configured networks</h3>
        {loading && <p>Loading...</p>}
        {!loading && networks.length === 0 && <p>No trusted networks configured.</p>}
        <ul className="list">
          {networks.map((network) => (
            <li key={network.id} className="list-item">
              {editingId === network.id ? (
                <div className="form-row">
                  <input
                    type="text"
                    value={network.name}
                    onChange={(e) => updateField(network.id, 'name', e.target.value)}
                  />
                  <input
                    type="text"
                    value={network.cidr}
                    onChange={(e) => updateField(network.id, 'cidr', e.target.value)}
                  />
                  <input
                    type="text"
                    value={network.description ?? ''}
                    onChange={(e) => updateField(network.id, 'description', e.target.value)}
                  />
                  <label className="checkbox">
                    <input
                      type="checkbox"
                      checked={network.isEnabled}
                      onChange={(e) => updateField(network.id, 'isEnabled', e.target.checked)}
                    />
                    Enabled
                  </label>
                  <button onClick={() => handleUpdate(network.id)} className="button primary">
                    Save
                  </button>
                  <button onClick={() => setEditingId(null)} className="button secondary">
                    Cancel
                  </button>
                </div>
              ) : (
                <div className="row">
                  <div className="details">
                    <strong>{network.name}</strong>
                    <span className="meta">{network.cidr}</span>
                    {network.description && <span className="meta">{network.description}</span>}
                    <span className={`badge ${network.isEnabled ? 'success' : 'muted'}`}>
                      {network.isEnabled ? 'Enabled' : 'Disabled'}
                    </span>
                  </div>
                  <div className="actions">
                    <button onClick={() => setEditingId(network.id)} className="button secondary">
                      Edit
                    </button>
                    <button onClick={() => handleDelete(network.id)} className="button danger">
                      Delete
                    </button>
                  </div>
                </div>
              )}
            </li>
          ))}
        </ul>
      </section>
    </div>
  )
}
