import { Link } from 'react-router-dom'
import './index.css'

function VolLogin() {
    return (
        <div className="login-page">
            <div className="login-container">
                <h1>Welcome Back Volunteer</h1>

                <form className="login-form">
                    <label htmlFor="email">Email</label>
                    <input
                        type="email"
                        id="email"
                        placeholder="Enter your email"
                    />

                    <label htmlFor="password">Password</label>
                    <input
                        type="password"
                        id="password"
                        placeholder="Enter your password"
                    />

                    <div className="forgot-password">
                        <a href="#">Forgot password?</a>
                    </div>

                    <button type="submit" className="login-button">
                        Login
                    </button>
                </form>

                <p className="signup-text">
                    Don't have an account?{" "}
                    <a href="#">Sign up</a>
                </p>
            </div>
        </div>
    );
}

export default VolLogin;