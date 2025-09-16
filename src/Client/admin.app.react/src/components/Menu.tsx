// components/Menu.tsx
import { MenuConfig, TwoRowMenu } from "@docratis/ui.package.react"
import { Settings, FileBarChart2, User } from "lucide-react"
import { useAuth } from "../auth/AuthContext"
import DocratisLogo from "./DocratisLogo"

export default function Menu() {
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

    return <TwoRowMenu menus={menus} />
}
