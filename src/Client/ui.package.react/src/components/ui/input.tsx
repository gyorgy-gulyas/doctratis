import * as React from "react"
import { cn } from "../../lib/utils"

type InputProps = React.ComponentProps<"input"> & {
    description?: string
    descriptionAlign?: "left" | "center" | "right"
    clearable?: boolean
    errorMessage?: string
    maxLength?: number
    rightIcon?: React.ReactNode
    rightIconAction?: () => void
}

export function Input({
    className,
    type,
    description,
    descriptionAlign = "left",
    clearable,
    errorMessage,
    maxLength,
    rightIcon,
    rightIconAction,
    value,
    onChange,
    ...props
}: InputProps) {
    const inputRef = React.useRef<HTMLInputElement>(null)

    const stringValue = value?.toString() ?? ""
    const lengthExceeded = maxLength ? stringValue.length > maxLength : false
    const showError = Boolean(errorMessage) || lengthExceeded

    const handleClear = () => {
        if (onChange && inputRef.current) {
            const nativeSetter = Object.getOwnPropertyDescriptor(
                window.HTMLInputElement.prototype,
                "value"
            )?.set
            nativeSetter?.call(inputRef.current, "")
            inputRef.current.dispatchEvent(new Event("input", { bubbles: true }))
        }
    }

    return (
        <div className="flex flex-col gap-1">
            <div className="relative">
                <input
                    ref={inputRef}
                    type={type}
                    data-slot="input"
                    value={value}
                    onChange={onChange}
                    maxLength={maxLength}
                    className={cn(
                        "file:text-foreground placeholder:text-muted-foreground selection:bg-primary selection:text-primary-foreground border-input flex h-9 w-full min-w-0 rounded-md border bg-transparent px-3 py-1 text-base shadow-xs transition-[color,box-shadow] outline-none file:inline-flex file:h-7 file:border-0 file:bg-transparent file:text-sm file:font-medium md:text-sm",
                        "focus-visible:border-ring focus-visible:ring-ring/50 focus-visible:ring-[3px]",
                        "aria-invalid:ring-destructive/20 dark:aria-invalid:ring-destructive/40 aria-invalid:border-destructive",
                        props.disabled &&
                        "pointer-events-none cursor-not-allowed opacity-50 bg-muted",
                        props.readOnly &&
                        !props.disabled &&
                        "bg-muted/50 text-muted-foreground",
                        (clearable || rightIcon) && "pr-16",
                        showError &&
                        "border-destructive focus-visible:border-destructive focus-visible:ring-destructive/30",
                        className
                    )}
                    {...props}
                />

                {(clearable || rightIcon) && (
                    <div className="absolute right-2 top-1/2 -translate-y-1/2 flex items-center gap-2">
                        {clearable && stringValue && !props.readOnly && !props.disabled && (
                            <button
                                type="button"
                                aria-label="Clear input"
                                onClick={handleClear}
                                className="text-muted-foreground hover:text-foreground"
                            >
                                ✕
                            </button>
                        )}
                        {rightIcon && rightIconAction && (
                            <button
                                type="button"
                                onClick={rightIconAction}
                                className="text-muted-foreground hover:text-foreground"
                            >
                                {rightIcon}
                            </button>
                        )}
                    </div>
                )}
            </div>

            {errorMessage && (
                <p className="text-xs text-destructive">{errorMessage}</p>
            )}

            {maxLength && (
                <p
                    className={cn(
                        "text-xs",
                        lengthExceeded ? "text-destructive" : "text-muted-foreground"
                    )}
                >
                    {stringValue.length} / {maxLength}
                </p>
            )}

            {description && (
                <p
                    className={cn(
                        "text-xs text-muted-foreground",
                        descriptionAlign === "center" && "text-center",
                        descriptionAlign === "right" && "text-right"
                    )}
                >
                    {description}
                </p>
            )}
        </div>
    )
}
