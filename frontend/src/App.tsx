import { Link, Route, Routes, useNavigate } from 'react-router-dom'
import './styles.css'
import { AuthProvider, useAuth } from './contexts/AuthContext'
import { RequireAuth } from './components/RequireAuth'
import { ApplicationsPage } from './pages/ApplicationsPage'
import { ApprovalsPage } from './pages/ApprovalsPage'
import { DashboardPage } from './pages/DashboardPage'
import { DevicesPage } from './pages/DevicesPage'
import { HealthPage } from './pages/HealthPage'
import { IpExplorerPage } from './pages/IpExplorerPage'
import { LoginPage } from './pages/LoginPage'
import { MapPage } from './pages/MapPage'
import { NotificationsPage } from './pages/NotificationsPage'
import { TrustedNetworksPage } from './pages/TrustedNetworksPage'

function Header() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()

  const handleLogout = async () => {
    await logout()
    navigate('/login')
  }

  return (
    <header className="header">
      <div>
        <h1>Security Gateway</h1>
        <p className="subtitle">Self-hosted security gateway for Unraid</p>
      </div>
      {user && (
        <div className="user-menu">
          <span>{user.username}</span>
          <button onClick={handleLogout} className="button secondary">
            Logout
          </button>
        </div>
      )}
    </header>
  )
}

function Layout() {
  const { user } = useAuth()

  return (
    <div className="container">
      <Header />

      {user && (
        <nav className="nav">
          <Link className="nav-link" to="/">
            Dashboard
          </Link>
          <Link className="nav-link" to="/approvals">
            Approvals
          </Link>
          <Link className="nav-link" to="/applications">
            Apps
          </Link>
          <Link className="nav-link" to="/trusted-networks">
            Networks
          </Link>
          <Link className="nav-link" to="/devices">
            Devices
          </Link>
          <Link className="nav-link" to="/notifications">
            Alerts
          </Link>
          <Link className="nav-link" to="/map">
            Map
          </Link>
          <Link className="nav-link" to="/ip-explorer">
            IP Explorer
          </Link>
          <Link className="nav-link" to="/health">
            Health
          </Link>
        </nav>
      )}

      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route
          path="/"
          element={
            <RequireAuth>
              <DashboardPage />
            </RequireAuth>
          }
        />
        <Route
          path="/approvals"
          element={
            <RequireAuth>
              <ApprovalsPage />
            </RequireAuth>
          }
        />
        <Route
          path="/applications"
          element={
            <RequireAuth>
              <ApplicationsPage />
            </RequireAuth>
          }
        />
        <Route
          path="/trusted-networks"
          element={
            <RequireAuth>
              <TrustedNetworksPage />
            </RequireAuth>
          }
        />
        <Route
          path="/devices"
          element={
            <RequireAuth>
              <DevicesPage />
            </RequireAuth>
          }
        />
        <Route
          path="/notifications"
          element={
            <RequireAuth>
              <NotificationsPage />
            </RequireAuth>
          }
        />
        <Route
          path="/map"
          element={
            <RequireAuth>
              <MapPage />
            </RequireAuth>
          }
        />
        <Route
          path="/ip-explorer"
          element={
            <RequireAuth>
              <IpExplorerPage />
            </RequireAuth>
          }
        />
        <Route
          path="/health"
          element={
            <RequireAuth>
              <HealthPage />
            </RequireAuth>
          }
        />
      </Routes>

      <footer className="footer">
        <p>Security Gateway v1.0</p>
      </footer>
    </div>
  )
}

function App() {
  return (
    <AuthProvider>
      <Layout />
    </AuthProvider>
  )
}

export default App
