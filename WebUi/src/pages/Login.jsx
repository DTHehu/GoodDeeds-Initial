import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { api, saveTokens, saveDashboardPath } from '../services/api'
import Navbar from '../components/Navbar.jsx'
import { AlertIcon } from '../components/Icons.jsx'
import "../css/index.css"

function Login() {
    const navigate = useNavigate()

    const [email, setEmail] = useState('')
    const [password, setPassword] = useState('')
    const [error, setError] = useState('')
    const [busy, setBusy] = useState(false)

    async function TryLogin(email, password) {
        const body = {
            email: email,
            password: password
        }

        const apiResponse = await api.post('/auth/login', body)
        saveTokens(apiResponse.accessToken, apiResponse.refreshToken)

        return apiResponse
    }

    async function handleSubmit(event) {
        event.preventDefault()
        setError('')

        if (!email || !password) {
            setError('Enter your email and password.')
            return
        }

        setBusy(true)

        try {
            await TryLogin(email, password)
        } catch {
            setError("That email and password don't match.")
            setBusy(false)
            return
        }

        try {
            // The account type is decided by the account itself, not by the
            // button the user picked. A user that belongs to an organization
            // comes back with an organization on their profile.
            const user = await api.get('/auth/me')

            const dashboard = user.organization ? '/org-dashboard' : '/vol-dashboard'

            saveDashboardPath(dashboard)
            navigate(dashboard)
        } catch {
            setError('Signed in, but your account could not be loaded. Please try again.')
            setBusy(false)
        }
    }

    return (
        <div className="page">

            <Navbar />

            <main className="auth">
                <div className="auth-card">

                    <h1 className="auth-title">Welcome back</h1>
                    <p className="auth-sub">Volunteer or organization, sign in below.</p>

                    <form className="form" onSubmit={handleSubmit}>

                        <div className="field">
                            <label className="field-label" htmlFor="email">Email</label>
                            <input
                                className="input"
                                type="email"
                                id="email"
                                autoComplete="email"
                                placeholder="you@example.com"
                                value={email}
                                onChange={(e) => setEmail(e.target.value)}
                            />
                        </div>

                        <div className="field">
                            <label className="field-label" htmlFor="password">Password</label>
                            <input
                                className="input"
                                type="password"
                                id="password"
                                autoComplete="current-password"
                                placeholder="Enter your password"
                                value={password}
                                onChange={(e) => setPassword(e.target.value)}
                            />
                        </div>

                        {error && (
                            <p className="alert alert-error">
                                <AlertIcon />
                                {error}
                            </p>
                        )}

                        <button type="submit" className="btn btn-primary btn-block mt-1" disabled={busy}>
                            {busy ? 'Signing in...' : 'Login'}
                        </button>

                    </form>

                    <p className="auth-foot">
                        Don't have an account?{' '}
                        <Link className="link" to="/register">Sign up</Link>
                    </p>

                </div>
            </main>

        </div>
    )
}

export default Login
