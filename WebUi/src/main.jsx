import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { BrowserRouter, Routes, Route } from 'react-router-dom'
import "./index.css"
import Home from './Home.jsx'
import OrgLogin from './orgLogin.jsx'
import VolLogin from './volLogin.jsx'

createRoot(document.getElementById('root')).render(
  <StrictMode>
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<Home />} />
        <Route path="/vol-login" element={<VolLogin />} />
        <Route path="/org-login" element={<OrgLogin />} />
      </Routes>
    </BrowserRouter>
  </StrictMode>,
)