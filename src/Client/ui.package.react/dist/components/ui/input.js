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
import * as React from "react";
import { X } from "lucide-react";
import { cn } from "../../lib/utils";
function Input(_a) {
    var _b, _c;
    var { className, type, description, descriptionAlign = "left", clearable } = _a, props = __rest(_a, ["className", "type", "description", "descriptionAlign", "clearable"]);
    const inputRef = React.useRef(null);
    const [value, setValue] = React.useState((_c = (_b = props.value) === null || _b === void 0 ? void 0 : _b.toString()) !== null && _c !== void 0 ? _c : "");
    const handleClear = () => {
        var _a;
        setValue("");
        if (inputRef.current) {
            const nativeInputValueSetter = (_a = Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype, "value")) === null || _a === void 0 ? void 0 : _a.set;
            nativeInputValueSetter === null || nativeInputValueSetter === void 0 ? void 0 : nativeInputValueSetter.call(inputRef.current, "");
            inputRef.current.dispatchEvent(new Event("input", { bubbles: true }));
        }
    };
    return (_jsxs("div", { className: "flex flex-col gap-1", children: [_jsxs("div", { className: "relative", children: [_jsx("input", Object.assign({ ref: inputRef, type: type, "data-slot": "input", value: value, onChange: (e) => setValue(e.target.value), className: cn("file:text-foreground placeholder:text-muted-foreground selection:bg-primary selection:text-primary-foreground border-input flex h-9 w-full min-w-0 rounded-md border bg-transparent px-3 py-1 text-base shadow-xs transition-[color,box-shadow] outline-none file:inline-flex file:h-7 file:border-0 file:bg-transparent file:text-sm file:font-medium md:text-sm", "focus-visible:border-ring focus-visible:ring-ring/50 focus-visible:ring-[3px]", "aria-invalid:ring-destructive/20 dark:aria-invalid:ring-destructive/40 aria-invalid:border-destructive", props.disabled &&
                            "pointer-events-none cursor-not-allowed opacity-50 bg-muted", props.readOnly && !props.disabled && "bg-muted/50 text-muted-foreground", clearable && value && "pr-8", className) }, props)), clearable && value && !props.readOnly && !props.disabled && (_jsx("button", { type: "button", "aria-label": "Clear input", onClick: handleClear, className: "absolute right-2 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground", children: _jsx(X, { className: "h-4 w-4" }) }))] }), description && (_jsx("p", { className: cn("text-xs text-muted-foreground", descriptionAlign === "center" && "text-center", descriptionAlign === "right" && "text-right"), children: description }))] }));
}
export { Input };
//# sourceMappingURL=input.js.map