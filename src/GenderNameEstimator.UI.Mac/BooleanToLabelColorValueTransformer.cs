namespace GenderNameEstimator.UI.Mac;

[Register(nameof(BooleanToLabelColorValueTransformer))]
public class BooleanToLabelColorValueTransformer : NSValueTransformer
{
    public BooleanToLabelColorValueTransformer() : base()
    {
    }

    public BooleanToLabelColorValueTransformer(NSObjectFlag t) : base(t)
    {
    }

    public BooleanToLabelColorValueTransformer(IntPtr handle) : base(handle)
    {
    }

    public override NSObject TransformedValue(NSObject? value)
    {
        return value is NSNumber n && !n.BoolValue
            ? NSColor.DisabledControlText
            : NSColor.Label;
    }
}
