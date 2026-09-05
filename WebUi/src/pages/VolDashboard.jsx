import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { api } from '../services/api'
import "../css/index.css"

function VolDashboard() {

    const [events, setEvents] = useState([])
    const [search, setSearch] = useState("")
    const [error, setError] = useState("")

    useEffect(() => {

        let cancelled = false

        api.get("/events/events")
            .then((data) => {
                if (cancelled) return

                setEvents(data)
                setError("")
            })
            .catch((requestError) => {
                if (cancelled) return

                console.error(requestError)
                setError("Could not load events. You may need to log in again.")
            })

        return () => {
            cancelled = true
        }
    }, [])

    const filteredEvents = events.filter((event) => {

        const term = search.toLowerCase()

        return (event.title || "").toLowerCase().includes(term) ||
            (event.description || "").toLowerCase().includes(term) ||
            (event.location || "").toLowerCase().includes(term)
    })

    return (
        <div className="home-page">

            {/* Navbar */}
            <nav className="navbar">

                <div className="link">
                    <Link to="/">GoodDeads</Link>
                </div>

                <div className="link">
                    <Link to="/login">Login</Link>
                </div>

            </nav>


            {/* About Section */}
            <section className="about">

                <h2>VOLUNTEER DASHBOARD</h2>

                <p>
                    Connect with organizations and find opportunities
                    to make a difference in your community.
                </p>

            </section>


            {/* Search */}
            <section className="search-section">

                <h2>Find an Opportunity</h2>

                <input
                    type="text"
                    placeholder="Search events..."
                    value={search}
                    onChange={(e) => setSearch(e.target.value)}
                />

            </section>


            {/* Events */}
            <section className="info-container">

                <h2>Volunteer Opportunities</h2>

                {error && <p className="error">{error}</p>}

                <div className="info-cards">

                    {filteredEvents.map((event) => (

                        <div className="info-card" key={event.id}>

                            <h3>{event.title}</h3>

                            <p>
                                {event.description}
                            </p>

                            <p>
                                <strong>Location:</strong>{" "}
                                {event.location}
                            </p>

                            <p>
                                <strong>Start:</strong>{" "}
                                {new Date(event.startTime).toLocaleString()}
                            </p>

                            <p>
                                <strong>End:</strong>{" "}
                                {new Date(event.endTime).toLocaleString()}
                            </p>

                            <div className="card-buttons">

                                <Link
                                    to={`/event/${event.id}`}
                                    className="primary-button"
                                >
                                    View Information
                                </Link>

                                <button className="primary-button">
                                    Register for Event
                                </button>

                            </div>

                        </div>

                    ))}

                </div>

            </section>

        </div>
    )
}

export default VolDashboard