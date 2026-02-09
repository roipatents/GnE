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
            _data[$"{record.FirstName.Trim()}\u001F{record.CountryCode?.Trim()}"] = record;
        }
    }

    private static int GetFieldIndex(CsvReader reader, string name, int defaultIndex)
    {
        var i = reader.GetOrdinal(name);
        return i == -1 ? defaultIndex : i;
    }

    public DataRecord GetDataRecord(string? firstName, string? countryCode)
    {
        return _data.TryGetValue($"{firstName?.Trim()}\u001F{countryCode?.Trim()}", out var record)
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
