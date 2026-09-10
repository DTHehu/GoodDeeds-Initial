import { useEffect, useState } from 'react'
import { api } from '../services/api'
import { formatDate, formatEventWhen, isUpcoming } from '../services/format'
import Navbar from '../components/Navbar.jsx'
import Modal from '../components/Modal.jsx'
import EmptyState from '../components/EmptyState.jsx'
import {
    AlertIcon,
    BuildingIcon,
    CalendarIcon,
    CheckCircleIcon,
    ClockIcon,
    MapPinIcon,
    SearchIcon,
} from '../components/Icons.jsx'
import "../css/index.css"

function VolDashboard() {

    const [events, setEvents] = useState([])
    const [registeredEvents, setRegisteredEvents] = useState([])
    const [search, setSearch] = useState("")
    const [error, setError] = useState("")
    const [loading, setLoading] = useState(true)
    const [selectedEvent, setSelectedEvent] = useState(null)
    const [registerError, setRegisterError] = useState("")
    const [submitting, setSubmitting] = useState(false)
    const [registered, setRegistered] = useState(false)
    const [organizationName, setOrganizationName] = useState("")

    useEffect(() => {

        let cancelled = false

        Promise.all([
            api.get("/events/events"),
            api.get("/events/registered")
        ])
            .then(([allEvents, myEvents]) => {
                if (cancelled) return

                setEvents(allEvents)
                setRegisteredEvents(myEvents)
                setError("")
                setLoading(false)
            })
            .catch((requestError) => {
                if (cancelled) return

                console.error(requestError)
                setError("Could not load events. You may need to log in again.")
                setLoading(false)
            })

        return () => {
            cancelled = true
        }
    }, [])

    function loadRegisteredEvents() {
        api.get("/events/registered").then(setRegisteredEvents).catch(console.error)
    }

    const registeredIds = new Set(registeredEvents.map((event) => event.id))

    const filteredEvents = events.filter((event) => {

        const term = search.toLowerCase()

        return (event.title || "").toLowerCase().includes(term) ||
            (event.description || "").toLowerCase().includes(term) ||
            (event.location || "").toLowerCase().includes(term)
    })

    async function openPopup(event) {
        setSelectedEvent(event)
        setRegisterError("")
        setRegistered(false)
        setOrganizationName("")

        try {
            const [isRegistered, organization] = await Promise.all([
                api.get(`/events/${event.id}/registration`),
                api.get(`/organizations/${event.organizationId}`)
            ])

            setRegistered(isRegistered)
            setOrganizationName(organization.name)
        } catch (err) {
            console.error(err)
        }
    }

    function closePopup() {
        setSelectedEvent(null)
    }

    async function registerForEvent() {
        setSubmitting(true)
        setRegisterError("")

        try {
            await api.post("/events/register", { eventId: selectedEvent.id })
            setRegistered(true)
            loadRegisteredEvents()
        } catch (err) {
            console.error(err)
            setRegisterError(err.message || "Could not register for this event.")
        }

        setSubmitting(false)
    }

    async function unregisterForEvent() {
        setSubmitting(true)
        setRegisterError("")

        try {
            await api.del(`/events/${selectedEvent.id}/register`)
            setRegistered(false)
            loadRegisteredEvents()
        } catch (err) {
            console.error(err)
            setRegisterError(err.message || "Could not unregister from this event.")
        }

        setSubmitting(false)
    }

    return (
        <div className="page">

            <Navbar />

            <header className="page-head">
                <div className="page-head-inner">

                    <div>
                        <p className="page-eyebrow">Volunteer</p>
                        <h1 className="page-title">Your dashboard</h1>
                        <p className="page-sub">
                            Find opportunities near you and keep track of everything
                            you have signed up for.
                        </p>
                    </div>

                    <div className="stat-row">
                        <div className="stat">
                            <span className="stat-value">{events.length}</span>
                            <span className="stat-label">Opportunities</span>
                        </div>

                        <div className="stat">
                            <span className="stat-value">{registeredEvents.length}</span>
                            <span className="stat-label">Registered</span>
                        </div>
                    </div>

                </div>
            </header>

            <main className="page-body">
                <div className="dash">

                    {/* Sidebar */}
                    <aside className="dash-side">
                        <div className="panel">

                            <div className="panel-head">
                                <h2 className="panel-title">My events</h2>
                                <span className="badge badge-brand">{registeredEvents.length}</span>
                            </div>

                            {registeredEvents.length === 0 ? (
                                <p className="text-sm leading-relaxed text-slate-500">
                                    You haven't registered for any events yet. Pick one from the
                                    list to get started.
                                </p>
                            ) : (
                                <div className="panel-scroll">
                                    {registeredEvents.map((event) => (
                                        <button
                                            type="button"
                                            key={event.id}
                                            className="list-item"
                                            onClick={() => openPopup(event)}
                                        >
                                            <span className="list-item-title">{event.title}</span>
                                            <span className="list-item-sub">
                                                <CalendarIcon />
                                                {formatDate(event.startTime)}
                                            </span>
                                        </button>
                                    ))}
                                </div>
                            )}

                        </div>
                    </aside>

                    {/* Opportunities */}
                    <div className="dash-main">

                        <div className="section-head">
                            <div className="search">
                                <SearchIcon />
                                <input
                                    className="input"
                                    type="text"
                                    placeholder="Search by title, description or place..."
                                    value={search}
                                    onChange={(e) => setSearch(e.target.value)}
                                />
                            </div>

                            <p className="text-sm text-slate-500">
                                {filteredEvents.length} {filteredEvents.length === 1 ? 'opportunity' : 'opportunities'}
                            </p>
                        </div>

                        {error && (
                            <p className="alert alert-error mb-6">
                                <AlertIcon />
                                {error}
                            </p>
                        )}

                        {loading && (
                            <div className="card-grid">
                                {[0, 1, 2, 3].map((key) => (
                                    <div className="skeleton-card" key={key}>
                                        <div className="skeleton h-4 w-2/3" />
                                        <div className="skeleton mt-4 h-3 w-full" />
                                        <div className="skeleton mt-2 h-3 w-5/6" />
                                        <div className="skeleton mt-6 h-9 w-full" />
                                    </div>
                                ))}
                            </div>
                        )}

                        {!loading && !error && filteredEvents.length === 0 && (
                            <EmptyState
                                icon={<CalendarIcon />}
                                title={search ? "No events matched" : "No opportunities yet"}
                                text={search
                                    ? `Nothing matched "${search}". Try a broader search.`
                                    : "Check back soon, organizations post new events regularly."}
                            />
                        )}

                        <div className="card-grid">

                            {filteredEvents.map((event) => (

                                <article className="card" key={event.id}>

                                    <div className="mb-3 flex flex-wrap items-center gap-2">
                                        {isUpcoming(event.startTime) ? (
                                            <span className="badge badge-brand">Upcoming</span>
                                        ) : (
                                            <span className="badge badge-muted">Past</span>
                                        )}

                                        {registeredIds.has(event.id) && (
                                            <span className="badge badge-amber">
                                                <CheckCircleIcon />
                                                Registered
                                            </span>
                                        )}
                                    </div>

                                    <h2 className="card-title">{event.title}</h2>

                                    {event.description && (
                                        <p className="card-text line-clamp-3">{event.description}</p>
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
                                            onClick={() => openPopup(event)}
                                            className="btn btn-primary"
                                        >
                                            More information
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
                    title={selectedEvent.title}
                    subtitle={organizationName || "Event details"}
                    onClose={closePopup}
                    footer={
                        <>
                            <button onClick={closePopup} className="btn btn-secondary">
                                Close
                            </button>

                            {registered ? (
                                <button
                                    className="btn btn-danger"
                                    onClick={unregisterForEvent}
                                    disabled={submitting}
                                >
                                    {submitting ? "Unregistering..." : "Unregister"}
                                </button>
                            ) : (
                                <button
                                    className="btn btn-primary"
                                    onClick={registerForEvent}
                                    disabled={submitting}
                                >
                                    {submitting ? "Registering..." : "Register for event"}
                                </button>
                            )}
                        </>
                    }
                >

                    {registered && (
                        <p className="alert alert-success mb-5">
                            <CheckCircleIcon />
                            You are registered for this event.
                        </p>
                    )}

                    {registerError && (
                        <p className="alert alert-error mb-5">
                            <AlertIcon />
                            {registerError}
                        </p>
                    )}

                    <div className="detail-list">

                        {organizationName && (
                            <div className="detail">
                                <BuildingIcon />
                                <span>
                                    <span className="detail-label">Organization</span>
                                    <span className="detail-value">{organizationName}</span>
                                </span>
                            </div>
                        )}

                        <div className="detail">
                            <MapPinIcon />
                            <span>
                                <span className="detail-label">Location</span>
                                <span className="detail-value">{selectedEvent.location}</span>
                            </span>
                        </div>

                        <div className="detail">
                            <ClockIcon />
                            <span>
                                <span className="detail-label">When</span>
                                <span className="detail-value">
                                    {formatEventWhen(selectedEvent.startTime, selectedEvent.endTime)}
                                </span>
                            </span>
                        </div>

                        {selectedEvent.description && (
                            <div className="detail">
                                <CalendarIcon />
                                <span>
                                    <span className="detail-label">Description</span>
                                    <span className="detail-value leading-relaxed">
                                        {selectedEvent.description}
                                    </span>
                                </span>
                            </div>
                        )}

                    </div>

                </Modal>
            )}

        </div>
    )
}

export default VolDashboard
