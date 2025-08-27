import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
import { Link } from "react-router-dom";
import { Card, CardHeader, CardTitle, CardDescription } from "@docratis/ui.package.react";
import { ChevronRight } from "lucide-react";
export function Tile({ to, icon, title, description }) {
    return (_jsx(Link, { to: to, className: "group block focus:outline-none focus-visible:ring-2 focus-visible:ring-ring/60 rounded-xl", children: _jsxs(Card, { className: "relative overflow-hidden transition hover:shadow-md hover:-translate-y-0.5", children: [_jsx("div", { className: "pointer-events-none absolute inset-0 bg-gradient-to-r from-primary/0 to-primary/0 group-hover:to-primary/5 transition-colors" }), _jsxs(CardHeader, { className: "flex items-center gap-4 py-4", children: [_jsx("div", { className: "grid place-items-center size-12 rounded-md bg-primary/10 text-primary group-hover:bg-primary/15 transition", children: _jsx("div", { className: "size-6", children: icon }) }), _jsxs("div", { className: "flex-1 text-left", children: [_jsx(CardTitle, { className: "text-base leading-tight", children: title }), _jsx(CardDescription, { className: "text-sm leading-snug", children: description })] }), _jsx(ChevronRight, { className: "size-5 text-muted-foreground opacity-0 group-hover:opacity-100 transition" })] })] }) }));
}
//# sourceMappingURL=tile.js.map