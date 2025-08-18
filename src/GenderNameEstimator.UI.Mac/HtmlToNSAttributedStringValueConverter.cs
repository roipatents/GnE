namespace GenderNameEstimator.UI.Mac;

[Register(nameof(HtmlToNsAttributedStringValueConverter))]
public class HtmlToNsAttributedStringValueConverter : NSValueTransformer
{
    public HtmlToNsAttributedStringValueConverter()
    {
    }

    public HtmlToNsAttributedStringValueConverter(NSObjectFlag t) : base(t)
    {
    }

    public HtmlToNsAttributedStringValueConverter(IntPtr handle) : base(handle)
    {
    }

    public static NSAttributedString CreateAttributedStringFromHtml(string html, NSFont? font = null, NSColor? color = null)
    {
        static string ToHexByte(nfloat value)
        {
            var intValue = (int)Math.Round(value * 0xFF);
            return intValue.ToString("X2");
        }

        font ??= NSFont.SystemFontOfSize(NSFont.SystemFontSize);
#pragma warning disable CA1422 // Validate platform compatibility
        color = (color ?? NSColor.Label).UsingColorSpace(NSColorSpace.CalibratedRGB);
#pragma warning restore CA1422 // Validate platform compatibility
        color.GetRgba(out var r, out var g, out var b, out _);
        html = $"<span style=\"font-family: {font!.DisplayName}; font-size: {font.PointSize * 1.1}px; color: #{ToHexByte(r)}{ToHexByte(g)}{ToHexByte(b)}\">{html.Replace("\n", "<br/>\n")}</span>";
        return NSAttributedString.CreateWithHTML(NSData.FromString(html), out _);
    }

    public override NSObject TransformedValue(NSObject? value)
    {
        switch (value)
        {
            case NSString str:
                return CreateAttributedStringFromHtml(str);
            case NSAttributedString attrStr:
                return attrStr;
            default:
                return CreateAttributedStringFromHtml(value?.ToString() ?? "");
        }
    }
}
