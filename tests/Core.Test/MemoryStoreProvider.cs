using PolyPersist;
using PolyPersist.Net.BlobStore.Memory;
using PolyPersist.Net.ColumnStore.Memory;
using PolyPersist.Net.Core;
using PolyPersist.Net.DocumentStore.Memory;

namespace Core.Test
{
    public class MemoryStoreProvider : StoreProvider
    {
        private IDocumentStore _documentStore;
        private IBlobStore _blobStore;
        private IColumnStore _columnStore;

        public MemoryStoreProvider()
        {
            _documentStore = new Memory_DocumentStore("");
            _blobStore = new Memory_BlobStore("");
            _columnStore = new Memory_ColumnStore("");
        }

        protected override IDocumentStore GetDocumentStore() => _documentStore;
        protected override IBlobStore GetBlobStore() => _blobStore;
        protected override IColumnStore GetColumnStore() => _columnStore;
    }
}
