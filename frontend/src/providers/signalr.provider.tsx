import { useAuth } from '@/context/AuthContext';
import { createContext, useContext, useEffect } from 'react';
import { createSignalRContext } from 'react-signalr/signalr';
import type { Context, Hub } from 'react-signalr/src/signalr/types';

interface SignalRProviderWrapperContextType {
    SignalRContext: Context<Hub<string, string>>
}

const SignalRContext = createSignalRContext();
const SignalRProviderWrapperContext = createContext<SignalRProviderWrapperContextType | undefined>(undefined);

export const SignalRProviderWrapper = ({ children }: { children: React.ReactNode }) => {
  const { user } = useAuth();

  useEffect(() => {
    if (user && SignalRContext.connection?.connectionId) {
      SignalRContext.invoke("join", user.googleId);
    }
  }, [user]);

  return (
    <SignalRProviderWrapperContext.Provider value={{SignalRContext}}>
        <SignalRContext.Provider url={`${import.meta.env.VITE_DATA_ANALYST_API_URL}/data-session-hub`}>
            {children}
        </SignalRContext.Provider>
    </SignalRProviderWrapperContext.Provider>
  );
};

export const useSignalRWrapper = () => {
    const context = useContext(SignalRProviderWrapperContext);
    if (context === undefined) {
        throw new Error('useSignalRWrapper must be used withing an SignalRProviderWrapperProvider')
    }
    return context;
}
