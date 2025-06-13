import { useAuthApi } from "@/shared/api/data-analyst-api-client"
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";

export const useLogout = () => {
    const api = useAuthApi();
    const queryClient = useQueryClient();
    const mutation = useMutation({
        mutationKey: ['logout'],
        mutationFn: async () => {
            return await api?.authLogoutPostRaw();
        },
        onSettled: () => {
            queryClient.invalidateQueries({
                queryKey: ['login']
            })
        }
    });

    return mutation;
}

export const useLogin = () => {
    const api = useAuthApi();
    const query = useQuery({
        queryKey: ['login'],
        queryFn: () => {
            return api?.authGoogleLoginGet();
        }
    });

    return query;
}