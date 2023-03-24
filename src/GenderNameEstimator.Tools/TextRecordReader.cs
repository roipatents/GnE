using System.Collections;

namespace GenderNameEstimator.Tools;

public abstract class TextRecordReader : IDisposable, IEnumerable<ITextRecord>, ITextRecord
{
    protected readonly List<string> CurrentRecord = new();

    // TODO: Maybe just have a list instead, in case the headers aren't unique?
    public Dictionary<string, int>? Headers = null;

    public bool HasHeaders { get; protected set; }

    public int FieldCount => CurrentRecord.Count;

    public string? this[string name] => this[GetOrdinal(name)];

    public string? this[int i] => i < 0 || i >= CurrentRecord.Count ? null : CurrentRecord[i];

    public string? GetName(int i) => Headers?.Where(kvp => kvp.Value == i).Select(kvp => kvp.Key).FirstOrDefault();

    public int GetOrdinal(string name) => Headers is null || !Headers.TryGetValue(name, out var i) ? -1 : i;

    public string[] GetValues() => CurrentRecord.ToArray();

    public int CurrentRowIndex { get; protected set; } = -1;

    public virtual int RowCount => -1;

    public void Dispose()
    {
        Close();
        GC.SuppressFinalize(this);
    }

    public event EventHandler? RowRead;

    public bool ReadNext()
    {
        if (OnReadNext())
        {
            RowRead?.Invoke(this, EventArgs.Empty);
            return true;
        }
        return false;
    }

    protected abstract bool OnReadNext();

    public virtual void Close() { }

    public IEnumerator<ITextRecord> GetEnumerator()
    {
        while (ReadNext())
        {
            yield return this;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

}
