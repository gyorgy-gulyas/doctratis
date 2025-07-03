namespace Core.Auditing.Worker
{
    public interface IAuditEntry
    {
        public Task SaveEntry();
    }

    public class AuditEntry<T> : IAuditEntry
        where T : IAuditTrail, new()
    {
        public readonly T _entry;

        public AuditEntry(T entry)
        {
            _entry = entry;
        }
        
        public Task SaveEntry()
        {
            T trail = new()
            {
                entityType = _entry.GetType().Name,
                entityId = _entry.id,
                PartitionKey = _entry.id,
                payload = JsonSerializer.Serialize(new { header, accesses }),
     
                timestamp = DateTime.UtcNow,
                trailOperation = _entry.operation,
                userId = ctx.ClientInfo.CallingUserId,
                userName = ctx.ClientInfo.CallingUserId,
            };        
        }
    }

    public interface IAuditEntryContainer
    {
        public void AddEntry(IAuditEntry entry);
    }
}