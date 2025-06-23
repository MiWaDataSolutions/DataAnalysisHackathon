import { useEffect, useState } from "react";
import { AuthApi, DataSessionApi, GraphingApi, UsersApi } from "./data-analyst-api/apis";
import { Configuration, type DataSession } from "./data-analyst-api";

export const useAuthApi = () => {
    const [apiClient, setApiClient] = useState<AuthApi | null>(null);


    // 3. Create the API client when the token changes
    useEffect(() => {
        let config = new Configuration({
            basePath: import.meta.env.VITE_DATA_ANALYST_API_URL,
        });

        setApiClient(new AuthApi(config));
    }, []);

    return apiClient;
}

export const useUserApi = () => {
    const [apiClient, setApiClient] = useState<UsersApi>();


    // 3. Create the API client when the token changes
    useEffect(() => {
        let config = new Configuration({
            basePath: import.meta.env.VITE_DATA_ANALYST_API_URL,
        });

        setApiClient(new UsersApi(config));
    }, []);

    return apiClient;
}

export const useDataSessionApi = () => {
    const [apiClient, setApiClient] = useState<DataSessionApi>();


    // 3. Create the API client when the token changes
    useEffect(() => {
        let config = new Configuration({
            basePath: import.meta.env.VITE_DATA_ANALYST_API_URL,
            credentials: 'include'
        });

        setApiClient(new DataSessionApi(config));
    }, []);

    return apiClient;
}

export const useGraphingApi = () => {
    const [apiClient, setApiClient] = useState<GraphingApi>();


    // 3. Create the API client when the token changes
    useEffect(() => {
        let config = new Configuration({
            basePath: import.meta.env.VITE_DATA_ANALYST_API_URL,
            credentials: 'include'
        });

        setApiClient(new GraphingApi(config));
    }, []);

    return apiClient;
}