import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
import { useId, useState, forwardRef } from "react";
export const PasswordControl = forwardRef(function PasswordControl({ id, name = "password", label = "Jelszó", value, onChange, placeholder = "••••••", required, autoComplete = "current-password", disabled, error, helperText, className, inputProps, }, ref) {
    const autoId = useId();
    const inputId = id !== null && id !== void 0 ? id : `pwd-${autoId}`;
    const [visible, setVisible] = useState(false);
    const hintId = error || helperText ? `${inputId}-hint` : undefined;
    return (_jsxs("div", { className: className, style: { position: "relative" }, children: [_jsx("label", { htmlFor: inputId, style: { display: "block", marginBottom: ".25rem" }, children: label }), _jsx("input", Object.assign({}, inputProps, { ref: ref, id: inputId, name: name, type: visible ? "text" : "password", value: value, onChange: onChange, placeholder: placeholder, required: required, autoComplete: autoComplete, disabled: disabled, "aria-invalid": !!error, "aria-describedby": hintId, style: Object.assign({ paddingRight: "2.25rem" }, ((inputProps === null || inputProps === void 0 ? void 0 : inputProps.style) || {})) })), _jsx("button", { type: "button", onClick: () => setVisible((v) => !v), "aria-label": visible ? "Jelszó elrejtése" : "Jelszó megjelenítése", "aria-pressed": visible, title: visible ? "Elrejtés" : "Megjelenítés", style: {
                    position: "absolute",
                    right: ".5rem",
                    top: "50%",
                    transform: "translateY(-50%)",
                    background: "none",
                    border: "none",
                    padding: 0,
                    width: "1.75rem",
                    height: "1.75rem",
                    cursor: "pointer",
                    lineHeight: 1,
                    fontSize: "1.1rem",
                    opacity: disabled ? 0.5 : 1
                }, children: visible ? "🙈" : "👁️" }), (error || helperText) && (_jsx("small", { id: hintId, style: {
                    display: "block",
                    marginTop: ".25rem",
                    color: error ? "var(--red, #b00020)" : "inherit"
                }, children: error !== null && error !== void 0 ? error : helperText }))] }));
});
//# sourceMappingURL=PasswordControl.js.map