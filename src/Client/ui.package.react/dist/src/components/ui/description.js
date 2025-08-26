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
import { jsx as _jsx } from "react/jsx-runtime";
import { cn } from "../../lib/utils";
export function Description(_a) {
    var { children, align = "left", className } = _a, rest = __rest(_a, ["children", "align", "className"]);
    const alignClass = align === "center" ? "text-center" : align === "right" ? "text-right" : "text-left";
    return (_jsx("p", Object.assign({ className: cn("mt-1 text-xs text-muted-foreground", alignClass, className) }, rest, { children: children })));
}
//# sourceMappingURL=description.js.map