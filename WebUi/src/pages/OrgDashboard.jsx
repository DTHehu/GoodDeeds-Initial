import { useEffect, useState } from 'react'
import { api } from '../services/api'
import Navbar from '../components/Navbar.jsx'
import "../css/index.css"

function OrgDashboard() {

    const [showForm, setShowForm] = useState(false)
    const [eventName, setEventName] = useState("")
    const [description, setDescription] = useState("")
    const [location, setLocation] = useState("")
    const [startTime, setStartTime] = useState("")
    const [endTime, setEndTime] = useState("")
    const [events, setEvents] = useState([])
    const [error, setError] = useState("")

    async function loadEvents() {
        const [user, allEvents] = await Promise.all([
            api.get('/auth/me'),
            api.get('/events/events')
        ])

        const orgId = user.organization ? user.organization.id : null

        return allEvents.filter((event) => event.organizationId === orgId)
    }

    useEffect(() => {

        let cancelled = false

        loadEvents()
            .then((orgEvents) => {
                if (cancelled) return

                setEvents(orgEvents)
                setError("")
            })
            .catch((requestError) => {
                if (cancelled) return

                console.error(requestError)
                setError("Could not load your events.")
            })

        return () => {
            cancelled = true
        }
    }, [])

    async function addEvent(e) {
        e.preventDefault()

        const newEvent = {
            title: eventName,
            description: description,
            location: location,
            // datetime-local has no timezone. Reading it through Date treats it
            // as the browser's local time, and toISOString converts to UTC.
            startTime: new Date(startTime).toISOString(),
            endTime: new Date(endTime).toISOString()
        }

        try {
            await api.post('/events', newEvent)

            setEventName("")
            setDescription("")
            setLocation("")
            setStartTime("")
            setEndTime("")
            setShowForm(false)

            setEvents(await loadEvents())

        } catch (error) {
            console.error(error)
            alert("There was a problem creating the event.")
        }
    }

    return (
        <div className="home-page">

            <Navbar />


            <div className="dashboard">
                {/* Sidebar */}
                <aside className="sidebar">

                    <h3>Organization</h3>

                    <button
                        className="primary-button"
                        onClick={() => setShowForm(!showForm)}
                    >
                        Add Event
                    </button>

                    {showForm && (
                        <form className="form" onSubmit={addEvent}>

                            <label>Event Name</label>
                            <input
                                className="input"
                                type="text"
                                value={eventName}
                                onChange={(e) => setEventName(e.target.value)}
                                required
                            />

                            <label>Description</label>
                            <textarea
                                className="input"
                                value={description}
                                onChange={(e) => setDescription(e.target.value)}
                                required
                            />

                            <label>Location</label>
                            <input
                                className="input"
                                type="text"
                                value={location}
                                onChange={(e) => setLocation(e.target.value)}
                                required
                            />

                            <label>Start Time</label>
                            <input
                                className="input"
                                type="datetime-local"
                                value={startTime}
                                onChange={(e) => setStartTime(e.target.value)}
                                required
                            />

                            <label>End Time</label>
                            <input
                                className="input"
                                type="datetime-local"
                                value={endTime}
                                onChange={(e) => setEndTime(e.target.value)}
                                required
                            />

                            <button type="submit" className="primary-button">
                                Create Event
                            </button>

                        </form>
                    )}

                </aside>


                {/* Dashboard Content */}
                <main className="dashboard-content">

                    <section className="about">

                        <h2>ORGANIZATION DASHBOARD</h2>

                        <p>
                            Create volunteer opportunities and connect
                            with people who want to help.
                        </p>

                    </section>


                    <section className="info-container">

                        <h2>Your Events</h2>

                        {error && <p className="error">{error}</p>}

                        <div className="info-cards">

                            {events.map((event) => (

                                <div className="info-card" key={event.id}>

                                    <h3>{event.title}</h3>

                                    <p>{event.description}</p>

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

                                </div>

                            ))}

                        </div>

                    </section>

                </main>

            </div>

        </div>
    )
}

export default OrgDashboard