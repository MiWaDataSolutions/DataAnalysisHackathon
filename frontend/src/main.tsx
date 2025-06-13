import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
// Removed: import { GoogleOAuthProvider } from '@react-oauth/google';
import './index.css';
import App from './App.tsx';
import LoginPage from './components/LoginPage.tsx';
import ProtectedRoute from './components/ProtectedRoute.tsx';
import { AuthProvider } from './context/AuthContext.tsx'; // Added

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <AuthProvider> {/* Wrapped with AuthProvider */}
      <BrowserRouter>
        <Routes>
          <Route path="/login" element={<LoginPage />} />
        <Route
          path="/"
          element={
            <ProtectedRoute>
              <App />
            </ProtectedRoute>
          }
        />
        {/* Optional: Redirect logged-in users from /login to / */}
        <Route
          path="/login-redirect"
            element={ // This specific redirect logic might need re-evaluation with AuthContext
                      // For now, keeping it, but ProtectedRoute will handle most cases.
              localStorage.getItem('isLoggedIn') === 'true' ? ( // This localStorage check is now outdated
              <Navigate to="/" replace />
            ) : (
              <LoginPage />
            )
          }
        />
        {/* Optional: Default fallback if no other route matches */}
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </BrowserRouter>
  </StrictMode>,
);
