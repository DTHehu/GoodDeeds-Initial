import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { api, saveTokens } from '../services/api'
import "../css/index.css"

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

            navigate(user.organization ? '/org-dashboard' : '/vol-dashboard')
        } catch {
            setError('Signed in, but your account could not be loaded. Please try again.')
            setBusy(false)
        }
    }

    return (
        <div className="lr-page">

            <nav className="navbar">
                <div className="link">
                    <Link to="/">GoodDeeds</Link>
                </div>
                <div className="link">
                    <Link to="/login">Login</Link>
                </div>
            </nav>

            <div className="lr-content">
                <div className="lr-container">
                    <h1>Login</h1>
                    <p>
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

                        <button type="submit" className="primary-button" disabled={busy}>
                            {busy
                                ? 'Signing in...'
                                : `Login as ${accountType === 'volunteer' ? 'Volunteer' : 'Organization'}`}
                        </button>
                    </form>

                    <p className="link">
                        Don't have an account?{' '}
                        <Link to="/register">Sign up</Link>
                    </p>

                </div>
            </div>
        </div>
    )
}

export default Login