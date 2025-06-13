import { useGetMe } from '@/hooks/data-analyst-api/user.query';
import type { MeDto, MeUserDto } from '@/shared/api/data-analyst-api';
import { createContext, useContext, useState, useEffect, type ReactNode, useMemo } from 'react';

interface AuthContextType {
  data: {
    userData: MeDto | undefined;
    userIsLoading: boolean;
  };
  checkAuthStatus: () => Promise<void>;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider = ({ children }: { children: ReactNode }) => {
  const [isAuthenticated, setIsAuthenticated] = useState<boolean>(false);
  const [user, setUser] = useState<MeUserDto | null>(null);
  const { data: userData, error: userError, isLoading: userIsLoading, refetch: userRefetch } = useGetMe();

  const checkAuthStatus = async () => {
    try {
      const response = await userRefetch(); // Relies on Vite proxy
      if (response.isSuccess) {
        if (userData?.isAuthenticated && userData.user) {
          setIsAuthenticated(true);
          setUser(userData.user);
        } else {
          setIsAuthenticated(false);
          setUser(null);
        }
      } else {
        // response.status === 401 often means not authenticated
        setIsAuthenticated(false);
        setUser(null);
      }
    } catch (error) {
      console.error("Error checking auth status:", error);
      setIsAuthenticated(false);
      setUser(null);
    } 
  };

  useEffect(() => {
    checkAuthStatus();
  }, []); // Runs once on mount
  
  const contextValue = useMemo(
    () => ({
      data: {
        userData,
        userIsLoading
      },
      checkAuthStatus,
    }),
    [userData, userIsLoading, checkAuthStatus]
  );

  return (
    <AuthContext.Provider value={contextValue}>
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
