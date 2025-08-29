var __rest = (this && this.__rest) || function (s, e) {
    var t = {};
    for (var p in s) if (Object.prototype.hasOwnProperty.call(s, p) && e.indexOf(p) < 0)
        t[p] = s[p];
    if (s != null && typeof Object.getOwnPropertySymbols === "function")
        for (var i = 0, p = Object.getOwnPropertySymbols(s); i < p.length; i++) {
            if (e.indexOf(p[i]) < 0 && Object.prototype.propertyIsEnumerable.call(s, p[i]))
                t[p[i]] = s[p[i]];
        }
    return t;
};
import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
import { Loader2 } from "lucide-react";
import { Button } from "./button";
import { cn } from "../../lib/utils";
export function LoadingButton(_a) {
    var { isLoading = false, loadingText, spinnerPosition = "end", disabled, children, className } = _a, props = __rest(_a, ["isLoading", "loadingText", "spinnerPosition", "disabled", "children", "className"]);
    const label = isLoading && loadingText ? loadingText : children;
    const spinner = (_jsx(Loader2, { className: "h-4 w-4 animate-spin", "aria-hidden": "true" }));
    return (_jsxs(Button, Object.assign({}, props, { disabled: isLoading || disabled, "aria-busy": isLoading, "aria-live": "polite", "aria-label": typeof label === "string" ? label : undefined, "data-loading": isLoading ? "" : undefined, className: cn(
        // biztos, ami biztos: középre igazítás + kis rés a tartalom között
        "justify-center gap-2", className), children: [isLoading && spinnerPosition !== "end" && spinner, spinnerPosition === "only"
                ? _jsx("span", { className: "sr-only", children: children })
                : label, isLoading && spinnerPosition === "end" && spinner] })));
}
//# sourceMappingURL=loading-button.js.map