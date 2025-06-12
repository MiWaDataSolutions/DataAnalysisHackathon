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
        const decodedToken = jwt_decode<DecodedToken>(credentialResponse.credential);
        console.log('Login Success (raw token):', decodedToken);

        const firstLoginKey = `hasLoggedInBefore_${decodedToken.sub}`;
        const hasUserLoggedInBefore = localStorage.getItem(firstLoginKey) === 'true';

        if (!hasUserLoggedInBefore) {
          console.log('First time login for user:', decodedToken.email);
          const userData = {
            googleId: decodedToken.sub,
            email: decodedToken.email,
            name: decodedToken.name,
            profilePictureUrl: decodedToken.picture,
          };

          const response = await fetch('/api/users/login', {
            method: 'POST',
            headers: {
              'Content-Type': 'application/json',
              'Authorization': `Bearer ${credentialResponse.credential}`
            },
            body: JSON.stringify(userData),
          });

          if (response.ok) {
            console.log('Backend call successful for first-time login.');
            localStorage.setItem(firstLoginKey, 'true');
            localStorage.setItem('isLoggedIn', 'true');
            alert(`Login Successful! Welcome, ${decodedToken.name}`);
            navigate('/');
          } else {
            const errorText = await response.text();
            console.error('Backend call failed for first-time login:', errorText);
            alert(`Could not finalize your first login with the server. Error: ${errorText}. Please try again.`);
            // Do NOT set isLoggedIn to true and do NOT navigate
            return;
          }
        } else {
          // Not a first-time login, proceed as usual
          console.log('Returning user:', decodedToken.email);
          localStorage.setItem('isLoggedIn', 'true');
          alert(`Welcome back, ${decodedToken.name}!`);
          navigate('/');
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
