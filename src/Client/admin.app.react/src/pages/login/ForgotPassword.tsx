// src/pages/login/ForgotPasswordPage.tsx
import type { FormEvent } from "react";
import { useState } from "react";
import { useNavigate, Link } from "react-router-dom";
import { LoginIF, type ApiError } from "@docratis/bff.api.package.ts";
import { Input, Label, LoadingButton } from "@docratis/ui.package.react";

export default function ForgotPasswordPage() {
  const nav = useNavigate();

  const [email, setEmail] = useState("");
  const [err, setErr] = useState("");
  const [ok, setOk] = useState(""); // sikerüzenet
  const [isLoading, setIsLoading] = useState(false);

  const onSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setErr("");
    setOk("");
    setIsLoading(true);

    try {
      // ⬇️ Ha máshogy hívják nálad, cseréld:
      await LoginIF.V1.ForgotPassword(email);

      setOk(
        "Ha létezik ilyen e-mail cím, elküldtük a jelszó-visszaállítási linket."
      );
      nav("/login/email", { replace: true });
    } catch (e: unknown) {
      const ae = e as ApiError;
      setErr(`Nem sikerült elküldeni a visszaállítási e-mailt: ${ae.message ?? ""} ${ae.additionalInformation ?? ""}`);
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <form onSubmit={onSubmit} className="p-6 max-w-sm mx-auto space-y-6 w-96">
      <h1>Elfelejtett jelszó</h1>

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
      </div>

      {/* üzenetek */}
      {err && <div className="text-destructive min-h-5">{err}</div>}
      {ok && <div className="text-green-600 dark:text-green-400 min-h-5">{ok}</div>}

      <div className="flex flex-col gap-3">
        <LoadingButton
          type="submit"
          size="lg"
          className="w-full"
          isLoading={isLoading}
          loadingText="Küldés…"
        >
          Visszaállítási link küldése
        </LoadingButton>

        <Link to="/login/email" className="text-sm text-primary underline underline-offset-4 text-center">
          Vissza a bejelentkezéshez
        </Link>
      </div>
    </form>
  );
}
