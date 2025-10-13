import "../../lib/TreeGrid.TypeScript.API"
import "../../lib/TreeGrid.TypeScript.React"

interface TreeGridProps {
    config: any // TreeGrid JSON config
}

function TreeGrid({ config }: TreeGridProps) {
    return (<div className="h-96">
        <bdo
            is='treegrid'
            debug='check'
            layout_url="Layouts/StaticDef.js"
            data_url="Layouts/StaticData.js">
        </bdo>
    </div>
    )
}

export {
    TreeGrid,
}
