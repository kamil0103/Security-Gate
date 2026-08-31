import { useEffect, useState } from 'react'
import {
  type NotificationChannel,
  type NotificationChannelType,
  type CreateNotificationChannelRequest,
  fetchNotificationChannels,
  createNotificationChannel,
  updateNotificationChannel,
  deleteNotificationChannel,
  testNotificationChannel,
} from '../lib/api'

const CHANNEL_TYPES: NotificationChannelType[] = ['Email', 'Telegram', 'Discord', 'Ntfy', 'WebPush']

const emptyChannel: CreateNotificationChannelRequest = {
  name: '',
  type: 'Email',
  isEnabled: true,
  configuration: '{}',
}

function formatConfig(config: string): string {
  try {
    return JSON.stringify(JSON.parse(config), null, 2)
  } catch {
    return config
  }
}

export function NotificationsPage() {
  const [channels, setChannels] = useState<NotificationChannel[]>([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [form, setForm] = useState(emptyChannel)
  const [editingId, setEditingId] = useState<string | null>(null)
  const [testMessage, setTestMessage] = useState<{ subject: string; body: string } | null>(null)

  const loadChannels = async () => {
    setLoading(true)
    setError(null)
    try {
      const data = await fetchNotificationChannels()
      setChannels(data)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load notification channels')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    loadChannels()
  }, [])

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault()
    setError(null)
    try {
      await createNotificationChannel(form)
      setForm(emptyChannel)
      await loadChannels()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to create channel')
    }
  }

  const handleUpdate = async (id: string) => {
    const channel = channels.find((c) => c.id === id)
    if (!channel) return
    setError(null)
    try {
      await updateNotificationChannel(id, {
        name: channel.name,
        type: channel.type,
        isEnabled: channel.isEnabled,
        configuration: channel.configuration,
      })
      setEditingId(null)
      await loadChannels()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to update channel')
    }
  }

  const handleDelete = async (id: string) => {
    if (!confirm('Delete this notification channel?')) return
    setError(null)
    try {
      await deleteNotificationChannel(id)
      await loadChannels()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to delete channel')
    }
  }

  const handleTest = async (id: string) => {
    if (!testMessage) return
    setError(null)
    try {
      await testNotificationChannel(id, testMessage.subject, testMessage.body)
      setTestMessage(null)
      alert('Test notification sent')
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to send test')
    }
  }

  const updateField = (id: string, field: keyof NotificationChannel, value: string | boolean) => {
    setChannels((prev) => prev.map((c) => (c.id === id ? { ...c, [field]: value } : c)))
  }

  return (
    <div className="page">
      <h2>Notifications</h2>

      {error && <div className="alert error">{error}</div>}

      <section className="card">
        <h3>Add notification channel</h3>
        <form onSubmit={handleCreate} className="form-stack">
          <div className="form-row">
            <input
              type="text"
              placeholder="Name"
              value={form.name}
              onChange={(e) => setForm({ ...form, name: e.target.value })}
              required
            />
            <select
              value={form.type}
              onChange={(e) => setForm({ ...form, type: e.target.value as NotificationChannelType })}
            >
              {CHANNEL_TYPES.map((t) => (
                <option key={t} value={t}>
                  {t}
                </option>
              ))}
            </select>
            <label className="checkbox">
              <input
                type="checkbox"
                checked={form.isEnabled}
                onChange={(e) => setForm({ ...form, isEnabled: e.target.checked })}
              />
              Enabled
            </label>
          </div>
          <textarea
            rows={6}
            value={form.configuration}
            onChange={(e) => setForm({ ...form, configuration: e.target.value })}
            placeholder='{"smtpHost":"...","port":587,...}'
          />
          <button type="submit" className="button primary">
            Create channel
          </button>
        </form>
      </section>

      <section className="card">
        <h3>Configured channels</h3>
        {loading && <p>Loading...</p>}
        {!loading && channels.length === 0 && <p>No notification channels configured.</p>}
        <ul className="list">
          {channels.map((channel) => (
            <li key={channel.id} className="list-item">
              {editingId === channel.id ? (
                <div className="form-stack">
                  <div className="form-row">
                    <input
                      type="text"
                      value={channel.name}
                      onChange={(e) => updateField(channel.id, 'name', e.target.value)}
                    />
                    <select
                      value={channel.type}
                      onChange={(e) => updateField(channel.id, 'type', e.target.value)}
                    >
                      {CHANNEL_TYPES.map((t) => (
                        <option key={t} value={t}>
                          {t}
                        </option>
                      ))}
                    </select>
                    <label className="checkbox">
                      <input
                        type="checkbox"
                        checked={channel.isEnabled}
                        onChange={(e) => updateField(channel.id, 'isEnabled', e.target.checked)}
                      />
                      Enabled
                    </label>
                  </div>
                  <textarea
                    rows={6}
                    value={channel.configuration}
                    onChange={(e) => updateField(channel.id, 'configuration', e.target.value)}
                  />
                  <div className="actions">
                    <button onClick={() => handleUpdate(channel.id)} className="button primary">
                      Save
                    </button>
                    <button onClick={() => setEditingId(null)} className="button secondary">
                      Cancel
                    </button>
                  </div>
                </div>
              ) : (
                <div className="row">
                  <div className="details">
                    <strong>{channel.name}</strong>
                    <span className="meta">{channel.type}</span>
                    <span className={`badge ${channel.isEnabled ? 'success' : 'muted'}`}>
                      {channel.isEnabled ? 'Enabled' : 'Disabled'}
                    </span>
                    <pre className="config-preview">{formatConfig(channel.configuration)}</pre>
                  </div>
                  <div className="actions">
                    <button onClick={() => setTestMessage({ subject: 'Test', body: 'This is a test notification' })} className="button secondary">
                      Test
                    </button>
                    <button onClick={() => setEditingId(channel.id)} className="button secondary">
                      Edit
                    </button>
                    <button onClick={() => handleDelete(channel.id)} className="button danger">
                      Delete
                    </button>
                  </div>
                </div>
              )}

              {testMessage && editingId !== channel.id && (
                <div className="form-stack" style={{ marginTop: '1rem' }}>
                  <input
                    type="text"
                    value={testMessage.subject}
                    onChange={(e) => setTestMessage({ ...testMessage, subject: e.target.value })}
                  />
                  <textarea
                    rows={3}
                    value={testMessage.body}
                    onChange={(e) => setTestMessage({ ...testMessage, body: e.target.value })}
                  />
                  <div className="actions">
                    <button onClick={() => handleTest(channel.id)} className="button primary">
                      Send test
                    </button>
                    <button onClick={() => setTestMessage(null)} className="button secondary">
                      Cancel
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
