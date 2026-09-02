import { useState } from 'react' 
import { Link } from 'react-router-dom' 
import './index.css' 

function Register() { 
    const [accountType, setAccountType] = useState('volunteer') 

    return ( 
        <div className="register-page"> 

            <nav className="navbar"> 
                <div className="nav-links"> 
                    <Link to="/">GoodDeads</Link> 
                </div> 

                <div className="nav-links"> 
                    <Link to="/login">Login</Link> 
                    <Link to="/register">Register</Link> 
                </div>
            </nav> 

            <div className="register-content"> 
                <div className="register-container"> 
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

                        <form className="register-form"> 
                            <div className="form-group"> 
                                <label htmlFor="name"> 
                                    {accountType === 'volunteer' ? 'Full Name' : 'Organization Name'} 
                                </label> 

                                <input type="text"id="name" placeholder={  
                                    accountType === 'volunteer' ? 'Enter your full name' : 'Enter your organization name' 
                                    }/> 
                            </div> 

                            <div className="form-group"> 
                                <label htmlFor="email">Email</label> 
                                <input type="email" id="email" placeholder="Enter your email"/> 
                            </div> 

                            <div className="form-group"> 
                                <label htmlFor="password">Password</label> 
                                <input type="password" id="password" placeholder="Create a password" /> 
                            </div> 

                            {accountType === 'organization' && ( 
                                <div className="form-group"> 
                                    <label htmlFor="description"> 
                                        Organization Description 
                                    </label> 
                                    <textarea id="description" placeholder="Tell us about your organization" /> 
                                </div> 
                            )}

                            <Link to="/login" type="submit" className="register-submit"> 
                                Create { 
                                accountType === 'volunteer' ? 'Volunteer' : 'Organization' 
                                } Account 
                            </Link> 

                        </form> 
                        <p className="login-link"> 
                            Already have an account?{' '} 
                            <Link to="/login">Login</Link> 
                        </p> 
                </div> 
            </div> 
        </div> 
    ) 
} 

export default Register