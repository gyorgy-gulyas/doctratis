import * as ProjectIF_v1 from "../../../types/TemplateManagement/Projects/ProjectIF_v1";
export declare const ProjectIF: {
    V1: {
        createProject(name: string, description: string): Promise<ProjectIF_v1.ProjectSummaryDTO>;
        updateProject(project: ProjectIF_v1.ProjectDetailsDTO): Promise<ProjectIF_v1.ProjectDetailsDTO>;
        listAccessibleProjects(): Promise<ProjectIF_v1.ProjectIdentityAssignmentDTO[]>;
        listAccessibleProjectsForUser(userId: string): Promise<ProjectIF_v1.ProjectIdentityAssignmentDTO[]>;
        getProject(projectId: string): Promise<ProjectIF_v1.ProjectDetailsDTO>;
        addProjectAccess(projectId: string, identityId: string, role: ProjectIF_v1.ProjectAccessRoles): Promise<ProjectIF_v1.ProjectAccessDTO>;
    };
};
//# sourceMappingURL=ProjectIF_v1.RestClient.d.ts.map