namespace GenderNameEstimator.Tools;

public abstract class FileProcessor
{
    // TODO: This is clunky; use dependency injection / factories and encapsulation instead of inheritance.  Even having subclasses register factory methods for themselves is still unwieldy
    public static (FileProcessor, FileProcessorOptions) Create(string extension)
    {
        switch (extension.ToLower())
        {
            case ".csv":
                return (new Csv.CsvProcessor(), new Csv.CsvProcessorOptions());

            case ".xlsx":
                return (new Xlsx.XlsxProcessor(), new Xlsx.XlsxProcessorOptions());

            default:
                throw new InvalidOperationException($"Cannot process files with extension: '{extension}'");
        }
    }

    protected static string GetColumnNameForSummary(FieldInfo field, TextRecordReader reader)
    {
        if (string.IsNullOrEmpty(field.Name))
        {
            if (field.Index is null)
            {
                return "unused";
            }
            if (reader.HasHeaders)
            {
                var name = reader.GetName(field.Index.Value);
                if (name is null)
                {
                    return "unknown";
                }
                return $"'{name}'";
            }
            return field.Index.Value.ToString();
        }
        return $"'{field.Name}'";
    }

    private static string? GetValueOrNull(TextRecordReader reader, int index)
    {
        if (index < 0)
        {
            return null;
        }
        return reader[index];
    }

    protected static void ValidateOptions(FileProcessorOptions options)
    {
        if (string.IsNullOrEmpty(options.InputFileName))
        {
#pragma warning disable CA2208 // Instantiate argument exceptions correctly
            throw new ArgumentException("Input File is required", $"{nameof(options)}.{nameof(options.InputFileName)}");
#pragma warning restore CA2208 // Instantiate argument exceptions correctly
        }
    }

    protected static IEnumerable<DataRecord> GetDataRecords(Processor processor, FileProcessorOptions options, TextRecordReader reader, SummaryInfo summaryInfo)
    {
        var firstNameIndex = options.FirstName.GetIndex(reader) ?? throw new InvalidOperationException("First Name Index could not be determined");
        var countryCodeIndex = options.CountryCode.GetIndex(reader) ?? throw new InvalidOperationException("Country Code Index could not be determined");
        var personIdIndex = options.PersonId.GetIndex(reader) ?? -1;
        var disclosureIdIndex = options.DisclosureId.GetIndex(reader) ?? -1;

        var onSummaryMismatch = options.OnSummaryMismatch;
        if (onSummaryMismatch is not null)
        {
            summaryInfo.SummaryMismatch += (sender, args) => onSummaryMismatch(args);
        }

        var onRowRead = options.OnRowRead;
        if (onRowRead is not null)
        {
            reader.RowRead += (_, _) => onRowRead(reader);
        }

        foreach (var record in processor.Process(reader, firstNameIndex, countryCodeIndex))
        {
            yield return record;
            summaryInfo.Add(GetValueOrNull(reader, personIdIndex), GetValueOrNull(reader, disclosureIdIndex), record);
        }
    }

    public abstract void Process(Processor processor, FileProcessorOptions options);
}

public class FileProcessorOptions
{
    private string? _outputFileName;

    public string? InputFileName { get; set; }

    public string? OutputFileName
    {
        get => string.IsNullOrEmpty(_outputFileName) && !string.IsNullOrEmpty(InputFileName)
            ? Path.Join(Path.GetDirectoryName(InputFileName), $"{Path.GetFileNameWithoutExtension(InputFileName)}-appended{Path.GetExtension(InputFileName)}")
            : _outputFileName;
        set => _outputFileName = value;
    }

    public bool HasHeaders { get; set; } = true;

    public FieldInfo FirstName { get; set; }

    public FieldInfo CountryCode { get; set; }

    public FieldInfo PersonId { get; set; }

    public FieldInfo DisclosureId { get; set; }

    public Action<TextRecordReader>? OnRowRead { get; set; }

    public Action<SummaryInfo.MismatchEventArgs>? OnSummaryMismatch { get; set; }
}

public struct FieldInfo
{
    public string? Name { get; set; }

    public int? Index { get; set; }

    public static FieldInfo Empty { get; } = new();

    public static implicit operator FieldInfo(string name)
    {
        return new FieldInfo
        {
            Name = name
        };
    }

    public static implicit operator FieldInfo(int index)
    {
        return new FieldInfo
        {
            Index = index
        };
    }

    public static implicit operator FieldInfo(int? index)
    {
        return new FieldInfo
        {
            Index = index
        };
    }

    public int? GetIndex(TextRecordReader reader)
    {
        if (Index is not null)
        {
            return Index;
        }
        if (!string.IsNullOrEmpty(Name) && reader.HasHeaders && reader.Headers is not null && reader.Headers.TryGetValue(Name, out var index))
        {
            return index;
        }
        return null;
    }
}
