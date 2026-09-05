import { Link } from 'react-router-dom'
import { isLoggedIn, getDashboardPath } from '../services/api'
import Navbar from '../components/Navbar.jsx'
import "../css/index.css"

function Home() {

  const loggedIn = isLoggedIn()

  return (
    <div className="home-page">

      <Navbar />

      <section className="about">

        <h2>Volunteering Made Easy</h2>

        <p>
          Connect with organizations and find opportunities
          to make a difference in your community.
        </p>

        {loggedIn ? (
          <Link to={getDashboardPath()} className="primary-button hero-button">
            Go to your dashboard
          </Link>
        ) : (
          <Link to="/register" className="primary-button hero-button">
            Get started
          </Link>
        )}

      </section>

      <section className="info-container">

        <h2>Welcome to GoodDeeds</h2>

        <p>Choose how you would like to use GoodDeeds.</p>

        <div className="info-cards">

          <div className="info-card">

            <h3>Volunteer</h3>

            <p>
              Find volunteer opportunities and make a difference in your community.
            </p>

            {!loggedIn && (
              <Link to="/register" className="primary-button">
                Sign up to volunteer
              </Link>
            )}

          </div>

          <div className="info-card">

            <h3>Organization</h3>

            <p>
              Create volunteer opportunities and connect with people who want to help.
            </p>

            {!loggedIn && (
              <Link to="/register" className="primary-button">
                Register your organization
              </Link>
            )}

          </div>

        </div>

      </section>

    </div>
  )
}

export default Home
