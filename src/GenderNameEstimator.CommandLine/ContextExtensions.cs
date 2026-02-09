/*
 * The MIT License (MIT)
 *
 * Copyright © 2023-2026, Richardson Oliver Insights, LLC, All Rights Reserved
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */
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

