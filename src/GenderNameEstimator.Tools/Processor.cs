using System.Reflection;

using GenderNameEstimator.Tools.Csv;

namespace GenderNameEstimator.Tools;

public class Processor
{
    private Dictionary<string, DataRecord> _data = new(StringComparer.InvariantCultureIgnoreCase);

    public Processor(string dataFile = "wgnd_2_0_name-gender-code.csv")
    {
        if (!Path.IsPathRooted(dataFile))
        {
            dataFile = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? AppDomain.CurrentDomain.BaseDirectory, dataFile));
        }
        using var reader = new CsvReader();
        reader.Open(dataFile);
        Load(reader);
    }

    public Processor(CsvReader reader)
    {
        Load(reader);
    }

    private void Load(CsvReader reader)
    {
        var nameIndex = GetFieldIndex(reader, "name", 0);
        var codeIndex = GetFieldIndex(reader, "code", 1);
        var genderIndex = GetFieldIndex(reader, "gender", 2);
        var wgtIndex = GetFieldIndex(reader, "wgt", 3);
        // NOTE: Data should be sorted by name, code, and gender, otherwise, we would have to look up the previous winner each time
        var winner = new DataRecord();
        while (reader.ReadNext())
        {
            var record = new DataRecord
            {
                FirstName = reader[nameIndex],
                CountryCode = reader[codeIndex],
                Gender = reader[genderIndex]?[0] ?? Gender.Unknown,
                Accuracy = decimal.TryParse(reader[wgtIndex] ?? "0", out var decimalWeight) ? decimalWeight
                    : double.TryParse(reader[wgtIndex], out var doubleWeight) ? (decimal)doubleWeight
                    : 0m
            };
            // TODO: Do we want to include an error margin to represent indeterminate entries when combined with other entries with a greater weight?
            if (winner.FirstName == record.FirstName && winner.CountryCode == record.CountryCode)
            {
                // TODO: Do we need to take into account epsilon comparisons?  Decimal doesn't inherently have this issue, but the round off error in the file might
                if (record.Accuracy > winner.Accuracy)
                {
                    winner = record;
                }
                else if (record.Accuracy == winner.Accuracy)
                {
                    // Duplicate rows are not expected
                    // Equal likelihoods of being a man, a woman, or unknown / anything else become indeterminate
                    winner.Gender = Gender.Indeterminate;
                }
            }
            else
            {
                AddRecord(winner);
                winner = record;
            }
        }
        AddRecord(winner);
    }

    private void AddRecord(DataRecord record)
    {
        if (!string.IsNullOrEmpty(record.FirstName))
        {
            _data[$"{record.FirstName}\u001F{record.CountryCode}"] = record;
        }
    }

    private static int GetFieldIndex(CsvReader reader, string name, int defaultIndex)
    {
        var i = reader.GetOrdinal(name);
        return i == -1 ? defaultIndex : i;
    }

    public DataRecord GetDataRecord(string? firstName, string? countryCode)
    {
        return _data.TryGetValue($"{firstName}\u001F{countryCode}", out var record)
            ? record
            : DataRecord.NotFound;
    }

    public IEnumerable<DataRecord> Process(TextRecordReader reader, string firstNameField, string countryCodeField)
    {
        return Process(reader, reader.GetOrdinal(firstNameField), reader.GetOrdinal(countryCodeField));
    }

    public IEnumerable<DataRecord> Process(TextRecordReader reader, int firstNameIndex, int countryCodeIndex)
    {
        return reader.Select(record => GetDataRecord(record[firstNameIndex], record[countryCodeIndex]));
    }
}
