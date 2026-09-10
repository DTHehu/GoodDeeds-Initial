import { useEffect, useState } from 'react'
import { api } from '../services/api'
import { formatEventWhen } from '../services/format'
import Navbar from '../components/Navbar.jsx'
import Modal from '../components/Modal.jsx'
import EmptyState from '../components/EmptyState.jsx'
import {
    AlertIcon,
    BuildingIcon,
    CalendarIcon,
    MailIcon,
    MapPinIcon,
    PhoneIcon,
    SearchIcon,
} from '../components/Icons.jsx'
import "../css/index.css"

function Organizations() {

    const [organizations, setOrganizations] = useState([])
    const [error, setError] = useState("")
    const [loading, setLoading] = useState(true)
    const [search, setSearch] = useState("")

    const [selectedOrg, setSelectedOrg] = useState(null)
    const [orgEvents, setOrgEvents] = useState([])
    const [eventsError, setEventsError] = useState("")
    const [eventsLoading, setEventsLoading] = useState(false)

    useEffect(() => {

        let cancelled = false

        api.get("/organizations")
            .then((data) => {
                if (cancelled) return

                setOrganizations(data)
                setError("")
                setLoading(false)
            })
            .catch((requestError) => {
                if (cancelled) return

                console.error(requestError)
                setError("Could not load organizations.")
                setLoading(false)
            })

        return () => {
            cancelled = true
        }
    }, [])

    const term = search.trim().toLowerCase()

    const visibleOrganizations = organizations.filter((organization) =>
        (organization.name || "").toLowerCase().includes(term) ||
        (organization.description || "").toLowerCase().includes(term)
    )

    async function viewEvents(organization) {
        setSelectedOrg(organization)
        setOrgEvents([])
        setEventsError("")
        setEventsLoading(true)

        try {
            const allEvents = await api.get("/events/events")
            setOrgEvents(allEvents.filter((event) => event.organizationId === organization.id))
        } catch (requestError) {
            console.error(requestError)
            setEventsError("Could not load this organization's events.")
        }

        setEventsLoading(false)
    }

    function closePopup() {
        setSelectedOrg(null)
    }

    return (
        <div className="page">

            <Navbar />

            <header className="page-head">
                <div className="page-head-inner">

                    <div>
                        <p className="page-eyebrow">Directory</p>
                        <h1 className="page-title">Organizations</h1>
                        <p className="page-sub">
                            Browse the organizations creating volunteer opportunities on GoodDeeds.
                        </p>
                    </div>

                    <div className="stat-row">
                        <div className="stat">
                            <span className="stat-value">{organizations.length}</span>
                            <span className="stat-label">Listed</span>
                        </div>
                    </div>

                </div>
            </header>

            <main className="page-body shell section">

                <div className="section-head">
                    <div className="search">
                        <SearchIcon />
                        <input
                            className="input"
                            type="text"
                            placeholder="Search organizations..."
                            value={search}
                            onChange={(event) => setSearch(event.target.value)}
                        />
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
                                <div className="skeleton h-11 w-11 rounded-xl" />
                                <div className="skeleton mt-4 h-4 w-2/3" />
                                <div className="skeleton mt-3 h-3 w-full" />
                                <div className="skeleton mt-2 h-3 w-4/5" />
                                <div className="skeleton mt-6 h-9 w-full" />
                            </div>
                        ))}
                    </div>
                )}

                {!loading && !error && visibleOrganizations.length === 0 && (
                    <EmptyState
                        icon={<BuildingIcon />}
                        title={term ? "No matches" : "No organizations yet"}
                        text={term
                            ? `Nothing matched "${search}". Try a different search.`
                            : "Once an organization signs up, it will show up here."}
                    />
                )}

                <div className="card-grid">

                    {visibleOrganizations.map((organization) => (

                        <div className="card" key={organization.id}>

                            <div className="card-initial">
                                {(organization.name || "?").charAt(0).toUpperCase()}
                            </div>

                            <h2 className="card-title">{organization.name}</h2>

                            {organization.description && (
                                <p className="card-text">{organization.description}</p>
                            )}

                            <div className="card-meta">

                                <p className="meta-row">
                                    <MailIcon />
                                    <span className="min-w-0 break-words">{organization.contactEmail}</span>
                                </p>

                                {organization.phoneNumber && (
                                    <p className="meta-row">
                                        <PhoneIcon />
                                        <span>{organization.phoneNumber}</span>
                                    </p>
                                )}

                            </div>

                            <div className="card-actions mt-auto">
                                <button
                                    onClick={() => viewEvents(organization)}
                                    className="btn btn-secondary"
                                >
                                    <CalendarIcon />
                                    View events
                                </button>
                            </div>

                        </div>

                    ))}

                </div>

            </main>

            {selectedOrg && (
                <Modal
                    title={selectedOrg.name}
                    subtitle="Upcoming and past events"
                    onClose={closePopup}
                    footer={
                        <button onClick={closePopup} className="btn btn-secondary">
                            Close
                        </button>
                    }
                >

                    {eventsError && (
                        <p className="alert alert-error">
                            <AlertIcon />
                            {eventsError}
                        </p>
                    )}

                    {eventsLoading && (
                        <div className="row-list">
                            <div className="skeleton h-14 w-full rounded-lg" />
                            <div className="skeleton h-14 w-full rounded-lg" />
                        </div>
                    )}

                    {!eventsLoading && !eventsError && orgEvents.length === 0 && (
                        <EmptyState
                            small
                            icon={<CalendarIcon />}
                            title="No events yet"
                            text="This organization hasn't posted any opportunities."
                        />
                    )}

                    {!eventsLoading && orgEvents.length > 0 && (
                        <div className="row-list">
                            {orgEvents.map((event) => (
                                <div className="row" key={event.id}>
                                    <div className="min-w-0">
                                        <p className="row-title">{event.title}</p>
                                        <p className="row-sub">
                                            {formatEventWhen(event.startTime, event.endTime)}
                                        </p>
                                    </div>

                                    <span className="badge badge-muted shrink-0">
                                        <MapPinIcon />
                                        {event.location}
                                    </span>
                                </div>
                            ))}
                        </div>
                    )}

                </Modal>
            )}

        </div>
    )
}

export default Organizations
