using Microsoft.Extensions.Logging;

// TODO: Use custom transformers to convert the LogLevel values to text / colors
// TODO: Accessibility for color blindness

namespace GenderNameEstimator.UI.Mac;

[Register(nameof(MessageInfo))]
public partial class MessageInfo : NSObject
{
    private readonly LogLevel _entryType;

    public MessageInfo(LogLevel entryType, string process, string message)
    {
        Time = (NSDate)DateTime.Now;
        _entryType = entryType;
        Process = process;
        Message = message;
    }

    [Export(nameof(EntryType))]
    public string EntryType => _entryType.ToString().ToUpper();

    [Export(nameof(EntryTypeColor))]
    public NSColor EntryTypeColor => _entryType switch
    {
        LogLevel.Trace => NSColor.Text,
        LogLevel.Debug => NSColor.Text,
        LogLevel.Information => NSColor.Green,
        LogLevel.Warning => NSColor.Orange,
        LogLevel.Error => NSColor.Red,
        LogLevel.Critical => NSColor.FromRgb(0x9b, 0x00, 0x00),
        LogLevel.None => NSColor.Text,
        _ => NSColor.Text
    };

    [Export(nameof(Process))]
    public string Process { get; }

    [Export(nameof(Message))]
    public string Message { get; }

    [Export(nameof(Time))]
    public NSDate Time { get; }
}
