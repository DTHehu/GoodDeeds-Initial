import { Link } from 'react-router-dom'
import { isLoggedIn, getDashboardPath } from '../services/api'

function Footer() {
    const loggedIn = isLoggedIn()

    return (
        <footer className="footer">
            <div className="footer-inner">

                <p>&copy; {new Date().getFullYear()} GoodDeeds &mdash; making it easier to help.</p>

                <div className="flex gap-5">
                    {loggedIn ? (
                        <>
                            <Link className="link" to={getDashboardPath()}>Dashboard</Link>
                            <Link className="link" to="/organizations">Organizations</Link>
                        </>
                    ) : (
                        <>
                            <Link className="link" to="/login">Login</Link>
                            <Link className="link" to="/register">Sign up</Link>
                        </>
                    )}
                </div>

            </div>
        </footer>
    )
}

export default Footer
