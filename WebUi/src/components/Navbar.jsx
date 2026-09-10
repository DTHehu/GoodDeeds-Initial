import { Link, NavLink, useNavigate } from 'react-router-dom'
import { isLoggedIn, getDashboardPath, clearTokens } from '../services/api'
import { HeartIcon, LogOutIcon } from './Icons.jsx'

function Navbar() {
    const navigate = useNavigate()
    const loggedIn = isLoggedIn()

    function logOut() {
        clearTokens()
        navigate('/login')
    }

    function linkClass({ isActive }) {
        return isActive ? 'nav-link is-active' : 'nav-link'
    }

    return (
        <nav className="navbar">
            <div className="navbar-inner">

                <Link to="/" className="brand">
                    <span className="brand-mark">
                        <HeartIcon className="h-[18px] w-[18px]" />
                    </span>
                    {/* Signed in there are more links to fit, so the phone
                        keeps the mark and drops the wordmark. */}
                    <span className={loggedIn ? 'brand-name hidden sm:inline' : 'brand-name'}>
                        GoodDeeds
                    </span>
                </Link>

                <div className="nav-links">

                    {loggedIn ? (
                        <>
                            <NavLink to={getDashboardPath()} className={linkClass}>
                                Dashboard
                            </NavLink>

                            <NavLink to="/organizations" className={linkClass}>
                                Organizations
                            </NavLink>

                            <span className="nav-divider" />

                            <button type="button" className="btn btn-ghost btn-sm" onClick={logOut}>
                                <LogOutIcon />
                                <span className="hidden sm:inline">Log out</span>
                            </button>
                        </>
                    ) : (
                        <>
                            <NavLink to="/login" className={linkClass}>
                                Login
                            </NavLink>

                            <Link to="/register" className="btn btn-primary btn-sm">
                                Sign up
                            </Link>
                        </>
                    )}

                </div>

            </div>
        </nav>
    )
}

export default Navbar
