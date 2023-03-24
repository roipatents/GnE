namespace GenderNameEstimator.UI.Mac;

[Register(nameof(WizardOutlineViewController))]
public abstract class WizardOutlineViewController : ViewControllerWithObservers
{
    public WizardOutlineViewController(ObjCRuntime.NativeHandle handle) : base(handle)
    {
    }

    [Export(nameof(IsCompleted))]
    abstract public bool IsCompleted { get; }

    [Export(nameof(IsEnabled))]
    virtual public bool IsEnabled { get => true; }

    public event EventHandler? Completed;

    public event EventHandler? Viewed;

    public static T Instantiate<T>() where T : WizardOutlineViewController
    {
#pragma warning disable CA1416 // Validate platform compatibility
        return (T)NSStoryboard.FromName("Main", null).InstantiateController(typeof(T).Name, null);
#pragma warning restore CA1416 // Validate platform compatibility
    }

    public override void ViewDidLoad()
    {
        base.ViewDidLoad();
    }

    public override void ViewWillAppear()
    {
        base.ViewWillAppear();
        Viewed?.Invoke(this, new EventArgs());
    }

    public override void DidChangeValue(string forKey)
    {
        base.DidChangeValue(forKey);
        switch (forKey)
        {
            case nameof(IsCompleted):
                if (IsCompleted)
                {
                    Completed?.Invoke(this, new EventArgs());
                }
                break;
        }
    }

    public virtual void OnLinkCommand(string command) { }
}
