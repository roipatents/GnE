using System.CommandLine;

using GenderNameEstimator.CommandLine;

using GenderNameEstimator.Tools;
using GenderNameEstimator.Tools.Csv;
using GenderNameEstimator.Tools.Xlsx;

var rootCommand = new RootCommand("GnE (pronounced \"Genie\" - the Gender-Name Estimator) takes an incoming XLSX or CSV file and outputs an appended version with additional columns for Gender and Accuracy based upon incoming columns for First Name and Country of Origin along with a summarized analysis of results.");

var firstNameOption = new Option<string>(new[]
{
    "--firstNameColumn",
    "--firstName",
    "--fnc",
    "--fn",
    "--f",
    "-f"
}, "The name or index of the column containing the first name")
{
    IsRequired = true
};
rootCommand.Add(firstNameOption);

var countryOption = new Option<string>(new[]
{
    "--countryColumn",
    "--country",
    "--cc",
    "--c",
    "-c"
}, "The name or index of the column containing the two-character code for the country of origin")
{
    IsRequired = true
};
rootCommand.Add(countryOption);

var personOption = new Option<string>(new[]
{
    "--personColumn",
    "--person",
    "--per",
}, "The name or index of the column containing the unique person identifier.  If not supplied, the combination of first name and country code are assumed to be unique");
rootCommand.Add(personOption);

var disclosureOption = new Option<string>(new[]
{
    "--discolsureColumn",
    "--disclosure",
    "--dis",
    "--patentColumn",
    "--patent",
    "--pat"
}, "The name or index of the column containing the unique disclosure / patent identifier");
rootCommand.Add(disclosureOption);

var dataFileOption = new Option<FileInfo>(new[]
{
    "--dataFile",
    "--data",
    "--d",
    "-d"
}, "The path to the World Gender-Name Dictionary name-gender-code CSV file used to determine gender and accuracy.  It defaults to the version provided with the software.  See https://github.com/IES-platform/r4r_gender/tree/main/wgnd");
rootCommand.Add(dataFileOption);
dataFileOption.AddValidator(result =>
{
    switch (result.GetValueForOption(dataFileOption)?.Extension.ToLower() ?? "")
    {
        case "":
        case ".csv":
            break;
        default:
            result.ErrorMessage = "Only CSV data files are supported";
            break;
    }
});

var outputOption = new Option<FileInfo>(new[]
{
    "--output",
    "--o",
    "-o"
}, "The path to the output file.  It defaults to the name of the input file with \"-appendend\" before the extension");
rootCommand.Add(outputOption);

var summaryOption = new Option<FileInfo>(new[]
{
    "--summary",
    "--s",
    "-s"
}, "The path to the output summary file.  It defaults to the name of the input file with \"-appendend.txt\" in place of the extension.  This option only applies to CSV processing");
rootCommand.Add(summaryOption);

var noHeaderOption = new Option<bool>(new[]
{
    "--no-headers",
    "--nh",
    "-n"
}, "Indicates that the input file does not have a header row");
rootCommand.Add(noHeaderOption);

var rowsToSkipOption = new Option<int>(new[]
{
    "--rows-to-skip",
    "--skip",
}, () => 0, "The number of input rows to skip.  It only applies to XSLX files and must be >= 0");
rootCommand.Add(rowsToSkipOption);

var rowsToTrimOption = new Option<int>(new[]
{
    "--rows-to-trim",
    "--trim",
}, () => 0, "The number of input rows to trim from the end of the data set.  It only applies to XSLX files and must be >= 0");
rootCommand.Add(rowsToTrimOption);

var worksheetOption = new Option<string>(new[]
{
    "--worksheet",
    "--sheet",
    "--w",
    "-w"
}, "Indicates the name or index of the worksheet to process.  It only applies to XSLX files.  By default, the active worksheet in the XLSX file is used");
rootCommand.Add(worksheetOption);

var fileArgument = new Argument<FileInfo>("file", "The input file to process");
fileArgument.AddValidator(result =>
{
    switch (result.GetValueForArgument(fileArgument).Extension.ToLower())
    {
        case ".csv":
        case ".xlsx":
            break;
        default:
            result.ErrorMessage = "Only CSV and XLSX input files are supported";
            break;
    }
});
rootCommand.Add(fileArgument);

summaryOption.AddValidator(result =>
{
    if (string.IsNullOrEmpty(result.GetValueForOption(summaryOption)?.FullName))
    {
        return;
    }
    switch (result.GetValueForArgument(fileArgument).Extension.ToLower())
    {
        case "":
        case ".csv":
            break;

        default:
            result.ErrorMessage = "This option is only valid for CSV files";
            break;
    }
});

worksheetOption.AddValidator(result =>
{
    switch (result.GetValueForArgument(fileArgument).Extension.ToLower())
    {
        case "":
        case ".xslx":
            break;

        default:
            result.ErrorMessage = "This option is only valid for XSLX files";
            break;
    }
});

rowsToSkipOption.AddValidator(result =>
{
    if (result.GetValueForOption(rowsToSkipOption) < 0)
    {
        result.ErrorMessage = "Value must be >= 0";
    }
    switch (result.GetValueForArgument(fileArgument)?.Extension.ToLower() ?? "")
    {
        case "":
        case ".xslx":
            break;

        default:
            result.ErrorMessage = "This option is only valid for XSLX files";
            break;
    }
});

rowsToTrimOption.AddValidator(result =>
{
    if (result.GetValueForOption(rowsToTrimOption) < 0)
    {
        result.ErrorMessage = "Value must be >= 0";
    }
    switch (result.GetValueForArgument(fileArgument)?.Extension.ToLower() ?? "")
    {
        case "":
        case ".xslx":
            break;

        default:
            result.ErrorMessage = "This option is only valid for XSLX files";
            break;
    }
});

rootCommand.SetHandler(context =>
{
    var inputFilename = context.GetValue(fileArgument).FullName;
    var (processor, options) = FileProcessor.Create(Path.GetExtension(inputFilename));

    bool hasProgress = false;
    options.OnRowRead = (reader) =>
    {
        if (reader.CurrentRowIndex % 10 == 0)
        {
            hasProgress = true;
            context.Console.Write(".");
        }
    };
    options.OnSummaryMismatch = args =>
    {
        context.Console.Error.Write($"{Environment.NewLine}Person [{args.PersonId}] has mismatched entries: [{args.OldData.FirstName}]-[{args.OldData.CountryCode}] => {args.OldData.Gender}, {args.OldData.Accuracy} vs. [{args.NewData.FirstName}]-[{args.NewData.CountryCode}] => {args.NewData.Gender}, {args.NewData.Accuracy}{Environment.NewLine}");
    };

    options.CountryCode = context.GetColumnInfo(countryOption);
    options.DisclosureId = context.GetColumnInfo(disclosureOption);
    options.FirstName = context.GetColumnInfo(firstNameOption);
    options.HasHeaders = !context.GetValue(noHeaderOption);
    options.InputFileName = inputFilename;
    options.OutputFileName = context.GetValue(outputOption)?.FullName;
    options.PersonId = context.GetColumnInfo(personOption);

    // TODO: Find a better way to handle this
    switch (options)
    {
        case CsvProcessorOptions csvOptions:
            csvOptions.SummaryFileName = context.GetValue(summaryOption)?.FullName;
            break;

        case XlsxProcessorOptions xlsxOptions:
            xlsxOptions.ReaderOptions = new XlsxReaderOptions
            {
                HasHeaderRow = options.HasHeaders,
                RowsToSkip = context.GetValue(rowsToSkipOption),
                RowsToTrim = context.GetValue(rowsToTrimOption),
                Worksheet = context.GetColumnInfo(worksheetOption)
            };
            break;
    }

    context.Console.WriteLine($"Processing \"{inputFilename}\"");
    var dataFile = context.GetValue(dataFileOption);
    processor.Process(dataFile is null
        ? new Processor()
        : new Processor(dataFile.FullName), options);

    if (hasProgress)
    {
        context.Console.Write(Environment.NewLine);
    }
    context.Console.WriteLine($"Results written to \"{options.OutputFileName}\"");
    {
        if (options is CsvProcessorOptions csvOptions)
        {
            context.Console.WriteLine($"Summary written to \"{csvOptions.SummaryFileName}\"");
        }
    }
});

return rootCommand.Invoke(args);
