import { Link } from 'react-router-dom'
import { isLoggedIn, getDashboardPath } from '../services/api'
import Navbar from '../components/Navbar.jsx'
import "../css/index.css"

function Home() {

  const loggedIn = isLoggedIn()

  return (
    <div className="home-page">

      <Navbar />

      <section className="about home-hero">

        <p className="eyebrow">Small actions. Stronger communities.</p>

        <h2>Find a way to do good today.</h2>

        <p>
          GoodDeeds connects volunteers with local organizations and meaningful
          opportunities to make a difference.
        </p>

        {loggedIn ? (
          <Link to={getDashboardPath()} className="primary-button hero-button">
            Open your dashboard
          </Link>
        ) : (
          <Link to="/register" className="primary-button hero-button">
            Find your opportunity
          </Link>
        )}

      </section>

      <section className="info-container home-intro">

        <p className="eyebrow">How GoodDeeds works</p>

        <h2>There is a place for you here.</h2>

        <p>Whether you are ready to lend a hand or rally people around a cause, start with the path that fits you.</p>

        <div className="info-cards">

          <div className="info-card">

            <h3>Volunteer</h3>

            <p>
              Discover flexible local events, meet people who care, and give your time to causes that matter.
            </p>

            {!loggedIn && (
              <Link to="/register" className="primary-button">
                Join as a volunteer
              </Link>
            )}

          </div>

          <div className="info-card">

            <h3>Organization</h3>

            <p>
              Share the work your organization is doing and find committed volunteers to help move it forward.
            </p>

            {!loggedIn && (
              <Link to="/register" className="primary-button">
                Create an organization account
              </Link>
            )}

          </div>

        </div>

      </section>

    </div>
  )
}

export default Home
