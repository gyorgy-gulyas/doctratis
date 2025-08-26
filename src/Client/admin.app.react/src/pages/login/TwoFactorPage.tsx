import type { FormEvent } from "react";
import { useState } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import { useAuth } from "../../auth/AuthContext";
import { InputOTP, InputOTPGroup, InputOTPSlot, InputOTPSeparator, LoadingButton } from "@docratis/ui.package.react";
import { ApiError, BFFRestClient, LoginIF, SignInResult } from "@docratis/bff.api.package.ts";

export default function TwoFactorPage() {
    const auth = useAuth();
    const nav = useNavigate();
    const [q] = useSearchParams();
    const from = q.get("from") ?? "/";

    const [code, setCode] = useState("");
    const [err, setErr] = useState("");
    const [isLoading, setIsLoading] = useState(false);

    const onSubmit = async (e: FormEvent) => {
        e.preventDefault();
        setErr("");
        try {
            setIsLoading(true);
            const tokens = await LoginIF.V1.Login2FA(code);

            auth.login({
                token: tokens,
                accountId: auth.accountId,
                accountName: auth.accountName,
            })
            nav(from, { replace: true });
        } catch (e) {
            setErr(`Sikertelen belépés.${(e as ApiError).message} ${(e as ApiError).additionalInformation}`);
        }
        finally {
            setIsLoading(false);
        }
    };

    return (
        <form onSubmit={onSubmit} className="p-6 max-w-sm mx-auto space-y-6 w-96">
            <h1 className="text-xl font-semibold">Kétlépcsős azonosítás</h1>
            <div className={isLoading ? "space-y-6 pointer-events-none opacity-50" : "space-y-6"}>
                <div className="space-y-2">
                    <label className="text-sm text-muted-foreground">
                        Írd be az <span className="font-medium">6 jegyű</span> kódot:
                    </label>

                    <InputOTP
                        maxLength={6}
                        value={code}
                        onChange={setCode}
                        inputMode="numeric"
                        pattern="\d*" // only numbers
                        containerClassName="justify-center">
                        <InputOTPGroup>
                            <InputOTPSlot index={0} />
                            <InputOTPSlot index={1} />
                            <InputOTPSlot index={2} />
                        </InputOTPGroup>
                        <InputOTPSeparator />
                        <InputOTPGroup>
                            <InputOTPSlot index={3} />
                            <InputOTPSlot index={4} />
                            <InputOTPSlot index={5} />
                        </InputOTPGroup>
                    </InputOTP>
                </div>

                {err && <div className="text-sm text-destructive">{err}</div>}

                <LoadingButton type="submit" size="lg" className="w-full" isLoading={isLoading} loadingText="Ellenörzés…" disabled={code.length !== 6}>
                    Belépés
                </LoadingButton>
            </div>
        </form>
    );
}
