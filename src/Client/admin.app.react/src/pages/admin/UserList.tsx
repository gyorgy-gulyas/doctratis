import React from "react";
import { encode } from "html-entities";


interface TreeGridProps {
    config?: string;
}

export default class UserList extends React.Component<TreeGridProps> {
    Grid: TGrid;

    constructor(props: TreeGridProps) {
        super(props);

        // TreeGrid globális hookok
        window.Grids.OnInit = function (grid: TGrid) {
            grid!.Component = this;
            this.Grid = grid;
        };
        window.Grids.OnLoadError = function () {
            alert(
                "TreeGrid cannot load!\nCheck if the NodeJS server runs on http://localhost:8000"
            );
        };
    }

    componentDidMount() {
        if (window.StartTreeGrid) {
            window.StartTreeGrid();
        } else {
            console.error("StartTreeGrid is not available on window");
        }
    }

    render() {
        // <<< külön változókban a definíciók
        const layoutXml = `
          <Grid>
            <Cfg
                Editing='0'
                Deleting='0'
                Adding='0'
                Searching='1'
                SearchAction='Filter'
                Filtering='1'
                ConstHeight='100%'
                ConstWidth='100%'
                Style='White'/>
            <Toolbar
                Visible='1'
                Cells='Search,Export,Print,Columns'
                Tag='toolbar'
            />
            <MenuCfg
                ShowDeleted='0'
            />
            <Cols>
              <C Name='ID' Width='80' Type='Int' CanEdit='0'/>
              <C Name='Name' Width='200' Type='Text' CanEdit='0' CanFilter='1'/>
              <C Name='Status' Width='100' Type='Enum' Enum='Active,Inactive' CanEdit='1' CanFilter='1'/>
            </Cols>
          </Grid>
        `;

        const rowsXml = `
          <?xml version="1.0" encoding="UTF-8"?>
          <Grid>
            <Body>
              <B>
                <I id='1' ID='101' Name='Elsõ elem' Status='Active' CanDelete='0'/>
                <I id='2' ID='102' Name='Második elem' Status='Inactive' CanDelete='1'/>
              </B>
            </Body>
          </Grid>
        `;

        return (
            <div className="w-full h-full border border-gray-300">
                <div id="toolbar" />
                <div id="w-full h-10 bg-blue-100">
                    <bdo
                        id="SampleGrid"
                        style={{ width: "100%", height: "100%" }}
                        layout_data={layoutXml}
                        data_data={rowsXml}
                        debug="check"
                        exportpdf_type="Cfg,Def,Cols,All"
                    />
                </div>
            </div>
        );
    }
}
