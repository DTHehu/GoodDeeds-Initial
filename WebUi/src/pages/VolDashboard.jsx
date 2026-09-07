import { useEffect, useState } from 'react'
import { api } from '../services/api'
import Navbar from '../components/Navbar.jsx'
import "../css/index.css"

function VolDashboard() {

    const [events, setEvents] = useState([])
    const [search, setSearch] = useState("")
    const [error, setError] = useState("")
    const [selectedEvent, setSelectedEvent] = useState(null)
    const [registerError, setRegisterError] = useState("")
    const [registering, setRegistering] = useState(false)
    const [registered, setRegistered] = useState(false)

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

    function openPopup(event) {
        setSelectedEvent(event)
        setRegisterError("")
        setRegistered(false)
    }

    function closePopup() {
        setSelectedEvent(null)
    }

    async function registerForEvent() {
        setRegistering(true)
        setRegisterError("")

        try {
            await api.post("/events/register", { eventId: selectedEvent.id })
            setRegistered(true)
        } catch (err) {
            console.error(err)
            setRegisterError(err.message || "Could not register for this event.")
        }

        setRegistering(false)
    }

    return (
        <div className="home-page">

            <Navbar />
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
                                <button
                                    onClick={() => openPopup(event)}
                                    className="primary-button"
                                >
                                    More Information
                                </button>
                            </div>

                        </div>

                    ))}

                </div>

            </section>

            {selectedEvent && (
                <div className="popup">
                    <div className="popup-content">

                        <p>
                            <strong>Organization:</strong>{" "}
                            {selectedEvent.organizationId}
                        </p>

                        <p>
                            <strong>Title:</strong>{" "}
                            {selectedEvent.title}
                        </p>

                        <p>
                            <strong>Description:</strong>{" "}
                            {selectedEvent.description}
                        </p>

                        <p>
                            <strong>Location:</strong>{" "}
                            {selectedEvent.location}
                        </p>

                        <p>
                            <strong>Start:</strong>{" "}
                            {new Date(selectedEvent.startTime).toLocaleString()}
                        </p>

                        <p>
                            <strong>End:</strong>{" "}
                            {new Date(selectedEvent.endTime).toLocaleString()}
                        </p>

                        {registerError && <p className="error">{registerError}</p>}
                        {registered && <p className="success">You're registered for this event.</p>}

                        {!registered && (
                            <button
                                className="primary-button"
                                onClick={registerForEvent}
                                disabled={registering}
                            >
                                {registering ? "Registering..." : "Register for Event"}
                            </button>
                        )} {" "}

                        <button onClick={closePopup} className="primary-button">
                            Close
                        </button>

                    </div>
                </div>
            )}

        </div>
    )
}

export default VolDashboard
