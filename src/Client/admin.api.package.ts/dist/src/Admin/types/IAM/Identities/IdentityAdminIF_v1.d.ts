export declare enum AccountTypes {
    User = "User",
    ExternalSystem = "ExternalSystem",
    InternalService = "InternalService"
}
export interface LdapDomainDTO {
    id: string;
    etag: string;
    LastUpdate: Date;
    name: string;
    description: string;
    netbiosName: string;
    domainControllers: LdapDomainDTO.DomainController[];
    baseDn: string;
    useSecureLdap: boolean;
    serviceAccountUser: string;
    serviceAccountPassword: string;
}
export declare namespace LdapDomainDTO {
    interface DomainController {
        host: string;
        port: number;
    }
}
export interface LdapDomainSummaryDTO {
    id: string;
    name: string;
    description: string;
}
export interface AccountSummaryDTO {
    id: string;
    Type: AccountTypes;
    Name: string;
    isActive: boolean;
}
export interface AccountDTO {
    id: string;
    etag: string;
    LastUpdate: Date;
    data: AccountDataDTO;
}
export interface AccountDataDTO {
    Type: AccountTypes;
    Name: string;
    isActive: boolean;
    contacts: ContactInfo[];
}
export interface ContactInfo {
    contactType: string;
    email: string;
    phoneNumber: string;
}
export interface AuthDTO {
    id: string;
    etag: string;
    LastUpdate: Date;
    method: AuthDTO.Methods;
    isActive: boolean;
}
export declare namespace AuthDTO {
    enum Methods {
        Email = "Email",
        ActiveDirectory = "ActiveDirectory",
        KAU = "KAU",
        Certificate = "Certificate"
    }
}
export interface TwoFactorConfigurationDTO {
    enabled: boolean;
    method: TwoFactorConfigurationDTO.Methods;
    phoneNumber: string;
    email: string;
}
export declare namespace TwoFactorConfigurationDTO {
    enum Methods {
        TOTP = "TOTP",
        SMS = "SMS",
        Email = "Email"
    }
}
export interface EmailAuthDTO {
    id: string;
    etag: string;
    LastUpdate: Date;
    isActive: boolean;
    email: string;
    isEmailConfirmed: boolean;
    passwordExpiresAt: string;
    twoFactor: TwoFactorConfigurationDTO;
}
export interface ADAuthDTO {
    id: string;
    etag: string;
    LastUpdate: Date;
    isActive: boolean;
    LdapDomainId: string;
    LdapDomainName: string;
    userName: string;
    twoFactor: TwoFactorConfigurationDTO;
}
export interface KAUAuthDTO {
    id: string;
    etag: string;
    LastUpdate: Date;
    isActive: boolean;
    KAUUserId: string;
    legalName: string;
    email: string;
    twoFactor: TwoFactorConfigurationDTO;
}
export interface CertificateAuthDTO {
    id: string;
    etag: string;
    LastUpdate: Date;
    isActive: boolean;
    certificateThumbprint: string;
    serialNumber: string;
    issuer: string;
    subject: string;
    publicKeyHash: string;
    validFrom: Date;
    validUntil: Date;
    isRevoked: boolean;
    revocationReason: string;
    revokedAt: Date;
}
export interface CsrInputDTO {
    csrPem: string;
    profile: string;
}
//# sourceMappingURL=IdentityAdminIF_v1.d.ts.map