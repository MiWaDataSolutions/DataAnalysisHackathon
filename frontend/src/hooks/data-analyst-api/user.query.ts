import { useUserApi } from "@/shared/api/data-analyst-api-client";
import { useQuery } from "@tanstack/react-query";

export const useGetMe = () => {
    const api = useUserApi();
    const query = useQuery({
        queryKey: ['me'],
        queryFn: () => {
            return api!.apiUsersMeGet();
        }
    });

    return query;
}