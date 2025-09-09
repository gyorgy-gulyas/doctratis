import * as IdentityAdminIF_v1 from "../../../types/IAM/Identities/IdentityAdminIF_v1";
export declare const IdentityAdminIF: {
    V1: {
        RegisterLdapDomain(ldap: IdentityAdminIF_v1.LdapDomainDTO): Promise<IdentityAdminIF_v1.LdapDomainDTO>;
        UpdateRegisteredLdapDomain(ldap: IdentityAdminIF_v1.LdapDomainDTO): Promise<IdentityAdminIF_v1.LdapDomainDTO>;
        GetAllRegisteredLdapDomain(): Promise<IdentityAdminIF_v1.LdapDomainSummaryDTO[]>;
        GetRegisteredLdapDomain(id: string): Promise<IdentityAdminIF_v1.LdapDomainDTO>;
        getAllAccount(): Promise<IdentityAdminIF_v1.AccountSummaryDTO[]>;
        getAccount(id: string): Promise<IdentityAdminIF_v1.AccountDTO>;
        createAccount(username: string, accountType: IdentityAdminIF_v1.AccountTypes): Promise<IdentityAdminIF_v1.AccountDTO>;
        updateAccount(accountId: string, etag: string, data: IdentityAdminIF_v1.AccountDataDTO): Promise<IdentityAdminIF_v1.AccountDTO>;
        listAuthsForAccount(accountId: string): Promise<IdentityAdminIF_v1.AuthDTO[]>;
        setActiveForAuth(accountId: string, authId: string, etag: string, isActive: boolean): Promise<IdentityAdminIF_v1.AuthDTO>;
        createtEmailAuth(accountId: string, email: string, initialPassword: string, twoFactor: IdentityAdminIF_v1.TwoFactorConfigurationDTO): Promise<IdentityAdminIF_v1.EmailAuthDTO>;
        getEmailAuth(accountId: string, authId: string): Promise<IdentityAdminIF_v1.EmailAuthDTO>;
        setTwoFactorOnEmailAuth(accountId: string, authId: string, etag: string, twoFactor: IdentityAdminIF_v1.TwoFactorConfigurationDTO): Promise<IdentityAdminIF_v1.EmailAuthDTO>;
        resetPasswordOnEmailAuth(accountId: string, authId: string, etag: string, newPassword: string): Promise<IdentityAdminIF_v1.EmailAuthDTO>;
        createADAuth(accountId: string, ldapDomainId: string, adUsername: string, twoFactor: IdentityAdminIF_v1.TwoFactorConfigurationDTO): Promise<IdentityAdminIF_v1.ADAuthDTO>;
        getADAuth(accountId: string, authId: string): Promise<IdentityAdminIF_v1.ADAuthDTO>;
        setTwoFactorOnADAuth(accountId: string, authId: string, etag: string, twoFactor: IdentityAdminIF_v1.TwoFactorConfigurationDTO): Promise<IdentityAdminIF_v1.ADAuthDTO>;
        createKAUAuth(accountId: string, kauUserId: string, twoFactor: IdentityAdminIF_v1.TwoFactorConfigurationDTO): Promise<IdentityAdminIF_v1.KAUAuthDTO>;
        getKAUAuth(accountId: string, authId: string): Promise<IdentityAdminIF_v1.KAUAuthDTO>;
        setTwoFactorOnKAUAuth(accountId: string, authId: string, etag: string, twoFactor: IdentityAdminIF_v1.TwoFactorConfigurationDTO): Promise<IdentityAdminIF_v1.KAUAuthDTO>;
        createCertificateAuthFromCSR(accountId: string, data: IdentityAdminIF_v1.CsrInputDTO): Promise<IdentityAdminIF_v1.CertificateAuthDTO>;
        revokeCertificate(accountId: string, authId: string, etag: string, reason: string): Promise<IdentityAdminIF_v1.CertificateAuthDTO>;
        getCertificateAuth(accountId: string, authId: string): Promise<IdentityAdminIF_v1.CertificateAuthDTO>;
    };
};
//# sourceMappingURL=IdentityAdminIF_v1.RestClient.d.ts.map