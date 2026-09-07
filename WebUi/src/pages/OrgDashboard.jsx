import { useEffect, useState } from 'react'
import { api } from '../services/api'
import Navbar from '../components/Navbar.jsx'
import "../css/index.css"

function OrgDashboard() {

    const [showForm, setShowForm] = useState(false)
    const [editingEventId, setEditingEventId] = useState(null)
    const [eventName, setEventName] = useState("")
    const [description, setDescription] = useState("")
    const [location, setLocation] = useState("")
    const [startDate, setStartDate] = useState("")
    const [startClock, setStartClock] = useState("")
    const [endDate, setEndDate] = useState("")
    const [endClock, setEndClock] = useState("")
    const [events, setEvents] = useState([])
    const [error, setError] = useState("")
    const [formError, setFormError] = useState("")

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

    function resetForm() {
        setEventName("")
        setDescription("")
        setLocation("")
        setStartDate("")
        setStartClock("")
        setEndDate("")
        setEndClock("")
    }

    function splitDateTime(isoString) {
        const dateTime = new Date(isoString)
        const pad = (n) => String(n).padStart(2, "0")

        const date = `${dateTime.getFullYear()}-${pad(dateTime.getMonth() + 1)}-${pad(dateTime.getDate())}`
        const clock = `${pad(dateTime.getHours())}:${pad(dateTime.getMinutes())}`

        return [date, clock]
    }

    function startCreate() {
        resetForm()
        setEditingEventId(null)
        setFormError("")
        setShowForm(true)
    }

    function startEdit(event) {
        const [eventStartDate, eventStartClock] = splitDateTime(event.startTime)
        const [eventEndDate, eventEndClock] = splitDateTime(event.endTime)

        setEventName(event.title)
        setDescription(event.description)
        setLocation(event.location)
        setStartDate(eventStartDate)
        setStartClock(eventStartClock)
        setEndDate(eventEndDate)
        setEndClock(eventEndClock)
        setEditingEventId(event.id)
        setFormError("")
        setShowForm(true)
    }

    function cancelForm() {
        resetForm()
        setEditingEventId(null)
        setShowForm(false)
    }

    async function saveEvent(e) {
        e.preventDefault()
        setFormError("")

        const startTime = new Date(`${startDate}T${startClock}`)
        const endTime = new Date(`${endDate}T${endClock}`)

        if (endTime <= startTime) {
            setFormError("End time must be after start time.")
            return
        }

        const eventPayload = {
            title: eventName,
            description: description,
            location: location,
            startTime: startTime.toISOString(),
            endTime: endTime.toISOString()
        }

        const wasEditing = editingEventId != null

        try {
            if (wasEditing) {
                await api.put(`/events/${editingEventId}`, eventPayload)
            } else {
                await api.post('/events', eventPayload)
            }
        } catch (error) {
            console.error(error)
            alert(wasEditing ? "There was a problem updating the event." : "There was a problem creating the event.")
            return
        }

        cancelForm()

        // Event is already saved; a failure here just means the list is stale.
        try {
            setEvents(await loadEvents())
        } catch (error) {
            console.error(error)
            setError(`Event ${wasEditing ? "updated" : "created"}, but the list could not be refreshed. Reload the page to see it.`)
        }
    }

    async function deleteEvent(event) {
        if (!window.confirm(`Cancel "${event.title}"? This cannot be undone.`)) {
            return
        }

        try {
            await api.del(`/events/${event.id}`)
            setEvents(await loadEvents())
        } catch (error) {
            console.error(error)
            setError("Could not cancel the event.")
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
                        onClick={showForm ? cancelForm : startCreate}
                    >
                        {showForm ? "Cancel" : "Add Event"}
                    </button>

                    {showForm && (
                        <form className="event-form" onSubmit={saveEvent}>

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
                                <label>Start</label>
                                <div className="datetime-row">
                                    <input
                                        type="date"
                                        value={startDate}
                                        onChange={(e) => setStartDate(e.target.value)}
                                        required
                                    />
                                    <input
                                        type="time"
                                        value={startClock}
                                        onChange={(e) => setStartClock(e.target.value)}
                                        required
                                    />
                                </div>
                            </div>

                            <div className="form-group">
                                <label>End</label>
                                <div className="datetime-row">
                                    <input
                                        type="date"
                                        value={endDate}
                                        onChange={(e) => setEndDate(e.target.value)}
                                        required
                                    />
                                    <input
                                        type="time"
                                        value={endClock}
                                        onChange={(e) => setEndClock(e.target.value)}
                                        required
                                    />
                                </div>
                            </div>

                            {formError && <p className="error">{formError}</p>}

                            <button type="submit" className="primary-button">
                                {editingEventId ? "Save Changes" : "Create Event"}
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
                                      <button
                                          onClick={() => startEdit(event)}
                                          className="primary-button"
                                      >
                                          Edit
                                      </button>
                                      <button
                                          onClick={() => deleteEvent(event)}
                                          className="primary-button"
                                      >
                                          Cancel Event
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
