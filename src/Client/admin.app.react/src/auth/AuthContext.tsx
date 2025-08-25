import type { TokensDTO } from "docratis.ts.api";
import { createContext, useContext, useMemo, useState, useCallback } from "react";

type AuthState = {
    isAuth: boolean;
    token: TokensDTO | null;
    accountId: string | null;
    accountName: string | null;
};

type AuthCtx = AuthState & {
    login: (p: { token: TokensDTO; accountId: string; accountName: string }) => void;
    logout: () => void;
};

const Ctx = createContext<AuthCtx>({
    isAuth: false,
    token: null,
    accountId: null,
    accountName: null,
    login: () => { },
    logout: () => { },
});

export const useAuth = () => useContext(Ctx);

export function AuthProvider({ children }: { children: React.ReactNode }) {
    const [state, setState] = useState<AuthState>({
        isAuth: false,
        token: null,
        accountId: null,
        accountName: null,
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
            accountId: null,
            accountName: null,
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
