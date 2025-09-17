/* layouts\MainLayout.tsx */
import { MenuConfig, TwoRowMenu } from "@docratis/ui.package.react"
import { Outlet } from "react-router-dom"
import { Settings, FileBarChart2, User } from "lucide-react"
import { useAuth } from "../auth/AuthContext"
import DocratisLogo from "../components/DocratisLogo"

export default function MainLayout() {

    const { accountName } = useAuth()

    const menus: MenuConfig[] = [
        {
            label: "",
            key: "home",
            noUnderline: true,
            icon: <DocratisLogo className="h-5" />,
            submenus: [],
        },
        {
            label: "Admin",
            key: "admin",
            icon: Settings,
            submenus: [
                { label: "Users", path: "/admin/users" },
                { label: "Roles", path: "/admin/roles" },
                { label: "Permissions", path: "/admin/permissions" },
            ],
        },
        {
            label: "Reports",
            key: "reports",
            icon: FileBarChart2,
            submenus: [
                { label: "Daily", path: "/reports/daily" },
                { label: "Monthly", path: "/reports/monthly" },
            ],
        },
        {
            label: accountName || "User",
            key: "user",
            icon: User,
            rightAligned: true,
            submenus: [
                { label: "Profil", path: "/profile" },
                { label: "Logout", path: "/logout" },
            ],
        },
    ]

    return (
        <div className="flex flex-col min-h-screen">
            {/* Felsõ navigáció */}
            <TwoRowMenu menus={menus} />

            {/* Oldal tartalom */}
            <main className="flex-1 p-4">
                <Outlet />
            </main>
        </div>
    )
}
