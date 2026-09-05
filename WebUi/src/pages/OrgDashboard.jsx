import { useState } from 'react'
import { Link } from 'react-router-dom'
import "../css/index.css"

function OrgDashboard() {

    const [showForm, setShowForm] = useState(false)
    const [eventName, setEventName] = useState("")
    const [description, setDescription] = useState("")
    const [location, setLocation] = useState("")
    const [startTime, setStartTime] = useState("")
    const [endTime, setEndTime] = useState("")

    async function addEvent(e) {
        e.preventDefault()

        const newEvent = {
            organizationId: "orgID",
            title: eventName,
            description: description,
            location: location,
            startTime: startTime,
            endTime: endTime
        }

        try {
            const response = await fetch("http://localhost:5160/api/events", {
                method: "POST",
                headers: {
                    "Content-Type": "application/json"
                },
                body: JSON.stringify(newEvent)
            })

            if (!response.ok) {
                throw new Error("Failed to create event")
            }

            alert("Event created successfully!")

            setEventName("")
            setDescription("")
            setLocation("")
            setStartTime("")
            setEndTime("")
            setShowForm(false)

        } catch (error) {
            console.error(error)
            alert("There was a problem creating the event.")
        }
    }

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

                        <div className="info-cards">
                        </div>

                    </section>

                </main>

            </div>

        </div>
    )
}

export default OrgDashboard