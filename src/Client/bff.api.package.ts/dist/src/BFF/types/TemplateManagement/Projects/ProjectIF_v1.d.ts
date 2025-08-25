export declare enum ProjectStatuses {
    Draft = "Draft",
    Active = "Active",
    Locked = "Locked",
    Archived = "Archived",
    Deleted = "Deleted"
}
export declare enum ProjectAccessRoles {
    Reader = "Reader",
    Editor = "Editor",
    Owner = "Owner",
    Auditor = "Auditor",
    Admin = "Admin"
}
export declare enum ProjectAccessStatuses {
    Pending = "Pending",
    Active = "Active",
    Suspended = "Suspended",
    Revoked = "Revoked",
    Deleted = "Deleted"
}
export interface ProjectSummaryDTO {
    id: string;
    Name: string;
    Description: string;
    Tags: string[];
    Status: ProjectStatuses;
}
export interface ProjectFolderDTO {
    id: string;
    Name: string;
    Description: string;
    SubFolders: ProjectFolderDTO[];
}
export interface ProjectIdentityAssignmentDTO {
    ProjectId: string;
    ProjectName: string;
    ProjectDescription: string;
    ProjectStatus: ProjectStatuses;
    IdentityId: string;
    IdentityName: string;
    AccessRole: ProjectAccessRoles;
    AccessStatus: ProjectAccessStatuses;
}
export interface ProjectAccessDTO {
    id: string;
    etag: string;
    LastUpdate: Date;
    IdentityId: string;
    IdentityName: string;
    Role: ProjectAccessRoles;
    Status: ProjectAccessStatuses;
}
export interface ProjectDetailsDTO {
    id: string;
    etag: string;
    LastUpdate: Date;
    Name: string;
    Description: string;
    Tags: string[];
    Status: ProjectStatuses;
    SubFolders: ProjectFolderDTO[];
    Accesses: ProjectAccessDTO[];
    CreatedAt: Date;
    CreatedBy: string;
}
//# sourceMappingURL=ProjectIF_v1.d.ts.map