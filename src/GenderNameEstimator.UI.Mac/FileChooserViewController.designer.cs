namespace GenderNameEstimator.UI.Mac;

[Register("FileChooserViewController")]
partial class FileChooserViewController
{
    [Outlet]
    AppKit.NSTableView FileList { get; set; }

    [Outlet]
    AppKit.NSButton OpenFileButton { get; set; }

    [Action("OnButtonClicked:")]
    partial void OnButtonClicked(NSObject sender);

    void ReleaseDesignerOutlets()
    {
        if (FileList != null)
        {
            FileList.Dispose();
            FileList = null;
        }

        if (OpenFileButton != null)
        {
            OpenFileButton.Dispose();
            OpenFileButton = null;
        }
    }
}
