import { useState } from 'react'
import { Link } from 'react-router-dom'
import { api } from "../services/api.js";
import Navbar from '../components/Navbar.jsx'
import { AlertIcon, CheckCircleIcon } from '../components/Icons.jsx'
import "../css/index.css"

function Register() {
    const [accountType, setAccountType] = useState('volunteer')
    const [name, setName] = useState('')
    const [email, setEmail] = useState('')
    const [password, setPassword] = useState('')
    const [contactEmail, setContactEmail] = useState('')
    const [phoneNumber, setPhoneNumber] = useState('')
    const [description, setDescription] = useState('')

    const [error, setError] = useState('')
    const [registered, setRegistered] = useState(false)
    const [busy, setBusy] = useState(false)

    const isOrganization = accountType === 'organization'

    async function tryUserRegister(email, password, name) {
        const body = { email, password, name };
        return await api.post('/auth/register', body);
    }

    async function tryOrgRegister(email, password, name, contactEmail, phoneNumber, description) {
        const body = { email, password, name, contactEmail, phoneNumber, description };
        return await api.post('/auth/registerOrg', body);
    }

    async function handleSubmit(event) {
        event.preventDefault();
        setError('');

        if (busy) return;
        setBusy(true);

        try {
            if (!isOrganization) {
                await tryUserRegister(email, password, name);
            } else {
                await tryOrgRegister(email, password, name, contactEmail, phoneNumber, description);
            }
            setRegistered(true);
        } catch (err) {
            console.error("Registration error:", err);
            setError(err.message || "There was a problem creating your account.");
        } finally {
            setBusy(false);
        }
    }

    return (
        <div className="page">

            <Navbar />

            <main className="auth">
                <div className="auth-card">

                    {registered ? (
                        <div className="text-center">

                            <div className="mx-auto mb-5 grid h-14 w-14 place-items-center rounded-full bg-brand-50 text-brand-700">
                                <CheckCircleIcon className="h-7 w-7" />
                            </div>

                            <h1 className="auth-title">Account created</h1>

                            <p className="auth-sub">
                                Your {isOrganization ? 'organization' : 'volunteer'} account is ready.
                                You can log in with {email} now.
                            </p>

                            <Link to="/login" className="btn btn-primary btn-block">
                                Go to login
                            </Link>

                        </div>
                    ) : (
                        <>
                            <h1 className="auth-title">Create an account</h1>
                            <p className="auth-sub">Choose the type of account you want to create.</p>

                            <div className="segmented mb-6">
                                <button
                                    type="button"
                                    className={isOrganization ? 'segment' : 'segment is-active'}
                                    onClick={() => setAccountType('volunteer')}
                                >
                                    Volunteer
                                </button>

                                <button
                                    type="button"
                                    className={isOrganization ? 'segment is-active' : 'segment'}
                                    onClick={() => setAccountType('organization')}
                                >
                                    Organization
                                </button>
                            </div>

                            <form className="form" onSubmit={handleSubmit}>

                                <div className="field">
                                    <label className="field-label" htmlFor="name">
                                        {isOrganization ? 'Organization name' : 'Full name'}
                                    </label>

                                    <input
                                        className="input"
                                        type="text"
                                        id="name"
                                        placeholder={isOrganization ? 'Enter your organization name' : 'Enter your full name'}
                                        value={name}
                                        onChange={(e) => setName(e.target.value)}
                                    />
                                </div>

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
                                        autoComplete="new-password"
                                        placeholder="Create a password"
                                        value={password}
                                        onChange={(e) => setPassword(e.target.value)}
                                    />
                                </div>

                                {isOrganization && (
                                    <>
                                        <div className="field">
                                            <label className="field-label" htmlFor="phonenumber">Phone number</label>
                                            <input
                                                className="input"
                                                id="phonenumber"
                                                placeholder="(555) 555-5555"
                                                value={phoneNumber}
                                                onChange={(e) => setPhoneNumber(e.target.value)}
                                            />
                                        </div>

                                        <div className="field">
                                            <label className="field-label" htmlFor="orgemail">Organization email</label>
                                            <input
                                                className="input"
                                                type="email"
                                                id="orgemail"
                                                placeholder="contact@organization.org"
                                                value={contactEmail}
                                                onChange={(e) => setContactEmail(e.target.value)}
                                            />
                                        </div>

                                        <div className="field">
                                            <label className="field-label" htmlFor="description">Organization description</label>
                                            <textarea
                                                className="textarea"
                                                id="description"
                                                placeholder="Tell volunteers what your organization does"
                                                value={description}
                                                onChange={(e) => setDescription(e.target.value)}
                                            />
                                        </div>
                                    </>
                                )}

                                {error && (
                                    <p className="alert alert-error">
                                        <AlertIcon />
                                        {error}
                                    </p>
                                )}

                                <button type="submit" className="btn btn-primary btn-block mt-1" disabled={busy}>
                                    {busy
                                        ? 'Creating account...'
                                        : `Create ${isOrganization ? 'organization' : 'volunteer'} account`}
                                </button>

                            </form>

                            <p className="auth-foot">
                                Already have an account?{' '}
                                <Link className="link" to="/login">Login</Link>
                            </p>
                        </>
                    )}

                </div>
            </main>

        </div>
    )
}

export default Register;
