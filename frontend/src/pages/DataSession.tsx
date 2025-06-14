import { useGetDataSessionById } from "@/hooks/data-analyst-api/data-session.query";
import { useParams } from "react-router-dom"

export const DataSession = () => {
    const { dataSessionId } = useParams();

    const { data: dataSessionData, error: dataSessionError } = useGetDataSessionById(dataSessionId!);
    if (dataSessionError) throw dataSessionError;

    return (
        <div>
            <h1>{dataSessionData?.name ?? 'Name Will Generate After First File is Uploaded'}</h1>
        </div>
    )
}