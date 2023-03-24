using System.Text;
using Microsoft.Extensions.Logging;

namespace GenderNameEstimator.UI.Mac;

// TODO: Include a mechanism for indicating whether the controller is enabled (i.e. the process has enough information to start)
public partial class RunProcessViewController : WizardOutlineViewController
{
    private string? _processName;
    private bool _isCompleted;
    private CancellationTokenSource? _tokenSource;

    public static RunProcessViewController Create(string processName, Func<CancellationToken, RunProcessViewController, Task> mainProcessCreator, Func<bool>? processPrequesitesMet = null)
    {
        var result = Instantiate<RunProcessViewController>();
        result.ProcessName = processName;
        result.MainProcessCreator = mainProcessCreator;
        result.ProcessPrerequisitesMet = processPrequesitesMet;
        return result;
    }

    public RunProcessViewController(ObjCRuntime.NativeHandle handle) : base(handle)
    {
        AddObservers(MainProgress.WhenPropertiesChange(() => SetAnimating(MainProgressIndicator, MainProgress.IsAnimating), pbi => pbi.IsAnimating));
        AddObservers(SubProgress.WhenPropertiesChange(() => SetAnimating(SubProgressIndicator, SubProgress.IsAnimating), pbi => pbi.IsAnimating));
    }

    private static void SetAnimating(NSProgressIndicator progressIndicator, bool isAnimating)
    {
        if (progressIndicator is null)
        {
            return;
        }
        if (isAnimating)
        {
            progressIndicator.StartAnimation(null);
        }
        else
        {
            progressIndicator.StopAnimation(null);
        }
    }

    public Func<CancellationToken, RunProcessViewController, Task>? MainProcessCreator { get; set; }

    public Func<bool>? ProcessPrerequisitesMet { get; set; }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _tokenSource?.Dispose();
        }
    }

    public override bool IsCompleted => _isCompleted;

    [Export(nameof(ProcessName))]
    public string? ProcessName
    {
        get
        {
            return _processName;
        }
        set
        {
            this.ChangeStringField(ref _processName, value, t => t.ProcessName, t => t.ButtonText);
        }
    }

    [Export(nameof(ButtonText))]
    public string ButtonText => $"{(IsRunning ? "Cancel" : "Run")} {ProcessName}";

    [Export(nameof(IsRunning))]
    public bool IsRunning
    {
        get
        {
            return _tokenSource is not null;
        }
        set
        {
            if (IsRunning == value)
            {
                return;
            }
            using (this.ChangeSection(t => t.IsRunning, t => t.ButtonText))
            {
                var tokenSource = _tokenSource;
                if (tokenSource is not null)
                {
                    tokenSource.Cancel();
                    tokenSource.Dispose();
                }
                if (value)
                {
                    tokenSource = _tokenSource = new();
                    var task = MainProcessCreator?.Invoke(_tokenSource.Token, this)
                        ?.ContinueWith(task =>
                        {
                            InvokeOnMainThread(() =>
                            {
                                var process = new StringBuilder(ProcessName);

                                if (task.IsCompletedSuccessfully)
                                {
                                    if (!MainProgress.IsHidden)
                                    {
                                        MainProgress.Label = $"{ProcessName} Completed";
                                    }
                                    if (!SubProgress.IsHidden)
                                    {
                                        SubProgress.IsAnimating = false;
                                        SubProgress.Label = null;
                                    }
                                    GetLogger(process.ToString()).LogInformation("Completed");
                                    using (this.ChangeSection(t => t.IsCompleted))
                                    {
                                        _isCompleted = true;
                                    }
                                }
                                else if (task.IsCanceled || (task.IsFaulted && task.Exception?.InnerException is OperationCanceledException))
                                {
                                    if (!MainProgress.IsHidden)
                                    {
                                        process.Append(": ").Append(MainProgress.Label);
                                        MainProgress.Label += " (canceled)";
                                        MainProgress.IsAnimating = false;
                                    }
                                    if (!SubProgress.IsHidden)
                                    {
                                        if (MainProgress.IsHidden)
                                        {
                                            process.Append(": ");
                                        }
                                        else
                                        {
                                            process.Append(" - ");
                                        }
                                        process.Append(SubProgress.Label);
                                        SubProgress.Label += " (canceled)";
                                        SubProgress.IsAnimating = false;
                                    }
                                    GetLogger(process.ToString()).LogInformation("Canceled");
                                }
                                else if (task.IsFaulted)
                                {
                                    var exception = task.Exception?.Flatten();

                                    if (!MainProgress.IsHidden)
                                    {
                                        process.Append(": ").Append(MainProgress.Label);
                                        MainProgress.Label += " (failed)";
                                        MainProgress.IsAnimating = false;
                                    }
                                    if (!SubProgress.IsHidden)
                                    {
                                        if (MainProgress.IsHidden)
                                        {
                                            process.Append(": ");
                                        }
                                        else
                                        {
                                            process.Append(" - ");
                                        }
                                        process.Append(SubProgress.Label);
                                        SubProgress.Label += " (failed)";
                                        SubProgress.IsAnimating = false;
                                    }
                                    GetLogger(process.ToString()).LogCritical(
                                        exception?.InnerExceptions.Count == 1
                                            ? exception.InnerExceptions[0]
                                            : exception,
                                        "Failed");

                                    using var alert = new NSAlert
                                    {
                                        AlertStyle = NSAlertStyle.Critical,
                                        MessageText = $"{ProcessName} Failed!",
                                        InformativeText = string.Join("\n\n", exception?.InnerExceptions.Select(ex => ex.Message) ?? Enumerable.Empty<string>())
                                    };
                                    alert.RunModal();
                                }
                                using (this.ChangeSection(t => t.IsRunning, t => t.ButtonText))
                                {
                                    tokenSource.Dispose();
                                    if (ReferenceEquals(_tokenSource, tokenSource))
                                    {
                                        _tokenSource = null;
                                    }
                                }
                            });
                        });
                }
                else if (ReferenceEquals(_tokenSource, tokenSource))
                {
                    _tokenSource = null;
                }
            }
        }
    }

    [Export(nameof(MainProgress))]
    public ProgressBarInfo MainProgress { get; } = new();

    [Export(nameof(SubProgress))]
    public ProgressBarInfo SubProgress { get; } = new();

    partial void RunCancelClicked(NSObject sender)
    {
        IsRunning = !IsRunning;
    }

    public override void OnLinkCommand(string command)
    {
        switch (command)
        {
            case "run":
                if (!IsRunning)
                {
                    IsRunning = true;
                }
                break;
            default:
                base.OnLinkCommand(command);
                break;
        }
    }

    public override bool IsEnabled => (ProcessPrerequisitesMet?.Invoke() ?? true) && base.IsEnabled;

    public override void ViewDidAppear()
    {
        base.ViewDidAppear();
        if (!IsCompleted && View.Window is not null && RunCancelButton is not null && RunCancelButton.AcceptsFirstResponder())
        {
            View.Window.MakeFirstResponder(RunCancelButton);
        }
    }

    public ILogger GetLogger(string process)
    {
        return new Logger(this, process);
    }

    [Export(nameof(Messages))]
    public NSMutableArray<MessageInfo> Messages { get; } = new();

    public void AddMessage(MessageInfo message)
    {
        InvokeOnMainThread(() =>
        {
            using (this.ChangeSection(t => t.Messages))
            {
                Messages.Add(message);
            }
        });
    }

    private class Logger : ILogger
    {
        private readonly RunProcessViewController _controller;

        public Logger(RunProcessViewController controller, string process)
        {
            _controller = controller;
            Process = process;
        }

        // TODO: Create an appropriate state variable for context instead?
        public string Process { get; }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => default;

        public bool IsEnabled(LogLevel logLevel) =>
            logLevel == LogLevel.Information ||
            logLevel == LogLevel.Warning ||
            logLevel == LogLevel.Error ||
            logLevel == LogLevel.Critical;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var message = formatter(state, exception);
            if (exception is not null)
            {
                message += $" [{exception.Message}]";
            }
            _controller.AddMessage(new MessageInfo(logLevel, Process, message));
        }
    }
}

[Register(nameof(ProgressBarInfo))]
public class ProgressBarInfo : NSObject
{
    private string? _label;
    private int _maximum;
    private int _current;
    private bool _isIndeterminate;
    private bool _isAnimating;

    [Export(nameof(Label))]
    public string? Label
    {
        get
        {
            return _label;
        }
        set
        {
            InvokeOnMainThread(() =>
            {
                this.ChangeStringField(ref _label, value, t => t.Label, t => t.IsHidden);
            });
        }
    }

    [Export(nameof(IsHidden))]
    public bool IsHidden => string.IsNullOrEmpty(Label) || (Maximum == 0 && !IsIndeterminate);

    [Export(nameof(Maximum))]
    public int Maximum
    {
        get
        {
            return _maximum;
        }
        set
        {
            InvokeOnMainThread(() =>
            {
                this.ChangeField(ref _maximum, value, t => t.Maximum, t => t.IsHidden);
            });
        }
    }

    [Export(nameof(Current))]
    public int Current
    {
        get
        {
            return _current;
        }
        set
        {
            InvokeOnMainThread(() =>
            {
                this.ChangeField(ref _current, value, t => t.Current);
            });
        }
    }

    [Export(nameof(IsIndeterminate))]
    public bool IsIndeterminate
    {
        get
        {
            return _isIndeterminate;
        }
        set
        {
            InvokeOnMainThread(() =>
            {
                this.ChangeField(ref _isIndeterminate, value, t => t.IsIndeterminate, t => t.IsHidden);
            });
        }
    }

    [Export(nameof(IsAnimating))]
    public bool IsAnimating
    {
        get
        {
            return _isAnimating;
        }
        set
        {
            InvokeOnMainThread(() =>
            {
                this.ChangeField(ref _isAnimating, value, t => t.IsAnimating);
            });
        }
    }
}
