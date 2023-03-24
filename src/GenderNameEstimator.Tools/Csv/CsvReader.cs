using System.Text;

namespace GenderNameEstimator.Tools.Csv;

public class CsvReader : TextRecordReader
{
    private readonly StringBuilder _buffer = new();
    private readonly StringBuilder _rawLine = new();
    private TextReader? _reader = null;
    private bool _closeReader = false;

    // TODO: Include setting for end-of-line sequence?

    public char Delimiter { get; }

    public char Quote { get; }

    public static CsvReader Create(string fileName)
    {
        return Create(fileName, true);
    }

    public static CsvReader Create(string fileName, bool hasHeaders)
    {
        return Create(fileName, hasHeaders, ',', '"');
    }

    public static CsvReader Create(string fileName, bool hasHeaders, char delimiter, char quote)
    {
        var result = new CsvReader(hasHeaders, delimiter, quote);
        try
        {
            result.Open(new StreamReader(fileName, Encoding.UTF8, true), true);
            return result;
        }
        catch
        {
            result.Dispose();
            throw;
        }
    }

    public static CsvReader Create(TextReader reader)
    {
        return Create(reader, true);
    }

    public static CsvReader Create(TextReader reader, bool hasHeaders)
    {
        return Create(reader, hasHeaders, ',', '"');
    }

    public static CsvReader Create(TextReader reader, bool hasHeaders, char delimiter, char quote)
    {
        var result = new CsvReader(hasHeaders, delimiter, quote);
        try
        {
            result.Open(reader);
            return result;
        }
        catch
        {
            result.Dispose();
            throw;
        }
    }

    public CsvReader() : this(true)
    {

    }

    // TODO:  Make all options part of opening the file and move open semantics to the constructors for RAII.  Given the number of options, they would best be wrapped in a class or struct.
    public CsvReader(bool hasHeaders) : this(hasHeaders, ',', '"')
    {

    }

    public CsvReader(bool hasHeaders, char delimiter, char quote)
    {
        HasHeaders = hasHeaders;
        Delimiter = delimiter;
        Quote = quote;
    }

    public override void Close()
    {
        if (_closeReader && _reader is not null)
        {
            _reader.Close();
        }
        _reader = null;
        _closeReader = false;
        base.Close();
    }

    public void Open(string filename)
    {
        Open(new StreamReader(filename, Encoding.UTF8, true), true);
    }

    public void Open(string filename, Encoding encoding)
    {
        Open(new StreamReader(filename, encoding), true);
    }

    public void Open(Stream stream, bool closeStream = false)
    {
        Open(new StreamReader(stream, Encoding.UTF8, true, leaveOpen: !closeStream), true);
    }

    public void Open(Stream stream, Encoding encoding, bool closeStream = false)
    {
        Open(new StreamReader(stream, encoding, leaveOpen: !closeStream), true);
    }

    public void Open(TextReader reader, bool closeReader = false)
    {
        if (_closeReader && _reader is not null)
        {
            _reader.Close();
        }
        _reader = reader;
        _closeReader = closeReader;
        CurrentRowIndex = -1;
        if (HasHeaders && OnReadNext())
        {
            RawHeaderLine = _rawLine.ToString();
            Headers = new Dictionary<string, int>(CurrentRecord.Count);
            for (int i = 0; i < CurrentRecord.Count; i++)
            {
                var s = CurrentRecord[i];
                if (!Headers.ContainsKey(s))
                {
                    Headers[s] = i;
                }
            }
        }
        else
        {
            RawHeaderLine = null;
            Headers = null;
        }
    }

    protected override bool OnReadNext()
    {
        if (_reader is null)
        {
            return false;
        }
        CurrentRecord.Clear();
        _buffer.Clear();
        _rawLine.Clear();
        var state = ParseState.BeginningOfField;
        while (true)
        {
            int read = _reader.Read();
            if (read == -1)
            {
                if (CurrentRecord.Count == 0 && _buffer.Length == 0)
                {
                    return false;
                }
                CurrentRecord.Add(_buffer.ToString());
                CurrentRowIndex++;
                return true;
            }

            // TODO: Keep track of and allow access to the raw input string?
            var c = (char)read;
            _rawLine.Append(c);
            switch (state)
            {
                case ParseState.BeginningOfField:
                    if (c == Delimiter)
                    {
                        CurrentRecord.Add("");
                    }
                    else if (c == Quote)
                    {
                        state = ParseState.InQuotes;
                    }
                    else if (c == '\n' || c == '\r')
                    {
                        if (CurrentRecord.Count != 0 || _buffer.Length != 0)
                        {
                            CurrentRecord.Add(_buffer.ToString());
                            CurrentRowIndex++;
                            return true;
                        }
                        // TODO: Make skipping blank lines optional?
                    }
                    else
                    {
                        _buffer.Append(c);
                        state = ParseState.OutsideOfQuotes;
                    }
                    break;

                case ParseState.OutsideOfQuotes:
                    if (c == Delimiter)
                    {
                        CurrentRecord.Add(_buffer.ToString());
                        _buffer.Clear();
                        state = ParseState.BeginningOfField;
                    }
                    else if (c == '\n' || c == '\r')
                    {
                        CurrentRecord.Add(_buffer.ToString());
                        CurrentRowIndex++;
                        return true;
                    }
                    else
                    {
                        _buffer.Append(c);
                    }
                    break;

                case ParseState.InQuotes:
                    if (c == Quote)
                    {
                        state = ParseState.PossibleEndQuote;
                    }
                    else
                    {
                        _buffer.Append(c);
                    }
                    break;

                case ParseState.PossibleEndQuote:
                    if (c == Delimiter)
                    {
                        CurrentRecord.Add(_buffer.ToString());
                        _buffer.Clear();
                        state = ParseState.BeginningOfField;
                    }
                    else if (c == Quote)
                    {
                        _buffer.Append(c);
                        state = ParseState.InQuotes;
                    }
                    else if (c == '\n' || c == '\r')
                    {
                        CurrentRecord.Add(_buffer.ToString());
                        CurrentRowIndex++;
                        return true;
                    }
                    else
                    {
                        _buffer.Append(Quote);
                        _buffer.Append(c);
                        state = ParseState.OutsideOfQuotes;
                    }
                    break;
            }
        }
    }

    public string RawLine => _rawLine.ToString();

    public string? RawHeaderLine { get; private set; } = null;

    private enum ParseState
    {
        BeginningOfField = 0,
        OutsideOfQuotes = 1,
        InQuotes = 2,
        PossibleEndQuote = 3
    }
}
