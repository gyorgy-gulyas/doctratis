import React, { useId, useState, forwardRef } from "react";

export type PasswordControlProps = {
  id?: string;
  name?: string;
  label?: string;
  value: string;
  onChange: (e: React.ChangeEvent<HTMLInputElement>) => void;
  placeholder?: string;
  required?: boolean;
  autoComplete?: "current-password" | "new-password" | "one-time-code";
  disabled?: boolean;
  error?: string | null;
  helperText?: string;
  className?: string;
  inputProps?: React.InputHTMLAttributes<HTMLInputElement>;
};

export const PasswordControl = forwardRef<HTMLInputElement, PasswordControlProps>(
  function PasswordControl(
    {
      id,
      name = "password",
      label = "Jelszó",
      value,
      onChange,
      placeholder = "••••••",
      required,
      autoComplete = "current-password",
      disabled,
      error,
      helperText,
      className,
      inputProps,
    },
    ref
  ) {
    const autoId = useId();
    const inputId = id ?? `pwd-${autoId}`;
    const [visible, setVisible] = useState(false);
    const hintId = error || helperText ? `${inputId}-hint` : undefined;

    return (
      <div className={className} style={{ position: "relative" }}>
        <label htmlFor={inputId} style={{ display: "block", marginBottom: ".25rem" }}>
          {label}
        </label>

        <input
          {...inputProps}
          ref={ref}
          id={inputId}
          name={name}
          type={visible ? "text" : "password"}
          value={value}
          onChange={onChange}
          placeholder={placeholder}
          required={required}
          autoComplete={autoComplete}
          disabled={disabled}
          aria-invalid={!!error}
          aria-describedby={hintId}
          style={{ paddingRight: "2.25rem", ...(inputProps?.style || {}) }}
        />

        <button
          type="button"
          onClick={() => setVisible((v) => !v)}
          aria-label={visible ? "Jelszó elrejtése" : "Jelszó megjelenítése"}
          aria-pressed={visible}
          title={visible ? "Elrejtés" : "Megjelenítés"}
          style={{
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
          }}
        >
          {visible ? "🙈" : "👁️"}
        </button>

        {(error || helperText) && (
          <small
            id={hintId}
            style={{
              display: "block",
              marginTop: ".25rem",
              color: error ? "var(--red, #b00020)" : "inherit"
            }}
          >
            {error ?? helperText}
          </small>
        )}
      </div>
    );
  }
);
