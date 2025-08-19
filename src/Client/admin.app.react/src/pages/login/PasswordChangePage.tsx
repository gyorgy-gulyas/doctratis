import type { FormEvent } from "react";
import { useState } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import { useAuth } from "../../auth/AuthContext";

export default function PasswordChangePage() {
    const { changePassword } = useAuth();
    const nav = useNavigate();
    const [q] = useSearchParams();
    const from = q.get("from") ?? "/";

    const [oldPwd, setOldPwd] = useState("");
    const [newPwd, setNewPwd] = useState("");
    const [err, setErr] = useState("");

    const onSubmit = async (e: FormEvent) => {
        e.preventDefault();
        setErr("");
        try {
            await changePassword(oldPwd, newPwd);
            nav(from, { replace: true });
        } catch {
            setErr("Jelszócsere sikertelen.");
        }
    };

    return (
        <form onSubmit={onSubmit} className="p-6 max-w-sm mx-auto space-y-3">
            <h1>Jelszócsere</h1>
            <input value={oldPwd} onChange={(e) => setOldPwd(e.target.value)} type="password" placeholder="Régi jelszó" />
            <input value={newPwd} onChange={(e) => setNewPwd(e.target.value)} type="password" placeholder="Új jelszó" />
            {err && <div>{err}</div>}
            <button type="submit">Mentés</button>
        </form>
    );
}
