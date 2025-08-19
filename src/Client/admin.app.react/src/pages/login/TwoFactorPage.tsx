import type { FormEvent } from "react";
import { useState } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import { useAuth } from "../../auth/AuthContext";

export default function TwoFactorPage() {
    const { completeTwoFactor } = useAuth();
    const nav = useNavigate();
    const [q] = useSearchParams();
    const from = q.get("from") ?? "/";

    const [code, setCode] = useState("");
    const [err, setErr] = useState("");

    const onSubmit = async (e: FormEvent) => {
        e.preventDefault();
        setErr("");
        try {
            await completeTwoFactor(code);
            nav(from, { replace: true });
        } catch {
            setErr("2FA hiba, próbáld újra.");
        }
    };

    return (
        <form onSubmit={onSubmit} className="p-6 max-w-sm mx-auto space-y-3">
            <h1>2FA</h1>
            <input value={code} onChange={(e) => setCode(e.target.value)} placeholder="Kód" />
            {err && <div>{err}</div>}
            <button type="submit">Megerősítés</button>
        </form>
    );
}
