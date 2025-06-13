import type { MeUserDto } from '@/shared/api/data-analyst-api';
import React, { createContext, useContext, useState, useEffect, type ReactNode } from 'react';
import { useNavigate } from 'react-router-dom';

interface AuthContextType {
  isAuthenticated: boolean;
  user: MeUserDto | null;
  isLoading: boolean;
  checkAuthStatus: () => Promise<void>;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider = ({ children }: { children: ReactNode }) => {
  const [isAuthenticated, setIsAuthenticated] = useState<boolean>(false);
  const [user, setUser] = useState<MeUserDto | null>(null);
  const [isLoading, setIsLoading] = useState<boolean>(true);
  
  // const { navigate } = useNavigate();

  const checkAuthStatus = async () => {
    setIsLoading(true);
    try {
      const response = await fetch(
        import.meta.env.VITE_DATA_ANALYST_API_URL + '/api/users/me',
        { credentials: 'include' }
      );
      if (response.ok) {
        const data = await response.json();
        if (data.isAuthenticated && data.user) {
          setIsAuthenticated(true);
          setUser(data.user);
        } else {
          setIsAuthenticated(false);
          setUser(null);
        }
      } else {
        setIsAuthenticated(false);
        setUser(null);
      }
    } catch (error) {
      console.error("Error checking auth status:", error);
      setIsAuthenticated(false);
      setUser(null);
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
