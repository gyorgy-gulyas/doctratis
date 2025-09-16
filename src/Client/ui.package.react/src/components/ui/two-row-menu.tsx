// components/two-row-menu.tsx
import { useState, isValidElement } from "react"
import { Link, useLocation } from "react-router-dom"
import { cn } from "../../lib/utils"

export interface MenuConfig {
    label: string
    key: string
    icon?: React.ElementType | React.ReactNode
    rightAligned?: boolean
    noUnderline?: boolean // if true, menu and submenus will not use underline border
    submenus: { label: string; path: string }[]
}

interface TwoRowMenuProps {
    menus: MenuConfig[]
}

export function TwoRowMenu({ menus }: TwoRowMenuProps) {
    const [activeMenu, setActiveMenu] = useState(menus[0]?.key)
    const location = useLocation()

    const leftMenus = menus.filter((m) => !m.rightAligned)
    const rightMenus = menus.filter((m) => m.rightAligned)

    const active = menus.find((m) => m.key === activeMenu)

    // Helper to render icons: supports both ReactNode and Component
    const renderIcon = (icon: MenuConfig["icon"]) => {
        if (!icon) return null
        if (isValidElement(icon)) {
            // Already a ReactNode (e.g. <DocratisLogo />), render directly
            return icon
        }
        const IconComp = icon as React.ElementType
        return <IconComp className="w-4 h-4" />
    }

    return (
        <div className="w-full">
            {/* Top row (black background with main menu items) */}
            <nav className="w-full bg-black text-white h-10 flex items-center px-4 justify-between">
                {/* Left-aligned menus */}
                <div className="flex gap-2">
                    {leftMenus.map((menu) => {
                        const isActive = activeMenu === menu.key
                        return (
                            <button
                                key={menu.key}
                                onClick={() => setActiveMenu(menu.key)}
                                className={cn(
                                    "flex items-center gap-1 px-3 py-1 text-sm font-medium normal-case transition-colors",
                                    menu.noUnderline
                                        ? isActive
                                            ? "text-sky-500"
                                            : "text-gray-200 hover:text-sky-300"
                                        : isActive
                                            ? "border-b-2 border-sky-500 text-sky-500"
                                            : "border-b-2 border-transparent text-gray-200 hover:text-sky-300 hover:border-sky-300"
                                )}
                            >
                                {renderIcon(menu.icon)}
                                {menu.label}
                            </button>
                        )
                    })}
                </div>

                {/* Right-aligned menus */}
                <div className="flex gap-2">
                    {rightMenus.map((menu) => {
                        const isActive = activeMenu === menu.key
                        return (
                            <button
                                key={menu.key}
                                onClick={() => setActiveMenu(menu.key)}
                                className={cn(
                                    "flex items-center gap-1 px-3 py-1 text-sm font-medium normal-case transition-colors",
                                    menu.noUnderline
                                        ? isActive
                                            ? "text-sky-500"
                                            : "text-gray-200 hover:text-sky-300"
                                        : isActive
                                            ? "border-b-2 border-sky-500 text-sky-500"
                                            : "border-b-2 border-transparent text-gray-200 hover:text-sky-300 hover:border-sky-300"
                                )}
                            >
                                {renderIcon(menu.icon)}
                                {menu.label}
                            </button>
                        )
                    })}
                </div>
            </nav>

            {/* Bottom row (submenu items) */}
            {active && (
                <nav
                    className={cn(
                        "w-full bg-gray-100 border-b h-10 flex items-center px-4 font-bold",
                        active.rightAligned ? "justify-end" : "justify-start"
                    )}
                >
                    {active.submenus.map((submenu) => {
                        const isActive = location.pathname === submenu.path
                        return (
                            <Link
                                key={submenu.path}
                                to={submenu.path}
                                className={cn(
                                    "px-3 py-1 text-sm transition-colors",
                                    active.noUnderline
                                        ? isActive
                                            ? "text-sky-500"
                                            : "text-gray-800 hover:text-sky-300"
                                        : isActive
                                            ? "text-sky-500 border-b-2 border-sky-500"
                                            : "text-gray-800 hover:text-sky-300 border-b-2 border-transparent"
                                )}
                            >
                                {submenu.label}
                            </Link>
                        )
                    })}
                </nav>
            )}
        </div>
    )
}
