import * as LoginIF_v1 from "../../../types/IAM/Identities/LoginIF_v1";
export declare const LoginIF: {
    V1: {
        LoginWithEmailPassword(email: string, password: string): Promise<LoginIF_v1.LoginResultDTO>;
        ConfirmEmail(email: string, token: string): Promise<{}>;
        ChangePassword(email: string, oldPassword: string, newPassword: string): Promise<{}>;
        ForgotPassword(email: string): Promise<{}>;
        ResetPassword(email: string, token: string, newPassword: string): Promise<{}>;
        LoginWithAD(username: string, password: string): Promise<LoginIF_v1.LoginResultDTO>;
        Login2FA(code: string): Promise<LoginIF_v1.TokensDTO>;
        RefreshTokens(refreshToken: string): Promise<LoginIF_v1.TokensDTO>;
        GetKAULoginURL(redirectUrl: string): Promise<string>;
    };
};
//# sourceMappingURL=LoginIF_v1.RestClient.d.ts.map