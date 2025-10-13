import { jsx as _jsx } from "react/jsx-runtime";
import "../../lib/TreeGrid.TypeScript.API";
import "../../lib/TreeGrid.TypeScript.React";
function TreeGrid({ config }) {
    return (_jsx("div", { className: "h-96", children: _jsx("bdo", { is: 'treegrid', debug: 'check', layout_url: "Layouts/StaticDef.js", data_url: "Layouts/StaticData.js" }) }));
}
export { TreeGrid, };
//# sourceMappingURL=TreeGrid.js.map