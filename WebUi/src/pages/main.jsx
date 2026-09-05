import { StrictMode } from 'react' 
import { createRoot } from 'react-dom/client' 
import { BrowserRouter, Routes, Route } from 'react-router-dom' 
import "../css/index.css" 
import Home from './Home.jsx' 
import OrgDashboard from './OrgDashboard.jsx' 
import VolDashboard from './VolDashboard.jsx' 
import Register from './Register.jsx'
import Login from './Login.jsx'
import ProtectedRoute from '../components/ProtectedRoute.jsx'

createRoot(document.getElementById('root')).render(
  <StrictMode>
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<Home />} />
        <Route path="/login" element={<Login />} />
        <Route path="/register" element={<Register />} />

        <Route
          path="/org-dashboard"
          element={
            <ProtectedRoute>
              <OrgDashboard />
            </ProtectedRoute>
          }
        />

        <Route
          path="/vol-dashboard"
          element={
            <ProtectedRoute>
              <VolDashboard />
            </ProtectedRoute>
          }
        />
      </Routes>
    </BrowserRouter>
  </StrictMode>,
)