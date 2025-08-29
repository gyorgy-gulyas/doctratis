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
import { cn } from "../../lib/utils";
export function Input(_a) {
    var _b;
    var { className, type, description, descriptionAlign = "left", clearable, errorMessage, maxLength, rightIcon, rightIconAction, value, onChange } = _a, props = __rest(_a, ["className", "type", "description", "descriptionAlign", "clearable", "errorMessage", "maxLength", "rightIcon", "rightIconAction", "value", "onChange"]);
    const inputRef = React.useRef(null);
    const stringValue = (_b = value === null || value === void 0 ? void 0 : value.toString()) !== null && _b !== void 0 ? _b : "";
    const lengthExceeded = maxLength ? stringValue.length > maxLength : false;
    const showError = Boolean(errorMessage) || lengthExceeded;
    const handleClear = () => {
        var _a;
        if (onChange && inputRef.current) {
            const nativeSetter = (_a = Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype, "value")) === null || _a === void 0 ? void 0 : _a.set;
            nativeSetter === null || nativeSetter === void 0 ? void 0 : nativeSetter.call(inputRef.current, "");
            inputRef.current.dispatchEvent(new Event("input", { bubbles: true }));
        }
    };
    return (_jsxs("div", { className: "flex flex-col gap-1", children: [_jsxs("div", { className: "relative", children: [_jsx("input", Object.assign({ ref: inputRef, type: type, "data-slot": "input", value: value, onChange: onChange, maxLength: maxLength, className: cn("file:text-foreground placeholder:text-muted-foreground selection:bg-primary selection:text-primary-foreground border-input flex h-9 w-full min-w-0 rounded-md border bg-transparent px-3 py-1 text-base shadow-xs transition-[color,box-shadow] outline-none file:inline-flex file:h-7 file:border-0 file:bg-transparent file:text-sm file:font-medium md:text-sm", "focus-visible:border-ring focus-visible:ring-ring/50 focus-visible:ring-[3px]", "aria-invalid:ring-destructive/20 dark:aria-invalid:ring-destructive/40 aria-invalid:border-destructive", props.disabled &&
                            "pointer-events-none cursor-not-allowed opacity-50 bg-muted", props.readOnly &&
                            !props.disabled &&
                            "bg-muted/50 text-muted-foreground", (clearable || rightIcon) && "pr-16", showError &&
                            "border-destructive focus-visible:border-destructive focus-visible:ring-destructive/30", className) }, props)), (clearable || rightIcon) && (_jsxs("div", { className: "absolute right-2 top-1/2 -translate-y-1/2 flex items-center gap-2", children: [clearable && stringValue && !props.readOnly && !props.disabled && (_jsx("button", { type: "button", "aria-label": "Clear input", onClick: handleClear, className: "text-muted-foreground hover:text-foreground", children: "\u2715" })), rightIcon && rightIconAction && (_jsx("button", { type: "button", onClick: rightIconAction, className: "text-muted-foreground hover:text-foreground", children: rightIcon }))] }))] }), errorMessage && (_jsx("p", { className: "text-xs text-destructive", children: errorMessage })), maxLength && (_jsxs("p", { className: cn("text-xs", lengthExceeded ? "text-destructive" : "text-muted-foreground"), children: [stringValue.length, " / ", maxLength] })), description && (_jsx("p", { className: cn("text-xs text-muted-foreground", descriptionAlign === "center" && "text-center", descriptionAlign === "right" && "text-right"), children: description }))] }));
}
//# sourceMappingURL=input.js.map