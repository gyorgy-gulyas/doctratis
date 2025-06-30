using PolyPersist;
using PolyPersist.Net.Context;
using TemplateManagement.Projects.Project;

namespace TemplateManagement.Projects.Service.Implementations
{
    public class ProjectStoreContext : StoreContext
    {
        public readonly IDocumentCollection<ProjectHeader> ProjectHeaders;
        public readonly IDocumentCollection<ProjectAccess> ProjectAccesses;

        public ProjectStoreContext(IStoreProvider storeProvider)
            : base(storeProvider)
        {
            IDocumentStore documentStore = (IDocumentStore)storeProvider.getStore(IStore.StorageModels.Document);

            ProjectHeaders = documentStore.GetCollectionByName<ProjectHeader>(nameof(ProjectHeader)).Result;
            if (ProjectHeaders == null)
                ProjectHeaders = documentStore.CreateCollection<ProjectHeader>(nameof(ProjectHeader)).Result;

            ProjectAccesses = documentStore.GetCollectionByName<ProjectAccess>(nameof(ProjectAccess)).Result;
            if (ProjectAccesses == null)
                ProjectAccesses = documentStore.CreateCollection<ProjectAccess>(nameof(ProjectAccess)).Result;
        }
    }
}
