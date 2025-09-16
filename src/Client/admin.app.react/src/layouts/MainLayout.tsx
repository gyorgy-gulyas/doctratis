/* layouts\MainLayout.tsx */
import { Outlet } from "react-router-dom"
import Menu from "../components/Menu"

export default function MainLayout() {
    return (
        <div className="flex flex-col min-h-screen">
            {/* Felsõ navigáció */}
            <Menu />

            {/* Oldal tartalom */}
            <main className="flex-1 p-4">
                <Outlet />
            </main>
        </div>
    )
}
