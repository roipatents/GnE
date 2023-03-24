namespace GenderNameEstimator.UI.Mac;

[Register(nameof(NotNullOrEmptyValueTransformer))]
public class NotNullOrEmptyValueTransformer : NSValueTransformer
{
    public NotNullOrEmptyValueTransformer() : base()
    {
    }

    public NotNullOrEmptyValueTransformer(NSObjectFlag t) : base(t)
    {
    }

    public NotNullOrEmptyValueTransformer(IntPtr handle) : base(handle)
    {
    }

    public override NSObject TransformedValue(NSObject? value)
    {
        return new NSNumber(value is NSString s && s.Length > 0);
    }
}
