import { Link } from 'react-router-dom' 
import "../css/index.css" 

function Home() { 
  return ( 
    <div className="home-page"> 

      {/*Navbar*/} 
      <nav className="navbar"> 
        <div className="link"> 
          <Link to="/">GoodDeads</Link> 
        </div> 
        <div className="link"> 
          <Link to="/login">Login</Link> 
        </div> 
      </nav> 


      {/*About Section*/} 
      <section className="about"> 
        <h2>Volunteering Made Easy</h2> 
        <p> 
          Connect with organizations and find opportunities 
          to make a difference in your community. 
        </p> 
      </section> 

      {/*Information Section*/} 
      <section className="info-container"> 
        <h2>Welcome to GoodDeeds</h2> 
        <p>Choose how you would like to use GoodDeeds.</p> 

        <div className="info-cards"> 

          {/*Volunteer*/} 
          <div className="info-card"> 
            <h3>Volunteer</h3> 
            <p> 
              Find volunteer opportunities and make a difference in your community. 
            </p> 
          </div> 

          {/*Organization*/} 
          <div className="info-card"> 
            <h3>Organization</h3> 
            <p> 
              Create volunteer opportunities and connect with people who want to help. 
            </p> 
          </div>
          
        </div> 

      </section> 
    </div> 
  ) 
} 

export default Home 