namespace GenderNameEstimator.UI.Mac;

public partial class FileChooserViewController : WizardOutlineViewController
{
    private string? _buttonText = "";

    private FileChooserViewModel? _model;

    private bool _enabled;

    public static FileChooserViewController Create(
        FileChooserViewModel model,
        string buttonText,
        params UniformTypeIdentifiers.UTType[] allowedContentTypes)
    {
        return Create(model, buttonText, false, allowedContentTypes);
    }

    public static FileChooserViewController Create(
        FileChooserViewModel model,
        string buttonText,
        bool multiple,
        params UniformTypeIdentifiers.UTType[] allowedContentTypes)
    {
        var result = Instantiate<FileChooserViewController>();
        result.Model = model;
        result.ButtonText = buttonText;
        result.Multiple = multiple;
        result.AllowedContentTypes = allowedContentTypes;
        return result;
    }

    public FileChooserViewController(ObjCRuntime.NativeHandle handle) : base(handle)
    {
    }

#pragma warning disable CA1416 // Validate platform compatibility
    public UniformTypeIdentifiers.UTType[] AllowedContentTypes { get; set; } = Array.Empty<UniformTypeIdentifiers.UTType>();
#pragma warning restore CA1416 // Validate platform compatibility

    public override void ViewDidLoad()
    {
        SizeToFit();
    }

    public override void ViewDidAppear()
    {
        base.ViewDidAppear();
        if (!IsCompleted && View.Window is not null && OpenFileButton is not null && OpenFileButton.AcceptsFirstResponder())
        {
            View.Window.MakeFirstResponder(OpenFileButton);
        }
    }

    [Export(nameof(ButtonText))]
    public string? ButtonText
    {
        get
        {
            return _buttonText;
        }
        set
        {
            this.ChangeStringField(ref _buttonText, value, t => t.ButtonText);
        }
    }

    public bool Multiple { get; set; }

    [Export(nameof(Model))]
    public FileChooserViewModel? Model
    {
        get => _model;
        set
        {
            if (this.ChangeField(ref _model, value, t => t.Model, t => t.IsCompleted))
            {
                ClearObservers();
                if (_model is not null)
                {
                    AddObservers(this.Properties(t => t.IsCompleted).DependOn(_model.Properties(m => m.File)));
                    AddObservers(_model.WhenPropertiesChange(SizeToFit, m => m.Files));
                }
            };
        }
    }

    private void SizeToFit()
    {
        FileList?.SizeToFit();
    }

    public override bool IsCompleted => _model?.File != null;

    partial void OnButtonClicked(NSObject sender)
    {
        if (_model is null)
        {
            return;
        }
        var chooser = NSOpenPanel.OpenPanel;
#pragma warning disable CA1416 // Validate platform compatibility
        chooser.AllowedContentTypes = AllowedContentTypes;
#pragma warning restore CA1416 // Validate platform compatibility
        chooser.AllowsMultipleSelection = Multiple;
        chooser.AllowsOtherFileTypes = false;
        chooser.CanChooseDirectories = false;
        chooser.CanChooseFiles = true;
        chooser.CanCreateDirectories = false;
        chooser.ShowsHiddenFiles = false;
        chooser.ShowsResizeIndicator = true;
        chooser.Title = _buttonText ?? "";
        var lastFilename = _model.Files.LastOrDefault<FileItem>()?.Name;
        var currentPath = string.IsNullOrEmpty(lastFilename)
            ? null
            : Path.GetDirectoryName(lastFilename);
        if (!string.IsNullOrWhiteSpace(currentPath) && Directory.Exists(currentPath))
        {
            chooser.DirectoryUrl = NSUrl.CreateFileUrl(currentPath, true, null);
        }
        if (chooser.RunModal() == (int)NSModalResponse.OK)
        {
            if (Multiple)
            {
                // TODO: Allow the user to rearrange files?
                _model.AddFiles(chooser.Urls);
            }
            else
            {
                _model.File = new FileItem(chooser.Url);
            }
        }
    }

    public override void OnLinkCommand(string command)
    {
        switch (command)
        {
            case "choose":
                OnButtonClicked(null);
                break;
            default:
                base.OnLinkCommand(command);
                break;
        }
    }

    public override void KeyDown(NSEvent theEvent)
    {
        if (theEvent.KeyCode == (ushort)NSKey.Delete && FileList.SelectedRow >= 0)
        {
            _model?.RemoveFiles(FileList.SelectedRows);
        }
        else
        {
            base.KeyDown(theEvent);
        }
    }

    public override bool IsEnabled => _enabled;

    public void SetEnabled(bool value)
    {
        this.ChangeField(ref _enabled, value, t => t.IsEnabled);
    }

    public void ChooseFile()
    {
        OnButtonClicked(null);
    }
}

[Register(nameof(FileChooserViewModel))]
public class FileChooserViewModel : NSObject
{
    [Export(nameof(File))]
    public FileItem? File
    {
        get
        {
            return Files.AsEnumerable().FirstOrDefault();
        }
        set
        {
            if (value is not null)
            {
                if (Files.Count == 1 && Files[0] == value)
                {
                    return;
                }
            }
            else
            {
                if (Files.Count == 0 || (Files.Count == 1 && Files[0] is null))
                {
                    return;
                }
            }
            using (this.ChangeSection(t => t.File, t => t.Files))
            {
                Files.RemoveAllObjects();
                if (value is not null)
                {
                    Files.Add(value);
                }
            }
        }
    }

    [Export(nameof(Files))]
    public NSMutableArray<FileItem> Files { get; } = new();

    public void AddFile(string filename)
    {
        using (this.ChangeSection(t => t.File, t => t.Files))
        {
            Files.Add(new FileItem(filename));
        }
    }

    public void AddFiles(IEnumerable<NSUrl> urls)
    {
        using (this.ChangeSection(t => t.File, t => t.Files))
        {
            Files.AddObjects(urls.Select(url => new FileItem(url)).ToArray());
        }
    }

    public void AddFiles(IEnumerable<string> filenames)
    {
        using (this.ChangeSection(t => t.File, t => t.Files))
        {
            Files.AddObjects(filenames.Select(filename => new FileItem(filename)).ToArray());
        }
    }

    public void InsertFile(string filename, nint index)
    {
        using (this.ChangeSection(t => t.File, t => t.Files))
        {
            Files.Insert(new FileItem(filename), index);
        }
    }

    public void RemoveFile(nint index)
    {
        using (this.ChangeSection(t => t.File, t => t.Files))
        {
            Files.RemoveObject(index);
        }
    }

    public void RemoveFiles(NSIndexSet indexes)
    {
        using (this.ChangeSection(t => t.File, t => t.Files))
        {
            Files.RemoveObjectsAtIndexes(indexes);
        }
    }

    public void ClearFiles()
    {
        if (Files.Count == 0)
        {
            return;
        }
        using (this.ChangeSection(t => t.File, t => t.Files))
        {
            Files.RemoveAllObjects();
        }
    }
}
