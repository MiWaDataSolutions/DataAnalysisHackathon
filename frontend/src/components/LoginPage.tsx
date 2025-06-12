import React from 'react';
import { GoogleLogin, CredentialResponse } from '@react-oauth/google';
import jwt_decode from 'jwt-decode';
import { useNavigate } from 'react-router-dom';

interface DecodedToken {
  sub: string;
  email: string;
  name: string;
  picture: string;
  // Add other fields if present in your JWT
}

const LoginPage: React.FC = () => {
  const navigate = useNavigate();

  const handleLoginSuccess = async (credentialResponse: CredentialResponse) => {
    if (credentialResponse.credential) {
      try {
        const decodedToken = jwt_decode<DecodedToken>(credentialResponse.credential); // Still useful for immediate UI, or could be removed if all info comes from backend User object
        console.log('Login Success (Google token decoded):', decodedToken);

        // Prepare user data for the body, primarily for Name and ProfilePictureUrl
        // GoogleId and Email will be primarily taken from the token by the backend
        const userRequestBody = {
          googleId: decodedToken.sub, // Can be sent for completeness or backend cross-check
          email: decodedToken.email, // Can be sent for completeness
          name: decodedToken.name,
          profilePictureUrl: decodedToken.picture,
        };

        const response = await fetch('/api/users/login', {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${credentialResponse.credential}`
          },
          body: JSON.stringify(userRequestBody),
        });

        if (response.ok) {
          const data = await response.json();
          console.log('Backend call successful. Response data:', data);

          // Use isFirstLoginToApp from backend response
          if (data.isFirstLoginToApp) {
            alert(`Welcome, ${data.User.Name}! This is your first login to our app.`);
          } else {
            alert(`Welcome back, ${data.User.Name}!`);
          }

          localStorage.setItem('isLoggedIn', 'true');
          navigate('/');
        } else {
          // Backend call failed
          const errorText = await response.text();
          console.error('Backend call failed:', errorText);
          alert(`Login failed with the server. Error: ${errorText}. Please try again.`);
          // Do NOT set isLoggedIn to true and do NOT navigate
          return;
        }
      } catch (error) {
        console.error('Error during login process:', error);
        alert('An error occurred during login. Please check the console and try again.');
      }
    } else {
      console.error('Login failed: No credential returned');
      alert('Login Failed: No credential returned.');
    }
  };

  const handleLoginError = () => {
    console.error('Login Failed');
    alert('Login Failed. Please try again.');
  };

  return (
    <div>
      <h2>Login Page</h2>
      <GoogleLogin
        onSuccess={handleLoginSuccess}
        onError={handleLoginError}
      />
    </div>
  );
};

export default LoginPage;
