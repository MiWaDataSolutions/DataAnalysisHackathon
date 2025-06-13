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
        console.log('Login Success (Google token decoded):', decodedToken);

        // Hypothetical imports - these files do not actually exist yet
        // Assuming UsersService based on backend controller name, and models for User and the response.
        // The actual paths and names would depend on the generator's output.
        // import { UsersService, User as UserRequestBody, LoginResponse } from '../../shared/api/generated';
        // import { OpenAPI } from '../../shared/api/generated/core/OpenAPI';

        // For the purpose of this subtask, we'll mock these imports so the code can be written.
        // In a real scenario, these would be actual imports.
        const mockApi = {
          UsersService: {
            postApiUsersLogin: async (payload: { requestBody: any }): Promise<any> => {
              // This is a mock implementation.
              // In reality, this would be the generated API client call.
              console.log("Mocked API call to UsersService.postApiUsersLogin with payload:", payload);
              // Simulate a successful response structure based on backend
              // This part would not be here if the client was actually generated.
              // The actual generated client would make the HTTP request.
              const MOCK_BACKEND_IS_FIRST_LOGIN = !localStorage.getItem(`mock_seen_${payload.requestBody.googleId}`);
              if (MOCK_BACKEND_IS_FIRST_LOGIN) localStorage.setItem(`mock_seen_${payload.requestBody.googleId}`, 'true');

              return Promise.resolve({
                Message: MOCK_BACKEND_IS_FIRST_LOGIN ? "First-time login successful (mocked)." : "Returning user login successful (mocked).",
                User: {
                  GoogleId: payload.requestBody.googleId,
                  Email: payload.requestBody.email,
                  Name: payload.requestBody.name,
                  ProfilePictureUrl: payload.requestBody.profilePictureUrl,
                },
                IsFirstLoginToApp: MOCK_BACKEND_IS_FIRST_LOGIN
              });
            }
          },
          OpenAPI: {
            HEADERS: {} as Record<string, string>
          }
        };
        // End of mock imports

        try {
          // Configure OpenAPI global headers for Bearer token
          mockApi.OpenAPI.HEADERS = {
            'Authorization': `Bearer ${credentialResponse.credential}`
          };

          const requestBody = { // Type would be UserRequestBody if generated
            googleId: decodedToken.sub, // May not be strictly needed by backend if it uses token claims solely for ID
            email: decodedToken.email, // Same as above
            name: decodedToken.name,
            profilePictureUrl: decodedToken.picture,
          };

          // Assumed method call: UsersService.postApiUsersLogin
          // The actual method name would be derived from the OpenAPI spec (e.g., operationId or path).
          const apiResponse = await mockApi.UsersService.postApiUsersLogin({ requestBody: requestBody });

          console.log('Backend call successful using generated client (mocked):', apiResponse);

          if (apiResponse.IsFirstLoginToApp) {
            alert(`Welcome, ${apiResponse.User?.Name}! This is your first login to our app (via generated client).`);
          } else {
            alert(`Welcome back, ${apiResponse.User?.Name}! (via generated client)`);
          }
          localStorage.setItem('isLoggedIn', 'true');
          navigate('/');

        } catch (error: any) {
          console.error('Backend API call failed using generated client (mocked):', error);
          // Generated clients often throw specific error types (e.g., ApiError)
          // that can include status codes and response bodies.
          const message = error.body?.Message || error.message || 'Could not connect to server. Please try again.';
          alert(`API Error: ${message}`);
          // Ensure isLoggedIn is not set and user is not navigated
        }
      } catch (error) {
        // This outer catch handles errors from jwt_decode or other unexpected issues
        console.error('Error during login process (outer catch):', error);
        alert('An unexpected error occurred during login. Please check the console and try again.');
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
