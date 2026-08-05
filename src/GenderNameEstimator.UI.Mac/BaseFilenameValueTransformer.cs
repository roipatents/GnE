namespace GenderNameEstimator.UI.Mac;

[Register(nameof(BaseFilenameValueTransformer))]
public class BaseFilenameValueTransformer : NSValueTransformer
{
    public BaseFilenameValueTransformer() : base()
    {
    }

    public BaseFilenameValueTransformer(NSObjectFlag t) : base(t)
    {
    }

    public BaseFilenameValueTransformer(IntPtr handle) : base(handle)
    {
    }

    public override NSObject TransformedValue(NSObject? value)
    {
        return new NSString(value is null
            ? string.Empty
            : Path.GetFileName(value.ToString()) ?? string.Empty);
    }
}
