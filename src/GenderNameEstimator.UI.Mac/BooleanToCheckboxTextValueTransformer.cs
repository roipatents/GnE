namespace GenderNameEstimator.UI.Mac;

[Register(nameof(BooleanToCheckboxTextValueTransformer))]
public class BooleanToCheckboxTextValueTransformer : NSValueTransformer
{
    private static readonly NSString CheckedText = (NSString)"✅";

    private static readonly NSString UncheckedText = (NSString)"";

    public BooleanToCheckboxTextValueTransformer()
    {
    }

    public BooleanToCheckboxTextValueTransformer(NSObjectFlag t) : base(t)
    {
    }

    public BooleanToCheckboxTextValueTransformer(IntPtr handle) : base(handle)
    {
    }

    public override NSObject TransformedValue(NSObject? value)
    {
        return value is NSNumber n && n.BoolValue
            ? CheckedText
            : UncheckedText;
    }
}
