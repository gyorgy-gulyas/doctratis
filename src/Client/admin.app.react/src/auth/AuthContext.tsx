import { LoginIF, SignInResult } from "docratis.ts.api";
import { createContext, useContext, useState } from "react";

type Auth = {
    isAuth: boolean;
    token: string | null;
    login: (email: string, password: string) => Promise<void>;
    logout: () => void;
};

const Ctx = createContext<Auth | null>(null);
export const useAuth = () => {
    const v = useContext(Ctx);
    if (!v) throw new Error("AuthProvider missing");
    return v;
};

export function AuthProvider({ children }: { children: React.ReactNode }) {
    const [token, setToken] = useState<string | null>(null);

    const login = async (email: string, password: string) => {
        const loginResult = await LoginIF.V1.LoginWithEmailPassword(email, password);

        if (loginResult.result == SignInResult.Ok)
            setToken(loginResult.tokens.AccessToken);
    };

    const logout = () => setToken(null);

    return (
        <Ctx.Provider value={{ isAuth: !!token, token, login, logout }}>
            {children}
        </Ctx.Provider>
    );
}