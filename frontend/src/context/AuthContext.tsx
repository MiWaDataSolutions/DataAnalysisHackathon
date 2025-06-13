import React, { createContext, useContext, useState, useEffect, ReactNode } from 'react';

export interface AuthUser { // Exporting for use in other components if needed
  GoogleId: string;
  Email: string;
  Name: string;
  ProfilePictureUrl?: string;
}

interface AuthContextType {
  isAuthenticated: boolean;
  user: AuthUser | null;
  isLoading: boolean;
  checkAuthStatus: () => Promise<void>;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider = ({ children }: { children: ReactNode }) => {
  const [isAuthenticated, setIsAuthenticated] = useState<boolean>(false);
  const [user, setUser] = useState<AuthUser | null>(null);
  const [isLoading, setIsLoading] = useState<boolean>(true);

  const checkAuthStatus = async () => {
    setIsLoading(true);
    try {
      const response = await fetch('/api/users/me'); // Relies on Vite proxy
      if (response.ok) {
        const data = await response.json();
        if (data.isAuthenticated && data.User) {
          setIsAuthenticated(true);
          setUser(data.User);
          // localStorage.setItem('isLoggedIn', 'true'); // No longer needed with httpOnly cookie
        } else {
          setIsAuthenticated(false);
          setUser(null);
          // localStorage.removeItem('isLoggedIn'); // No longer needed
        }
      } else {
        // response.status === 401 often means not authenticated
        setIsAuthenticated(false);
        setUser(null);
        // localStorage.removeItem('isLoggedIn'); // No longer needed
      }
    } catch (error) {
      console.error("Error checking auth status:", error);
      setIsAuthenticated(false);
      setUser(null);
      // localStorage.removeItem('isLoggedIn'); // No longer needed
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    checkAuthStatus();
  }, []); // Runs once on mount

  return (
    <AuthContext.Provider value={{ isAuthenticated, user, isLoading, checkAuthStatus }}>
      {children}
    </AuthContext.Provider>
  );
};

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};
