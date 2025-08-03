namespace Core.Auditing.Worker
{
    public interface IAuditEntry
    {
        public Task SaveEntry();
    }

    public interface IAuditEntryContainer
    {
        public void AddEntryForBackgrondSave(IAuditEntry entry);
    }
}