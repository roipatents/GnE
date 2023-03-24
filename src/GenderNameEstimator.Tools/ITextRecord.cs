namespace GenderNameEstimator.Tools;

public interface ITextRecord
{
    string? this[int i] { get; }
    string? this[string name] { get; }
    int FieldCount { get; }
    string? GetName(int i);
    int GetOrdinal(string name);
    string[] GetValues();
}
