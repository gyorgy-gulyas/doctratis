import type { FormEvent } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import { useState } from "react";

import { useAuth } from "../../auth/AuthContext";
import { AuthModes } from "../../auth/types";

export default function EmailPasswordLoginPage() {
    const { login } = useAuth();
    const nav = useNavigate();
    const [q] = useSearchParams();
    const from = q.get("from") ?? "/";

    const [email, setEmail] = useState("");
    const [pwd, setPwd] = useState("");
    const [err, setErr] = useState("");

    const onSubmit = async (e: FormEvent) => {
        e.preventDefault();
        setErr("");
        try {
            const next = await login({
                provider: AuthModes.EmailPassword,
                data: { email, password: pwd },
            });

            switch (next.kind) {
                case "ok":
                    nav(from, { replace: true });
                    break;
                case "twofactor":
                    nav(`/login/2fa?from=${encodeURIComponent(from)}`, { replace: true });
                    break;
                case "passwordChange":
                    nav(`/login/password-change?from=${encodeURIComponent(from)}`, { replace: true });
                    break;
                case "redirect":
                    nav(next.url);
                    break;
                case "error":
                    setErr(next.message || "Sikertelen belépés.");
                    break;
            }
        } catch (e) {
            setErr(`Sikertelen belépés.${e}`);
        }
    };

    return (
        <form onSubmit={onSubmit} className="p-6 max-w-sm mx-auto space-y-3">
            <h1>Belépés (Email & Jelszó)</h1>
            <input value={email} onChange={(e) => setEmail(e.target.value)} placeholder="Email" />
            <input value={pwd} onChange={(e) => setPwd(e.target.value)} type="password" placeholder="Jelszó" />
            {err && <div>{err}</div>}
            <button type="submit">Belépés</button>
        </form>
    );
}
