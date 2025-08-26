import * as React from "react"
import { cn } from "../../lib/utils"

export type DescriptionAlign = "left" | "center" | "right"

export interface DescriptionProps extends React.HTMLAttributes<HTMLParagraphElement> {
  align?: DescriptionAlign
}

export function Description({ children, align = "left", className, ...rest }: DescriptionProps) {
  const alignClass =
    align === "center" ? "text-center" : align === "right" ? "text-right" : "text-left"

  return (
    <p
      className={cn("mt-1 text-xs text-muted-foreground", alignClass, className)}
      {...rest}
    >
      {children}
    </p>
  )
}
