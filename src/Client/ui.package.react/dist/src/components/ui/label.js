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
import * as LabelPrimitive from "@radix-ui/react-label";
import { cn } from "../../lib/utils";
import { Info } from "lucide-react";
import { Tooltip, TooltipTrigger, TooltipContent, TooltipProvider } from "./tooltip";
function Label(_a) {
    var { className, children, infoContent } = _a, props = __rest(_a, ["className", "children", "infoContent"]);
    return (_jsxs(LabelPrimitive.Root, Object.assign({ "data-slot": "label", className: cn("flex items-center gap-2 text-sm leading-none font-medium select-none", "group-data-[disabled=true]:pointer-events-none group-data-[disabled=true]:opacity-50", "peer-disabled:cursor-not-allowed peer-disabled:opacity-50", className) }, props, { children: [children, infoContent && (_jsx(TooltipProvider, { delayDuration: 150, skipDelayDuration: 300, children: _jsxs(Tooltip, { children: [_jsx(TooltipTrigger, { asChild: true, children: _jsx("span", { className: "group inline-flex items-center justify-center cursor-help", children: _jsx(Info, { className: cn("h-4 w-4 transition-colors", "text-muted-foreground/70", // vil�gosabb sz�rke alap
                                    "group-hover:text-sky-600", // hoverre k�k
                                    "focus-visible:text-sky-600"), "aria-hidden": "true" }) }) }), _jsx(TooltipContent, { children: typeof infoContent === "string" ? _jsx("p", { children: infoContent }) : infoContent })] }) }))] })));
}
export { Label };
//# sourceMappingURL=label.js.map