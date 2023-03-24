namespace GenderNameEstimator.UI.Mac;

[Register("MainViewController")]
partial class MainViewController
{
    [Outlet]
    AppKit.NSView DetailsView { get; set; }

    [Outlet]
    AppKit.NSButton NextButton { get; set; }

    [Outlet]
    AppKit.NSOutlineView OutlineView { get; set; }

    [Outlet]
    AppKit.NSTextView Prompt { get; set; }

    [Outlet]
    AppKit.NSTreeController TreeController { get; set; }

    [Action("Next:")]
    partial void Next(Foundation.NSObject sender);

    [Action("Previous:")]
    partial void Previous(Foundation.NSObject sender);

    void ReleaseDesignerOutlets()
    {
        if (DetailsView != null)
        {
            DetailsView.Dispose();
            DetailsView = null;
        }

        if (OutlineView != null)
        {
            OutlineView.Dispose();
            OutlineView = null;
        }

        if (Prompt != null)
        {
            Prompt.Dispose();
            Prompt = null;
        }

        if (TreeController != null)
        {
            TreeController.Dispose();
            TreeController = null;
        }

        if (NextButton != null)
        {
            NextButton.Dispose();
            NextButton = null;
        }
    }
}
