import { Link, useNavigate } from 'react-router-dom'
import { isLoggedIn, getDashboardPath, clearTokens } from '../services/api'

function Navbar() {
    const navigate = useNavigate()
    const loggedIn = isLoggedIn()

    function logOut() {
        clearTokens()
        navigate('/login')
    }

    return (
        <nav className="navbar">

            <div className="link">
                <Link to="/">GoodDeeds</Link>
            </div>

            <div className="nav-links">

                {loggedIn ? (
                    <>
                        <Link to={getDashboardPath()}>Dashboard</Link>

                        <button type="button" className="nav-button" onClick={logOut}>
                            Log out
                        </button>
                    </>
                ) : (
                    <>
                        <Link to="/login">Login</Link>
                        <Link to="/register">Sign up</Link>
                    </>
                )}

            </div>

        </nav>
    )
}

export default Navbar
