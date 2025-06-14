namespace DataAnalysisHackathonBackend.Exceptions
{
    public class RecordNotFoundException: Exception
    {
        public RecordNotFoundException() { }

        public RecordNotFoundException(string entityName, Guid requestedEntityId)
            : base($"No {entityName} found with id of {requestedEntityId}")
        {
            
        }
    }
}
