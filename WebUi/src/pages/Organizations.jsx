import { useEffect, useState } from 'react'
import { api } from '../services/api'
import Navbar from '../components/Navbar.jsx'
import "../css/index.css"

function Organizations() {

    const [organizations, setOrganizations] = useState([])
    const [error, setError] = useState("")

    const [selectedOrg, setSelectedOrg] = useState(null)
    const [orgEvents, setOrgEvents] = useState([])
    const [eventsError, setEventsError] = useState("")

    useEffect(() => {

        let cancelled = false

        api.get("/organizations")
            .then((data) => {
                if (cancelled) return

                setOrganizations(data)
                setError("")
            })
            .catch((requestError) => {
                if (cancelled) return

                console.error(requestError)
                setError("Could not load organizations.")
            })

        return () => {
            cancelled = true
        }
    }, [])

    async function viewEvents(organization) {
        setSelectedOrg(organization)
        setOrgEvents([])
        setEventsError("")

        try {
            const allEvents = await api.get("/events/events")
            setOrgEvents(allEvents.filter((event) => event.organizationId === organization.id))
        } catch (requestError) {
            console.error(requestError)
            setEventsError("Could not load this organization's events.")
        }
    }

    function closePopup() {
        setSelectedOrg(null)
    }

    return (
        <div className="home-page">

            <Navbar />

            <section className="about">

                <h2>ORGANIZATIONS</h2>

                <p>
                    Browse the organizations creating volunteer opportunities on GoodDeeds.
                </p>

            </section>

            <section className="info-container">

                {error && <p className="error">{error}</p>}

                {!error && organizations.length === 0 && (
                    <p>No organizations yet.</p>
                )}

                <div className="info-cards">

                    {organizations.map((organization) => (

                        <div className="info-card" key={organization.id}>

                            <h3>{organization.name}</h3>

                            {organization.description && <p>{organization.description}</p>}

                            <p>
                                <strong>Contact:</strong>{" "}
                                {organization.contactEmail}
                            </p>

                            {organization.phoneNumber && (
                                <p>
                                    <strong>Phone:</strong>{" "}
                                    {organization.phoneNumber}
                                </p>
                            )}

                            <div className="card-buttons">
                                <button
                                    onClick={() => viewEvents(organization)}
                                    className="primary-button"
                                >
                                    View Events
                                </button>
                            </div>

                        </div>

                    ))}

                </div>

            </section>

            {selectedOrg && (
                <div className="popup">
                    <div className="popup-content">

                        <p>
                            <strong>Organization:</strong>{" "}
                            {selectedOrg.name}
                        </p>

                        {eventsError && <p className="error">{eventsError}</p>}

                        {!eventsError && orgEvents.length === 0 && (
                            <p>No events yet.</p>
                        )}

                        {orgEvents.map((event) => (
                            <p key={event.id}>
                                <strong>{event.title}</strong>{" "}
                                &mdash; {event.location},{" "}
                                {new Date(event.startTime).toLocaleString()}
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

export default Organizations
