import { useEffect, useState } from 'react'
import { api } from '../services/api'
import { formatDateTime, formatEventWhen, isUpcoming } from '../services/format'
import Navbar from '../components/Navbar.jsx'
import Modal from '../components/Modal.jsx'
import EmptyState from '../components/EmptyState.jsx'
import {
    AlertIcon,
    CalendarIcon,
    ClockIcon,
    MailIcon,
    MapPinIcon,
    PencilIcon,
    PlusIcon,
    TrashIcon,
    UsersIcon,
    XIcon,
} from '../components/Icons.jsx'
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
    const [saving, setSaving] = useState(false)
    const [loading, setLoading] = useState(true)

    const [selectedEvent, setSelectedEvent] = useState(null)
    const [registrations, setRegistrations] = useState([])
    const [registrationsError, setRegistrationsError] = useState("")
    const [registrationsLoading, setRegistrationsLoading] = useState(false)

    const [pendingDelete, setPendingDelete] = useState(null)
    const [deleting, setDeleting] = useState(false)

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
                setLoading(false)
            })
            .catch((requestError) => {
                if (cancelled) return

                console.error(requestError)
                setError("Could not load your events.")
                setLoading(false)
            })

        return () => {
            cancelled = true
        }
    }, [])

    const upcomingCount = events.filter((event) => isUpcoming(event.startTime)).length

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

        window.scrollTo({ top: 0, behavior: 'smooth' })
    }

    function cancelForm() {
        resetForm()
        setEditingEventId(null)
        setFormError("")
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

        setSaving(true)

        try {
            if (wasEditing) {
                await api.put(`/events/${editingEventId}`, eventPayload)
            } else {
                await api.post('/events', eventPayload)
            }
        } catch (error) {
            console.error(error)
            setFormError(wasEditing
                ? "There was a problem updating the event."
                : "There was a problem creating the event.")
            setSaving(false)
            return
        }

        setSaving(false)
        cancelForm()

        // Event is already saved; a failure here just means the list is stale.
        try {
            setEvents(await loadEvents())
        } catch (error) {
            console.error(error)
            setError(`Event ${wasEditing ? "updated" : "created"}, but the list could not be refreshed. Reload the page to see it.`)
        }
    }

    async function confirmDelete() {
        setDeleting(true)

        try {
            await api.del(`/events/${pendingDelete.id}`)
            setEvents(await loadEvents())
            setError("")
        } catch (error) {
            console.error(error)
            setError("Could not cancel the event.")
        }

        setDeleting(false)
        setPendingDelete(null)
    }

    async function viewVolunteers(event) {
        setSelectedEvent(event)
        setRegistrations([])
        setRegistrationsError("")
        setRegistrationsLoading(true)

        try {
            setRegistrations(await api.get(`/events/${event.id}/registrations`))
        } catch (requestError) {
            console.error(requestError)
            setRegistrationsError("Could not load volunteers for this event.")
        }

        setRegistrationsLoading(false)
    }

    function closePopup() {
        setSelectedEvent(null)
    }

    // Confirmed reads as good news, anything still pending as a nudge.
    function statusBadgeClass(status) {
        const settled = String(status).toLowerCase() === "confirmed"
        return settled ? "badge badge-brand shrink-0" : "badge badge-amber shrink-0"
    }

    return (
        <div className="page">

            <Navbar />

            <header className="page-head">
                <div className="page-head-inner">

                    <div>
                        <p className="page-eyebrow">Organization</p>
                        <h1 className="page-title">Your dashboard</h1>
                        <p className="page-sub">
                            Create volunteer opportunities and connect with people
                            who want to help.
                        </p>
                    </div>

                    <div className="stat-row">
                        <div className="stat">
                            <span className="stat-value">{events.length}</span>
                            <span className="stat-label">Events</span>
                        </div>

                        <div className="stat">
                            <span className="stat-value">{upcomingCount}</span>
                            <span className="stat-label">Upcoming</span>
                        </div>
                    </div>

                </div>
            </header>

            <main className="page-body">
                <div className="dash">

                    {/* Create / edit panel */}
                    <aside className="dash-side">
                        <div className="panel">

                            <div className="panel-head">
                                <h2 className="panel-title">
                                    {showForm
                                        ? (editingEventId ? "Edit event" : "New event")
                                        : "Events"}
                                </h2>

                                {showForm && (
                                    <button
                                        type="button"
                                        className="icon-button"
                                        onClick={cancelForm}
                                        aria-label="Close form"
                                    >
                                        <XIcon />
                                    </button>
                                )}
                            </div>

                            {!showForm && (
                                <>
                                    <p className="mb-4 text-sm leading-relaxed text-slate-500">
                                        Post an opportunity and volunteers will be able to
                                        find and register for it.
                                    </p>

                                    <button className="btn btn-primary btn-block" onClick={startCreate}>
                                        <PlusIcon />
                                        Add event
                                    </button>
                                </>
                            )}

                            {showForm && (
                                <form className="form" onSubmit={saveEvent}>

                                    <div className="field">
                                        <label className="field-label" htmlFor="event-name">Event name</label>
                                        <input
                                            className="input"
                                            id="event-name"
                                            type="text"
                                            placeholder="Saturday food drive"
                                            value={eventName}
                                            onChange={(e) => setEventName(e.target.value)}
                                            required
                                        />
                                    </div>

                                    <div className="field">
                                        <label className="field-label" htmlFor="event-description">Description</label>
                                        <textarea
                                            className="textarea"
                                            id="event-description"
                                            placeholder="What will volunteers be doing?"
                                            value={description}
                                            onChange={(e) => setDescription(e.target.value)}
                                            required
                                        />
                                    </div>

                                    <div className="field">
                                        <label className="field-label" htmlFor="event-location">Location</label>
                                        <input
                                            className="input"
                                            id="event-location"
                                            type="text"
                                            placeholder="123 Main St."
                                            value={location}
                                            onChange={(e) => setLocation(e.target.value)}
                                            required
                                        />
                                    </div>

                                    <div className="field">
                                        <label className="field-label">Start</label>
                                        <div className="field-row">
                                            <input
                                                className="input"
                                                type="date"
                                                aria-label="Start date"
                                                value={startDate}
                                                onChange={(e) => setStartDate(e.target.value)}
                                                required
                                            />
                                            <input
                                                className="input"
                                                type="time"
                                                aria-label="Start time"
                                                value={startClock}
                                                onChange={(e) => setStartClock(e.target.value)}
                                                required
                                            />
                                        </div>
                                    </div>

                                    <div className="field">
                                        <label className="field-label">End</label>
                                        <div className="field-row">
                                            <input
                                                className="input"
                                                type="date"
                                                aria-label="End date"
                                                value={endDate}
                                                onChange={(e) => setEndDate(e.target.value)}
                                                required
                                            />
                                            <input
                                                className="input"
                                                type="time"
                                                aria-label="End time"
                                                value={endClock}
                                                onChange={(e) => setEndClock(e.target.value)}
                                                required
                                            />
                                        </div>
                                    </div>

                                    {formError && (
                                        <p className="alert alert-error">
                                            <AlertIcon />
                                            {formError}
                                        </p>
                                    )}

                                    <div className="flex gap-2">
                                        <button
                                            type="button"
                                            className="btn btn-secondary flex-1"
                                            onClick={cancelForm}
                                        >
                                            Cancel
                                        </button>

                                        <button type="submit" className="btn btn-primary flex-1" disabled={saving}>
                                            {saving
                                                ? "Saving..."
                                                : (editingEventId ? "Save changes" : "Create event")}
                                        </button>
                                    </div>

                                </form>
                            )}

                        </div>
                    </aside>

                    {/* Events */}
                    <div className="dash-main">

                        <div className="section-head">
                            <div>
                                <h2 className="section-title">Your events</h2>
                                <p className="section-sub">
                                    Everything your organization has posted.
                                </p>
                            </div>
                        </div>

                        {error && (
                            <p className="alert alert-error mb-6">
                                <AlertIcon />
                                {error}
                            </p>
                        )}

                        {loading && (
                            <div className="card-grid">
                                {[0, 1, 2].map((key) => (
                                    <div className="skeleton-card" key={key}>
                                        <div className="skeleton h-4 w-2/3" />
                                        <div className="skeleton mt-4 h-3 w-full" />
                                        <div className="skeleton mt-2 h-3 w-5/6" />
                                        <div className="skeleton mt-6 h-9 w-full" />
                                    </div>
                                ))}
                            </div>
                        )}

                        {!loading && !error && events.length === 0 && (
                            <EmptyState
                                icon={<CalendarIcon />}
                                title="No events yet"
                                text="Create your first opportunity and volunteers will be able to sign up for it."
                                action={
                                    <button className="btn btn-primary" onClick={startCreate}>
                                        <PlusIcon />
                                        Add event
                                    </button>
                                }
                            />
                        )}

                        <div className="card-grid">

                            {events.map((event) => (

                                <article className="card" key={event.id}>

                                    <div className="mb-3">
                                        {isUpcoming(event.startTime) ? (
                                            <span className="badge badge-brand">Upcoming</span>
                                        ) : (
                                            <span className="badge badge-muted">Past</span>
                                        )}
                                    </div>

                                    <h3 className="card-title">{event.title}</h3>

                                    {event.description && (
                                        <p className="card-text line-clamp-2">{event.description}</p>
                                    )}

                                    <div className="card-meta">

                                        <p className="meta-row">
                                            <MapPinIcon />
                                            <span>{event.location}</span>
                                        </p>

                                        <p className="meta-row">
                                            <ClockIcon />
                                            <span>{formatEventWhen(event.startTime, event.endTime)}</span>
                                        </p>

                                    </div>

                                    <div className="card-actions mt-auto">
                                        <button
                                            onClick={() => viewVolunteers(event)}
                                            className="btn btn-secondary btn-sm"
                                        >
                                            <UsersIcon />
                                            Volunteers
                                        </button>

                                        <button
                                            onClick={() => startEdit(event)}
                                            className="btn btn-secondary btn-sm"
                                        >
                                            <PencilIcon />
                                            Edit
                                        </button>

                                        <button
                                            onClick={() => setPendingDelete(event)}
                                            className="btn btn-danger btn-sm flex-none"
                                            aria-label={`Cancel ${event.title}`}
                                            title="Cancel event"
                                        >
                                            <TrashIcon />
                                        </button>
                                    </div>

                                </article>

                            ))}

                        </div>

                    </div>

                </div>
            </main>

            {selectedEvent && (
                <Modal
                    title="Registered volunteers"
                    subtitle={selectedEvent.title}
                    onClose={closePopup}
                    footer={
                        <button onClick={closePopup} className="btn btn-secondary">
                            Close
                        </button>
                    }
                >

                    {registrationsError && (
                        <p className="alert alert-error">
                            <AlertIcon />
                            {registrationsError}
                        </p>
                    )}

                    {registrationsLoading && (
                        <div className="row-list">
                            <div className="skeleton h-14 w-full rounded-lg" />
                            <div className="skeleton h-14 w-full rounded-lg" />
                        </div>
                    )}

                    {!registrationsLoading && !registrationsError && registrations.length === 0 && (
                        <EmptyState
                            small
                            icon={<UsersIcon />}
                            title="No one has registered yet"
                            text="Share the event so volunteers can find it."
                        />
                    )}

                    {!registrationsLoading && registrations.length > 0 && (
                        <div className="row-list">
                            {registrations.map((registration) => (
                                <div className="row" key={registration.userId}>

                                    <div className="min-w-0">
                                        <p className="row-title">{registration.name}</p>
                                        <p className="row-sub flex items-center gap-1.5">
                                            <MailIcon className="h-3.5 w-3.5" />
                                            {registration.email}
                                        </p>
                                    </div>

                                    <span className={statusBadgeClass(registration.status)}>
                                        {registration.status}
                                    </span>

                                </div>
                            ))}
                        </div>
                    )}

                </Modal>
            )}

            {pendingDelete && (
                <Modal
                    title="Cancel this event?"
                    subtitle={pendingDelete.title}
                    onClose={() => setPendingDelete(null)}
                    footer={
                        <>
                            <button
                                className="btn btn-secondary"
                                onClick={() => setPendingDelete(null)}
                                disabled={deleting}
                            >
                                Keep event
                            </button>

                            <button
                                className="btn btn-danger-solid"
                                onClick={confirmDelete}
                                disabled={deleting}
                            >
                                {deleting ? "Cancelling..." : "Cancel event"}
                            </button>
                        </>
                    }
                >
                    <p className="text-sm leading-relaxed text-slate-600">
                        This removes the event and everyone registered for it. It cannot be undone.
                    </p>

                    <p className="mt-4 text-sm text-slate-500">
                        {formatDateTime(pendingDelete.startTime)} &middot; {pendingDelete.location}
                    </p>
                </Modal>
            )}

        </div>
    )
}

export default OrgDashboard
