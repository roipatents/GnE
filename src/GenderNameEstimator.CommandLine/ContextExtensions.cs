using System.CommandLine;
using System.CommandLine.Invocation;

using GenderNameEstimator.Tools;

namespace GenderNameEstimator.CommandLine;

public static class ContextExtensions
{
    public static T? GetValue<T>(this InvocationContext context, Option<T> option)
    {
        return context.ParseResult.GetValueForOption(option);
    }

    public static T GetValue<T>(this InvocationContext context, Argument<T> argument)
    {
        return context.ParseResult.GetValueForArgument(argument);
    }

    public static FieldInfo GetColumnInfo(this InvocationContext context, Option<string> option)
    {
        return GetColumnInfo(context.GetValue(option));
    }

    public static FieldInfo GetColumnInfo(string? columnNameOrIndex)
    {
        if (string.IsNullOrEmpty(columnNameOrIndex))
        {
            return FieldInfo.Empty;
        }
        if (int.TryParse(columnNameOrIndex, out var index))
        {
            // NOTE: Command line options are one-based, whereas internal column indices are zero-based
            return index - 1;
        }
        return columnNameOrIndex;
    }
}

