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
import { cn } from "../../lib/utils";
function Input(_a) {
    var { className, type, description, descriptionAlign = "left" } = _a, props = __rest(_a, ["className", "type", "description", "descriptionAlign"]);
    return (_jsxs("div", { className: "flex flex-col gap-1", children: [_jsx("input", Object.assign({ type: type, "data-slot": "input", className: cn("file:text-foreground placeholder:text-muted-foreground selection:bg-primary selection:text-primary-foreground dark:bg-input/30 border-input flex h-9 w-full min-w-0 rounded-md border bg-transparent px-3 py-1 text-base shadow-xs transition-[color,box-shadow] outline-none file:inline-flex file:h-7 file:border-0 file:bg-transparent file:text-sm file:font-medium disabled:pointer-events-none disabled:cursor-not-allowed disabled:opacity-50 md:text-sm", "focus-visible:border-ring focus-visible:ring-ring/50 focus-visible:ring-[3px]", "aria-invalid:ring-destructive/20 dark:aria-invalid:ring-destructive/40 aria-invalid:border-destructive", className) }, props)), description && (_jsx("p", { className: cn("text-xs text-muted-foreground", descriptionAlign === "center" && "text-center", descriptionAlign === "right" && "text-right"), children: description }))] }));
}
export { Input };
//# sourceMappingURL=input.js.map