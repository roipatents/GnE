using System.Linq.Expressions;

namespace GenderNameEstimator.UI.Mac;

public static class CocoaExtensions
{
    public static bool ChangeStringField(this NSObject obj, ref string? field, string? value, params string[] fieldNames)
    {
        return obj.ChangeField(ref field, value ?? "", fieldNames);
    }

    public static bool ChangeStringField<TThis>(this TThis reference, ref string? field, string? value, params Expression<Func<TThis, object?>>[] properties)
        where TThis : NSObject
    {
        return ChangeStringField(reference, ref field, value, properties.Select(GetPropertyName).ToArray());
    }

    public static bool ChangeStringField(this (NSObject Reference, LambdaExpression[] Properties) reference, ref string? field, string? value)
    {
        return ChangeStringField(reference.Reference, ref field, value, reference.Properties.Select(GetPropertyName).ToArray());
    }

    public static bool ChangeField<T>(this NSObject obj, ref T field, T value, params string[] fieldNames)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }
        using (obj.ChangeSection(fieldNames))
        {
            field = value;
        }
        return true;
    }

    public static bool ChangeField<TThis, TProp>(this TThis reference, ref TProp field, TProp value, params Expression<Func<TThis, object?>>[] properties)
        where TThis : NSObject
    {
        return ChangeField(reference, ref field, value, properties.Select(GetPropertyName).ToArray());
    }

    public static bool ChangeField<T>(this (NSObject Reference, LambdaExpression[] Properties) reference, ref T field, T value)
    {
        return ChangeField(reference.Reference, ref field, value, reference.Properties.Select(GetPropertyName).ToArray());
    }

    public static IDisposable ChangeSection(this NSObject obj, params string[] fieldNames)
    {
        return new InternalChangeSection(obj, fieldNames);
    }

    public static IDisposable ChangeSection<TThis>(this TThis reference, params Expression<Func<TThis, object?>>[] properties)
        where TThis : NSObject
    {
        return ChangeSection(reference, properties.Select(GetPropertyName).ToArray());
    }

    public static IDisposable ChangeSection(this (NSObject Reference, LambdaExpression[] Properties) reference)
    {
        return ChangeSection(reference.Reference, reference.Properties.Select(GetPropertyName).ToArray());
    }

    public static (NSObject Reference, LambdaExpression[] Properties) Properties<TThis>(this TThis reference, params Expression<Func<TThis, object?>>[] properties)
        where TThis : NSObject => (reference, properties);

    public static IEnumerable<(NSObject Reference, LambdaExpression[] Properties)> PropertiesOfAny<TThis>(this IEnumerable<TThis> references, params Expression<Func<TThis, object?>>[] properties)
        where TThis : NSObject => references.Select(reference => reference.Properties(properties));

    // TODO: Use the expressions to get property attributes, and thus it will work without even following a naming convention where the property name and the exported name are the same
    public static IEnumerable<IDisposable> DependOn(this (NSObject Reference, LambdaExpression[] Properties) dependent,
        params (NSObject Reference, LambdaExpression[] Properties)[] references)
    {
        var dependentPropertyNames = dependent.Properties.Select(GetPropertyName).ToArray();

        foreach (var reference in references)
        {
            foreach (var referencePropertyName in reference.Properties.Select(GetPropertyName))
            {
                yield return reference.Reference.AddObserver(referencePropertyName, NSKeyValueObservingOptions.Prior, change =>
                {
                    if (change.IsPrior)
                    {
                        foreach (var dependentPropertyName in dependentPropertyNames)
                        {
                            dependent.Reference.WillChangeValue(dependentPropertyName);
                        }
                    }
                    else
                    {
                        foreach (var dependentPropertyName in dependentPropertyNames)
                        {
                            dependent.Reference.DidChangeValue(dependentPropertyName);
                        }
                    }
                });
            }
        }
    }

    public static IEnumerable<IDisposable> WhenPropertiesChange<TThis>(this TThis reference, Action action, params Expression<Func<TThis, object?>>[] properties)
        where TThis : NSObject
    {
        foreach (var referencePropertyName in properties.Select(GetPropertyName))
        {
            yield return reference.AddObserver(referencePropertyName, NSKeyValueObservingOptions.New, _ => action());
        }
    }

    public static IEnumerable<IDisposable> WhenPropertiesChange(this (NSObject Reference, LambdaExpression[] Properties) reference, Action action)
    {
        foreach (var referencePropertyName in reference.Properties.Select(GetPropertyName))
        {
            yield return reference.Reference.AddObserver(referencePropertyName, NSKeyValueObservingOptions.New, _ => action());
        }
    }

    public static IEnumerable<IDisposable> WhenPropertiesOfAnyChange<TThis>(this IEnumerable<TThis> references, Action action, params Expression<Func<TThis, object?>>[] properties)
        where TThis : NSObject
    {
        foreach (var referencePropertyName in properties.Select(GetPropertyName))
        {
            foreach (var reference in references)
            {
                yield return reference.AddObserver(referencePropertyName, NSKeyValueObservingOptions.New, _ => action());
            }
        }
    }

    public static IEnumerable<T> AsEnumerable<T>(this NSMutableArray<T> array) where T : class, ObjCRuntime.INativeObject
    {
        return (IEnumerable<T>)array;
    }

    public static void SizeColumnToFitContents(this NSTableView tableView, nint column)
    {
        // TODO: Get this to work properly and then test on a large data set
        var tableColumn = tableView.TableColumns()[column];
        nfloat maxWidth = tableColumn.HeaderCell?.CellSize.Width ?? 0;
        for (nint row = 0; row < tableView.RowCount; row++)
        {
            var view = tableView.GetView(column, row, true);
            var width = view.IntrinsicContentSize.Width;
            if (width > maxWidth)
            {
                maxWidth = width;
            }
        }
        if (maxWidth > tableColumn.MaxWidth)
        {
            tableColumn.Width = tableColumn.MaxWidth;
        }
        else if (maxWidth < tableColumn.MinWidth)
        {
            tableColumn.Width = tableColumn.MinWidth;
        }
        else
        {
            tableColumn.Width = maxWidth;
        }
    }

    public static NSTableView SizeColumnsToFitContents(this NSTableView tableView)
    {
        for (nint column = 0; column < tableView.ColumnCount; column++)
        {
            tableView.SizeColumnToFitContents(column);
        }
        return tableView;
    }

    private static string GetPropertyName(LambdaExpression propertyExpression)
    {
        switch (propertyExpression.Body)
        {
            case UnaryExpression unaryExpression when unaryExpression.NodeType == ExpressionType.Convert
                && unaryExpression.Operand is MemberExpression memberExpression:
                return memberExpression.Member.Name;
            case MemberExpression memberExpression:
                return memberExpression.Member.Name;
            default:
                throw new ArgumentException("Expressions must be a simple property reference, not a calculation or a chain of references");
        }
    }

    private class InternalChangeSection : IDisposable
    {
        private readonly NSObject _obj;
        private readonly string[] _fieldNames;

        public InternalChangeSection(NSObject obj, params string[] fieldNames)
        {
            _obj = obj;
            _fieldNames = fieldNames;
            foreach (var field in fieldNames)
            {
                _obj.WillChangeValue(field);
            }
        }

        public void Dispose()
        {
            for (int i = _fieldNames.Length - 1; i >= 0; i--)
            {
                _obj.DidChangeValue(_fieldNames[i]);
            }
        }
    }
}
