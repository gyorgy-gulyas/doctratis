// src/pages/login/LoginMethodPage.tsx
import { Mail, KeyRound, Server } from "lucide-react"
import { Tile } from "@docratis/ui.package.react";

export default function LoginMethodPage() {
    return (
        <div className="p-6 mx-auto w-full max-w-xl space-y-6">
            <h1 className="text-xl font-bold">Válassz bejelentkezési módot</h1>

            <div className="flex flex-col gap-3">
                <Tile
                    to="/login/email"
                    icon={<Mail />}
                    title="Email + Jelszó"
                    description="Email cím és jelszó megadásával szeretnék belépni."
                />
                <Tile
                    to="/login/kau"
                    icon={<KeyRound />}
                    title="KAÜ"
                    description="Ügyfélkapuval vagy DÁP alkalmazással szeretnék belépni."
                />
                <Tile
                    to="/login/ad"
                    icon={<Server />}
                    title="Active Directory"
                    description="Domain felhasználóval szeretnék belépni."
                />
            </div>
        </div>
    )
}
