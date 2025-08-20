using PolyPersist;
using ServiceKit.Net;

namespace Core.Auditing.Worker
{
    public abstract class AuditLog<T> : IAuditEntry
        where T : IAuditLog, IRow, new()
    {
        public readonly CallingContext _callingContext;
        public readonly string _operation;

        public AuditLog(CallingContext ctx, string operation)
        {
            _callingContext = ctx;
            _operation = operation;
        }

        public async Task SaveEntry()
        {
            T newTrail = new T()
            {
                correlationId = _callingContext.CorrelationId,
                operation = _operation,
                PartitionKey = GetPartitionKey(),
                payload = GetLogSpecificPayloadJSON(),
                timestamp = DateTime.UtcNow,
                idenityId = _callingContext.IdentityId,
                idenityName = _callingContext.IdentityName,
            };

            FillAddtionalMembers(newTrail);

            var table = GetTable();
            await table.Insert(newTrail);
        }

        protected abstract string GetPartitionKey();
        protected abstract string GetLogSpecificPayloadJSON();
        protected abstract IColumnTable<T> GetTable();
        protected abstract void FillAddtionalMembers(T trail);
    }
}
