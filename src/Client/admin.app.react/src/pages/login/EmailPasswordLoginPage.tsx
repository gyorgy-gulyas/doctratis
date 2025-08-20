import type { FormEvent } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import { useState } from "react";
import { BFFRestClient, LoginIF, SignInResult } from "docratis.ts.api";
import { useAuth } from "../../auth/AuthContext";

export default function EmailPasswordLoginPage() {
    const auth = useAuth();
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
            const r = await LoginIF.V1.LoginWithEmailPassword(email, pwd);

            switch (r.result) {
                case SignInResult.Ok:
                    {
                        const bff = BFFRestClient.getInstance()
                        bff.setAuthorization(r.accountId, r.accountName, r.tokens.AccessToken);

                        if (r.requires2FA)
                            nav(`/login/2fa?from=${encodeURIComponent(from)}`, { replace: true });
                        else {
                            auth.isAuth = true
                            nav(from, { replace: true });
                        }
                    }
                    break;
                case SignInResult.InvalidUserNameOrPassword:
                    setErr("Invalid username or password provided");
                    break;
                case SignInResult.EmailNotConfirmed:
                    setErr("Email has not been confirmed by the user");
                    break;
                case SignInResult.UserIsNotActive:
                    setErr("The user account is deactivated or locked");
                    break;
                case SignInResult.PasswordExpired:
                    nav(`/login/password-change?from=${encodeURIComponent(from)}&email=${encodeURIComponent(email)}`, { replace: true });
                    break;
                default:
                    return { kind: "error", message: "Unknown error" };
            }
        } catch (e) {
            setErr(`Sikertelen belépés.${e}`);
        }
    };

    return (
        <form onSubmit={onSubmit} className="p-6 max-w-sm mx-auto space-y-3">
            <h1>Belépés (Email & Jelszó)</h1>
            <div>
                <input value={email} onChange={(e) => setEmail(e.target.value)} placeholder="Email" />
            </div>
            <div>
                <input value={pwd} onChange={(e) => setPwd(e.target.value)} type="password" placeholder="Jelszó" />
            </div>
            {err && <div>{err}</div>}
            <div>
                <button type="submit">Belépés</button>
            </div>
        </form>
    );
}
