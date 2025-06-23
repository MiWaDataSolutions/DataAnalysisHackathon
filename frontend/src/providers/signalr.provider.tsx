import { useAuth } from '@/context/AuthContext';
import * as signalR from '@microsoft/signalr';
import { createContext, useCallback, useContext, useEffect, useRef } from 'react';

type SignalREventHandler = (...args: any[]) => void;

interface SignalRContextType {
  connection: signalR.HubConnection | null;
  invoke: (methodName: string, ...args: any[]) => Promise<any>;
  useSignalREffect: (event: string, handler: SignalREventHandler, deps?: any[]) => void;
}

const SignalRContext = createContext<SignalRContextType | undefined>(undefined);

export const SignalRProviderWrapper = ({ children }: { children: React.ReactNode }) => {
  const { user } = useAuth();
  const connectionRef = useRef<signalR.HubConnection | null>(null);

  // Create connection only once
  useEffect(() => {
    const connection = new signalR.HubConnectionBuilder()
      .withUrl(`${import.meta.env.VITE_DATA_ANALYST_API_URL}/data-session-hub`, { withCredentials: true })
      .withAutomaticReconnect()
      .build();

    connectionRef.current = connection;

    connection
      .start()
      .then(() => {
        if (user?.googleId) {
          connection.invoke("join", user.googleId);
        }
      })
      .catch(console.error);

    return () => {
      connection.stop();
    };
    // Only run once on mount
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  // Join on user change
  useEffect(() => {
    if (user?.googleId && connectionRef.current?.state === signalR.HubConnectionState.Connected) {
      connectionRef.current.invoke("join", user.googleId);
    }
  }, [user]);

  // Provide a hook to subscribe to events
  const useSignalREffect = useCallback(
    (event: string, handler: SignalREventHandler, deps: any[] = []) => {
      useEffect(() => {
        const conn = connectionRef.current;
        if (!conn) return;
        conn.on(event, handler);
        return () => {
          conn.off(event, handler);
        };
        // eslint-disable-next-line react-hooks/exhaustive-deps
      }, [event, ...deps]);
    },
    []
  );

  const invoke = useCallback((methodName: string, ...args: any[]) => {
    if (!connectionRef.current) return Promise.reject("No connection");
    return connectionRef.current.invoke(methodName, ...args);
  }, []);

  return (
    <SignalRContext.Provider value={{ connection: connectionRef.current, invoke, useSignalREffect }}>
      {children}
    </SignalRContext.Provider>
  );
};

export const useSignalRWrapper = () => {
  const context = useContext(SignalRContext);
  if (!context) throw new Error("useSignalRWrapper must be used within a SignalRProviderWrapper");
  return context;
};
