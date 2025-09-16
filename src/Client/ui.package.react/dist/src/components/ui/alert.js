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
import * as React from "react";
import { cva } from "class-variance-authority";
import { cn } from "../../lib/utils";
import { AlertCircleIcon } from "lucide-react";
/**
 * Alap alert st�lusok:
 * - p-4
 * - h-3 w-3 ikon (bal fel�l)
 * - els� elem igaz�t�sa az ikonhoz: -translate-y-0.5
 * - minden tov�bbi elem padding-left: pl-7
 */
const alertVariants = cva("relative w-full rounded-lg p-4 " +
    "[&>svg]:absolute [&>svg]:left-4 [&>svg]:top-4 " +
    "[&>svg]:h-4 [&>svg]:w-4 [&>svg]:text-foreground " +
    "[&>svg+*]:-translate-y-0.5 " +
    "[&>svg~*]:pl-7", {
    variants: {
        variant: {
            default: "bg-background text-foreground",
            destructive: "border-destructive/50 text-destructive dark:border-destructive [&>svg]:text-destructive",
        },
    },
    defaultVariants: {
        variant: "default",
    },
});
const Alert = React.forwardRef((_a, ref) => {
    var { className, variant, noBorder, noborder } = _a, props = __rest(_a, ["className", "variant", "noBorder", "noborder"]);
    const borderless = (noBorder !== null && noBorder !== void 0 ? noBorder : noborder) === true;
    return (_jsx("div", Object.assign({ ref: ref, role: "alert", className: cn(alertVariants({ variant }), borderless ? "border-0" : "border", className) }, props)));
});
Alert.displayName = "Alert";
const AlertTitle = React.forwardRef((_a, ref) => {
    var { className } = _a, props = __rest(_a, ["className"]);
    return (_jsx("h5", Object.assign({ ref: ref, className: cn("mb-1 font-medium leading-none tracking-tight", className) }, props)));
});
AlertTitle.displayName = "AlertTitle";
const AlertDescription = React.forwardRef((_a, ref) => {
    var { className } = _a, props = __rest(_a, ["className"]);
    return (_jsx("div", Object.assign({ ref: ref, className: cn("text-sm [&_p]:leading-relaxed", className) }, props)));
});
AlertDescription.displayName = "AlertDescription";
export { Alert, AlertTitle, AlertDescription, AlertCircleIcon };
//# sourceMappingURL=alert.js.map