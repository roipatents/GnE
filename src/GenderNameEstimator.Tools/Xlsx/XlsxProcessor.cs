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
using System.Diagnostics;

using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace GenderNameEstimator.Tools.Xlsx;

public class XlsxProcessor : FileProcessor
{

    public override void Process(Processor processor, FileProcessorOptions options)
    {
        if (options is not XlsxProcessorOptions)
        {
            throw new ArgumentException($"Options must be of type {nameof(XlsxProcessorOptions)}", nameof(options));
        }
        ValidateOptions(options);

        Debug.Assert(!string.IsNullOrEmpty(options.InputFileName));
        if (string.IsNullOrEmpty(options.OutputFileName))
        {
            options.OutputFileName = options.InputFileName;
        }
        if (options.InputFileName != options.OutputFileName)
        {
            File.Copy(options.InputFileName, options.OutputFileName, true);
        }

        using var reader = new XlsxReader();
        XlsxReaderOptions readerOptions = ((XlsxProcessorOptions)options).ReaderOptions;
        reader.Open(options.OutputFileName, readerOptions);
        if (reader.Document is null)
        {
            throw new InvalidOperationException($"Could not open '{options.OutputFileName}'");
        }
        var worksheet = reader.Worksheet ?? throw new InvalidOperationException($"{nameof(reader)}.{nameof(reader.Worksheet)} is not available for writing");

        int genderColumn = worksheet.Dimension.End.Column + 1;
        int accuracyColumn = genderColumn + 1;
        if (reader.HasHeaders)
        {
            // TODO: Move the output column names to a class (or even make them configurable with reasoanble defaults)
            worksheet.Cells[reader.CurrentRowIndex, genderColumn].Value = "Gender";
            worksheet.Cells[reader.CurrentRowIndex, accuracyColumn].Value = "Accuracy";
        }

        var summaryInfo = new SummaryInfo();
        foreach (var record in GetDataRecords(processor, options, reader, summaryInfo))
        {
            worksheet.Cells[reader.CurrentRowIndex, genderColumn].Value = record.Gender;
            worksheet.Cells[reader.CurrentRowIndex, accuracyColumn].Value = record.Accuracy;
        }

        var headerRowIndex = worksheet.Dimension.Start.Row + readerOptions.RowsToSkip;
        if (!reader.HasHeaders)
        {
            headerRowIndex--;
        }
        var lastRowIndex = worksheet.Dimension.End.Row - readerOptions.RowsToTrim;

        var range = worksheet.Cells[
            headerRowIndex + 1,
            genderColumn,
            lastRowIndex,
            genderColumn];
        range.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;

        range = worksheet.Cells[
            headerRowIndex + 1,
            accuracyColumn,
            lastRowIndex,
            accuracyColumn];
        range.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
        range.Style.Numberformat.Format = "0.0%";

        if (reader.HasHeaders)
        {
            try
            {
                // This may fail if the existing header is part of a named range
                range = worksheet.Cells[headerRowIndex, worksheet.Dimension.Start.Column, headerRowIndex, worksheet.Dimension.End.Column];
                range.AutoFilter = true;
            }
            catch
            {
                // Ignored
            }
        }
        else
        {
            headerRowIndex++;
        }

        range = worksheet.Cells[
            headerRowIndex,
            worksheet.Dimension.Start.Column,
            lastRowIndex,
            worksheet.Dimension.End.Column];
        range.AutoFitColumns();

        worksheet.View.SelectedRange = worksheet.View.ActiveCell = worksheet.Cells[headerRowIndex, worksheet.Dimension.Start.Column].Address;

        reader.Document.Save();

        AddSummarySheet(reader, options, summaryInfo);
    }

    private static void AddSummarySheet(XlsxReader reader, FileProcessorOptions options, SummaryInfo summaryInfo)
    {
        // Insert summary worksheet
        Debug.Assert(reader.Document is not null);
        var worksheet = reader.Document.Workbook.Worksheets.Add("Results");
        reader.Document.Workbook.Worksheets.MoveToStart(worksheet.Index);
        // TODO: The move of the Results worksheet causes every other worksheet index to incrememnt by one.  Is there a good way to indicate this beyond just knowing?
        reader.Document.Workbook.View.ActiveTab = worksheet.Index;

        // TODO: Centralize the headings and WGND file version information
        var range = worksheet.Cells["A1"];
        range.Value = "GnE Results";
        range.Style.Font.Bold = range.Style.Font.UnderLine = true;
        range.Style.Font.Size = 22;
        range.Style.Font.UnderLineType = ExcelUnderLineType.Single;

        worksheet.Cells["A2"].Value = "Generated";
        range = worksheet.Cells["B2"];
        range.Value = DateTime.Today;
        range.Style.Numberformat.Format = "mmmm d, yyyy";
        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;

        worksheet.Cells["A3"].Value = "Dictionary";
        worksheet.Cells["B3"].Value = "WIPO World Gender-Name Dictionary 2.0";

        worksheet.Cells["A4"].Value = "Data source";
        worksheet.Cells["B4"].Value = reader.CurrentWorksheetName;

        worksheet.Cells["A5"].Value = "Columns Used";

        void SetCellLink(string address, string uri, string? text = null)
        {
            var range = worksheet.Cells[address];
            range.Value = text ?? uri;
            range.Hyperlink = new Uri(uri);
            range.Style.Font.UnderLine = true;
            range.Style.Font.UnderLineType = ExcelUnderLineType.Single;
            range.Style.Font.Color.SetColor(255, 51, 102, 204);
        }

        SetCellLink("A8", "https://roipatents.com/");

        SetCellLink("A11", "https://increasingdii.org/");

        var row = 12;

        void SkipRow()
        {
            row++;
        }

        void SetCellValueAndFormat(ExcelWorksheet worksheet, string address, object value, string format)
        {
            var cell = worksheet.Cells[address];
            cell.Value = value;
            cell.Style.Numberformat.Format = format;
        }

        void SetCellPercentage(ExcelWorksheet worksheet, string address, double value)
        {
            SetCellValueAndFormat(worksheet, address, value, "0.0%");
        }

        void SetCellFloatingoint(ExcelWorksheet worksheet, string address, double value)
        {
            SetCellValueAndFormat(worksheet, address, value, "0.0");
        }

        void AddHeading(string text, bool includePercent)
        {
            SkipRow();

            // Heading
            var range = worksheet.Cells["A" + row++];
            range.Value = text;
            range.Style.Font.Bold = range.Style.Font.UnderLine = true;
            range.Style.Font.UnderLineType = ExcelUnderLineType.Single;

            // Sub-headings
            range = worksheet.Cells["B" + row + ":E" + row++];
            range.SetCellValue(0, 0, "Item");
            range.SetCellValue(0, 1, "Total");
            range.SetCellValue(0, 2, includePercent ? "Percent" : "");
            range.SetCellValue(0, 3, "Comments");
            range.Style.Font.UnderLine = true;
            range.Style.Font.UnderLineType = ExcelUnderLineType.Single;
        }

        void AddRowWithoutPercent(string item, int total, string comment = "")
        {
            worksheet.Cells["B" + row].Value = item;
            worksheet.Cells["C" + row].Value = total;
            worksheet.Cells["D" + row].Value = "";
            worksheet.Cells["E" + row++].Value = comment;
        }

        void AddRowWithFolatingPointTotal(string item, double total, string comment = "")
        {
            worksheet.Cells["B" + row].Value = item;
            SetCellFloatingoint(worksheet, "C" + row, total);
            worksheet.Cells["D" + row].Value = "";
            worksheet.Cells["E" + row++].Value = comment;
        }

        void AddRow(string item, CountAndPercentage info, string comment = "")
        {
            worksheet.Cells["B" + row].Value = item;
            worksheet.Cells["C" + row].Value = info.Count;
            SetCellPercentage(worksheet, "D" + row, info.Percentage);
            worksheet.Cells["E" + row++].Value = comment;
        }

        // TODO: Centralize this.  We could create an interface or abstract base class and an implementation class with the appropriate methods with access to state
        AddHeading("Basic Counts", false);
        AddRowWithoutPercent("Number of Patents/Apps/Disclosures", summaryInfo.UniqueDisclosurePeople.Count, "Number of distinct disclosures/patents/applications listed");
        AddRowWithoutPercent("Number of Unique Inventors", summaryInfo.PeopleRecords.Count, "Looks for inventor uniqueness based on provided email addresses/employee identifiers");
        AddRowWithoutPercent("Number of Total Inventors", summaryInfo.RowCount, "Total number of listed inventors, e.g. each instance of Jane Doe counts");

        AddHeading("Women Inventor Rate Estimate", true);
        var inventorRate = summaryInfo.GetInventorRate();
        AddRow("Number of Unique Inventors: All", inventorRate.all);
        AddRow("Number of Unique Inventors: Women", inventorRate.women);
        AddRow("Number of Unique Inventors: Men", inventorRate.men);
        AddRow("Number of Unique Inventors: Undetermined", inventorRate.undetermined);

        AddHeading("Patent Output Estimate", true);
        var disclosureOutput = summaryInfo.GetDisclosureOutput();
        AddRow("Number of Disclosures/Patents/Apps: All", disclosureOutput.all);
        AddRow("Number with at Least one Woman Inventor", disclosureOutput.atLeastOneWoman);
        AddRow("Number with at Least one Man Inventor", disclosureOutput.atLeastOneMan);
        AddRow("Number with at Least one Undetermined Inventor", disclosureOutput.atLeastOneUndetermined);
        SkipRow();
        AddRow("Number with Solo Woman Inventor", disclosureOutput.soloWoman, "Only counts patents/apps/disclosures with a single inventor who is estimated to be a woman");
        AddRow("Number with Solo Man Inventor", disclosureOutput.soloMan, "Only counts patents/apps/disclosures with a single inventor who is estimated to be a man");

        AddHeading("Fractional Inventorship Rate Estimate", false);
        var fractionalInvertorship = summaryInfo.GetFractionalInventorship();
        AddRowWithFolatingPointTotal("Number of Disclosures/Patents/Apps: All", fractionalInvertorship.all);
        AddRowWithFolatingPointTotal("Weighted Count of Disclosures: Women", fractionalInvertorship.women);
        AddRowWithFolatingPointTotal("Weighted Count of Disclosures: Men", fractionalInvertorship.men);
        AddRowWithFolatingPointTotal("Weighted Count of Disclosures: Undetermined", fractionalInvertorship.undetermined);

        range = worksheet.Cells[
            worksheet.Dimension.Start.Row,
            worksheet.Dimension.Start.Column,
            worksheet.Dimension.End.Row,
            worksheet.Dimension.End.Column];
        range.AutoFitColumns();

        // NOTE: These long text values are set AFTER autosizing
        worksheet.Cells["B5"].Value = $"First Name = {GetColumnNameForSummary(options.FirstName, reader)}, Country Code = {GetColumnNameForSummary(options.CountryCode, reader)}, Disclosure ID = {GetColumnNameForSummary(options.DisclosureId, reader)}, Person ID = {GetColumnNameForSummary(options.PersonId, reader)}";

        worksheet.Cells["A7"].Value = "Program code released by Richardson Oliver Insights under CC-BY-SA 40 license. Find out how to contribute and help Diversity Equity and Inclusion initiatives in the inventor base and download/updates at:";

        worksheet.Cells["A10"].Value = "Learn about the Diversity Pledge at:";

        worksheet.View.SelectedRange = worksheet.View.ActiveCell = "A1";

        reader.Document.Save();
    }
}

public class XlsxProcessorOptions : FileProcessorOptions
{
    public XlsxReaderOptions ReaderOptions { get; set; } = new();
}
