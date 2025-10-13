{
Cfg : {
   CfgId:"Static",       //  Grid identification for saving configuration to cookies
   PrintLoad:"1", PrintCols:"2", PrintLocation:"3", PrintPageBreaks:"1", PrintRows:"50", // Printing options, download all rows for printing
   Paging:'2', ChildPaging:'2', // Both paging set to server
   ChildPageLength:"20", // Server paging for child pages, splits children to given number of rows and loads them separately when they become visible due scroll
   SaveSession:'1',      // Stores IO Session to cookies to identify the client on server and access appropriate grid instance
   Prepared:'1',         // The StaticData.js are prepared, so you can set this attribute to speed up loading
   ShowDeleted:'0',      // This example hides deleted row instead of coloring them red
   MaxHeight:'1',        // Grid maximizes height of the main tag on page
   MinTagHeight:'350',   // Minimal height of the main tag
   Sort:'P,M',           // To sort grid according to partner and Month for first time (when no configuration saved)
   Group:'P',            // To group grid by partner for first time (when no configuration saved)
   GroupRestoreSort:'1', // Restores sorting state after grouping that was before grouping
   GroupSortMain:'1',    // When grouping always sorts according to main column ascending
   MaxGroupLength:'0',   // Suppresses dividing rows to more groups when grouping because it is controlled by MaxChildren of all rows
   Adding:'0' ,          // Suppress adding new rows when grid is not grouped
   FilterEmpty:'1',      // When filtering, hides group rows that have all children hidden, see the Group row have set CanFilter:'2'
   IndexEnum:'1',        // All enums are set by index
   NameCol:'*P* (*M*)',  // Row will be identifies by Partner name in messages (e.g. in deleting rows)
   ChangeWinScrollType:'1', LimitScroll:"5",   // Responsive design, for small windows sets NoHScroll
   ScrollParentHorz:'Scroll', // Tag id that scrolls grid when set NoHScroll 
   },
Panel: { Copy: '1' }, // Shows the add / copy button on left row panel
Actions: { 
   OnUngroup:'Grid.Adding=0;',  // Suppress adding new rows when grid is not grouped
   OnGroup:'Grid.Adding=Grid.Group.search(/P$/)>=0?1:0;', // Permits adding rows only if the last group is by Partner
   OnRightClickCell:'Grid.Component.showCustomMenu(Row,Col)' // Custom event handler, shows the calling method of the framework component; Shows some custom popup menu on right click to any cell
   }, 
Lang: { 
   Alert: { ErrAdd:'Cannot add new partner here!' } // Changes text of adding error message
   }, 
Root: { AcceptDef:'' }, // By default (when no grouping is active) rows cannot be added or moved

Def: {
   // Base row settings 
   // CDef='' AcceptDef='' mean that no rows can be added or moved to children of the row
   // CopyTo are used only in sharing to copy incoming changes from data rows to Partner group row
   R: { 
      CDef:'', AcceptDef:'', CanEdit:'0', MCanEdit:'1', OCanEdit:'1', ICanEdit:'1', ECanEdit:'1', CalcOrder:'G,D,F',
      R:'0', C:'0', S:'', A:'0',
      PCopyTo:'Parent,M,Parent,P', RCopyTo:'Parent,R', CCopyTo:'Parent,C', SCopyTo:'Parent,S', XCopyTo:'Parent,X', NCopyTo:'Parent,N', ACopyTo:'Parent,A', BCopyTo:'Parent,B'
      },

   // Base group setting - for group row created when grouping
   // It also inherits attributes from group row defined in Defaults.xml 
   // CanFilter='2' means that this row will be hidden when it does not have any visible children when filtering, see Cfg FilterEmpty 
   // Action suppresses adding new rows when grid is grouped by another column than Partner
   Group: { 
      CanFilter:'2', Calculated:'1', CalcOrder:'O,I,E,G,D,F,B', OCanEdit:'0', ICanEdit:'0', ECanEdit:'0', NoUpload:'0',
      OFormula:'sum()', IFormula:'sum()', EFormula:'sum()', GFormula:'sum()', DFormula:'sum()', FFormula:'sum()',
      PCopyTo:'', MCopyTo:'', RCopyTo:'', CCopyTo:'', SCopyTo:'', XCopyTo:'', NCopyTo:'', ACopyTo:'', BCopyTo:''
      },

   // Group row created when grouping by partner (set by GroupCol attribute) 
   // GroupCols tells that this default row is used only when grid is grouped by Region, Country, State, Partner in that order
   // GroupMain tells to show grouped tree in Month column
   // This default is specific to this example and is used to edit all cells specific to Partner.
   // EditCols controls editing and bubbling changes to children.
   // This is the only row in this example that can be deleted, moved or added.
   // The children of this default are automatically created when this row added as new row (12 months).
   // ParentAcceptDef sets AcceptDef attribute of the parent row to let adding and moving this row
   GroupPartner : { 
      Def:'Group', GroupCol:'P', GroupCols:'|*P', GroupMain:'M', GroupMainCaption:'Partner / Month',
      EditCols:'Main,R,C,S,X,N,A,B', CanDelete:'1', CanDrag:'1', CanCopy:'1', AddDefChildren:'0',
      OnGroup:'Grid.Adding=1;',
      P:'New partner', M:'New partner', MType:'Text', ParentCDef:'GroupPartner', ParentAcceptDef:'GroupPartner', CDef:'R', AcceptDef:'R', 
      Items: [
         { M:'0' },
         { M:'1' },
         { M:'2' },
         { M:'3' },
         { M:'4' },
         { M:'5' },
         { M:'6' },
         { M:'7' },
         { M:'8' },
         { M:'9' },
         { M:'10' },
         { M:'11' }
         ]
      },

   // Group row created when grouping by Region or Country or State 
   // GroupCols tells that this default row is used only when grid is grouped by Region, Country, State, Partner in that order
   // EditCols and .CopyTo attributes controls editing and bubbling changes to children.
   GroupLocPartner: {
      Def:'Group', GroupCol:'R,C,S', GroupCols:'|P|R,P|C,P|S,P|R,C,P|R,S,P|C,S,P|R,C,S,P|', GroupMain:'M', GroupMainCaption:'Location / Partner / Month',
      EditCols:'Main', RCopyTo:'Children,R', CCopyTo:'Children,C', SCopyTo:'Children,S',
      XVisible:'0', NVisible:'0', AVisible:'0', BVisible:'0'
      },

   // Group row created when grouping by Region or Country or State
   // GroupCols tells that this default row is used only when grid is grouped by Region, Country, State (without Partner!) in that order 
   // GroupMain tells to show grouped tree in Partner column
   // This default inherits attributes from GroupLocPartner and just changes some.
   GroupLoc: {
      Def:'GroupLocPartner', GroupCol:'R,C,S', GroupCols:'|R|C|S|R,C|R,S|C,S|R,C,S', GroupMain:'P', GroupMainCaption:'Location / Partner',
      MVisible:'0'
      },

   // Group row created for all other grouping ending by Partner column
   // This default inherits attributes from GroupOther and just changes some
   GroupOtherPartner: { Def:'GroupOther', GroupCols:'|*P', GroupMain:'M' },

   // Group row created for all other conditions than fulfilled by previous group rows.
   // It does not provide editing capabilities
   // It is usual grouping row defined in applications
   GroupOther: {
      Def:'Group', GroupMain:'P',
      MVisible:'0', MCanEdit:'0', RVisible:'0', CVisible:'0', SVisible:'0',
      XVisible:'0', NVisible:'0', AVisible:'0', BVisible:'0'
      }
   },

LeftCols: [

   // Partner, main column for other groupings, width 130px, when grouped 200px 
   // Shows value as tooltip
   // Is in one group with Month because of spanned fixed row - cannot be moved outside the group
   { Name:'P', Width:'130', GroupWidth:'210', Type:'Text', ToolTip:'1', Group:'1', NumberSort:'2' },

   // Month, main column for grouping by partner, width 80px, when grouped 200px
   { Name:'M', Width:'85', GroupWidth:'210', Type:'Enum', Group:'1',  NumberSort:'2', Enum:'|01/2004|02/2004|03/2004|04/2004|05/2004|06/2004|07/2004|08/2004|09/2004|10/2004|11/2004|12/2004' }
        
   ],

Cols: [

   // Region
   { Name:'R', Width:'180', Type:'Enum', Refresh:'C,S', Group:'1', NumberSort:'1',
     Enum:'|Central & South Asia|East Asia & the Pacific|East Europe|Middle East & North Africa|North & Central America|South America|Sub-Saharan Africa|West Europe'
     },

   // Country 
   //  The 'C' column is related to 'R' column, it contains only countries from selected region
   { Name:'C', Width:'135', Type:'Enum', Related:'R', Refresh:'S', Group:'1', IntFormat:'(unknown)', NumberSort:'1',
     Enum0:"|ARMENIA|AZERBAIJAN|BANGLADESH|INDIA|KAZAKSTAN|PAKISTAN|SRI LANKA",
     Enum1:"|AUSTRALIA|BRUNEI|CHINA|HONG KONG|INDONESIA|JAPAN|KOREA, DPR|MALAYSIA|MONGOLIA|MYANMAR|NEW ZEALAND|PAPUA NEW GUINEA|PHILIPPINES|SINGAPORE|SOUTH KOREA|TAIWAN|THAILAND|VIETNAM",
     Enum2:"|ALBANIA|BELARUS|BULGARIA|CROATIA|CZECH REPUBLIC|ESTONIA|HUNGARY|LATVIA|LITHUANIA|MOLDOVA|POLAND|ROMANIA|RUSSIA|SERBIA-MONTENEGRO|SLOVAK REPUBLIC|SLOVENIA|UKRAINE",
     Enum3:"|ALGERIA|BAHRAIN|EGYPT|IRAN|IRAQ|ISRAEL|JORDAN|KUWAIT|LEBANON|LIBYA|MOROCCO|OMAN|QATAR|SAUDI ARABIA|SYRIA|TUNISIA|UAE|YEMEN",
     Enum4:"|BAHAMAS|CANADA|COSTA RICA|CUBA|DOMINICAN REPUBLIC|EL SALVADOR|GUATEMALA|HAITI|HONDURAS|JAMAICA|MEXICO|NICARAGUA|PANAMA|TRINIDAD & TOBAGO|UNITED STATES",
     Enum5:"|ARGENTINA|BOLIVIA|BRAZIL|CHILE|COLOMBIA|ECUADOR|GUYANA|PARAGUAY|PERU|SURINAME|URUGUAY|VENEZUELA",
     Enum6:"|ANGOLA|BOTSWANA|BURKINA FASO|CAMEROON|CONGO|CONGO DR|COTE D'IVOIRE|ETHIOPIA|GABON|GAMBIA|GHANA|GUINEA|GUINEA-BISSAU|KENYA|LIBERIA|MADAGASCAR|MALAWI|MALI|MOZAMBIQUE|NAMIBIA|NIGER|NIGERIA|SENEGAL|SIERRA LEONE|SOMALIA|SOUTH AFRICA|SUDAN|TANZANIA|TOGO|UGANDA|ZAMBIA|ZIMBABWE",
     Enum7:"|AUSTRIA|BELGIUM|CYPRUS|DENMARK|FINLAND|FRANCE|GERMANY|GREECE|ICELAND|IRELAND|ITALY|LUXEMBOURG|MALTA|NETHERLANDS|NORWAY|PORTUGAL|SPAIN|SWEDEN|SWITZERLAND|TURKEY|UNITED KINGDOM",
     },

   // State 
   // The 'S' column is related to 'C' column, it contains only states from selected country 
   // If the country is not divided to states, it is empty and read-only 
   // This column has set GroupType:16 - when grouping by State, it does not create groups for empty states
   { Name:'S', Width:'95', Type:'Enum', Related:'R,C', Group:'1', GroupEmpty:'0', IntFormat:'(unknown)', NumberSort:'1',
     Enum4_14:"|Alabama|Alaska|Arizona|Arkansas|California|Colorado|Connecticut|Delaware|Florida|Georgia|Hawaii|Idaho|Illinois|Indiana|Iowa|Kansas|Kentucky|Louisiana|Maine|Maryland|Massachusetts|Michigan|Minnesota|Mississippi|Missouri|Montana|Nebraska|Nevada|New Hampshire|New Jersey|New Mexico|New York|North Carolina|North Dakota|Ohio|Oklahoma|Oregon|Pennsylvania|Rhode Island|South Carolina|South Dakota|Tennessee|Texas|Utah|Vermont|Virginia|Washington|West Virginia|Wisconsin|Wyoming"
     },

   { Name:'X', Width:'95', Type:'Bool', Format:'||x' }, // Registered
   { Name:'N', Width:'105', Type:'Date', Format:'d' }, // Since
   { Name:'A', Width:'85', Type:'Enum', Enum:'|week|month|quarter|half year|year' }, // Calls per
   { Name:'B', Width:'70', Type:'Int' }, // Rabat

   { Name:'O', Width:'75', Type:'Int' }, // Orders
   { Name:'I', Width:'105', Type:'Float', Format:',0.00' }, // Income
   { Name:'E', Width:'105', Type:'Float', Format:',0.00' }, // Expenses
   { Name:'G', Width:'105', Type:'Float', Format:',0.00', Formula:'I-E' }, // Gross profit
   { Name:'D', Width:'95', Type:'Float', Format:',0.00', Formula:'G>0?B*G/100:0' }, // Discount
   ],

 RightCols: [
   { Name:'F', Width:'105', Type:'Float', Format:',0.00', Formula:'G-D' } // Profit
   ],

// Column captions
Header: {
   R:'Region', C:'Country', S:'State', P:'Partner', M:'Month',
   X:'Registered', N:'Since', A:'Calls per', O:'Orders',
   I:'Income', E:'Expenses', G:'Gross profit', B:'Rabat', D:'Discount',
   F:'Profit' 
   },
Head : [
   // Filter row - to let user choose filter, changes filtering of area enums by selection only
   { Kind:'Filter', CanEdit:'1', PCaseSensitive:'0', A:'',
      RFilterOff:'(all)', RCanEmpty:'1', RShowMenu:'0', R:'', 
      CFilterOff:'(all)', CCanEmpty:'1', CShowMenu:'0', C:'',
      SFilterOff:'(all)', SCanEmpty:'1', SShowMenu:'0', S:''
      }
   ],
Foot: [
   // Bottom row with the summary results
   { id:'$Results', CanDelete:'0', CanEdit:'0', Calculated:'1', 			
      Spanned:'1', P:'Total results', PSpan:'2', 
      RVisible:'0', CVisible:'0', SVisible:'0',
      XVisible:'0', AVisible:'0', 
      CalcOrder:'O,I,E,G,D,F,B',
      BType:'Float', BFormat:'0.00"%"', BFormula:'G?D/G*100:0',
      OFormula:'sum()', IFormula:'sum()', EFormula:'sum()', GFormula:'sum()', DFormula:'sum()', FFormula:'sum()',
      OCanEdit:'0', ICanEdit:'0', ECanEdit:'0'
      } 
   ],
Solid: [

   // Group row - to let user choose or build grouping
   { Kind:'Group', Cells:'Caption,List,Custom,Hide', Space:'0', MenuName:'Views',
      Caption:'Choose&nbsp;view:', CaptionWidth:'80', CaptionType:'Html', CaptionCanEdit:'0', CaptionCanFocus:'0',
      ListHtmlPrefix:'<b>', ListHtmlPostfix:'</b>', ListWidth:'120',
      List:'|None|Partner|Location,&nbsp;Partner|Location|Month',
      Cols:'||P|R,C,S,P|R,C,S|M',
      ListCustom:'Other',
      HideType:'Bool', HideCanEdit:'1', HideLabelRight:'Hide grouped columns', HideFormula:'Grid.GroupHideCols',
      HideOnChange:'Grid.GroupHideCols=Value;if(Grid.Group)Grid.ChangeColsVisibility(Value?[Grid.MainCol]:Grid.Group.split(","),Value?Grid.Group.split(","):null);',
      HideTip:'Uncheck it to show filter cells for columns the grid is grouped by'
      }, 

   // Bottom simple pagers
   { id:'Pager', Space:'4', Cells:'Pager,Pages', MenuName:'Bottom pager', CanPrint:'0',
      PagerType:'Pager', PagesLeft:'10', PagesType:'Pages', PagesRelWidth:'1', PagesCount:'10', PagesStep:'5'
      },


   // Split toolbars for small displays, shown by Media
   { id:'Pager2', Mirror:'Pager', Visible:'0', Space:'4', Cells:'Pages', MenuName:'Bottom pages', CanHide:'0' },
   { id:'Pager1', Mirror:'Toolbar', Visible:'0', Space:'4', Cells:'Win,Pager', MenuName:'Bottom pager', CanHide:'0', PagerType:'Pager' },
   { id:"Toolbar1", MenuName:"Toolbar 1", Kind:"Toolbar1", Mirror:"Toolbar", Visible:"0", CanHide:"0", Cells05:"", Cells65:"", Cells60Cfg:"", Cells70Styles:"", Cells80Right:"" },
   { id:"Toolbar2", MenuName:"Toolbar 2", Kind:"Toolbar2", Mirror:"Toolbar", Visible:"0", CanHide:"0", Cells05:"", Cells20Data:"", Cells30Manipulate:"", Cells70Styles:"", Cells80Right:"", F2000Left:"0" },
   { id:"Toolbar3", MenuName:"Toolbar 3", Kind:"Toolbar3", Mirror:"Toolbar", Visible:"0", CanHide:"0", Cells05:"", Cells65:"", Cells20Data:"", Cells30Manipulate:"", Cells60Cfg:"" }

   ],
   // Adds Win cell ro main toolbar 
Toolbar: { Cells05:'Win',
     WinType:"Bool", WinLabelRight:"Window scroll", WinCanEdit:"1", WinFormula:"Grid.NoHScroll?1:0", WinTip:"Disable grid scrollbars and use page scrollbars", WinCanPrint:"0",
     WinOnChange:"ChangeWinScroll" },

Pager: { Width:'160', MenuName:'Right pager' }, // Right side pager

Lang: { Alert: { ErrAdd:'Cannot add new partner here!' } }, // Changes text of adding error message
Media: [
   {  MaxWidth:'799', Tag:'0',
      Cfg: { Size: 'Small' },
      Rows: {
         Group: { PanelWidth:'20' },
         Pager: { Visible:'0', CanHide:'0' },
         Pager1: { Visible:'1', CanHide:'1' },
         Pager2: { Visible:'1', CanHide:'1' },
         Toolbar: { Visible:'0', CanHide:'0' },
         Toolbar1: { Visible:'1', CanHide:'1' },
         Toolbar2: { Visible:'1', CanHide:'1' },
         Toolbar3: { Visible:'1', CanHide:'1' }
         },
      Cols: { F: { Width:'92' } }
      }
   ]
}