import { useState } from 'react' 
import { Link } from 'react-router-dom' 
import './index.css' 

function Login() { 
    const [accountType, setAccountType] = useState('volunteer') 
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

                            } onClick={() => setAccountType('organization')} > 
                            Organization 
                        </button> 
                    </div> 

                    <form className="login-form"> 
                        <div className="form-group"> 
                            <label htmlFor="email">Email</label> 
                            <input type="email" id="email" placeholder="Enter your email" /> 
                        </div>

                        <div className="form-group"> 
                            <label htmlFor="password">Password</label> 
                            <input type="password" id="password" placeholder="Enter your password"/> 
                        </div> 

                       <button type="submit" className="login-button"> 
                            <Link to={accountType === 'volunteer' ? '/vol-dashboard' : '/org-dashboard'}> 
                                Login as {accountType === 'volunteer' ? 'Volunteer' : 'Organization'} 
                            </Link> 
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