public class TemporaryFile : IDisposable
{
    public string FileName { get; }

    public TemporaryFile(string? extension = null)
    {
        FileName = Path.GetTempFileName();
        if (!string.IsNullOrEmpty(extension))
        {
            var oldName = FileName;
            try
            {
                FileName += extension;
                if (File.Exists(oldName))
                {
                    File.Move(oldName, FileName);
                }
            }
            finally
            {
                if (File.Exists(oldName))
                {
                    File.Delete(oldName);
                }
            }
        }
    }

    public void Dispose()
    {
        if (File.Exists(FileName))
        {
            File.Delete(FileName);
        }
        GC.SuppressFinalize(this);
    }

    public override string ToString()
    {
        return FileName;
    }

    public static implicit operator string(TemporaryFile temporaryFile)
    {
        return temporaryFile.ToString();
    }
}
