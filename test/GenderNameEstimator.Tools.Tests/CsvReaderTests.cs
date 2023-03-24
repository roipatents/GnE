public class CsvReaderTests
{
    private const string SUFFIX = ": {m}{p}";

    [TestCase("abc,1,3.5",
        TestName = "No quotes" + SUFFIX,
        ExpectedResult = new[] { "abc", "1", "3.5" })]
    [TestCase("abc\n,1,3.5",
        TestName = "No quotes EOL" + SUFFIX,
        ExpectedResult = new[] { "abc" })]
    [TestCase("\"abc\",1,3.5",
        TestName = "Quoted value" + SUFFIX,
        ExpectedResult = new[] { "abc", "1", "3.5" })]
    [TestCase("\"abc,1\",3.5",
        TestName = "Quoted delimiter" + SUFFIX,
        ExpectedResult = new[] { "abc,1", "3.5" })]
    [TestCase("\"abc\r\n\",1,3.5",
        TestName = "Quoted EOL" + SUFFIX,
        ExpectedResult = new[] { "abc\r\n", "1", "3.5" })]
    [TestCase("ab\"c,1,3.5",
        TestName = "Embedded quote" + SUFFIX,
        ExpectedResult = new[] { "ab\"c", "1", "3.5" })]
    [TestCase("\"ab\"\"c\",1,3.5",
        TestName = "Quoted doubled quotes" + SUFFIX,
        ExpectedResult = new[] { "ab\"c", "1", "3.5" })]
    [TestCase("\"ab\"c\",1,3.5",
        TestName = "Quoted single quote" + SUFFIX,
        ExpectedResult = new[] { "ab\"c\"", "1", "3.5" })]
    [TestCase("\r\n\n\rabc,1,3.5",
        TestName = "Empty lines are ignored" + SUFFIX,
        ExpectedResult = new[] { "abc", "1", "3.5" })]
    [TestCase("abc,1,3.5,",
        TestName = "Trailing delimiter" + SUFFIX,
        ExpectedResult = new[] { "abc", "1", "3.5", "" })]
    [TestCase(",abc,1,3.5",
        TestName = "Leading delimiter" + SUFFIX,
        ExpectedResult = new[] { "", "abc", "1", "3.5" })]
    public string[] ParsingRules(string input)
    {
        using var reader = new CsvReader(false);
        reader.Open(new StringReader(input), true);
        Assert.That(reader.ReadNext(), Is.True);
        return reader.GetValues();
    }

    [TestCase("")]
    [TestCase("\n")]
    [TestCase("\r\n")]
    [TestCase("\n\r")]
    [TestCase("\n\n")]
    public void BlankLinesAreSkipped(string input)
    {
        using var reader = new CsvReader(false);
        reader.Open(new StringReader(input), true);
        Assert.That(reader.ReadNext(), Is.False);
    }
}
