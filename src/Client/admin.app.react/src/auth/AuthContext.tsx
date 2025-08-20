import type { TokensDTO } from "docratis.ts.api";
import { createContext, useContext, useMemo, useState } from "react";

type AuthCtx = {
    isAuth: boolean;
    token: TokensDTO | null;
    accountId: string | null;
    accountName: string | null;
    login: (p: { token: TokensDTO; accountId: string; accountName: string }) => void;
    logout: () => void;
};

const Ctx = createContext<AuthCtx | null>(null);

export const useAuth = () => {
    const v = useContext(Ctx);
    if (!v) throw new Error("AuthProvider missing");
    return v;
};

export function AuthProvider({ children }: { children: React.ReactNode }) {
    const [token, setToken] = useState<TokensDTO | null>(null);
    const [isAuth, setIsAuth] = useState<boolean>(false);
    const [accountId, setAccountId] = useState<string | null>(null);
    const [accountName, setAccountName] = useState<string | null>(null);


    const value = useMemo(
        () => ({ isAuth, token, accountId, accountName, login, logout }),
        [isAuth, token, accountId, accountName]
    );

    const login: AuthCtx["login"] = ({ token, accountId, accountName }) => {
        setToken(token);
        setAccountId(accountId);
        setAccountName(accountName);
        setIsAuth(true);
    };

    const logout = () => {
        setIsAuth(false);
        setToken(null);
        setAccountId(null);
        setAccountName(null);
    };

    return (
        <Ctx.Provider value={value}>
            {children}
        </Ctx.Provider>
    );
}
