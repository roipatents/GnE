namespace GenderNameEstimator.UI.Mac;

[Register(nameof(NonNegativeValueTransformer))]
public class NonNegativeValueTransformer : NSValueTransformer
{
    public NonNegativeValueTransformer() : base()
    {
    }

    public NonNegativeValueTransformer(NSObjectFlag t) : base(t)
    {
    }

    public NonNegativeValueTransformer(IntPtr handle) : base(handle)
    {
    }

    public override NSObject TransformedValue(NSObject? value)
    {
        return value is null || (value is NSNumber n && n.Int32Value < 0) ? new NSNumber(0) : value;
    }
}
