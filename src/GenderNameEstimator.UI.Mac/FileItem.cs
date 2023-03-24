namespace GenderNameEstimator.UI.Mac;

[Register(nameof(FileItem))]
public class FileItem : NSObject
{
    private string? _name;

    private long _size;

    private NSDate? _modified;

    public FileItem() : base()
    {
    }

    public FileItem(string filename) : this()
    {
        Name = filename;
        var fi = new FileInfo(filename);
        if (fi.Exists)
        {
            Size = fi.Length;
            Modified = (NSDate)fi.LastWriteTime;
        }
        else
        {
            Size = -1;
            Modified = NSDate.DistantFuture;
        }
    }

    public FileItem(NSUrl url) : this(url.IsFileUrl && url.Path is not null ? url.Path : throw new ArgumentException("Invalid URL Type", nameof(url)))
    {
    }

    [Export(nameof(Name))]
    public string? Name
    {
        get
        {
            return _name;
        }
        set
        {
            this.ChangeStringField(ref _name, value, t => t.Name);
        }
    }

    [Export(nameof(Size))]
    public long Size
    {
        get
        {
            return _size;
        }
        set
        {
            this.ChangeField(ref _size, value, t => t.Size);
        }
    }

    [Export(nameof(Modified))]
    public NSDate? Modified
    {
        get
        {
            return _modified;
        }
        set
        {
            this.ChangeField(ref _modified, value, t => t.Modified);
        }
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as FileItem);
    }

    public bool Equals(FileItem? obj)
    {
        return ReferenceEquals(this, obj) ||
            (string.IsNullOrEmpty(Name) && string.IsNullOrEmpty(obj?.Name)) ||
            Name == obj?.Name;
    }

    public override int GetHashCode()
    {
        return Name?.GetHashCode() ?? 0;
    }

    public override bool IsEqual(NSObject? anObject)
    {
        return Equals(anObject);
    }
}
