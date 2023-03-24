// WARNING
//
// This file has been generated automatically by Visual Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace GenderNameEstimator.UI.Mac
{
    [Register("SpreadsheetColumnChooserViewController")]
    partial class SpreadsheetColumnChooserViewController
    {
        [Outlet]
        AppKit.NSMenu ContextMenu { get; set; }

        [Outlet]
        AppKit.NSTableColumn RowColumn { get; set; }

        [Outlet]
        AppKit.NSTableView TableView { get; set; }

        [Outlet]
        AppKit.NSComboBox WorksheetDropDown { get; set; }

        [Action("RefreshTableView:")]
        partial void RefreshTableView(Foundation.NSObject sender);

        [Action("TableViewSelected:")]
        partial void TableViewSelected(AppKit.NSTableView sender);

        [Action("ViewInFinder:")]
        partial void ViewInFinder(Foundation.NSObject sender);

        [Action("WorksheetChanged:")]
        partial void WorksheetChanged(Foundation.NSObject sender);

        void ReleaseDesignerOutlets()
        {
            if (RowColumn != null)
            {
                RowColumn.Dispose();
                RowColumn = null;
            }

            if (TableView != null)
            {
                TableView.Dispose();
                TableView = null;
            }

            if (WorksheetDropDown != null)
            {
                WorksheetDropDown.Dispose();
                WorksheetDropDown = null;
            }

            if (ContextMenu != null)
            {
                ContextMenu.Dispose();
                ContextMenu = null;
            }
        }
    }
}
