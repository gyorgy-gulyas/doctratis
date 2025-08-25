import React from "react";
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
export declare const PasswordControl: React.ForwardRefExoticComponent<PasswordControlProps & React.RefAttributes<HTMLInputElement>>;
//# sourceMappingURL=PasswordControl.d.ts.map