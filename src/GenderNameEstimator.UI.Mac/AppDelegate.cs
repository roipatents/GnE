namespace GenderNameEstimator.UI.Mac;

[Register(nameof(AppDelegate))]
public class AppDelegate : NSApplicationDelegate
{
    private NSWindow? _mainWindow;

    public override void DidFinishLaunching(NSNotification notification)
    {
        var application = NSApplication.SharedApplication;
        application.EnumerateWindows(NSWindowListOptions.OrderedFrontToBack, SetMainWindow);

        // A package or device-management launch may not activate the application.
        // Explicitly present the storyboard window so launch cannot appear to fail
        // while GnE continues running behind the foreground application.
        if (OperatingSystem.IsMacOSVersionAtLeast(14))
        {
            application.Activate();
        }
        else
        {
            application.ActivateIgnoringOtherApps(true);
        }
        _mainWindow?.MakeKeyAndOrderFront(this);
    }

    private void SetMainWindow(NSWindow window, ref bool stop)
    {
        if (window.ContentViewController is MainViewController)
        {
            _mainWindow = window;
            stop = true;
        }
    }

    public override void WillTerminate(NSNotification notification)
    {
        // Insert code here to tear down your application
        _mainWindow?.ContentViewController?.Dispose();
    }

    public override bool ApplicationShouldTerminateAfterLastWindowClosed(NSApplication sender)
    {
        return true;
    }

    public override bool OpenFile(NSApplication sender, string filename)
    {
        if (_mainWindow?.ContentViewController is MainViewController mvc && filename is not null && mvc.Model is not null)
        {
            mvc.Model.SourceFileChooserModel.File = new FileItem(filename);
        }
        return true;
    }
}
