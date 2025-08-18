import { FormEvent, useState } from "react";
import { useAuth } from "../auth/AuthContext";
import { useNavigate } from "react-router-dom";

export default function LoginPage() {
    const { login } = useAuth();
    const nav = useNavigate();
    const [u, setU] = useState(""), [p, setP] = useState("");
    const [err, setErr] = useState("");

    const onSubmit = async (e: FormEvent) => {
        e.preventDefault();
        setErr("");
        try {
            await login(u, p);
            nav("/", { replace: true });
        } catch {
            setErr("Sikertelen bel�p�s.");
        }
    };

    return (
        <form onSubmit={onSubmit} className="p-6 max-w-sm mx-auto space-y-3">
            <h1>Bel�p�s</h1>
            <input value={u} onChange={(e) => setU(e.target.value)} placeholder="Felhaszn�l� / Email" />
            <input value={p} onChange={(e) => setP(e.target.value)} type="password" placeholder="Jelsz�" />
            {err && <div>{err}</div>}
            <button type="submit">Bel�p�s</button>
        </form>
    );
}