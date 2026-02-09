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
using OfficeOpenXml;

namespace GenderNameEstimator.Tools.Xlsx;

public class XlsxReader : TextRecordReader
{
    static XlsxReader()
    {
        ExcelPackage.LicenseContext ??= LicenseContext.NonCommercial;
    }

    private int _headerIndex;
    private int _finalRowIndex;
    private int _startColumn;
    private int _endColumn;
    private int _rowsToSkip;
    private int _rowsToTrim;

    public ExcelPackage? Document { get; private set; } = null;

    public ExcelWorksheet? Worksheet { get; private set; } = null;

    private void SetWorksheet(ExcelWorksheet nextWorksheet)
    {
        Worksheet = nextWorksheet;
        if (Headers is null)
        {
            Headers = new Dictionary<string, int>();
        }
        else
        {
            Headers.Clear();
        }
        _startColumn = Worksheet.Dimension.Start.Column;
        _endColumn = Worksheet.Dimension.End.Column;
        _finalRowIndex = Worksheet.Dimension.End.Row - _rowsToTrim;
        _headerIndex = CurrentRowIndex = Worksheet.Dimension.Start.Row + _rowsToSkip - 1;
        if (HasHeaders && OnReadNext())
        {
            // NOTE: CurrentRecord will have been set by OnReadNext so that the FieldCount will be correct
            _headerIndex = CurrentRowIndex;
            for (int columnIndex = _startColumn; columnIndex <= _endColumn; columnIndex++)
            {
                ExcelRange cell = Worksheet.Cells[_headerIndex, columnIndex];
                var title = cell.Text;
                if (string.IsNullOrWhiteSpace(title))
                {
                    title = cell.EntireColumn.Range.Address.Split(':')[0];
                }
                Headers.TryAdd(title, columnIndex - Worksheet.Dimension.Start.Column);
            }
        }
        else
        {
            for (int columnIndex = _startColumn; columnIndex <= _endColumn; columnIndex++)
            {
                ExcelRange cell = Worksheet.Cells[Worksheet.Dimension.Start.Row, columnIndex];
                Headers.Add(cell.EntireColumn.Range.Address.Split(':')[0], Worksheet.Dimension.Start.Column - columnIndex);
                // NOTE: Sets FieldCount accordignly
                CurrentRecord.Add("");
            }
        }
        _finalRowIndex = Worksheet.Dimension.End.Row - _rowsToTrim;
    }

    public int CurrentWorksheetIndex
    {
        get
        {
            return Worksheet?.Index ?? -1;
        }

        set
        {
            if (Document is null)
            {
                throw new InvalidOperationException("Document is not open");
            }
            SetWorksheet(Document.Workbook.Worksheets[value]);
        }
    }

    public string? CurrentWorksheetName
    {
        get
        {
            return Worksheet?.Name;
        }

        set
        {
            if (Document is null)
            {
                throw new InvalidOperationException("Document is not open");
            }
            SetWorksheet(Document.Workbook.Worksheets.FirstOrDefault(sheet => string.Equals(sheet.Name, value, StringComparison.CurrentCultureIgnoreCase))
                ?? throw new IndexOutOfRangeException());
        }
    }

    public override void Close()
    {
        Worksheet?.Dispose();
        Document?.Dispose();
        base.Close();
    }

    private static ExcelPackage OpenDocument(string filename)
    {
        return File.Exists(filename)
                        ? new ExcelPackage(filename)
                        : throw new FileNotFoundException($"Cannot open file '{filename}'", filename);
    }

    public void Open(string filename, bool hasHeaderRow = true)
    {
        HasHeaders = hasHeaderRow;
        Document = OpenDocument(filename);
        CurrentWorksheetIndex = Document.Workbook.View.ActiveTab;
    }

    public void Open(string filename, int worksheetIndex)
    {
        Open(filename, true, worksheetIndex);
    }

    public void Open(string filename, bool hasHeaderRow, int worksheetIndex)
    {
        HasHeaders = hasHeaderRow;
        Document = OpenDocument(filename);
        CurrentWorksheetIndex = worksheetIndex;
    }

    public void Open(string filename, string worksheetName)
    {
        Open(filename, true, worksheetName);
    }

    public void Open(string filename, bool hasHeaderRow, string worksheetName)
    {
        HasHeaders = hasHeaderRow;
        Document = OpenDocument(filename);
        CurrentWorksheetName = worksheetName;
    }

    public void Open(string filename, XlsxReaderOptions options)
    {
        HasHeaders = options.HasHeaderRow;
        _rowsToSkip = options.RowsToSkip;
        _rowsToTrim = options.RowsToTrim;
        Document = OpenDocument(filename);
        if (options.Worksheet.Index >= 0)
        {
            CurrentWorksheetIndex = options.Worksheet.Index.Value;
        }
        else if (!string.IsNullOrEmpty(options.Worksheet.Name))
        {
            CurrentWorksheetName = options.Worksheet.Name;
        }
        else
        {
            CurrentWorksheetIndex = Document.Workbook.View.ActiveTab;
        }
    }

    protected override bool OnReadNext()
    {
        while (CurrentRowIndex < _finalRowIndex)
        {
            if (Worksheet is null)
            {
                throw new InvalidOperationException("Worksheet is not available");
            }
            CurrentRecord.Clear();
            ++CurrentRowIndex;
            // TODO: Make skipping empty rows optional?
            bool hasNonEmptyColumn = false;
            for (int columnIndex = _startColumn; columnIndex <= _endColumn; columnIndex++)
            {
                var text = Worksheet.Cells[CurrentRowIndex, columnIndex].Text;
                if (!string.IsNullOrEmpty(text))
                {
                    hasNonEmptyColumn = true;
                }
                CurrentRecord.Add(text);
            }
            if (hasNonEmptyColumn)
            {
                return true;
            }
        }
        return false;
    }

    public override int RowCount => _finalRowIndex - _headerIndex;
}

public class XlsxReaderOptions
{
    public bool HasHeaderRow { get; set; } = true;

    public int RowsToSkip { get; set; } = 0;

    public int RowsToTrim { get; set; } = 0;

    public FieldInfo Worksheet { get; set; }
}
