import React from 'react';
// Removed imports for @react-oauth/google and jwt-decode
// Removed hypothetical API client imports

const LoginPage: React.FC = () => {
  // Removed useNavigate hook if it was only for the old login flow's navigation

  const handleLogin = () => {
    // Redirect to the backend endpoint that starts the Google OAuth flow.
    // The backend will handle the OAuth dance and then redirect the user back to the frontend (e.g., to '/' or a specific dashboard page)
    // after establishing a session cookie.
    const redirectUrl = encodeURIComponent(`${window.location.origin}/`);
    console.log('redirectUrl', redirectUrl)
    window.location.href = `${import.meta.env.VITE_DATA_ANALYST_API_URL}/auth/google-login?returnUrl=${redirectUrl}`;
  };

  // Removed handleLoginSuccess and handleLoginError methods from the old flow

  return (
    <div>
      <h2>Login Page</h2>
      <p>Please log in to continue.</p>
      <button
        onClick={handleLogin}
        style={{
          padding: '10px 20px',
          fontSize: '16px',
          cursor: 'pointer',
          backgroundColor: '#4285F4',
          color: 'white',
          border: 'none',
          borderRadius: '4px'
        }}
      >
        Login with Google
      </button>
    </div>
  );
};

export default LoginPage;
