import { useState } from 'react' 
import { Link , useNavigate } from 'react-router-dom' 
import {api} from "../services/api.js";
import "../css/index.css"

function Register() { 
    const navigate = useNavigate()
    const [accountType, setAccountType] = useState('volunteer')
    const [name, setName] = useState('')
    const [email, setEmail] = useState('')
    const [password, setPassword] = useState('')
    const [contactEmail, setContactEmail] = useState('')
    const [phoneNumber, setPhoneNumber] = useState('')
    const [description, setDescription] = useState('')
    const [error, setError] = useState('')
    const [busy, setBusy] = useState(false)

    async function TryUserRegistration(email, password, name, contactEmail, phoneNumber, description) {
        const body = {
            email: email,
            password: password,
            name: name,
            contactEmail: contactEmail,
            phoneNumber: phoneNumber,
            description: description
        }

        const apiResponse = await api.post('/auth/registerOrg', body)
        return apiResponse
    }
    

    async function handleSubmit(event) {
        event.preventDefault()
        setError('')
        setBusy(true)
        try {
            await TryUserRegistration(email, password, name, contactEmail, phoneNumber, description)
            navigate('/login')
        } catch {
            console.error("Registration error:", error)
            setError("There was a problem creating your account.")
        }
        setBusy(false)
    }

    return (
        <div className="lr-page"> 

            <nav className="navbar"> 
                <div className="link"> 
                    <Link to="/">GoodDeads</Link> 
                </div> 

                <div className="link"> 
                    <Link to="/login">Login</Link> 
                </div>
            </nav> 

            <div className="lr-content"> 
                <div className="lr-container"> 
                        <h1>Create an Account</h1> 
                        <p>Choose the type of account you want to create.</p> 

                        <div className="account-type"> 
                            <button type="button"className={ 
                                accountType === 'volunteer' ? 'account-button active' : 'account-button' 
                                } onClick={() => setAccountType('volunteer')}> 
                                Volunteer 
                            </button> 

                            <button 
                                type="button"className={ 
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

                                <input type="text"id="name" placeholder={  
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
                                    <input type="email" id="orgemail" placeholder="Enter your orgization email"
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

                                {error && <p className="login-error">{error}</p>}

                            <button to="/login" type="submit" className="primary-button" disabled={busy}> 

                                {busy
                                ? 'Creating account...'
                                : `Create ${accountType === 'volunteer' ? 'Volunteer' : 'Organization'} Account`}
                            </button> 

                        </form> 
                        <p className="link"> 
                            Already have an account?{' '} 
                            <Link to="/login">Login</Link> 
                        </p> 
                </div> 
            </div> 
        </div> 
    ) 
} 

export default Register