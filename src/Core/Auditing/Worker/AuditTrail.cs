using JsonDiffPatchDotNet;
using Newtonsoft.Json.Linq;
using PolyPersist;
using PolyPersist.Net.Extensions;
using ServiceKit.Net;

namespace Core.Auditing.Worker
{
    public abstract class AuditTrail<T> : IAuditEntry
        where T : IAuditTrail, IRow, new()
    {
        public readonly CallingContext _callingContext;
        public readonly TrailOperations _operation;

        public AuditTrail(CallingContext ctx, TrailOperations operation)
        {
            _callingContext = ctx;
            _operation = operation;
        }

        public async Task SaveEntry()
        {
            await InitializeAsync();

            T newTrail = new T()
            {
                entityId = GetRootEntity().id,
                entityType = GetRootEntity().GetType().Name,
                PartitionKey = GetRootEntity().id,
                payload = GetEntitySpecificPayloadJSON(),
                timestamp = DateTime.UtcNow,
                trailOperation = _operation,
                idenityId = _callingContext.IdentityId,
                idenityName = _callingContext.IdentityName,
            };

            var table = GetTable();
            if (_operation != TrailOperations.Create)
            {
                var lastTrail = table
                    .AsQueryable()
                    .Where(t => t.id == newTrail.id && t.PartitionKey == newTrail.PartitionKey)
                    .OrderByDescending(t => t.timestamp)
                    .Take(1)
                    .FirstOrDefault();

                if (lastTrail != null)
                {
                    JToken oldJson = JToken.Parse(newTrail.payload);
                    JToken newJson = JToken.Parse(lastTrail.payload);

                    newTrail.previousTrailId = lastTrail.id;
                    newTrail.deltaPayload = new JsonDiffPatch().Diff(oldJson, newJson).ToString();
                }
            }
            await table.Insert(newTrail);
        }

        protected abstract Task InitializeAsync();
        protected abstract IEntity GetRootEntity();
        protected abstract string GetEntitySpecificPayloadJSON();
        protected abstract IColumnTable<T> GetTable();
        protected abstract void FillAddtionalMembers(T trail);
    }
}
