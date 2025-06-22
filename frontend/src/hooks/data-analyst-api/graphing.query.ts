import { useGraphingApi } from "@/shared/api/data-analyst-api-client";
import { useQuery } from "@tanstack/react-query";

export const useGetKPIData = (dataSessionId: string | undefined, dataSessionProcessedStatus: number | undefined) => {
    const api = useGraphingApi();

    const query = useQuery({
        queryKey: ['data-session-kpi-graphs', dataSessionId],
        queryFn: () => {
            return api!.apiGraphingGetKPIGraphsGet({
                dataSessionId
            });
        },
        enabled: dataSessionProcessedStatus == 2
    });

    return query;
}

export const useGetGraphData = (dataSessionId: string | undefined, dataSessionProcessedStatus: number | undefined) => {
    const api = useGraphingApi();

    const query = useQuery({
        queryKey: ['data-session-graphs', dataSessionId],
        queryFn: () => {
            return api!.apiGraphingGetGraphsGet({
                dataSessionId
            });
        },
        enabled: dataSessionProcessedStatus == 2
    });

    return query;
}