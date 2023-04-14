using System.Text;
using System.Text.RegularExpressions;

using OfficeOpenXml;

public partial class ProcessorTests
{
    static ProcessorTests()
    {
        ExcelPackage.LicenseContext ??= LicenseContext.Commercial;
    }

    private const string SUFFIX = ": {m}{p}";

    [Test]
    [Explicit]
    public void WGNDFileLoads()
    {
        var proc = new Processor();
    }

    private const string HEADERS = "name,code,gender,wgt";

    private const string TESTWGNDCONTENTS = HEADERS + @"
john,US,M,1
mary,US,F,1
pat,US,F,0.5
pat,US,M,0.5
";

    [TestCaseSource(nameof(ProcessCsvTestCases))]
    public string[] ProcessCsvTests(string input, bool hasHeaders)
    {
        using var inputFile = new TemporaryFile(".csv");
        using (var writer = new StreamWriter(inputFile, new UTF8Encoding(false), new FileStreamOptions
        {
            Mode = FileMode.Create,
            Access = FileAccess.Write,
            Share = FileShare.Read
        }))
        {
            writer.Write(input);
        }

        using var outputFile = new TemporaryFile(".csv");
        using var summaryFile = new TemporaryFile(".csv");

        using var wgndCsv = new CsvReader();
        wgndCsv.Open(new StringReader(TESTWGNDCONTENTS), true);

        var csvProc = new CsvProcessor();
        csvProc.Process(new Processor(wgndCsv), new CsvProcessorOptions
        {
            CountryCode = 1,
            FirstName = 0,
            HasHeaders = hasHeaders,
            InputFileName = inputFile,
            OutputFileName = outputFile,
            SummaryFileName = summaryFile
        });

        return new[] {
            File.ReadAllText(outputFile, Encoding.UTF8),
            File.ReadAllText(summaryFile, Encoding.UTF8)
        };
    }

    [TestCaseSource(nameof(ProcessXlsxTestCases))]
    public string[] ProcessXlsxTests(string input, bool hasHeaders)
    {
        using var inputFile = new TemporaryFile(".xlsx");
        using (var inputDocument = new ExcelPackage())
        {
            var worksheet = inputDocument.Workbook.Worksheets.Add("Sheet 1");
            worksheet.Cells[1, 1].LoadFromText(input.TrimStart(), new ExcelTextFormat
            {
                Delimiter = ',',
                Encoding = Encoding.UTF8,
                EOL = @"
"
            });
            inputDocument.SaveAs(inputFile);
        }

        using var outputFile = new TemporaryFile(".xlsx");

        using var wgndCsv = new CsvReader();
        wgndCsv.Open(new StringReader(TESTWGNDCONTENTS), true);

        var xlsxProc = new XlsxProcessor();
        xlsxProc.Process(new Processor(wgndCsv), new XlsxProcessorOptions
        {
            FirstName = 0,
            CountryCode = 1,
            HasHeaders = hasHeaders,
            InputFileName = inputFile,
            OutputFileName = outputFile,
            ReaderOptions =
            {
                HasHeaderRow = hasHeaders,
                RowsToSkip = 0,
                RowsToTrim = 0,
                Worksheet = 0
            }
        });

        using var outputDocument = new ExcelPackage(outputFile);
        return new[] {
            DumpWorksheet(outputDocument, 1, hasHeaders),
            TrailingCommasRegex().Replace(DumpWorksheet(outputDocument, 0, hasHeaders), "")
        };
    }

    private static string DumpWorksheet(ExcelPackage outputDocument, int worksheetIndex, bool hasHeaders)
    {
        var worksheet = outputDocument.Workbook.Worksheets[worksheetIndex];

        using var result = new MemoryStream();
        worksheet.Cells[worksheet.Dimension.Address].SaveToText(result, new ExcelOutputTextFormat
        {
            Delimiter = ',',
            Encoding = new UTF8Encoding(false),
            EOL = @"
",
            FirstRowIsHeader = hasHeaders
        });

        result.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(result, Encoding.UTF8);
        return EmptyCommasRegex().Replace(reader.ReadToEnd(), "");
    }

    private static IEnumerable<TestCaseData> ProcessXlsxTestCases()
    {
        var summary = $@"GnE Results
Generated,{DateTime.Today:MMMM d, yyyy}
Dictionary,WIPO World Gender-Name Dictionary 2.0
Data source,Sheet 1
Columns Used,First Name = 'first', Country Code = 'country', Disclosure ID = unused, Person ID = unused

Program code released by Richardson Oliver Insights under CC-BY-SA 40 license. Find out how to contribute and help Diversity Equity and Inclusion initiatives in the inventor base and download/updates at:
https://roipatents.com/

Learn about the Diversity Pledge at:
https://increasingdii.org/

Basic Counts
,Item,Total,,Comments
,Number of Patents/Apps/Disclosures,3,,Number of distinct disclosures/patents/applications listed
,Number of Unique Inventors,3,,Looks for inventor uniqueness based on provided email addresses/employee identifiers
,Number of Total Inventors,3,,Total number of listed inventors, e.g. each instance of Jane Doe counts

Women Inventor Rate Estimate
,Item,Total,Percent,Comments
,Number of Unique Inventors: All,3,100.0%
,Number of Unique Inventors: Women,1,33.3%
,Number of Unique Inventors: Men,1,33.3%
,Number of Unique Inventors: Undetermined,1,33.3%

Patent Output Estimate
,Item,Total,Percent,Comments
,Number of Disclosures/Patents/Apps: All,3,100.0%
,Number with at Least one Woman Inventor,1,33.3%
,Number with at Least one Man Inventor,1,33.3%
,Number with at Least one Undetermined Inventor,1,33.3%

,Number with Solo Woman Inventor,1,33.3%,Only counts patents/apps/disclosures with a single inventor who is estimated to be a woman
,Number with Solo Man Inventor,1,33.3%,Only counts patents/apps/disclosures with a single inventor who is estimated to be a man

Fractional Inventorship Rate Estimate
,Item,Total,,Comments
,Number of Disclosures/Patents/Apps: All,3.0
,Weighted Count of Disclosures: Women,1.0
,Weighted Count of Disclosures: Men,1.0
,Weighted Count of Disclosures: Undetermined,1.0";
        var summaryNoHeaders = summary.Replace("First Name = 'first', Country Code = 'country'", "First Name = 0, Country Code = 1");

        foreach (var tc in ProcessCsvTestCases())
        {
            tc.TestName = tc.TestName?.Replace("CSV", "XLSX");
            if (tc.ExpectedResult is string[] results && results.Length == 2)
            {
                results[0] = LastCommaAndDecimal().Replace(results[0].Trim(), match => $",{decimal.Parse(match.Groups[1].Value):0.0%}");
                results[1] = results[1].Contains("First Name = 'first'")
                    ? summary
                    : summaryNoHeaders;
                tc.ExpectedResult = results;
            }
            yield return tc;
        }
    }

    private static IEnumerable<TestCaseData> ProcessCsvTestCases()
    {
        var summary = $@"GnE Results
Generated	{DateTime.Today:MMMM d, yyyy}
Dictionary	WIPO World Gender-Name Dictionary 2.0
Columns Used	First Name = 'first', Country Code = 'country', Disclosure ID = unused, Person ID = unused

Program code released by Richardson Oliver Insights under CC-BY-SA 40 license. Find out how to contribute and help Diversity Equity and Inclusion initiatives in the inventor base and download/updates at:
https://roipatents.com/

Learn about the Diversity Pledge at:
https://increasingdii.org/

Basic Counts
------------
	Item		Total				Comments
	Number of Patents/Apps/Disclosures		3				Number of distinct disclosures/patents/applications listed
	Number of Unique Inventors		3				Looks for inventor uniqueness based on provided email addresses/employee identifiers
	Number of Total Inventors		3				Total number of listed inventors, e.g. each instance of Jane Doe counts

Women Inventor Rate Estimate
----------------------------
	Item		Total		Percent		Comments
	Number of Unique Inventors: All		3		100.0%		
	Number of Unique Inventors: Women		1		33.3%		
	Number of Unique Inventors: Men		1		33.3%		
	Number of Unique Inventors: Undetermined		1		33.3%		

Patent Output Estimate
----------------------
	Item		Total		Percent		Comments
	Number of Disclosures/Patents/Apps: All		3		100.0%		
	Number with at Least one Woman Inventor		1		33.3%		
	Number with at Least one Man Inventor		1		33.3%		
	Number with at Least one Undetermined Inventor		1		33.3%		

	Number with Solo Woman Inventor		1		33.3%		Only counts patents/apps/disclosures with a single inventor who is estimated to be a woman
	Number with Solo Man Inventor		1		33.3%		Only counts patents/apps/disclosures with a single inventor who is estimated to be a man

Fractional Inventorship Rate Estimate
-------------------------------------
	Item		Total				Comments
	Number of Disclosures/Patents/Apps: All		3.0				
	Weighted Count of Disclosures: Women		1.0				
	Weighted Count of Disclosures: Men		1.0				
	Weighted Count of Disclosures: Undetermined		1.0				
";
        var summaryNoHeaders = summary.Replace("First Name = 'first', Country Code = 'country'", "First Name = 0, Country Code = 1");

        yield return new TestCaseData(@"
first,country,other
John,US,1
Mary,US,2
Pat,US,3", true)
        {
            TestName = "Standard CSV w/ Headers" + SUFFIX,
            ExpectedResult = new[] {
                @"
first,country,other,Gender,Accuracy
John,US,1,M,1
Mary,US,2,F,1
Pat,US,3,I,0.5",
                summary
            }
        };
        yield return new TestCaseData(@"
first,country,other

John,US,1


Mary,US,2



Pat,US,3




", true)
        {
            TestName = "Extra Newline CSV w/ Headers" + SUFFIX,
            ExpectedResult = new[] {
                @"
first,country,other,Gender,Accuracy

John,US,1,M,1


Mary,US,2,F,1



Pat,US,3,I,0.5




",
                summary
            }
        };
        yield return new TestCaseData(@"
John,US,1
Mary,US,2
Pat,US,3", false)
        {
            TestName = "Standard CSV w/o Headers" + SUFFIX,
            ExpectedResult = new[] {
                @"
John,US,1,M,1
Mary,US,2,F,1
Pat,US,3,I,0.5",
                summaryNoHeaders
            }
        };
        yield return new TestCaseData(@"

John,US,1


Mary,US,2



Pat,US,3




", false)
        {
            TestName = "Extra Newline CSV w/o Headers" + SUFFIX,
            ExpectedResult = new[] {
                @"

John,US,1,M,1


Mary,US,2,F,1



Pat,US,3,I,0.5




",
                summaryNoHeaders
            }
        };
        yield return new TestCaseData(@"
first,country,other
 John , US ,1
 Mary,US ,2
Pat , US,3", true)
        {
            TestName = "Standard CSV w/ Headers w/ Spaces" + SUFFIX,
            ExpectedResult = new[] {
                @"
first,country,other,Gender,Accuracy
 John , US ,1,M,1
 Mary,US ,2,F,1
Pat , US,3,I,0.5",
                summary
            }
        };
    }

    [TestCaseSource(nameof(DataRulesTestCases))]
    public DataRecord DataRules(string input, string firstName, string countryCode)
    {
        using var csvReader = new CsvReader();

        csvReader.Open(new StringReader(HEADERS + input), true);
        var proc = new Processor(csvReader);
        return proc.GetDataRecord(firstName, countryCode);
    }

    private static IEnumerable<TestCaseData> DataRulesTestCases()
    {
        yield return new TestCaseData("", "John", "US")
        {
            TestName = "No Data" + SUFFIX,
            ExpectedResult = DataRecord.NotFound
        };

        yield return new TestCaseData(@"
Jon,US,M,1", "John", "US")
        {
            TestName = "Different Name" + SUFFIX,
            ExpectedResult = DataRecord.NotFound
        };

        yield return new TestCaseData(@"
John,UK,M,1", "John", "US")
        {
            TestName = "Different Country" + SUFFIX,
            ExpectedResult = DataRecord.NotFound
        };

        yield return new TestCaseData(@"
John,US,M,1", "John", "US")
        {
            TestName = "One Entry Man" + SUFFIX,
            ExpectedResult = new DataRecord
            {
                FirstName = "John",
                CountryCode = "US",
                Gender = Gender.Man,
                Accuracy = 1m
            }
        };

        yield return new TestCaseData(@"
John,US,F,0.75", "John", "US")
        {
            TestName = "One Entry Woman" + SUFFIX,
            ExpectedResult = new DataRecord
            {
                FirstName = "John",
                CountryCode = "US",
                Gender = Gender.Woman,
                Accuracy = 0.75m
            }
        };

        yield return new TestCaseData(@"
John,US,?,0.5", "John", "US")
        {
            TestName = "One Entry Unknown" + SUFFIX,
            ExpectedResult = new DataRecord
            {
                FirstName = "John",
                CountryCode = "US",
                Gender = Gender.Unknown,
                Accuracy = 0.5m
            }
        };

        yield return new TestCaseData(@"
John,US,F,0.5
John,US,M,0.5
", "John", "US")
        {
            TestName = "Equal Weight FM" + SUFFIX,
            ExpectedResult = new DataRecord
            {
                FirstName = "John",
                CountryCode = "US",
                Gender = Gender.Indeterminate,
                Accuracy = 0.5m
            }
        };

        yield return new TestCaseData(@"
John,US,M,0.5
John,US,F,0.5
", "John", "US")
        {
            TestName = "Equal Weight MF" + SUFFIX,
            ExpectedResult = new DataRecord
            {
                FirstName = "John",
                CountryCode = "US",
                Gender = Gender.Indeterminate,
                Accuracy = 0.5m
            }
        };

        yield return new TestCaseData(@"
John,US,?,0.5
John,US,F,0.5
", "John", "US")
        {
            TestName = "Equal Weight ?F" + SUFFIX,
            ExpectedResult = new DataRecord
            {
                FirstName = "John",
                CountryCode = "US",
                Gender = Gender.Indeterminate,
                Accuracy = 0.5m
            }
        };

        yield return new TestCaseData(@"
John,US,?,0.45
John,US,F,0.1
John,US,M,0.45
", "John", "US")
        {
            TestName = "Equal Weight ?M" + SUFFIX,
            ExpectedResult = new DataRecord
            {
                FirstName = "John",
                CountryCode = "US",
                Gender = Gender.Indeterminate,
                Accuracy = 0.45m
            }
        };

        yield return new TestCaseData(@"
John,US,?,0.25
John,US,F,0.3
John,US,M,0.25
", "John", "US")
        {
            TestName = "Winner F" + SUFFIX,
            ExpectedResult = new DataRecord
            {
                FirstName = "John",
                CountryCode = "US",
                Gender = Gender.Woman,
                Accuracy = 0.3m
            }
        };

        yield return new TestCaseData(@"
John,US,?,0.45
John,US,F,0.05
John,US,M,0.5
", "John", "US")
        {
            TestName = "Winner M" + SUFFIX,
            ExpectedResult = new DataRecord
            {
                FirstName = "John",
                CountryCode = "US",
                Gender = Gender.Man,
                Accuracy = 0.5m
            }
        };

        yield return new TestCaseData(@"
John,US,?,0.57
John,US,F,0.4
John,US,M,0.03
", "John", "US")
        {
            TestName = "Winner ?" + SUFFIX,
            ExpectedResult = new DataRecord
            {
                FirstName = "John",
                CountryCode = "US",
                Gender = Gender.Unknown,
                Accuracy = 0.57m
            }
        };
    }

    [GeneratedRegex("^,+$", RegexOptions.Multiline | RegexOptions.Compiled)]
    private static partial Regex EmptyCommasRegex();

    [GeneratedRegex(",+$", RegexOptions.Multiline | RegexOptions.Compiled)]
    private static partial Regex TrailingCommasRegex();

    [GeneratedRegex(@",(\d+(?:\.\d+)?)$", RegexOptions.Multiline | RegexOptions.Compiled)]
    private static partial Regex LastCommaAndDecimal();
}
