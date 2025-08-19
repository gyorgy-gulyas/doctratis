import { Link } from "react-router-dom";

export default function LoginMethodPage() {
    return (
        <div className="p-6 max-w-sm mx-auto space-y-4">
            <h1 className="text-xl font-bold">Válassz bejelentkezési módot</h1>

            <div className="flex flex-col gap-3">
                <Link
                    to="/login/email"
                    className="px-4 py-2 rounded bg-blue-600 text-white text-center hover:bg-blue-700"
                >
                    Email + Jelszó
                </Link>

                <Link
                    to="/login/kau"
                    className="px-4 py-2 rounded bg-green-600 text-white text-center hover:bg-green-700"
                >
                    KAU
                </Link>

                <Link
                    to="/login/ad"
                    className="px-4 py-2 rounded bg-purple-600 text-white text-center hover:bg-purple-700"
                >
                    Active Directory
                </Link>
            </div>
        </div>
    );
}
