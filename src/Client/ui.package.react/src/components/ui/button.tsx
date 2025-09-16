import * as React from "react"
import { Slot } from "@radix-ui/react-slot"
import { cva, type VariantProps } from "class-variance-authority"
import { cn } from "../../lib/utils"
import { Description, type DescriptionAlign } from "./description"
import { Tooltip, TooltipTrigger, TooltipContent, TooltipProvider } from "./tooltip"
import { Info } from "lucide-react"

const buttonVariants = cva(
  "inline-flex items-center justify-center gap-2 whitespace-nowrap rounded-md text-sm font-medium transition-all disabled:pointer-events-none disabled:opacity-50 [&_svg]:pointer-events-none [&_svg:not([class*='size-'])]:size-4 shrink-0 [&_svg]:shrink-0 outline-none focus-visible:border-ring focus-visible:ring-ring/50 focus-visible:ring-[3px] aria-invalid:ring-destructive/20 dark:aria-invalid:ring-destructive/40 aria-invalid:border-destructive",
  {
    variants: {
      variant: {
        default: "bg-primary text-primary-foreground shadow-xs hover:bg-primary/90",
        destructive:
          "bg-destructive text-white shadow-xs hover:bg-destructive/90 focus-visible:ring-destructive/20 dark:focus-visible:ring-destructive/40 dark:bg-destructive/60",
        outline:
          "border bg-background shadow-xs hover:bg-accent hover:text-accent-foreground dark:bg-input/30 dark:border-input dark:hover:bg-input/50",
        secondary: "bg-secondary text-secondary-foreground shadow-xs hover:bg-secondary/80",
        ghost: "hover:bg-accent hover:text-accent-foreground dark:hover:bg-accent/50",
        link: "text-primary underline-offset-4 hover:underline",
      },
      size: {
        default: "h-9 px-4 py-2 has-[>svg]:px-3",
        sm: "h-8 rounded-md gap-1.5 px-3 has-[>svg]:px-2.5",
        lg: "h-10 rounded-md px-6 has-[>svg]:px-4",
        icon: "size-9",
      },
    },
    defaultVariants: {
      variant: "default",
      size: "default",
    },
  }
)

export interface ButtonProps
  extends React.ComponentProps<"button">,
    VariantProps<typeof buttonVariants> {
  asChild?: boolean
  description?: string
  descriptionAlign?: DescriptionAlign
  infoContent?: React.ReactNode
}

function Button({
  className,
  variant,
  size,
  asChild = false,
  description,
  descriptionAlign,
  infoContent,
  children,
  ...props
}: ButtonProps) {
  const Comp = asChild ? Slot : "button"

  return (
    <div className="flex flex-col gap-1 w-full">
      <Comp
        data-slot="button"
        className={cn(buttonVariants({ variant, size, className }), "relative group")}
        {...props}
      >
        {children}

        {infoContent && (
          <TooltipProvider delayDuration={150} skipDelayDuration={300}>
            <Tooltip>
              <TooltipTrigger asChild>
                {/* külön trigger; ne indítsa a gomb click-jét */}
                <span
                  className="absolute right-1 top-1 inline-flex items-center justify-center"
                  onClick={(e) => e.stopPropagation()}
                >
                  <Info
                    className={cn(
                      "h-3 w-3 transition-colors",
                      "text-muted-foreground",
                      "group-hover:text-primary"
                    )}
                    aria-hidden="true"
                  />
                </span>
              </TooltipTrigger>
              <TooltipContent>
                {typeof infoContent === "string" ? <p>{infoContent}</p> : infoContent}
              </TooltipContent>
            </Tooltip>
          </TooltipProvider>
        )}
      </Comp>

      {description && <Description align={descriptionAlign}>{description}</Description>}
    </div>
  )
}

export { Button, buttonVariants }
