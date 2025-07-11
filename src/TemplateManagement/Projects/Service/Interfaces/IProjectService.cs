
// <auto-generated>
//     This code was generated by d3i.interpreter
//
//     Changes to this file may cause incorrect behavior and will be lost if the code is regenerated.
// </auto-generated>

using ServiceKit.Net;
using TemplateManagement.Projects;

namespace TemplateManagement.Projects
{
	public partial interface IProjectService
	{
		/// <return>Project.ProjectHeader</return>
		public Task<Response<Project.ProjectHeader>> createProject(CallingContext ctx, string name, string description, string createdBy);

		/// <return>Project.ProjectHeader</return>
		public Task<Response<Project.ProjectHeader>> updateProject(CallingContext ctx, Project.ProjectHeader project, List<Project.ProjectAccess> accesses);

		/// <return>List<Project.ProjectHeader></return>
		public Task<Response<List<Project.ProjectHeader>>> getAllProjectForUser(CallingContext ctx, string userId);

		/// <return>List<Project.ProjectAccess></return>
		public Task<Response<List<Project.ProjectAccess>>> getAllAccessForProject(CallingContext ctx, string projectId);

		/// <return>Project.ProjectHeader</return>
		public Task<Response<Project.ProjectHeader>> getProjectForUser(CallingContext ctx, string projectId, string userId);

		/// <return>Project.ProjectAccess</return>
		public Task<Response<Project.ProjectAccess>> addProjectAccess(CallingContext ctx, string projectId, string identityId, Project.ProjectAccess.Roles role);


		public partial class ProjectCreated_v1
		{
			public string projectId { get; set; }

			#region Clone & Copy 
			virtual public ProjectCreated_v1 Clone()
			{
				ProjectCreated_v1 clone = new();

				clone.projectId = new string(projectId.ToCharArray());

				return clone;
			}
			#endregion Clone & Copy 
		}

	}
}
