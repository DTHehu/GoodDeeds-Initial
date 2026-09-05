import { useState } from 'react'
import { Link } from 'react-router-dom'
import { api } from "../services/api.js";
import Navbar from '../components/Navbar.jsx'
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
            if (accountType === 'volunteer') {
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
        <div className="lr-page">
            <Navbar />

            <div className="lr-content">
                <div className="lr-container">

                    {registered ? (
                        <>
                            <h1>Account created</h1>

                            <p className="success">
                                Your {accountType === 'volunteer' ? 'volunteer' : 'organization'} account
                                is ready. You can log in with {email} now.
                            </p>

                            <Link to="/login" className="primary-button">
                                Go to login
                            </Link>
                        </>
                    ) : (
                    <>
                    <h1>Create an Account</h1>
                    <p>Choose the type of account you want to create.</p>

                    <div className="account-type">
                        <button type="button" className={
                            accountType === 'volunteer' ? 'account-button active' : 'account-button'
                        } onClick={() => setAccountType('volunteer')}>
                            Volunteer
                        </button>

                        <button
                            type="button" className={
                            accountType === 'organization' ? 'account-button active' : 'account-button'
                        } onClick={() => setAccountType('organization')}>
                            Organization
                        </button>
                    </div>

                    <form className="register-form" onSubmit={handleSubmit}>
                        <div className="form-group">
                            <label htmlFor="name">
                                {accountType === 'volunteer' ? 'Full Name' : 'Organization Name'}
                            </label>

                            <input type="text" id="name" placeholder={
                                accountType === 'volunteer' ? 'Enter your full name' : 'Enter your organization name'
                            }
                                   value={name}
                                   onChange={(e) => setName(e.target.value)}/>
                        </div>

                        <div className="form-group">
                            <label htmlFor="email">Email</label>
                            <input
                                type="email" id="email" placeholder="Enter your email"
                                value={email}
                                onChange={(e) => setEmail(e.target.value)}
                            />
                        </div>

                        <div className="form-group">
                            <label htmlFor="password">Password</label>
                            <input type="password" id="password" placeholder="Create a password"
                                   value={password}
                                   onChange={(e) => setPassword(e.target.value)}
                            />
                        </div>
                        
                        {accountType === 'organization' && (
                            <div className="form-group">
                                <label htmlFor="phonenumber">
                                    Phone Number
                                </label>
                                <input id="phonenumber" placeholder="Enter your phone number"
                                       value={phoneNumber}
                                       onChange={(e) => setPhoneNumber(e.target.value)}
                                />
                            </div>
                        )}

                        {accountType === 'organization' && (
                            <div className="form-group">
                                <label htmlFor="orgemail">
                                    Organization Email
                                </label>
                                <input type="email" id="orgemail" placeholder="Enter your organization email"
                                       value={contactEmail}
                                       onChange={(e) => setContactEmail(e.target.value)}
                                />
                            </div>
                        )}

                        {accountType === 'organization' && (
                            <div className="form-group">
                                <label htmlFor="description">
                                    Organization Description
                                </label>
                                <textarea id="description" placeholder="Tell us about your organization"
                                          value={description}
                                          onChange={(e) => setDescription(e.target.value)}
                                />
                            </div>
                        )}

                        {error && <p className="error">{error}</p>}

                        <button type="submit" className="primary-button" disabled={busy}>
                            {busy
                                ? 'Creating account...'
                                : `Create ${accountType === 'volunteer' ? 'Volunteer' : 'Organization'} Account`}
                        </button>
                    </form>

                    <p className="link">
                        Already have an account?{' '}
                        <Link to="/login">Login</Link>
                    </p>
                    </>
                    )}

                </div>
            </div>
        </div>
    )
}

export default Register;
