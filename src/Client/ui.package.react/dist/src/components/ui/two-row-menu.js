import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
// components/two-row-menu.tsx
import { useState, isValidElement } from "react";
import { Link, useLocation } from "react-router-dom";
import { cn } from "../../lib/utils";
export function TwoRowMenu({ menus }) {
    var _a;
    const [activeMenu, setActiveMenu] = useState((_a = menus[0]) === null || _a === void 0 ? void 0 : _a.key);
    const location = useLocation();
    const leftMenus = menus.filter((m) => !m.rightAligned);
    const rightMenus = menus.filter((m) => m.rightAligned);
    const active = menus.find((m) => m.key === activeMenu);
    // Helper to render icons: supports both ReactNode and Component
    const renderIcon = (icon) => {
        if (!icon)
            return null;
        if (isValidElement(icon)) {
            // Already a ReactNode (e.g. <DocratisLogo />), render directly
            return icon;
        }
        const IconComp = icon;
        return _jsx(IconComp, { className: "w-4 h-4" });
    };
    return (_jsxs("div", { className: "w-full", children: [_jsxs("nav", { className: "w-full bg-black text-white h-10 flex items-center px-4 justify-between", children: [_jsx("div", { className: "flex gap-2", children: leftMenus.map((menu) => {
                            const isActive = activeMenu === menu.key;
                            return (_jsxs("button", { onClick: () => setActiveMenu(menu.key), className: cn("flex items-center gap-1 px-3 py-1 text-sm font-medium normal-case transition-colors", menu.noUnderline
                                    ? isActive
                                        ? "text-sky-500"
                                        : "text-gray-200 hover:text-sky-300"
                                    : isActive
                                        ? "border-b-2 border-sky-500 text-sky-500"
                                        : "border-b-2 border-transparent text-gray-200 hover:text-sky-300 hover:border-sky-300"), children: [renderIcon(menu.icon), menu.label] }, menu.key));
                        }) }), _jsx("div", { className: "flex gap-2", children: rightMenus.map((menu) => {
                            const isActive = activeMenu === menu.key;
                            return (_jsxs("button", { onClick: () => setActiveMenu(menu.key), className: cn("flex items-center gap-1 px-3 py-1 text-sm font-medium normal-case transition-colors", menu.noUnderline
                                    ? isActive
                                        ? "text-sky-500"
                                        : "text-gray-200 hover:text-sky-300"
                                    : isActive
                                        ? "border-b-2 border-sky-500 text-sky-500"
                                        : "border-b-2 border-transparent text-gray-200 hover:text-sky-300 hover:border-sky-300"), children: [renderIcon(menu.icon), menu.label] }, menu.key));
                        }) })] }), active && (_jsx("nav", { className: cn("w-full bg-gray-100 border-b h-10 flex items-center px-4 font-bold", active.rightAligned ? "justify-end" : "justify-start"), children: active.submenus.map((submenu) => {
                    const isActive = location.pathname === submenu.path;
                    return (_jsx(Link, { to: submenu.path, className: cn("px-3 py-1 text-sm transition-colors", active.noUnderline
                            ? isActive
                                ? "text-sky-500"
                                : "text-gray-800 hover:text-sky-300"
                            : isActive
                                ? "text-sky-500 border-b-2 border-sky-500"
                                : "text-gray-800 hover:text-sky-300 border-b-2 border-transparent"), children: submenu.label }, submenu.path));
                }) }))] }));
}
//# sourceMappingURL=two-row-menu.js.map