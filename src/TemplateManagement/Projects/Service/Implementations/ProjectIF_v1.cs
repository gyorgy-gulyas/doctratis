using Microsoft.AspNetCore.Http.HttpResults;
using ServiceKit.Net;
using System.Xml.Linq;

namespace TemplateManagement.Projects.Service.Implementations
{
    public class ProjectIF_v1 : IProjectIF_v1
    {
        private readonly IProjectService _service;

        public ProjectIF_v1(IProjectService service)
        {
            _service = service;
        }

        async Task<Response<IProjectIF_v1.ProjectSummaryDTO>> IProjectIF_v1.createProject(CallingContext ctx, string name, string description, string createdBy)
        {
            var header = await _service.createProject(ctx, name, description, createdBy);

            return Response<IProjectIF_v1.ProjectSummaryDTO>.Success(header.ConvertToSummaryDTO());
        }

        async Task<Response<List<IProjectIF_v1.ProjectSummaryDTO>>> IProjectIF_v1.listAccesibleProjects(CallingContext ctx)
        {
            return null;
        }
    }

    internal static class ConversionExtensions
    {
        internal static IProjectIF_v1.ProjectStatuses Convert( this Project.ProjectStatuses @this)
        {
            return @this switch
            {
                Project.ProjectStatuses.Draft => IProjectIF_v1.ProjectStatuses.Draft,
                Project.ProjectStatuses.Active => IProjectIF_v1.ProjectStatuses.Active,
                Project.ProjectStatuses.Locked => IProjectIF_v1.ProjectStatuses.Locked,
                Project.ProjectStatuses.Archived => IProjectIF_v1.ProjectStatuses.Archived,
                Project.ProjectStatuses.Deleted => IProjectIF_v1.ProjectStatuses.Deleted,
                _ => throw new NotImplementedException(),
            };
        }

        internal static IProjectIF_v1.ProjectSummaryDTO ConvertToSummaryDTO( this Project.ProjectHeader @this)
        {
            return new IProjectIF_v1.ProjectSummaryDTO()
            {
                id = @this.id,
                Name = @this.Name,
                Description = @this.Description,
                Status = @this.Status.Convert(),
                Tags = @this.Tags,
            };
        }
    }
}
