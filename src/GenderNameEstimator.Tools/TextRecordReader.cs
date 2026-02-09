/*
 * The MIT License (MIT)
 *
 * Copyright © 2023-2026, Richardson Oliver Insights, LLC, All Rights Reserved
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */
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
