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
        return value is null
            ? new NSString()
            : (NSString)Path.GetFileName(value.ToString());
    }
}
