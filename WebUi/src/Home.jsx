import { Link } from 'react-router-dom'
import "./index.css"

function Home() {
  return (
    <div className="home-page">

      <nav className="navbar">
        <div className="nav-links">
          <Link to="/">Volunteering.com</Link>
        </div>

        <div className="nav-links">
          <Link to="/vol-login">Volunteer Login</Link>
          <Link to="/org-login">Organization Login</Link>
        </div>
      </nav>

      <section className="about">
        <h2>Volunteering Made Easy</h2>

        <p>
          Connect with organizations and find opportunities
          to make a difference in your community.
        </p>
      </section>

      <section className="login-options">
        <h2>Welcome to GoodDeeds</h2>

        <p>Choose how you would like to use GoodDeeds.</p>

        <div className="login-cards">

          {/* Volunteer */}
          <div className="login-card">
            <h3>Volunteer</h3>

            <p>
              Find volunteer opportunities and make a difference
              in your community.
            </p>

            <Link to="/vol-login" className="primary-button">
              Volunteer Login
            </Link>
          </div>

          {/* Organization */}
          <div className="login-card">
            <h3>Organization</h3>

            <p>
              Create volunteer opportunities and connect with
              people who want to help.
            </p>

            <Link to="/org-login" className="primary-button">
              Organization Login
            </Link>
          </div>

        </div>
      </section>

    </div>
  )
}

export default Home