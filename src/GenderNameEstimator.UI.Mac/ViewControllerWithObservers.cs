namespace GenderNameEstimator.UI.Mac;

public class ViewControllerWithObservers : NSViewController
{
    private readonly List<IDisposable> _observers = new();

    public ViewControllerWithObservers(ObjCRuntime.NativeHandle handle) : base(handle)
    {
    }

    protected void AddObservers(IEnumerable<IDisposable> observers)
    {
        _observers.AddRange(observers);
    }

    protected void ClearObservers()
    {
        foreach (var observer in _observers)
        {
            try
            {
                observer?.Dispose();
            }
            catch
            {
                // Ignore
            }
        }
        _observers.Clear();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            ClearObservers();
        }
    }
}
