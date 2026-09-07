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

    const [selectedEvent, setSelectedEvent] = useState(null)
    const [registrations, setRegistrations] = useState([])
    const [registrationsError, setRegistrationsError] = useState("")

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
            // datetime-local has no timezone, so Date treats it as local time; toISOString converts to UTC.
            startTime: new Date(startTime).toISOString(),
            endTime: new Date(endTime).toISOString()
        }

        try {
            await api.post('/events', newEvent)
        } catch (error) {
            console.error(error)
            alert("There was a problem creating the event.")
            return
        }

        setEventName("")
        setDescription("")
        setLocation("")
        setStartTime("")
        setEndTime("")
        setShowForm(false)

        // Event is already created; a failure here just means the list is stale.
        try {
            setEvents(await loadEvents())
        } catch (error) {
            console.error(error)
            setError("Event created, but the list could not be refreshed. Reload the page to see it.")
        }
    }

    async function viewVolunteers(event) {
        setSelectedEvent(event)
        setRegistrations([])
        setRegistrationsError("")

        try {
            setRegistrations(await api.get(`/events/${event.id}/registrations`))
        } catch (requestError) {
            console.error(requestError)
            setRegistrationsError("Could not load volunteers for this event.")
        }
    }

    function closePopup() {
        setSelectedEvent(null)
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
                        {showForm ? "Cancel" : "Add Event"}
                    </button>

                    {showForm && (
                        <form className="event-form" onSubmit={addEvent}>

                            <div className="form-group">
                                <label>Event Name</label>
                                <input
                                    type="text"
                                    value={eventName}
                                    onChange={(e) => setEventName(e.target.value)}
                                    required
                                />
                            </div>

                            <div className="form-group">
                                <label>Description</label>
                                <textarea
                                    value={description}
                                    onChange={(e) => setDescription(e.target.value)}
                                    required
                                />
                            </div>

                            <div className="form-group">
                                <label>Location</label>
                                <input
                                    type="text"
                                    value={location}
                                    onChange={(e) => setLocation(e.target.value)}
                                    required
                                />
                            </div>

                            <div className="form-group">
                                <label>Start Time</label>
                                <input
                                    type="datetime-local"
                                    value={startTime}
                                    onChange={(e) => setStartTime(e.target.value)}
                                    required
                                />
                            </div>

                            <div className="form-group">
                                <label>End Time</label>
                                <input
                                    type="datetime-local"
                                    value={endTime}
                                    onChange={(e) => setEndTime(e.target.value)}
                                    required
                                />
                            </div>

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
                                          onClick={() => viewVolunteers(event)}
                                          className="primary-button"
                                      >
                                          View Volunteers
                                      </button>
                                  </div>

                              </div>

                          ))}

                      </div>
                    </section>

                </main>

            </div>

            {selectedEvent && (
                <div className="popup">
                    <div className="popup-content">

                        <p>
                            <strong>Event:</strong>{" "}
                            {selectedEvent.title}
                        </p>

                        {registrationsError && <p className="error">{registrationsError}</p>}

                        {!registrationsError && registrations.length === 0 && (
                            <p>No one has registered yet.</p>
                        )}

                        {registrations.map((registration) => (
                            <p key={registration.userId}>
                                {registration.name} &mdash; {registration.email}{" "}
                                ({registration.status})
                            </p>
                        ))}

                        <button onClick={closePopup} className="primary-button">
                            Close
                        </button>

                    </div>
                </div>
            )}

        </div>
    )
}

export default OrgDashboard
