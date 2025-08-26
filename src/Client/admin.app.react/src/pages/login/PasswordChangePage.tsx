import type { FormEvent } from "react";
import { useState } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import { LoginIF, type ApiError } from "@docratis/bff.api.package.ts";
import { Input, Label, LoadingButton } from "@docratis/ui.package.react"

export default function PasswordChangePage() {
    const nav = useNavigate();
    const [q] = useSearchParams();
    const from = q.get("from") ?? "/";
    const email = q.get("email") ?? "/";

    const [oldPwd, setOldPwd] = useState("");
    const [newPwd, setNewPwd] = useState("");
    const [err, setErr] = useState("");
    const [isLoading, setIsLoading] = useState(false);

    const onSubmit = async (e: FormEvent) => {
        e.preventDefault();
        setErr("");
        try {
            setIsLoading(true);
            await LoginIF.V1.ChangePassword(email, oldPwd, newPwd);

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
                    <Input
                        id="oldPassword"
                        name="oldPassword"
                        type="password"
                        value={oldPwd}
                        onChange={(e) => setOldPwd(e.target.value)}
                        required
                    />
                </div>

                <div className="space-y-2">
                    <Label htmlFor="newPassword">Új jelszó</Label>
                    <Input
                        id="newPassword"
                        name="newPassword"
                        type="password"
                        value={newPwd}
                        onChange={(e) => setNewPwd(e.target.value)}
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
