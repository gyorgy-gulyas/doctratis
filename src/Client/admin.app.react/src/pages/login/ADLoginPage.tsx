import type { FormEvent } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import { useState, useMemo } from "react";
import { ApiError, BFFRestClient, LoginIF, SignInResult } from "@docratis/bff.api.package.ts";
import { useAuth } from "../../auth/AuthContext";
import { LoadingButton, Input, Label } from "@docratis/ui.package.react";

export default function ADLoginPage() {
    const auth = useAuth();
    const nav = useNavigate();
    const [q] = useSearchParams();
    const from = q.get("from") ?? "/";

    const [username, setUsername] = useState("");
    const [pwd, setPwd] = useState("");
    const [err, setErr] = useState("");
    const [isLoading, setIsLoading] = useState(false);

    // DOMAIN\user -> DOMAIN megjelenítése külön mezőben
    const domain = useMemo(() => {
        const i = username.indexOf("\\");
        return i > 0 ? username.slice(0, i) : "";
    }, [username]);

    const onSubmit = async (e: FormEvent) => {
        e.preventDefault();
        setErr("");
        try {
            setIsLoading(true);
            const r = await LoginIF.V1.LoginWithAD(username, pwd);

            switch (r.result) {
                case SignInResult.Ok: {
                    const bff = BFFRestClient.getInstance();
                    bff.setAuthorization(r.tokens.AccessToken, r.accountId, r.accountName);

                    auth.login({
                        token: r.tokens,
                        accountId: r.accountId,
                        accountName: r.accountName,
                    });

                    nav(from, { replace: true });
                    break;
                }
                case SignInResult.InvalidUserNameOrPassword:
                    setErr("Hibás felhasználónév vagy jelszó.");
                    break;
                case SignInResult.DomainNotSpecified:
                    setErr("A domain név megadása kötelező");
                    break;
                case SignInResult.DomainNotRegistered:
                    setErr("A megadott domain nincs regisztrálva a rendszerben");
                    break;
                case SignInResult.DomainUserNotRegistered:
                    setErr("A felhasznéló nincs regisztrálva a rendszerben");
                    break;
                case SignInResult.UserIsNotActive:
                    setErr("A felhasználói fiók inaktív vagy zárolt.");
                    break;
                default:
                    setErr("Ismeretlen hiba.");
            }
        } catch (e) {
            const a = e as ApiError;
            setErr(`Sikertelen belépés. ${a?.message ?? ""} ${a?.additionalInformation ?? ""}`);
        }
        finally {
            setIsLoading(false);
        }
    };

    return (
        <form onSubmit={onSubmit} className="p-6 max-w-xl mx-auto space-y-4">
            <h1 className="text-xl font-semibold">AD bejelentkezés</h1>
            <div className={isLoading ? "space-y-6 pointer-events-none opacity-50" : "space-y-6"}>
                <div className="space-y-2">
                    <Label htmlFor="username">Felhasználónév</Label>
                    <Input
                        id="username"
                        name="username"
                        autoComplete="username"
                        value={username}
                        onChange={(e) => setUsername(e.target.value)}
                        placeholder="DOMAIN\felhasznalo vagy felhasznalo"
                        required
                    />
                </div>

                <div className="space-y-2">
                    <Label htmlFor="password">Jelszó</Label>
                    <Input
                        id="password"
                        name="password"
                        type="password"
                        autoComplete="current-password"
                        value={pwd}
                        onChange={(e) => setPwd(e.target.value)}
                        placeholder="Jelszó"
                        required
                    />
                </div>

                {domain && (
                    <div className="space-y-2 h-6 py-2">
                        <Label htmlFor="domain">Domain: {domain}</Label>
                    </div>
                )}

                <div className="text-sm text-destructive h-10 my-2">{err}</div>

                <LoadingButton type="submit" size="lg" className="w-full" isLoading={isLoading} loadingText="Bejelentkezés…">
                    Belépés
                </LoadingButton>
            </div>
        </form >
    );
}
