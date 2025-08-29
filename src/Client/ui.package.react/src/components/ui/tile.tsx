import { Card, CardDescription, CardHeader, CardTitle } from "@docratis/ui.package.react"
import { ChevronRight } from "lucide-react"
import * as React from "react"
import { Link } from "react-router-dom"

type Props = {
    to: string
    icon: React.ReactNode
    title: string
    description: string
}

export function Tile({ to, icon, title, description }: Props) {
    return (
        <Link
            to={to}
            className="group block focus:outline-none focus-visible:ring-2 focus-visible:ring-ring/60 rounded-xl"
        >
            <Card className="relative overflow-hidden transition hover:shadow-md hover:-translate-y-0.5">
                {/* finom sz�n�tmenet hoverre */}
                <div className="pointer-events-none absolute inset-0 bg-gradient-to-r from-primary/0 to-primary/0 group-hover:to-primary/5 transition-colors" />
                <CardHeader className="flex items-center gap-4 py-4">
                    {/* ikon �badge� */}
                    <div className="grid place-items-center size-12 rounded-md bg-primary/10 text-primary group-hover:bg-primary/15 transition">
                        {/* ikon m�retez�s */}
                        <div className="size-6">{icon}</div>
                    </div>

                    {/* sz�veg */}
                    <div className="flex-1 text-left">
                        <CardTitle className="text-base leading-tight">{title}</CardTitle>
                        <CardDescription className="text-sm leading-snug">
                            {description}
                        </CardDescription>
                    </div>

                    {/* chevron jobb oldalt */}
                    <ChevronRight className="size-5 text-muted-foreground opacity-0 group-hover:opacity-100 transition" />
                </CardHeader>
            </Card>
        </Link>
    )
}
