using System.Collections;
using System.Diagnostics;
using System.Globalization;

namespace GenderNameEstimator.UI.Mac;

[DebuggerDisplay("{Value} ({IsEnabled}, {IsStepCandidate})")]
[Register(nameof(WizardOutlineItem))]
public class WizardOutlineItem : NSObject, IEnumerable<WizardOutlineItem>
{
    private string? _promptText = "";

    private readonly List<IDisposable> _observers = new();

    private WizardOutlineViewController? _viewController;

    [Export(nameof(Title))]
    public string? Title { get; set; }

    [Export(nameof(SequencePath))]
    public string SequencePath
    {
        get
        {
            if (Parent is null)
            {
                return "";
            }
            var parentSequencePath = Parent.SequencePath;
            if (parentSequencePath != "")
            {
                parentSequencePath += '.';
            }
            return parentSequencePath + (Parent.Children.IndexOf(this) + 1).ToString(CultureInfo.InvariantCulture);
        }
    }

    [Export(nameof(Value))]
    public string Value => $"{SequencePath}\t{Title}";

    [Export(nameof(Parent))]
    public WizardOutlineItem? Parent { get; set; }

    [Export(nameof(Children))]
    public NSMutableArray<WizardOutlineItem> Children { get; } = new();

    [Export(nameof(IsLeaf))]
    public bool IsLeaf => Children.Count == 0;

    [Export(nameof(ViewController))]
    public WizardOutlineViewController? ViewController
    {
        get
        {
            return _viewController;
        }
        set
        {
            if (this.ChangeField(ref _viewController, value, t => t.ViewController, t => t.IsEnabled))
            {
                ClearObservers();
                if (value is not null)
                {
                    AddObservers(this.Properties(t => t.IsEnabled).DependOn(value.Properties(m => m.IsEnabled)));
                }
            }
        }
    }

    [Export(nameof(IsEnabled))]
    public bool IsEnabled => ViewController?.IsEnabled ?? !string.IsNullOrWhiteSpace(PromptText);

    [Export(nameof(PromptText))]
    public string? PromptText
    {
        get
        {
            return _promptText;
        }
        set
        {
            this.ChangeStringField(ref _promptText, value, t => t.PromptText, t => t.IsEnabled);
        }
    }

    public IEnumerable<WizardOutlineItem> TreeAfter => Children.Concat(SelfOrAncestors.SelectMany(node => node.FollowingSiblings)).SelectMany(node => node.SelfOrDescendants);

    public IEnumerable<WizardOutlineItem> SelfOrDescendants
    {
        get
        {
            yield return this;
            foreach (var descendant in Children.AsEnumerable().SelectMany(child => child.SelfOrDescendants))
            {
                yield return descendant;
            }
        }
    }

    public IEnumerable<WizardOutlineItem> Ancestors
    {
        get
        {
            for (var node = Parent; node != null; node = node.Parent)
            {
                yield return node;
            }
        }
    }

    public IEnumerable<WizardOutlineItem> SelfOrAncestors
    {
        get
        {
            yield return this;
            foreach (var node in Ancestors)
            {
                yield return node;
            }
        }
    }

    public IEnumerable<WizardOutlineItem> FollowingSiblings => Parent?.ChildrenAfter(this) ?? Enumerable.Empty<WizardOutlineItem>();

    public IEnumerable<WizardOutlineItem> ChildrenAfter(WizardOutlineItem item)
    {
        if (Children.Count == 0)
        {
            yield break;
        }
        var index = Children.IndexOf(item);
        if (index >= Children.Count - 1)
        {
            yield break;
        }
        while (++index < Children.Count)
        {
            yield return Children[index];
        }
    }

    public IEnumerable<WizardOutlineItem> TreeBefore
    {
        get
        {
            var isAncestor = false;
            foreach (var node in SelfOrAncestors)
            {
                if (isAncestor)
                {
                    yield return node;
                }
                else
                {
                    isAncestor = true;
                }
                foreach (var descendant in node.PreceedingSiblings.SelectMany(node => node.DescendantsOrSelf))
                {
                    yield return descendant;
                }
            }
        }
    }

    public IEnumerable<WizardOutlineItem> DescendantsOrSelf
    {
        get
        {
            foreach (var descendant in Children.AsEnumerable().Reverse().SelectMany(child => child.DescendantsOrSelf))
            {
                yield return descendant;
            }
            yield return this;
        }
    }

    public IEnumerable<WizardOutlineItem> PreceedingSiblings => Parent?.ChildrenBefore(this) ?? Enumerable.Empty<WizardOutlineItem>();

    public IEnumerable<WizardOutlineItem> ChildrenBefore(WizardOutlineItem item)
    {
        if (Children.Count == 0)
        {
            yield break;
        }
        var index = Children.IndexOf(item);
        if (index == 0 || index >= Children.Count)
        {
            yield break;
        }
        do
        {
            yield return Children[--index];
        }
        while (index > 0);
    }

    [Export(nameof(Previous))]
    public WizardOutlineItem? Previous => TreeBefore.FirstOrDefault(item => item.IsStepCandidate);

    [Export(nameof(Next))]
    public WizardOutlineItem? Next => TreeAfter.FirstOrDefault(item => item.IsStepCandidate);

    public bool IsStepCandidate => ViewController is null
        ? !string.IsNullOrWhiteSpace(PromptText)
        : ViewController.IsEnabled;

    public WizardOutlineItem Container => this;

    public void Add(WizardOutlineItem child)
    {
        child.Parent = this;
        Children.Add(child);
    }

    public void AddMany(params WizardOutlineItem[] children)
    {
        foreach (var child in children)
        {
            Add(child);
        }
    }

    public IEnumerator<WizardOutlineItem> GetEnumerator()
    {
        return Children.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public override void WillChangeValue(string forKey)
    {
        base.WillChangeValue(forKey);
        if (forKey == nameof(IsEnabled))
        {
            Previous?.WillChangeValue(nameof(Next));
            Next?.WillChangeValue(nameof(Previous));
        }
    }

    public override void DidChangeValue(string forKey)
    {
        base.DidChangeValue(forKey);
        if (forKey == nameof(IsEnabled))
        {
            Previous?.DidChangeValue(nameof(Next));
            Next?.DidChangeValue(nameof(Previous));
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            ClearObservers();
            ViewController?.Dispose();
            foreach (var child in Children)
            {
                child?.Dispose();
            }
        }
    }

    private void AddObservers(IEnumerable<IDisposable> observers)
    {
        _observers.AddRange(observers);
    }

    private void ClearObservers()
    {
        foreach (var observer in _observers)
        {
            try
            {
                observer?.Dispose();
            }
            catch
            {
                // Ignore;
            }
        }
        _observers.Clear();
    }
}
