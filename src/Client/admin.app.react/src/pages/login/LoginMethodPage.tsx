import { Card, CardHeader, CardTitle, CardDescription } from "@docratis/ui.package.react"
import { Link } from "react-router-dom"
import { Mail, KeyRound, Server } from "lucide-react" // ikonok példának

export default function LoginMethodPage() {
  return (
    <div className="p-6 max-w-sm mx-auto space-y-6 w-96">
      <h1 className="text-xl font-bold">Válassz bejelentkezési módot</h1>

      <div className="flex flex-col gap-3">
        <Link to="/login/email">
          <Card className="cursor-pointer hover:bg-accent">
            <CardHeader className="flex flex-row items-center gap-2">
              {/* Ikon bal oldalt (20%) */}
              <div className="flex-none w-1/5 flex justify-center">
                <Mail className="h-10 w-10 text-primary" />
              </div>

              {/* Szövegek jobb oldalt (80%) */}
              <div className="flex-1">
                <CardTitle className="text-left">Email + Jelszó</CardTitle>
                <CardDescription className="text-left">
                  Email és jelszó megadásával szeretnék belépni.
                </CardDescription>
              </div>
            </CardHeader>
          </Card>
        </Link>

        <Link to="/login/kau">
          <Card className="cursor-pointer hover:bg-accent">
            <CardHeader className="flex flex-row items-center gap-2">
              <div className="flex-none w-1/5 flex justify-center">
                <KeyRound className="h-10 w-10 text-primary" />
              </div>
              <div className="flex-1">
                <CardTitle className="text-left">KAÜ</CardTitle>
                <CardDescription className="text-left">
                  Ügyfélkapuval vagy DÁP alkalmazással szeretnék belépni.
                </CardDescription>
              </div>
            </CardHeader>
          </Card>
        </Link>

        <Link to="/login/ad">
          <Card className="cursor-pointer hover:bg-accent">
            <CardHeader className="flex flex-row items-center gap-2">
              <div className="flex-none w-1/5 flex justify-center">
                <Server className="h-10 w-10 text-primary" />
              </div>
              <div className="flex-1">
                <CardTitle className="text-left">Active Directory</CardTitle>
                <CardDescription className="text-left">
                  Domain felhasználóval szeretnék belépni.
                </CardDescription>
              </div>
            </CardHeader>
          </Card>
        </Link>
      </div>
    </div>
  )
}
