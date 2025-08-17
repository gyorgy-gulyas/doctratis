using Core.Auditing.Worker;

namespace Core.Test.Mock
{
    public class Mock_AuditEntryContainer : IAuditEntryContainer
    {
        void IAuditEntryContainer.AddEntryForBackgrondSave(IAuditEntry entry)
        {
            entry.SaveEntry().Wait();
        }
    }
}
