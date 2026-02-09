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
namespace GenderNameEstimator.Tools;

public class SummaryInfo
{
    public Dictionary<string, HashSet<string>> UniqueDisclosurePeople { get; } = new(StringComparer.InvariantCultureIgnoreCase);

    public Dictionary<string, DataRecord> PeopleRecords { get; } = new(StringComparer.InvariantCultureIgnoreCase);

    public int RowCount { get; set; } = 0;

    public class MismatchEventArgs : EventArgs
    {
        public MismatchEventArgs(string personId, DataRecord oldData, DataRecord newData)
        {
            PersonId = personId;
            OldData = oldData;
            NewData = newData;
        }

        public string PersonId { get; }

        public DataRecord OldData { get; }

        public DataRecord NewData { get; }
    }

    public event EventHandler<MismatchEventArgs>? SummaryMismatch;

    public void Add(string? personId, string? disclosureId, DataRecord newDataRecord)
    {
        RowCount++;
        if (string.IsNullOrEmpty(personId))
        {
            personId = $"{newDataRecord.FirstName}\u001F{newDataRecord.CountryCode}";
        }
        if (PeopleRecords.TryGetValue(personId, out var currentDataRecord))
        {
            if (currentDataRecord.Gender != newDataRecord.Gender)
            {
                SummaryMismatch?.Invoke(this, new MismatchEventArgs(personId, currentDataRecord, newDataRecord));
                currentDataRecord.Gender = Gender.Indeterminate;
                // NOTE: DataRecord is a value type, so reassignment is necessary
                PeopleRecords[personId] = currentDataRecord;
            }
        }
        else
        {
            PeopleRecords[personId] = newDataRecord;
        }

        if (string.IsNullOrEmpty(disclosureId))
        {
            disclosureId = $"{RowCount}\u001F";
        }
        if (!UniqueDisclosurePeople.TryGetValue(disclosureId, out var disclosurePeople))
        {
            UniqueDisclosurePeople[disclosureId] = disclosurePeople = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
        }
        disclosurePeople.Add(personId);
    }

    public static bool PersonIsAWoman(DataRecord dataRecord) => dataRecord.Gender == Gender.Woman;

    public bool PersonIsAWoman(string id) => PeopleRecords.TryGetValue(id, out var dataRecord) && PersonIsAWoman(dataRecord);

    public static bool PersonIsAMan(DataRecord dataRecord) => dataRecord.Gender == Gender.Man;

    public bool PersonIsAMan(string id) => PeopleRecords.TryGetValue(id, out var dataRecord) && PersonIsAMan(dataRecord);

    public static bool PersonIsUndetermined(DataRecord dataRecord) => dataRecord.Gender != Gender.Woman && dataRecord.Gender != Gender.Man;

    public bool PersonIsUndetermined(string id) => !PeopleRecords.TryGetValue(id, out var dataRecord) || PersonIsUndetermined(dataRecord);

    public CountAndPercentage PeopleWhere(Func<DataRecord, bool> predicate)
    {
        return new CountAndPercentage(PeopleRecords.Count(kvp => predicate(kvp.Value)), PeopleRecords.Count);
    }

    public (
        CountAndPercentage all,
        CountAndPercentage women,
        CountAndPercentage men,
        CountAndPercentage undetermined) GetInventorRate()
    {
        return (
            new CountAndPercentage(PeopleRecords.Count),
            PeopleWhere(PersonIsAWoman),
            PeopleWhere(PersonIsAMan),
            PeopleWhere(PersonIsUndetermined)
        );
    }

    public bool AtLeastOneWoman(HashSet<string> people)
    {
        return people.Any(PersonIsAWoman);
    }

    public bool AtLeastOneMan(HashSet<string> people)
    {
        return people.Any(PersonIsAMan);
    }

    public bool AtLeastOneUndetermined(HashSet<string> people)
    {
        return people.Any(PersonIsUndetermined);
    }

    public bool SoloWoman(HashSet<string> people)
    {
        return people.Count == 1 && PersonIsAWoman(people.First());
    }

    public bool SoloMan(HashSet<string> people)
    {
        return people.Count == 1 && PersonIsAMan(people.First());
    }

    public CountAndPercentage DisclosuresWhere(Func<HashSet<string>, bool> predicate)
    {
        return new CountAndPercentage(UniqueDisclosurePeople.Count(kvp => predicate(kvp.Value)), UniqueDisclosurePeople.Count);
    }

    public (
        CountAndPercentage all,
        CountAndPercentage atLeastOneWoman,
        CountAndPercentage atLeastOneMan,
        CountAndPercentage atLeastOneUndetermined,
        CountAndPercentage soloWoman,
        CountAndPercentage soloMan) GetDisclosureOutput()
    {
        return (
            new CountAndPercentage(UniqueDisclosurePeople.Count),
            DisclosuresWhere(AtLeastOneWoman),
            DisclosuresWhere(AtLeastOneMan),
            DisclosuresWhere(AtLeastOneUndetermined),
            DisclosuresWhere(SoloWoman),
            DisclosuresWhere(SoloMan)
        );
    }

    public double GetWeightedSum(Func<string, bool> predicate)
    {
        return UniqueDisclosurePeople.Sum(kvp => ((double)kvp.Value.Count(predicate)) / kvp.Value.Count);
    }

    public (
        double all,
        double women,
        double men,
        double undetermined) GetFractionalInventorship()
    {
        return (
            UniqueDisclosurePeople.Count,
            GetWeightedSum(PersonIsAWoman),
            GetWeightedSum(PersonIsAMan),
            GetWeightedSum(PersonIsUndetermined)
        );
    }
}

public struct CountAndPercentage
{
    public CountAndPercentage(int total)
    {
        Count = total;
        Percentage = 1.0;
    }

    public CountAndPercentage(int count, int total)
    {
        Count = count;
        Percentage = ((double)count) / total;
    }

    public int Count { get; }

    public double Percentage { get; }
}
