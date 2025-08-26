import type { FormEvent } from "react";
import { useNavigate, useSearchParams, Link } from "react-router-dom";
import { useState } from "react";
import { ApiError, BFFRestClient, LoginIF, SignInResult } from "@docratis/bff.api.package.ts";
import { useAuth } from "../../auth/AuthContext";
import { Input, Label, LoadingButton } from "@docratis/ui.package.react"

export default function EmailPasswordLoginPage() {
    const auth = useAuth();
    const nav = useNavigate();
    const [q] = useSearchParams();
    const from = q.get("from") ?? "/";

    const [email, setEmail] = useState("");
    const [pwd, setPwd] = useState("");
    const [err, setErr] = useState("");
    const [isLoading, setIsLoading] = useState(false);

    const onSubmit = async (e: FormEvent) => {
        e.preventDefault();
        setErr("");
        try {
            setIsLoading(true);
            const r = await LoginIF.V1.LoginWithAD(email, pwd);

            switch (r.result) {
                case SignInResult.Ok:
                    {
                        const bff = BFFRestClient.getInstance()
                        bff.setAuthorization(r.tokens.AccessToken, r.accountId, r.accountName);
                        auth.login({
                            token: r.tokens,
                            accountId: r.accountId,
                            accountName: r.accountName,
                        });

                        if (r.requires2FA)
                            nav(`/login/2fa?from=${encodeURIComponent(from)}`, { replace: true });
                        else {
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
                    setErr("Email has not been confirmed by the user");
            }
        } catch (e) {
            setErr(`Sikertelen belépés.${(e as ApiError).message} ${(e as ApiError).additionalInformation}`);
        }
        finally {
            setIsLoading(false);
        }
    };

    return (
        <form onSubmit={onSubmit} className="p-6 max-w-sm mx-auto space-y-6 w-96">
            <h1>Belépés</h1>
            <div className={isLoading ? "space-y-6 pointer-events-none opacity-50" : "space-y-6"}>
                <div className="space-y-2">
                    <Label htmlFor="email">Email</Label>
                    <Input
                        id="email"
                        name="email"
                        type="email"
                        autoComplete="email"
                        value={email}
                        onChange={(e) => setEmail(e.target.value)}
                        placeholder="sample@domain.com"
                        required
                    />
                </div>

                <div className="space-y-2">
                    <Label htmlFor="password">Jelszó</Label>
                    <Input
                        id="password"
                        name="password"
                        type="password"
                        value={pwd}
                        onChange={(e) => setPwd(e.target.value)}
                        placeholder="your password"
                        required
                    />
                </div>
            </div>

            {err && <div className="text-destructive h-8">{err}</div>}

            <div>
                <LoadingButton type="submit" size="lg" className="w-full" isLoading={isLoading} loadingText="Bejelentkezés…">
                    Belépés
                </LoadingButton>
            </div>

            <Link
                to={"/login/forgot-password?email=" + email}
                className="block w-full text-center text-sm text-primary underline underline-offset-4 mt-2"
            >
                Elfelejtettem a jelszavam
            </Link>
        </form>
    );
}
