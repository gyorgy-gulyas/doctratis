import * as React from "react"
import { Loader2 } from "lucide-react"
import type { VariantProps } from "class-variance-authority"
import { cn } from "../../lib/utils"
import { Button, buttonVariants } from "./button"
import type { DescriptionAlign } from "./description"

type LoadingButtonProps =
    React.ComponentProps<"button"> &
    VariantProps<typeof buttonVariants> & {
        isLoading?: boolean
        /** Ha megadod, ezt a szöveget mutatja töltés közben */
        loadingText?: string
        /** Spinner pozíciója a gombon belül */
        spinnerPosition?: "start" | "end" | "only"
        asChild?: boolean

        infoContent?: React.ReactNode
        description?: string
        descriptionAlign?: DescriptionAlign
    }

export function LoadingButton({
    isLoading = false,
    loadingText,
    spinnerPosition = "end",
    disabled,
    children,
    className,
    infoContent,
    description,
    descriptionAlign,
    ...props
}: LoadingButtonProps) {
    const label =
        isLoading && loadingText ? loadingText : children

    const spinner = (
        <Loader2 className="h-4 w-4 animate-spin" aria-hidden="true" />
    )

    return (
        <Button
            {...props}
            disabled={isLoading || disabled}
            aria-busy={isLoading}
            aria-live="polite"
            aria-label={ typeof label === "string" ? label : undefined }
            data-loading={isLoading ? "" : undefined}
            className={cn( "justify-center gap-2",className)}
            infoContent={infoContent}
            description={description}
            descriptionAlign={descriptionAlign}
        >
            {isLoading && spinnerPosition !== "end" && spinner}
            {spinnerPosition === "only"
                ? <span className="sr-only">{children}</span>
                : label}
            {isLoading && spinnerPosition === "end" && spinner}
        </Button>
    )
}
