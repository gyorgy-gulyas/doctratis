import { createContext, useContext, useState } from "react";
import axios from "axios";

type Auth = {
    isAuth: boolean;
    token: string | null; // "eyJ..."
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
        const r = await axios.post("/api/login", { email, password });
        // feltételezve, hogy { token: "..." }
        setToken(r.data.token);
    };

    const logout = () => setToken(null);

    return (
        <Ctx.Provider value={{ isAuth: !!token, token, login, logout }}>
            {children}
        </Ctx.Provider>
    );
}