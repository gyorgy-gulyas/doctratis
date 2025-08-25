export declare enum SignInResult {
    Ok = "Ok",
    InvalidUserNameOrPassword = "InvalidUserNameOrPassword",
    EmailNotConfirmed = "EmailNotConfirmed",
    UserIsNotActive = "UserIsNotActive",
    PasswordExpired = "PasswordExpired",
    DomainNotSpecified = "DomainNotSpecified",
    DomainNotRegistered = "DomainNotRegistered",
    DomainUserNotRegistered = "DomainUserNotRegistered",
    KAUTokenError = "KAUTokenError",
    KAUUserNotFound = "KAUUserNotFound"
}
export interface TokensDTO {
    AccessToken: string;
    AccessTokenExpiresAt: Date;
    RefreshToken: string;
    RefreshTokenExpiresAt: Date;
}
export interface LoginResultDTO {
    result: SignInResult;
    tokens: TokensDTO;
    requires2FA: boolean;
    accountId: string;
    accountName: string;
}
//# sourceMappingURL=LoginIF_v1.d.ts.map