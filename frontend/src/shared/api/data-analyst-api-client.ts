import { useEffect, useState } from "react";
import { AuthApi, UsersApi } from "./data-analyst-api/apis";
import { Configuration } from "./data-analyst-api";

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