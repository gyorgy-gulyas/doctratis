import type { TokensDTO } from "@docratis/bff.api.package.ts";
import { createContext, useContext, useMemo, useState, useCallback } from "react";

type AuthState = {
    isAuth: boolean;
    token: TokensDTO | null;
    accountId: string;
    accountName: string;
};

type AuthCtx = AuthState & {
    login: (p: { token: TokensDTO; accountId: string; accountName: string }) => void;
    logout: () => void;
};

const Ctx = createContext<AuthCtx>({
    isAuth: false,
    token: null,
    accountId: "",
    accountName: "",
    login: () => { },
    logout: () => { },
});

export const useAuth = () => useContext(Ctx);

export function AuthProvider({ children }: { children: React.ReactNode }) {
    const [state, setState] = useState<AuthState>({
        isAuth: false,
        token: null,
        accountId: "",
        accountName: "",
    });

    const login = useCallback<AuthCtx["login"]>(({ token, accountId, accountName }) => {
        setState({
            isAuth: true,
            token,
            accountId,
            accountName,
        });
    }, []);

    const logout = useCallback<AuthCtx["logout"]>(() => {
        setState({
            isAuth: false,
            token: null,
            accountId: "",
            accountName: "",
        });
    }, []);

    const value = useMemo<AuthCtx>(() => ({
        ...state, login, logout
    }
    ), [state, login, logout]);


    return (
        <Ctx.Provider value={value}>
            {children}
        </Ctx.Provider>
    );
}
