import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import "../css/index.css"
import { api, saveTokens } from '../services/api'

function Login() {
    const navigate = useNavigate()

    const [accountType, setAccountType] = useState('volunteer')
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
            navigate(accountType === 'volunteer' ? '/vol-dashboard' : '/org-dashboard')
        } catch {
            setError("That email and password don't match.")
        }
        setBusy(false)
    }

    return (
        <div className="login-page">

            <nav className="navbar">
                <div className="logo">
                    <Link to="/">GoodDeeds</Link>
                </div>
                <div className="nav-links">
                    <Link to="/login">Login</Link>
                    <Link to="/register">Register</Link>
                </div>
            </nav>

            <div className="login-content">
                <div className="login-container">
                    <h1>Login</h1>
                    <p className="login-description">
                        Choose your account type to continue.
                    </p>

                    <div className="account-type">
                        <button type="button" className={
                            accountType === 'volunteer' ? 'account-button active' : 'account-button'
                        } onClick={() => setAccountType('volunteer')}>
                            Volunteer
                        </button>

                        <button type="button" className={
                            accountType === 'organization' ? 'account-button active' : 'account-button'
                        } onClick={() => setAccountType('organization')}>
                            Organization
                        </button>
                    </div>

                    <form className="login-form" onSubmit={handleSubmit}>
                        <div className="form-group">
                            <label htmlFor="email">Email</label>
                            <input
                                type="email"
                                id="email"
                                placeholder="Enter your email"
                                value={email}
                                onChange={(e) => setEmail(e.target.value)}
                            />
                        </div>

                        <div className="form-group">
                            <label htmlFor="password">Password</label>
                            <input
                                type="password"
                                id="password"
                                placeholder="Enter your password"
                                value={password}
                                onChange={(e) => setPassword(e.target.value)}
                            />
                        </div>

                        {error && <p className="login-error">{error}</p>}

                        <button type="submit" className="login-button" disabled={busy}>
                            {busy
                                ? 'Signing in...'
                                : `Login as ${accountType === 'volunteer' ? 'Volunteer' : 'Organization'}`}
                        </button>
                    </form>

                    <p className="signup-text">
                        Don't have an account?{' '}
                        <Link to="/register">Sign up</Link>
                    </p>

                </div>
            </div>
        </div>
    )
}

export default Login