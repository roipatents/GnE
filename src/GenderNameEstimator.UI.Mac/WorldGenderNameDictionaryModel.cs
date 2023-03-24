namespace GenderNameEstimator.UI.Mac;

[Register(nameof(GenderNameEstimatorModel))]
public class GenderNameEstimatorModel : NSObject
{
    private string? _wgndTargetFilename = "";
    private int _wgndTargetWorksheet = -1;

    [Export(nameof(SourceFileChooserModel))]
    public FileChooserViewModel SourceFileChooserModel { get; } = new();

    [Export(nameof(SourceColumnChooserModel))]
    public SpreadsheetColumnChooserViewModel SourceColumnChooserModel { get; } = new()
    {
        Columns = {
            new ColumnInfo("First Name", true),
            new ColumnInfo("Country Code", true),
            new ColumnInfo("Person ID"),
            new ColumnInfo("Disclosure / Patent ID"),
        }
    };

    //[Export(nameof(CountryCodeColumnChooserModel))]
    //public SpreadsheetColumnChooserViewModel CountryCodeColumnChooserModel { get; } = new();

    [Export(nameof(WGNDTargetFilename))]
    public string? WGNDTargetFilename
    {
        get => _wgndTargetFilename;
        set => this.ChangeStringField(ref _wgndTargetFilename, value, t => t.WGNDTargetFilename);
    }

    [Export(nameof(WGNDTargetWorksheet))]
    public int WGNDTargetWorksheet
    {
        get => _wgndTargetWorksheet;
        set => this.ChangeField(ref _wgndTargetWorksheet, value, t => t.WGNDTargetWorksheet);
    }
}
