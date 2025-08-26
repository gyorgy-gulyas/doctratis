import { Outlet } from "react-router-dom";
import DocratisLogo from "../components/DocratisLogo";
import { Label } from "@docratis/ui.package.react";

export default function AuthLayout() {
  return (
    <div className="min-h-screen flex flex-col bg-background text-foreground">
      {/* Fejléc (logo + felirat) */}
      <header className="w-full py-6 flex items-center justify-center">
        <div className="flex items-center gap-3">
          <DocratisLogo className="h-20" />
          <Label className="text-7xl">Docratis</Label>
        </div>
      </header>

      {/* Fő tartalom – vertikálisan középre igazítva, szépen középre húzva */}
      <main className="flex-1 flex items-center">
        <div className="w-full max-w-md mx-auto p-6">
          <Outlet />
        </div>
      </main>

      {/* Lábléc mindig legalul */}
      <footer className="fixed bottom-2 inset-x-0 text-center text-xs opacity-70 pointer-events-none">
        Minden jog fenntartva 2025
      </footer>
    </div>
  );
}
