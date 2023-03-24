namespace GenderNameEstimator.UI.Mac;

[Register(nameof(TableViewWithLeftClickContextMenu))]
public class TableViewWithLeftClickContextMenu : NSTableView
{
    public TableViewWithLeftClickContextMenu(ObjCRuntime.NativeHandle handle) : base(handle)
    {
    }

    public override void MouseDown(NSEvent theEvent)
    {
        // TODO: allow for customized conditions under which to show or not show the menu
        // TODO: What about the standard context menu logic?  Maybe override WillOpenMenu or MenuForEvent?
        base.MouseDown(theEvent);
        if (theEvent.ButtonNumber == 0 && Menu is not null)
        {
            Menu.PopUpMenu(null, ConvertPointFromView(theEvent.LocationInWindow, null), this);
        }
    }
}
