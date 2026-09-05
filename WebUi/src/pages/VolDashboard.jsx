import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import "../css/index.css"

function VolDashboard() {

    const [events, setEvents] = useState([])
    const [search, setSearch] = useState("")

    useEffect(() => {
        getEvents()
    }, [])

    async function getEvents() {

        try {
            const response = await fetch("http://localhost:5160/api/events/events")

            if (!response.ok) {
                throw new Error("Failed to get events")
            }

            const data = await response.json()

            setEvents(data)

        } catch (error) {
            console.error(error)
        }
    }

    const filteredEvents = events.filter((event) =>
        event.title.toLowerCase().includes(search.toLowerCase()) ||
        event.description.toLowerCase().includes(search.toLowerCase()) ||
        event.location.toLowerCase().includes(search.toLowerCase())
    )

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