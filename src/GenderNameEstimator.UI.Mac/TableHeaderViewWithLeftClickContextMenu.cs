namespace GenderNameEstimator.UI.Mac;

[Register(nameof(TableHeaderViewWithLeftClickContextMenu))]
public class TableHeaderViewWithLeftClickContextMenu : NSTableHeaderView
{
    public TableHeaderViewWithLeftClickContextMenu(ObjCRuntime.NativeHandle handle) : base(handle)
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

    public override NSMenu MenuForEvent(NSEvent theEvent)
    {
        // TODO: We exclude the pop-up for the right-click in the header since we can't seem to find a reliable way to get the associated column
#pragma warning disable CS8603 // Possible null reference return.
        return theEvent.Type == NSEventType.RightMouseDown ? null : base.MenuForEvent(theEvent);
#pragma warning restore CS8603 // Possible null reference return.
    }
}
