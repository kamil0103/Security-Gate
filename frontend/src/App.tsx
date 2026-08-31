import { Link, Route, Routes } from 'react-router-dom'
import './styles.css'
import { DashboardPage } from './pages/DashboardPage'
import { HealthPage } from './pages/HealthPage'
import { IpExplorerPage } from './pages/IpExplorerPage'
import { MapPage } from './pages/MapPage'

function App() {
  return (
    <div className="container">
      <header className="header">
        <h1>Security Gateway</h1>
        <p className="subtitle">Self-hosted security gateway for Unraid</p>
      </header>

      <nav className="nav">
        <Link className="nav-link" to="/">
          Dashboard
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

      <Routes>
        <Route path="/" element={<DashboardPage />} />
        <Route path="/map" element={<MapPage />} />
        <Route path="/ip-explorer" element={<IpExplorerPage />} />
        <Route path="/health" element={<HealthPage />} />
      </Routes>

      <footer className="footer">
        <p>Phase 13 milestone — global map</p>
      </footer>
    </div>
  )
}

export default App
