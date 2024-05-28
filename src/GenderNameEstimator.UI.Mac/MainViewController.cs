using Microsoft.Extensions.Logging;

using GenderNameEstimator.Tools;
using GenderNameEstimator.Tools.Csv;
using GenderNameEstimator.Tools.Xlsx;

namespace GenderNameEstimator.UI.Mac;

public partial class MainViewController : ViewControllerWithObservers
{

    private readonly WizardOutlineItem _outlineRoot;

    private WizardOutlineItem? _currentStep;

    private WizardOutlineViewController? _currentViewController;

    private readonly LinkCommandDelegateImpl _linkCommandDelegate;

    private readonly GenderNameEstimatorModel _model = new();

    private Task<Processor> _getProcessor;

    public GenderNameEstimatorModel Model { get => _model; }

    public MainViewController(ObjCRuntime.NativeHandle handle) : base(handle)
    {
        _linkCommandDelegate = new(this);

        bool processHasRun = false;
        Action resetHasRun = () => processHasRun = false;

        AddObservers(_model.SourceColumnChooserModel.WhenPropertiesChange(
            resetHasRun,
            m => m.Filename, m => m.RowsToSkip, m => m.RowsToTrim, m => m.Worksheet, m => m.Columns));
        // TODO: Come up with a better mechanism for managing observers on collections of items
        AddObservers(_model.SourceColumnChooserModel.WhenPropertiesChange(
            () => AddObservers(_model.SourceColumnChooserModel.Columns.AsEnumerable().WhenPropertiesOfAnyChange(resetHasRun, c => c.SelectedIndex)),
            m => m.Columns));
        AddObservers(_model.SourceColumnChooserModel.Columns.AsEnumerable().WhenPropertiesOfAnyChange(resetHasRun, c => c.SelectedIndex));

#pragma warning disable CA1416 // Validate platform compatibility
        var sourceFileChooser = FileChooserViewController.Create(
            _model.SourceFileChooserModel,
            "Choose Source Spreadsheet",
            UniformTypeIdentifiers.UTTypes.Spreadsheet,
            UniformTypeIdentifiers.UTTypes.CommaSeparatedText);
#pragma warning restore CA1416 // Validate platform compatibility
        sourceFileChooser.SetEnabled(true);

        AddObservers(_model.SourceFileChooserModel.WhenPropertiesChange(() =>
        {
            var file = _model.SourceFileChooserModel.File?.Name;
            _model.SourceColumnChooserModel.Filename = file ?? "";
        }, m => m.File));

        var sourceColumnChooser = SpreadsheetColumnChooserViewController.Create(_model.SourceColumnChooserModel);

        var gneMergeProcess = RunProcessViewController.Create("GnE Processing",
            (token, controller) =>
            {
                return Task.Run(() =>
                {
                    ProcessWGNDData(
                        _model.SourceColumnChooserModel,
                        controller,
                        token);
                    processHasRun = true;
                });
            },
            () => !string.IsNullOrEmpty(_model.SourceColumnChooserModel.Filename)
                && File.Exists(_model.SourceColumnChooserModel.Filename)
                && _model.SourceColumnChooserModel.Columns.AsEnumerable().All(column => !column.IsRequired || column.SelectedIndex >= 0));

        gneMergeProcess.Viewed += (sender, e) =>
        {
            if (processHasRun)
            {
                return;
            }
            gneMergeProcess.IsRunning = true;
        };

        AddObservers(gneMergeProcess.Properties(wmp => wmp.IsEnabled).DependOn(_model.SourceColumnChooserModel.Properties(m => m.Filename, m => m.Columns)));
        AddObservers(_model.SourceColumnChooserModel.WhenPropertiesChange(
            () => AddObservers(gneMergeProcess.Properties(wmp => wmp.IsEnabled).DependOn(
                _model.SourceColumnChooserModel.Columns.AsEnumerable().PropertiesOfAny(c => c.IsRequired, c => c.SelectedIndex).ToArray())),
            m => m.Columns));
        AddObservers(gneMergeProcess.Properties(wmp => wmp.IsEnabled).DependOn(
                        _model.SourceColumnChooserModel.Columns.AsEnumerable().PropertiesOfAny(c => c.IsRequired, c => c.SelectedIndex).ToArray()));

        var gneDataIntegrationStep = new WizardOutlineItem
        {
            Title = "GnE Integration",
            PromptText = @"Select Columns For Processing

Click within a column and select a role from the drop-down list.  The column header will be highlighted and will have a tool-tip to indicate your choice.

The following column roles are defined:

1.&emsp;First Name
&emsp;&emsp;This role is required and is used as part of the lookup into the Gender-Name model.

2.&emsp;Country Code
&emsp;&emsp;This role is required and is used as part of the lookup into the Gender-Name model.

3.&emsp;Person ID
&emsp;&emsp;This role is optional and is used to unqiuely identify a person when generating summary statistics.
&emsp;&emsp;If it is not provided, each row is considered a unique person.

4.&emsp;Disclosure / Patent ID
&emsp;&emsp;This role is optional and is used to unqiuely identify a disclosure / patent when generating summary statistics.
&emsp;&emsp;If it is not provided, each row is considered a unique disclosure / patent.",
            ViewController = sourceColumnChooser,
            Container =
                    {
                        new WizardOutlineItem
                        {
                            Title = "Run Merge of WGND Data",
                            PromptText = @"<a href=""run"">Run</a> the merge of the WGND data

Final file(s) based upon the source file name with suffixes will be generated for each step of the merge process:

For XLSX files, a single ""-appended"" suffixed file will be created, with additional columns added to the source worksheet and a ""Results"" worksheet added with a summary.

For CSV files, a ""-appended"" suffixed file will be created with the additional columns, and a ""-summary"" file will be created with a summary.",
                            ViewController = gneMergeProcess
                        }
                    }
        };

        sourceFileChooser.Completed += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(_model.SourceFileChooserModel.File?.Name))
            {
#pragma warning disable CS8604 // Possible null reference argument.
                NSDocumentController.SharedDocumentController.NoteNewRecentDocumentURL(new Uri(_model.SourceFileChooserModel.File.Name));
#pragma warning restore CS8604 // Possible null reference argument.
            }
            SelectItem(gneDataIntegrationStep);
            // TODO: Detect failure to open spreadsheet?
        };

        var info = NSBundle.MainBundle.InfoDictionary;
        var applicationInfo = $"{info["CFBundleName"]} ({info["CFBundleVersion"]})";
        _outlineRoot = new()
        {
            Container =
            {
                new WizardOutlineItem
                {
                    Title = "Overview",
                    PromptText = $@"GnE (pronounced ""Genie"" - the Gender-Name Estimator) is used to combine data from the World Gender-Name Dictionary model into a spreadsheet that contains first names and country codes.

The wizard steps will guide you through the process necessary to do so.

Please direct any feedback or questions to <a href=""mailto:ttrias@roipatents.com?subject={Uri.EscapeDataString(applicationInfo)}"">Tom Trias.</a>",
                    ViewController = sourceFileChooser
                },
                new WizardOutlineItem
                {
                    Title = "Open Source Spreadsheet",
                    PromptText = "<a href=\"choose\">Choose</a> a source spreadsheet with the set of first names and country codes.",
                    ViewController = sourceFileChooser
                },
                gneDataIntegrationStep,
            }
        };

        var finalReviewController = SpreadsheetColumnChooserViewController.Create(null);
        var finalReviewStep = new WizardOutlineItem
        {
            Title = "Review Completed Merge",
            PromptText = @"The Gender column contains one of the following result indicators:

&emsp;&emsp;""F"" - Woman - The person is most likely a woman based upon the model data.
&emsp;&emsp;""I"" - Indeterminate - There is equal maximum likelihood that the person has two or more possible results of woman, man, or unknown.
&emsp;&emsp;""M"" - Man - The persion is most likely a man based upon the model data.
&emsp;&emsp;""-"" - NotAvailable - The combination of First Name and Country of Origin are not contained in the model data.
&emsp;&emsp;""?"" - Unknown - The person's gender is unknown based upon the model data.

The Accuracy column contains the probability that the Gender is correct based upon the model expressed as a decimal between 0.0 and 1.0",
            ViewController = finalReviewController
        };

        _outlineRoot.Add(finalReviewStep);

        gneMergeProcess.Completed += (sender, e) =>
        {
            if (string.IsNullOrEmpty(_model.WGNDTargetFilename) || !File.Exists(_model.WGNDTargetFilename))
            {
                return;
            }
            finalReviewController.Model = new SpreadsheetColumnChooserViewModel(_model.SourceColumnChooserModel)
            {
                Filename = _model.WGNDTargetFilename,
                Worksheet = _model.WGNDTargetWorksheet
            };
            SelectItem(finalReviewStep);
            // TODO: Detect failure to open spreadsheet?
        };

        // TODO: Preferences for WGND source data
        _getProcessor = Task.Run(() => new Processor());
    }

    private void ProcessWGNDData(
        SpreadsheetColumnChooserViewModel sourceInformation,
        RunProcessViewController controller,
        CancellationToken token)
    {
        var mainProgress = controller.MainProgress;
        mainProgress.Label = "Opening Source Data";
        mainProgress.Current = 0;
        mainProgress.Maximum = 0;
        mainProgress.IsIndeterminate = true;
        mainProgress.IsAnimating = true;

        var (processor, options) = FileProcessor.Create(Path.GetExtension(sourceInformation.Filename) ?? "");
        using (var reader = sourceInformation.GetReader())
        {
            if (reader is null)
            {
                return;
            }

            mainProgress.Label = "Processing Source Data";
            if (reader.RowCount > 0)
            {
                mainProgress.Maximum = reader.RowCount;
                mainProgress.IsIndeterminate = false;
                options.OnRowRead = (_) => mainProgress.Current++;
            }

            _model.WGNDTargetWorksheet = reader is XlsxReader xlsxReader
                ? xlsxReader.CurrentWorksheetIndex + 1
                : -1;
        }

        var logger = controller.GetLogger("Post Process");
        options.OnSummaryMismatch = args => logger.LogWarning("Person [{personId}] has mismatched entries: [{OldFirstName}]-[{OldCountryCode}] => {OldGender}, {OldAccuracy} vs. [{NewFirstName}]-[{NewCountryCode}] => {NewGender}, {NewAccuracy}",
            args.PersonId,
            args.OldData.FirstName, args.OldData.CountryCode, args.OldData.Gender, args.OldData.Accuracy,
            args.NewData.FirstName, args.NewData.CountryCode, args.NewData.Gender, args.NewData.Accuracy);

        options.CountryCode = sourceInformation.GetColumnIndex("Country Code");
        options.DisclosureId = sourceInformation.GetColumnIndex("Disclosure / Patent ID");
        options.FirstName = sourceInformation.GetColumnIndex("First Name");
        options.HasHeaders = true;
        options.InputFileName = sourceInformation.Filename;
        options.PersonId = sourceInformation.GetColumnIndex("Person ID");

        // TODO: Find a better way to handle this
        switch (options)
        {
            case CsvProcessorOptions:
                break;

            case XlsxProcessorOptions xlsxOptions:
                xlsxOptions.ReaderOptions = new XlsxReaderOptions
                {
                    HasHeaderRow = options.HasHeaders,
                    RowsToSkip = sourceInformation.RowsToSkip,
                    RowsToTrim = sourceInformation.RowsToTrim,
                    Worksheet = sourceInformation.Worksheet >= 0 ? sourceInformation.Worksheet : Tools.FieldInfo.Empty
                };
                break;
        }

        processor.Process(_getProcessor.Result, options);
        _model.WGNDTargetFilename = options.OutputFileName;
    }

    [Action("openDocument:")]
    public void OpenDocument(NSObject sender)
    {
        foreach (var item in _outlineRoot.DescendantsOrSelf)
        {
            if (item.ViewController is FileChooserViewController chooser)
            {
                SelectItem(item);
                chooser.ChooseFile();
                return;
            }
        }
    }

    [Export(nameof(CurrentStep))]
    public WizardOutlineItem? CurrentStep
    {
        get
        {
            return _currentStep;
        }
        set
        {
            this.ChangeField(ref _currentStep, value, t => t.CurrentStep);
        }
    }

    [Export(nameof(CurrentViewController))]
    public WizardOutlineViewController? CurrentViewController
    {
        get
        {
            return _currentViewController;
        }
        set
        {
            if (this.ChangeField(ref _currentViewController, value, t => t.CurrentViewController))
            {
                FocusNextButton();
            }
        }
    }

    private void FocusNextButton()
    {
        if (_currentViewController is not null &&
            View.Window is not null &&
            NextButton is not null &&
            NextButton.AcceptsFirstResponder())
        {
            View.Window.MakeFirstResponder(NextButton);
        }
    }

    public override void ViewDidLoad()
    {
        base.ViewDidLoad();
        OutlineView.Delegate = new OutlineViewDelegeate(this);
        OutlineView.SizeLastColumnToFit();
        Prompt.Delegate = _linkCommandDelegate;
        SelectItem(_outlineRoot.Next);
        AddObservers(NextButton.WhenPropertiesChange(FocusNextButton, b => b.Enabled));
    }

    public override void ViewDidAppear()
    {
        base.ViewDidAppear();
        FocusNextButton();
    }

    partial void Next(NSObject sender)
    {
        var next = _currentStep?.Next;
        if (next is null)
        {
            return;
        }
        SelectItem(next);
    }

    partial void Previous(NSObject sender)
    {
        var previous = _currentStep?.Previous;
        if (previous is null)
        {
            return;
        }
        SelectItem(previous);
    }

    private void SelectItem(WizardOutlineItem? item)
    {
        ExpandAncestors(item?.Parent);
        // TODO: Get rid of the NSTreeController and bind directly to the objects
        var node = GetNodeForItem(item);
        var index = OutlineView.RowForItem(node);
        OutlineView.SelectRow(index, false);
    }

    private void ExpandAncestors(WizardOutlineItem? item)
    {
        if (item is null || ReferenceEquals(item, _outlineRoot))
        {
            return;
        }
        ExpandAncestors(item.Parent);
        var node = GetNodeForItem(item);
        OutlineView.ExpandItem(node);
    }

    private NSTreeNode? GetNodeForItem(WizardOutlineItem? item)
    {
        return GetNodeForItem((NSTreeNode)TreeController.ArrangedObjects, item);
    }

    private NSTreeNode? GetNodeForItem(NSTreeNode root, WizardOutlineItem? item)
    {
        if (ReferenceEquals(root.RepresentedObject, item))
        {
            return root;
        }
        if (root.Children is null)
        {
            return null;
        }
        foreach (var child in root.Children)
        {
            var result = GetNodeForItem(child, item);
            if (result is not null)
            {
                return result;
            }
        }
        return null;
    }

    [Export(nameof(OutlineItems))]
    public NSMutableArray<WizardOutlineItem> OutlineItems => _outlineRoot.Children;

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _outlineRoot.Dispose();
            _linkCommandDelegate.Dispose();
            OutlineView?.Delegate?.Dispose();
        }
    }

    private class OutlineViewDelegeate : NSOutlineViewDelegate
    {
        private readonly MainViewController _mainViewController;

        public OutlineViewDelegeate(MainViewController mainViewController)
        {
            _mainViewController = mainViewController;
        }

        public override void SelectionDidChange(NSNotification notification)
        {
            var view = (NSOutlineView?)notification.Object;
            if (view is null)
            {
                return;
            }
            var row = (WizardOutlineItem)((NSTreeNode)view.ItemAtRow(view.SelectedRow)).RepresentedObject;
            if (row.ViewController is not null)
            {
                _mainViewController.CurrentStep = row;
                SelectViewController(row.ViewController);
            }
            else if (!string.IsNullOrEmpty(row.PromptText))
            {
                _mainViewController.CurrentStep = row;
                var nextController = row.TreeAfter.Select(item => item.ViewController).FirstOrDefault(vc => vc is not null && vc.IsEnabled);
                if (nextController is not null)
                {
                    SelectViewController(nextController);
                }
            }
        }

        private void SelectViewController(WizardOutlineViewController controller)
        {
            _mainViewController.CurrentViewController = controller;

            var details = _mainViewController.DetailsView;
            var newView = controller.View;
            newView.Frame = details.Bounds;
            if (details.Subviews.Length > 0)
            {
                details.ReplaceSubviewWith(details.Subviews[0], newView);
            }
            else
            {
                details.AddSubview(newView);
            }
        }

        public override bool ShouldSelectItem(NSOutlineView outlineView, NSObject item)
        {
            var row = (WizardOutlineItem)((NSTreeNode)item).RepresentedObject;
            return row.IsEnabled;
        }
    }

    private class LinkCommandDelegateImpl : NSTextViewDelegate
    {
        private readonly MainViewController _controller;

        public LinkCommandDelegateImpl(MainViewController controller)
        {
            _controller = controller;
        }

        public override bool LinkClicked(NSTextView textView, NSObject link, nuint charIndex)
        {
            if (link is NSUrl url && string.IsNullOrEmpty(url.Scheme))
            {
                var s = url.ToString();
                if (s.StartsWith("step="))
                {
                    if (long.TryParse(s[5..], out var n))
                    {
                        var handle = (ObjCRuntime.NativeHandle)n;
                        var step = _controller._outlineRoot.DescendantsOrSelf.FirstOrDefault(step => step.Handle == handle);
                        if (step is not null)
                        {
                            _controller.SelectItem(step);
                        }
                    }
                }
                else
                {
                    _controller.CurrentViewController?.OnLinkCommand(s);
                }
                return true;
            }
            return false;
        }
    }
}
