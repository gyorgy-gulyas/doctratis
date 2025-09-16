import * as React from "react"
import { OTPInput, OTPInputContext } from "input-otp"
import { MinusIcon } from "lucide-react"
import { cn } from "../../lib/utils"
import { Alert, AlertTitle, AlertDescription, AlertCircleIcon } from "./alert"

type InputOTPProps = React.ComponentProps<typeof OTPInput> & {
    /** Wrapperre vonatkozó extra osztályok az OTPInput körül */
    containerClassName?: string

    /** Mezõ alatti leírás */
    description?: string
    descriptionAlign?: "left" | "center" | "right"

    /** Hibacím + részletek (node is lehet) */
    errorMessage?: string
    errorDescription?: React.ReactNode
}

function InputOTP({
    className,
    containerClassName,
    description,
    descriptionAlign = "left",
    errorMessage,
    errorDescription,
    ...props
}: InputOTPProps) {
    const showError = Boolean(errorMessage)

    return (
        <div className="flex flex-col gap-1">
            <OTPInput
                data-slot="input-otp"
                aria-invalid={showError || undefined}
                containerClassName={cn(
                    "flex items-center gap-2 has-disabled:opacity-50",
                    containerClassName
                )}
                className={cn("disabled:cursor-not-allowed", className)}
                {...props}
            />

            {errorMessage && (
                <Alert variant="destructive" noBorder>
                    <AlertCircleIcon />
                    <AlertTitle>{errorMessage}</AlertTitle>
                    {errorDescription && (
                        <AlertDescription>{errorDescription}</AlertDescription>
                    )}
                </Alert>
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

function InputOTPGroup({ className, ...props }: React.ComponentProps<"div">) {
    return (
        <div
            data-slot="input-otp-group"
            className={cn("flex items-center", className)}
            {...props}
        />
    )
}

function InputOTPSlot({
    index,
    className,
    ...props
}: React.ComponentProps<"div"> & {
    index: number
}) {
    const inputOTPContext = React.useContext(OTPInputContext)
    const { char, hasFakeCaret, isActive } = inputOTPContext?.slots[index] ?? {}

    return (
        <div
            data-slot="input-otp-slot"
            data-active={isActive}
            className={cn(
                "data-[active=true]:border-ring data-[active=true]:ring-ring/50 data-[active=true]:aria-invalid:ring-destructive/20 dark:data-[active=true]:aria-invalid:ring-destructive/40 aria-invalid:border-destructive data-[active=true]:aria-invalid:border-destructive dark:bg-input/30 border-input relative flex h-9 w-9 items-center justify-center border-y border-r text-sm shadow-xs transition-all outline-none first:rounded-l-md first:border-l last:rounded-r-md data-[active=true]:z-10 data-[active=true]:ring-[3px]",
                className
            )}
            {...props}
        >
            {char}
            {hasFakeCaret && (
                <div className="pointer-events-none absolute inset-0 flex items-center justify-center">
                    <div className="animate-caret-blink bg-foreground h-4 w-px duration-1000" />
                </div>
            )}
        </div>
    )
}

function InputOTPSeparator({ ...props }: React.ComponentProps<"div">) {
    return (
        <div data-slot="input-otp-separator" role="separator" {...props}>
            <MinusIcon />
        </div>
    )
}

export { InputOTP, InputOTPGroup, InputOTPSlot, InputOTPSeparator }
