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
using System.Text;

namespace GenderNameEstimator.Tools.Csv;

public class CsvProcessor : FileProcessor
{
    private static (string, string) HandleRawLine(string rawLine)
    {
        return rawLine[^1] == '\n' || rawLine[^1] == '\r'
            ? (rawLine[..^1], rawLine[^1..])
            : (rawLine, "");
    }

    public override void Process(Processor processor, FileProcessorOptions options)
    {
        if (options is not CsvProcessorOptions)
        {
            throw new ArgumentException($"Options must be of type {nameof(CsvProcessorOptions)}", nameof(options));
        }
        ValidateOptions(options);

        Debug.Assert(!string.IsNullOrEmpty(options.InputFileName));
        using var reader = CsvReader.Create(options.InputFileName, options.HasHeaders);

        Debug.Assert(!string.IsNullOrEmpty(options.OutputFileName));
        using var writer = new StreamWriter(options.OutputFileName, new UTF8Encoding(false), new FileStreamOptions
        {
            Mode = FileMode.Create,
            Access = FileAccess.Write,
            Share = FileShare.Read
        });
        if (reader.HasHeaders && reader.RawHeaderLine is not null)
        {
            var (pre, post) = HandleRawLine(reader.RawHeaderLine);
            writer.Write(pre);
            writer.Write(reader.Delimiter);
            writer.Write("Gender");
            writer.Write(reader.Delimiter);
            writer.Write("Accuracy");
            writer.Write(post);
        }

        var summaryInfo = new SummaryInfo();
        foreach (var record in GetDataRecords(processor, options, reader, summaryInfo))
        {
            var (pre, post) = HandleRawLine(reader.RawLine);
            writer.Write(pre);
            writer.Write(reader.Delimiter);
            writer.Write(record.Gender);
            writer.Write(reader.Delimiter);
            writer.Write(record.Accuracy);
            writer.Write(post);
        }
        writer.Write(reader.RawLine);

        CreateSummary((CsvProcessorOptions)options, reader, summaryInfo);
    }

    private static void CreateSummary(CsvProcessorOptions options, CsvReader reader, SummaryInfo summaryInfo)
    {
        Debug.Assert(!string.IsNullOrEmpty(options.SummaryFileName));
        using var writer = new StreamWriter(options.SummaryFileName, new UTF8Encoding(false), new FileStreamOptions
        {
            Mode = FileMode.Create,
            Access = FileAccess.Write,
            Share = FileShare.Read
        });

        // TODO: Centralize the headings and WGND file version information
        writer.WriteLine("GnE Results");
        writer.Write("Generated\t");
        writer.WriteLine(DateTime.Today.ToString("MMMM d, yyyy"));
        writer.WriteLine("Dictionary\tWIPO World Gender-Name Dictionary 2.0");
        writer.Write("Columns Used\tFirst Name = ");
        writer.Write(GetColumnNameForSummary(options.FirstName, reader));
        writer.Write(", Country Code = ");
        writer.Write(GetColumnNameForSummary(options.CountryCode, reader));
        writer.Write(", Disclosure ID = ");
        writer.Write(GetColumnNameForSummary(options.DisclosureId, reader));
        writer.Write(", Person ID = ");
        writer.WriteLine(GetColumnNameForSummary(options.PersonId, reader));
        writer.WriteLine();

        writer.WriteLine("Program code released by Richardson Oliver Insights under CC-BY-SA 40 license. Find out how to contribute and help Diversity Equity and Inclusion initiatives in the inventor base and download/updates at:");
        writer.WriteLine("https://roipatents.com/");
        writer.WriteLine();

        writer.WriteLine("Learn about the Diversity Pledge at:");
        writer.WriteLine("https://increasingdii.org/");

        void AddHeading(string text, bool includePercent)
        {
            writer.WriteLine();

            // Heading
            writer.WriteLine(text);
            writer.WriteLine(new string('-', text.Length));

            // Sub-headings
            writer.Write("\tItem\t\tTotal\t\t");
            if (includePercent)
            {
                writer.Write("Percent");
            }
            writer.WriteLine("\t\tComments");
        }

        void AddRowWithoutPercent(string item, int total, string comment = "")
        {
            writer.Write('\t');
            writer.Write(item);
            writer.Write("\t\t");
            writer.Write(total);
            writer.Write("\t\t\t\t");
            writer.WriteLine(comment);
        }

        void AddRowWithFolatingPointTotal(string item, double total, string comment = "")
        {
            writer.Write('\t');
            writer.Write(item);
            writer.Write("\t\t");
            writer.Write(total.ToString("0.0"));
            writer.Write("\t\t\t\t");
            writer.WriteLine(comment);
        }

        void AddRow(string item, CountAndPercentage info, string comment = "")
        {
            writer.Write('\t');
            writer.Write(item);
            writer.Write("\t\t");
            writer.Write(info.Count);
            writer.Write("\t\t");
            writer.Write(info.Percentage.ToString("0.0%"));
            writer.Write("\t\t");
            writer.WriteLine(comment);
        }

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
        writer.WriteLine();
        AddRow("Number with Solo Woman Inventor", disclosureOutput.soloWoman, "Only counts patents/apps/disclosures with a single inventor who is estimated to be a woman");
        AddRow("Number with Solo Man Inventor", disclosureOutput.soloMan, "Only counts patents/apps/disclosures with a single inventor who is estimated to be a man");

        AddHeading("Fractional Inventorship Rate Estimate", false);
        var fractionalInvertorship = summaryInfo.GetFractionalInventorship();
        AddRowWithFolatingPointTotal("Number of Disclosures/Patents/Apps: All", fractionalInvertorship.all);
        AddRowWithFolatingPointTotal("Weighted Count of Disclosures: Women", fractionalInvertorship.women);
        AddRowWithFolatingPointTotal("Weighted Count of Disclosures: Men", fractionalInvertorship.men);
        AddRowWithFolatingPointTotal("Weighted Count of Disclosures: Undetermined", fractionalInvertorship.undetermined);
    }
}

public class CsvProcessorOptions : FileProcessorOptions
{
    private string? _summaryFileName;

    public string? SummaryFileName
    {
        get => string.IsNullOrEmpty(_summaryFileName) && !string.IsNullOrEmpty(InputFileName)
            ? Path.Join(Path.GetDirectoryName(InputFileName), $"{Path.GetFileNameWithoutExtension(InputFileName)}-summary.txt")
            : _summaryFileName;
        set => _summaryFileName = value;
    }
}
