import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import Home from './Home.jsx'
import OrgLogin from './orgLogin.jsx'
import VolLogin from './volLogin.jsx'

createRoot(document.getElementById('root')).render(
  <StrictMode>
    <Home />
    <OrgLogin />
    <VolLogin />
  </StrictMode>,
)
