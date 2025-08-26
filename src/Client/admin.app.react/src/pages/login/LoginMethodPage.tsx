import { Button } from "@docratis/ui.package.react"
import { Link } from "react-router-dom";

export default function LoginMethodPage() {
    return (
        <div className="p-6 max-w-sm mx-auto space-y-6 w-96">
            <h1 className="text-xl font-bold">Válassz bejelentkezési módot</h1>
            <div className="flex flex-col gap-3">
                <Button asChild>
                    <Link to="/login/email">Email + Jelszó</Link>
                </Button>
                <Button asChild >
                    <Link to="/login/kau">KAU</Link>
                </Button>
                <Button asChild >
                    <Link to="/login/ad">Active Directory</Link>
                </Button>
            </div>
        </div>
    );
}
