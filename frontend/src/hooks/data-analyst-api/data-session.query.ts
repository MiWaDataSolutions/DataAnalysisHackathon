import type { DataSession } from "@/shared/api/data-analyst-api";
import { useDataSessionApi } from "@/shared/api/data-analyst-api-client"
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";

export const useGetDataSessions = (enabled: boolean) => {
    const api = useDataSessionApi();

    const query = useQuery({
        queryKey: ['data-sessions'],
        queryFn: () => {
            return api!.apiDataSessionGet();
        },
        enabled: enabled
    });

    return query;
}

export const useGetDataSessionById = (dataSessionId: string) => {
    const api = useDataSessionApi();

    const query = useQuery({
        queryKey: ['data-session', dataSessionId],
        queryFn: () => {
            return api!.apiDataSessionGetByIdGet({
                dataSessionId
            });
        }
    });

    return query;
}

export const useCreateDataSession = () => {
    const api = useDataSessionApi();
    const queryClient = useQueryClient();
    const mutation = useMutation({
        mutationKey: ['data-session'],
        mutationFn: async (dataSession: DataSession) => {
            return api?.apiDataSessionPost({
                dataSession
            });
        },
        onSettled: () => {
            queryClient.invalidateQueries({
                queryKey: ['data-sessions']
            });
        }
    });

    return mutation;
}

export const useDeleteDataSession = () => {
    const api = useDataSessionApi();
    const queryClient = useQueryClient();
    const mutation = useMutation({
        mutationKey: ['data-session'],
        mutationFn: async (dataSessionId: string) => {
            return api?.apiDataSessionDelete({
                dataSessionId
            });
        },
        onSettled: () => {
            queryClient.invalidateQueries({
                queryKey: ['data-sessions']
            });
        }
    });

    return mutation;
}