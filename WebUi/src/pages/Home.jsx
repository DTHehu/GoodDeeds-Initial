import { Link } from 'react-router-dom'
import { isLoggedIn, getDashboardPath } from '../services/api'
import Navbar from '../components/Navbar.jsx'
import Footer from '../components/Footer.jsx'
import {
  ArrowRightIcon,
  BuildingIcon,
  CalendarIcon,
  HeartIcon,
  SparkIcon,
  UsersIcon,
} from '../components/Icons.jsx'
import "../css/index.css"

const steps = [
  {
    icon: <UsersIcon />,
    title: "Create your account",
    text: "Sign up as a volunteer looking to help, or as an organization that needs hands.",
  },
  {
    icon: <CalendarIcon />,
    title: "Find or post an event",
    text: "Organizations post opportunities. Volunteers search them by title, place or cause.",
  },
  {
    icon: <HeartIcon />,
    title: "Show up and help",
    text: "Register in one click, and keep every event you signed up for on your dashboard.",
  },
]

function Home() {

  const loggedIn = isLoggedIn()

  return (
    <div className="page">

      <Navbar />

      <main className="page-body">

        <section className="hero">
          <div className="hero-inner">

            <span className="hero-eyebrow">
              <SparkIcon className="h-4 w-4" />
              Volunteering made easy
            </span>

            <h1 className="hero-title">
              Find the people who need your help.
            </h1>

            <p className="hero-sub">
              GoodDeeds connects volunteers with the organizations near them, so
              lending a hand takes minutes instead of phone calls.
            </p>

            <div className="hero-actions">
              {loggedIn ? (
                <Link to={getDashboardPath()} className="btn btn-invert">
                  Go to your dashboard
                  <ArrowRightIcon />
                </Link>
              ) : (
                <>
                  <Link to="/register" className="btn btn-invert">
                    Get started
                    <ArrowRightIcon />
                  </Link>

                  <Link to="/login" className="btn btn-outline-invert">
                    I already have an account
                  </Link>
                </>
              )}
            </div>

          </div>
        </section>

        <section className="shell section">

          <div className="section-head">
            <div>
              <h2 className="section-title">How GoodDeeds works</h2>
              <p className="section-sub">Three steps, whichever side you are on.</p>
            </div>
          </div>

          <div className="card-grid">
            {steps.map((step, index) => (
              <div className="card" key={step.title}>

                <div className="card-icon">{step.icon}</div>

                <span className="badge badge-muted self-start">Step {index + 1}</span>

                <h3 className="card-title mt-3">{step.title}</h3>

                <p className="card-text">{step.text}</p>

              </div>
            ))}
          </div>

        </section>

        <section className="shell pb-16">

          <div className="section-head">
            <div>
              <h2 className="section-title">Pick the account that fits you</h2>
              <p className="section-sub">Both are free, and take about a minute to set up.</p>
            </div>
          </div>

          <div className="grid gap-5 sm:grid-cols-2">

            <div className="card">

              <div className="card-icon">
                <UsersIcon />
              </div>

              <h3 className="card-title">Volunteer</h3>

              <p className="card-text">
                Browse opportunities from local organizations, register for the ones
                you can make, and track them all in one place.
              </p>

              {!loggedIn && (
                <div className="card-actions mt-auto">
                  <Link to="/register" className="btn btn-primary">
                    Sign up to volunteer
                  </Link>
                </div>
              )}

            </div>

            <div className="card">

              <div className="card-icon">
                <BuildingIcon />
              </div>

              <h3 className="card-title">Organization</h3>

              <p className="card-text">
                Post the events you need help with, edit them as plans change, and see
                exactly who has signed up.
              </p>

              {!loggedIn && (
                <div className="card-actions mt-auto">
                  <Link to="/register" className="btn btn-secondary">
                    Register your organization
                  </Link>
                </div>
              )}

            </div>

          </div>

        </section>

      </main>

      <Footer />

    </div>
  )
}

export default Home
