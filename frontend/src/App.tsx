import { useState } from 'react';
import reactLogo from './assets/react.svg';
import viteLogo from '/vite.svg';
import './App.css';
import { useAuth } from './context/AuthContext'; // Assuming path to AuthContext
import { useMediaQuery } from 'usehooks-ts'

function App() {
  const [count, setCount] = useState(0);
  const { isAuthenticated, user, checkAuthStatus } = useAuth();
  const isDesktop = useMediaQuery("(min-width: 768px)");

  const handleLogout = async () => {
    try {
      const response = await fetch(import.meta.env.VITE_DATA_ANALYST_API_URL + '/auth/logout', { // Matches backend route
        method: 'POST',
        headers: {
          // No specific CSRF token handling for this example, but needed in prod if backend uses it
          'Content-Type': 'application/json',
        },
        credentials: 'include',
      });
      if (response.ok) {
        console.log("Logout successful on backend.");
      } else {
        console.error("Backend logout failed.", await response.text());
      }
    } catch (error) {
      console.error("Error during logout fetch:", error);
    } finally {
      // Regardless of backend call success, refresh auth state on frontend
      await checkAuthStatus();
    }
  };

  return (
    <>
      {isAuthenticated && user && (
        <div style={{ position: 'absolute', top: '10px', right: '10px', textAlign: 'right', border: '1px solid #ccc', padding: '10px', borderRadius: '5px', backgroundColor: '#f9f9f9' }}>
          <p style={{ margin: 0, fontWeight: 'bold' }}>Welcome, {user.name || 'User'}!</p>
          {user.profilePictureUrl && (
            <img
              src={user.profilePictureUrl}
              alt="Profile"
              style={{ width: '40px', height: '40px', borderRadius: '50%', margin: '5px 0' }}
            />
          )}
          <p style={{ fontSize: '0.8em', margin: '0 0 10px 0' }}>({user.email})</p>
          <button
            onClick={handleLogout}
            style={{
              padding: '8px 15px',
              fontSize: '14px',
              cursor: 'pointer',
              backgroundColor: '#dc3545',
              color: 'white',
              border: 'none',
              borderRadius: '4px'
            }}
          >
            Logout
          </button>
        </div>
      )}
      <h1>Insightful</h1>
      <h2>Welcome to the App! {isAuthenticated ? "(Authenticated)" : "(Not Authenticated)"}</h2>
      
      <p>
        This project is for submission for the (<a href='https://googlecloudmultiagents.devpost.com/'>Google ADK Hackathon</a>)
        What this project does is it accepts a CSV file, analyzes the data and generates a dashboard with insights on the data.
      </p>
    </>
  );
}

export default App;
