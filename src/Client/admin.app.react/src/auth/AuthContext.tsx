import { createContext, useContext, useState } from "react";
import { AuthModes } from "./types";
import type { LoginPayload, LoginNext } from "./types";
import { LoginIF, SignInResult } from "docratis.ts.api";

type AuthCtx = {
    isAuth: boolean;
    token: string | null;
    login: (payload: LoginPayload) => Promise<LoginNext>;
    completeTwoFactor: (code: string) => Promise<void>;
    logout: () => void;
};

const Ctx = createContext<AuthCtx | null>(null);

export const useAuth = () => {
    const v = useContext(Ctx);
    if (!v) throw new Error("AuthProvider missing");
    return v;
};

export function AuthProvider({ children }: { children: React.ReactNode }) {
    const [token, setToken] = useState<string | null>(null);

    const login = async (payload: LoginPayload): Promise<LoginNext> => {
        switch (payload.provider) {
            case AuthModes.EmailPassword: {
                const { email, password } = payload.data;
                const r = await LoginIF.V1.LoginWithEmailPassword(email, password);

                switch (r.result) {
                    case SignInResult.Ok:
                        if (r.requires2FA)
                            return { kind: "twofactor", accessToken: r.tokens.AccessToken };
                        else
                            return { kind: "ok", accessToken: r.tokens.AccessToken };
                    case SignInResult.InvalidUserNameOrPassword:
                        return { kind: "error", message: "Invalid username or password provided" };
                    case SignInResult.EmailNotConfirmed:
                        return { kind: "error", message: "Email has not been confirmed by the user" };
                    case SignInResult.UserIsNotActive:
                        return { kind: "error", message: "The user account is deactivated or locked" };
                    case SignInResult.PasswordExpired:
                        return { kind: "passwordChange" };
                    default:
                        return { kind: "error", message: "Unknown error" };
                }
            }
            case AuthModes.AD: {
                const { username, password } = payload.data;
                const r = await LoginIF.V1.LoginWithAD(username, password);

                switch (r.result) {
                    case SignInResult.Ok:
                        if (r.requires2FA)
                            return { kind: "twofactor", accessToken: r.tokens.AccessToken };
                        else
                            return { kind: "ok", accessToken: r.tokens.AccessToken };
                    case SignInResult.InvalidUserNameOrPassword:
                        return { kind: "error", message: "Invalid username or password provided" };
                    case SignInResult.DomainNotSpecified:
                        return { kind: "error", message: "Username does not contain the domainname" };
                    case SignInResult.DomainNotRegistered:
                        return { kind: "error", message: "The domain is not allowed to use in the system" };
                    case SignInResult.DomainUserNotRegistered:
                        return { kind: "error", message: "domain user is not registered in the system" };
                    default:
                        return { kind: "error", message: "Unknown error" };
                }
            }
            case AuthModes.KAU: {
                const { frontendUrl } = payload.data;
                const redirectURL = await LoginIF.V1.GetKAULoginURL(frontendUrl);
                return { kind: "redirect", url: redirectURL };
            }
        }
    };

    const completeTwoFactor = async (code: string) => {
        const tokens = await LoginIF.V1.Login2FA(code);
        setToken(tokens.AccessToken);
    };

    const logout = () => {
        setToken(null);
    };

    return (
        <Ctx.Provider value={{ isAuth: !!token, token, login, completeTwoFactor, logout }}>
            {children}
        </Ctx.Provider>
    );
}
