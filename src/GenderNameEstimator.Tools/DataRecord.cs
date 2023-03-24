namespace GenderNameEstimator.Tools;

[System.Diagnostics.DebuggerDisplay("{FirstName}-{CountryCode} -> {Gender} {Accuracy}")]
public struct DataRecord
{
    public string? FirstName;
    public string? CountryCode;
    public char Gender;
    public decimal Accuracy;

    public static readonly DataRecord NotFound = new()
    {
        FirstName = null,
        CountryCode = null,
        Gender = Tools.Gender.NotAvailable,
        Accuracy = 0m
    };
}
