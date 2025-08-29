import type { FormEvent } from "react";
import { useState } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import { LoginIF, type ApiError } from "@docratis/bff.api.package.ts";
import { Input, Label, LoadingButton, PasswordInput, type PasswordRules } from "@docratis/ui.package.react"

const rules: PasswordRules = {
  minLength: 12,
  maxLength: 128,
  minUppercase: 1,
  minLowercase: 1,
  minDigits: 1,
  minNonAlphanumeric: 1,
  minUniqueChars: 5,
  maxRepeatRun: 3,
};

export default function PasswordChangePage() {
    const nav = useNavigate();
    const [q] = useSearchParams();
    const from = q.get("from") ?? "/";
    const email = q.get("email") ?? "/";

    const [oldPwd, setOldPwd] = useState("");
    const [newPwd1, setNewPwd1] = useState("");
    const [newPwd2, setNewPwd2] = useState("");
    const [err, setErr] = useState("");
    const [isLoading, setIsLoading] = useState(false);

    const onSubmit = async (e: FormEvent) => {
        e.preventDefault();
        setErr("");
        try {
            setIsLoading(true);
            await LoginIF.V1.ChangePassword(email, oldPwd, newPwd1);

            nav(from, { replace: true });
        } catch (e: unknown) {
            setErr(`Jelszócsere sikertelen: ${(e as ApiError).message}, ${(e as ApiError).additionalInformation}`);
        }
        finally {
            setIsLoading(false);
        }
    };

    return (
        <form onSubmit={onSubmit} className="p-6 max-w-sm mx-auto space-y-3 w-96">
            <h1>Jelszócsere</h1>
            <div className={isLoading ? "space-y-6 pointer-events-none opacity-50" : "space-y-6"}>
                <div className="space-y-2">
                    <Label htmlFor="email">Email</Label>
                    <Input
                        id="email"
                        name="email"
                        type="email"
                        autoComplete="email"
                        value={email}
                        placeholder="sample@domain.com"
                        readOnly
                    />
                </div>

                <div className="space-y-2">
                    <Label htmlFor="oldPassword">Régi jelszó</Label>
                    <PasswordInput
                        id="oldPassword"
                        name="oldPassword"
                        value={oldPwd}
                        onChange={(e) => setOldPwd(e.target.value)}
                        required
                    />
                </div>

                <div className="space-y-2">
                    <Label htmlFor="newPassword">Új jelszó</Label>
                    <PasswordInput
                        id="newPassword"
                        name="newPassword"
                        value={newPwd1}
                        onChange={(e) => setNewPwd1(e.target.value)}
                        passwordRules={rules}
                        emailForRules={email}
                        required
                    />
                </div>

                <div className="space-y-2">
                    <Label htmlFor="newPassword2">Új jelszó</Label>
                    <PasswordInput
                        id="newPassword2"
                        name="newPassword2"
                        value={newPwd2}
                        onChange={(e) => setNewPwd2(e.target.value)}
                        required
                    />
                </div>

                <div className="text-destructive h-8">{err}</div>

                <div>
                    <LoadingButton type="submit" size="lg" className="w-full" isLoading={isLoading} loadingText="Mentés…">
                        Jelszócsere
                    </LoadingButton>
                </div>
            </div>
        </form>
    );
}
