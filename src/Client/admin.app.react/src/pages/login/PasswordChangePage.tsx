import type { FormEvent } from "react";
import { useState } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import { LoginIF, type ApiError } from "docratis.ts.api";

export default function PasswordChangePage() {
    const nav = useNavigate();
    const [q] = useSearchParams();
    const from = q.get("from") ?? "/";
    const email = q.get("email") ?? "/";

    const [oldPwd, setOldPwd] = useState("");
    const [newPwd, setNewPwd] = useState("");
    const [err, setErr] = useState("");

    const onSubmit = async (e: FormEvent) => {
        e.preventDefault();
        setErr("");
        try {
            await LoginIF.V1.ChangePassword(email, oldPwd, newPwd);

            nav(from, { replace: true });
        } catch (e: unknown) {
            setErr(`Jelszócsere sikertelen: ${(e as ApiError).message}, ${(e as ApiError).additionalInformation}`);
        }
    };

    return (
        <form onSubmit={onSubmit} className="p-6 max-w-sm mx-auto space-y-3">
            <h1>Jelszócsere</h1>
            <div>
                <input value={email} type="text" readOnly />
            </div>
            <div>
                <input value={oldPwd} onChange={(e) => setOldPwd(e.target.value)} type="password" placeholder="Régi jelszó" />
            </div>
            <div>
                <input value={newPwd} onChange={(e) => setNewPwd(e.target.value)} type="password" placeholder="Új jelszó" />
            </div>
            {err && <div>{err}</div>}
            <div>
                <button type="submit">Mentés</button>
            </div>
        </form>
    );
}
