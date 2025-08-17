using PolyPersist;
using PolyPersist.Net.BlobStore.Memory;
using PolyPersist.Net.ColumnStore.Memory;
using PolyPersist.Net.Core;
using PolyPersist.Net.DocumentStore.Memory;

namespace Core.Test
{
    public class MemoryStoreProvider : StoreProvider
    {
        protected override IDocumentStore GetDocumentStore() => new Memory_DocumentStore("");
        protected override IBlobStore GetBlobStore() => new Memory_BlobStore("");
        protected override IColumnStore GetColumnStore() => new Memory_ColumnStore("");
    }
}
