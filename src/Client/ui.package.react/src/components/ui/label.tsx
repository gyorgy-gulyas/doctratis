import * as React from "react"
import * as LabelPrimitive from "@radix-ui/react-label"

import { cn } from "../../lib/utils"
import { Info } from "lucide-react"
import { Tooltip, TooltipTrigger, TooltipContent, TooltipProvider } from "./tooltip"

export interface LabelProps extends React.ComponentProps<typeof LabelPrimitive.Root> {
    infoContent?: React.ReactNode
}

function Label({ className, children, infoContent, ...props }: LabelProps) {
    return (
        <LabelPrimitive.Root
            data-slot="label"
            className={cn(
                "flex items-center gap-2 text-sm leading-none font-medium select-none",
                "group-data-[disabled=true]:pointer-events-none group-data-[disabled=true]:opacity-50",
                "peer-disabled:cursor-not-allowed peer-disabled:opacity-50",
                className
            )}
            {...props}
        >
            {children}

            {infoContent && (
                <TooltipProvider delayDuration={150} skipDelayDuration={300}>
                    <Tooltip>
                        <TooltipTrigger asChild>
                            <span className="group inline-flex items-center justify-center cursor-help">
                                <Info
                                    className={cn(
                                        "h-4 w-4 transition-colors",
                                        "text-muted-foreground/70", // világosabb szürke alap
                                        "group-hover:text-sky-600", // hoverre kék
                                        "focus-visible:text-sky-600"
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
        </LabelPrimitive.Root>
    )
}

export { Label }
