namespace GenderNameEstimator.UI.Mac;

[Register("RunProcessViewController")]
partial class RunProcessViewController
{
    [Outlet]
    AppKit.NSProgressIndicator MainProgressIndicator { get; set; }

    [Outlet]
    AppKit.NSButton RunCancelButton { get; set; }

    [Outlet]
    AppKit.NSProgressIndicator SubProgressIndicator { get; set; }

    [Action("RunCancelClicked:")]
    partial void RunCancelClicked(Foundation.NSObject sender);

    void ReleaseDesignerOutlets()
    {
        if (MainProgressIndicator != null)
        {
            MainProgressIndicator.Dispose();
            MainProgressIndicator = null;
        }

        if (SubProgressIndicator != null)
        {
            SubProgressIndicator.Dispose();
            SubProgressIndicator = null;
        }

        if (RunCancelButton != null)
        {
            RunCancelButton.Dispose();
            RunCancelButton = null;
        }
    }
}
