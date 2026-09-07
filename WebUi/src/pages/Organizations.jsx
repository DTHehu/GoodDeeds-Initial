import { useEffect, useState } from 'react'
import { api } from '../services/api'
import Navbar from '../components/Navbar.jsx'
import "../css/index.css"

function Organizations() {

    const [organizations, setOrganizations] = useState([])
    const [error, setError] = useState("")

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

                        </div>

                    ))}

                </div>

            </section>

        </div>
    )
}

export default Organizations
