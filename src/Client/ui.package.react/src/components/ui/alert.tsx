import * as React from "react"
import { cva, type VariantProps } from "class-variance-authority"
import { cn } from "../../lib/utils"
import { AlertCircleIcon } from "lucide-react";
/**
 * Alap alert stílusok:
 * - p-4
 * - h-3 w-3 ikon (bal felül)
 * - elsõ elem igazítása az ikonhoz: -translate-y-0.5
 * - minden további elem padding-left: pl-7
 */
const alertVariants = cva(
    "relative w-full rounded-lg p-4 " +
    "[&>svg]:absolute [&>svg]:left-4 [&>svg]:top-4 " +
    "[&>svg]:h-4 [&>svg]:w-4 [&>svg]:text-foreground " +
    "[&>svg+*]:-translate-y-0.5 " +
    "[&>svg~*]:pl-7",
    {
        variants: {
            variant: {
                default: "bg-background text-foreground",
                destructive:
                    "border-destructive/50 text-destructive dark:border-destructive [&>svg]:text-destructive",
            },
        },
        defaultVariants: {
            variant: "default",
        },
    }
)

type AlertBaseProps = React.HTMLAttributes<HTMLDivElement> &
    VariantProps<typeof alertVariants> & {
        /** Szegély kikapcsolása (alias: `noborder`) */
        noBorder?: boolean
        noborder?: boolean
    }

const Alert = React.forwardRef<HTMLDivElement, AlertBaseProps>(
    ({ className, variant, noBorder, noborder, ...props }, ref) => {
        const borderless = (noBorder ?? noborder) === true
        return (
            <div
                ref={ref}
                role="alert"
                className={cn(
                    alertVariants({ variant }),
                    borderless ? "border-0" : "border",
                    className
                )}
                {...props}
            />
        )
    }
)
Alert.displayName = "Alert"

const AlertTitle = React.forwardRef<
    HTMLHeadingElement,
    React.HTMLAttributes<HTMLHeadingElement>
>(({ className, ...props }, ref) => (
    <h5
        ref={ref}
        className={cn("mb-1 font-medium leading-none tracking-tight", className)}
        {...props}
    />
))
AlertTitle.displayName = "AlertTitle"

const AlertDescription = React.forwardRef<
    HTMLParagraphElement,
    React.HTMLAttributes<HTMLParagraphElement>
>(({ className, ...props }, ref) => (
    <div ref={ref} className={cn("text-sm [&_p]:leading-relaxed", className)} {...props} />
))
AlertDescription.displayName = "AlertDescription"


export { Alert, AlertTitle, AlertDescription, AlertCircleIcon }
