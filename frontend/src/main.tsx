import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
// Removed: import { GoogleOAuthProvider } from '@react-oauth/google';
import './index.css';
import App from './App.tsx';
import LoginPage from './components/LoginPage.tsx';
import ProtectedRoute from './components/ProtectedRoute.tsx';
import { AuthProvider } from './context/AuthContext.tsx'; // Added
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';

const queryClient = new QueryClient();
createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <QueryClientProvider client={queryClient}>
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
            element={
              <ProtectedRoute>
                <Navigate to="/" replace />
              </ProtectedRoute>
            }
          />
          {/* Optional: Default fallback if no other route matches */}
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </BrowserRouter>
      </AuthProvider>
    </QueryClientProvider>
  </StrictMode>,
);
