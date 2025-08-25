"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.PasswordControl = void 0;
const jsx_runtime_1 = require("react/jsx-runtime");
const react_1 = require("react");
exports.PasswordControl = (0, react_1.forwardRef)(function PasswordControl({ id, name = "password", label = "Jelszó", value, onChange, placeholder = "••••••", required, autoComplete = "current-password", disabled, error, helperText, className, inputProps, }, ref) {
    const autoId = (0, react_1.useId)();
    const inputId = id !== null && id !== void 0 ? id : `pwd-${autoId}`;
    const [visible, setVisible] = (0, react_1.useState)(false);
    const hintId = error || helperText ? `${inputId}-hint` : undefined;
    return ((0, jsx_runtime_1.jsxs)("div", { className: className, style: { position: "relative" }, children: [(0, jsx_runtime_1.jsx)("label", { htmlFor: inputId, style: { display: "block", marginBottom: ".25rem" }, children: label }), (0, jsx_runtime_1.jsx)("input", Object.assign({}, inputProps, { ref: ref, id: inputId, name: name, type: visible ? "text" : "password", value: value, onChange: onChange, placeholder: placeholder, required: required, autoComplete: autoComplete, disabled: disabled, "aria-invalid": !!error, "aria-describedby": hintId, style: Object.assign({ paddingRight: "2.25rem" }, ((inputProps === null || inputProps === void 0 ? void 0 : inputProps.style) || {})) })), (0, jsx_runtime_1.jsx)("button", { type: "button", onClick: () => setVisible((v) => !v), "aria-label": visible ? "Jelszó elrejtése" : "Jelszó megjelenítése", "aria-pressed": visible, title: visible ? "Elrejtés" : "Megjelenítés", style: {
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
                }, children: visible ? "🙈" : "👁️" }), (error || helperText) && ((0, jsx_runtime_1.jsx)("small", { id: hintId, style: {
                    display: "block",
                    marginTop: ".25rem",
                    color: error ? "var(--red, #b00020)" : "inherit"
                }, children: error !== null && error !== void 0 ? error : helperText }))] }));
});
//# sourceMappingURL=PasswordControl.js.map