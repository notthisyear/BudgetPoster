using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;

namespace BudgetPoster.Common
{
    internal static class GeneralExtensionMethods
    {
        private static readonly Dictionary<string, (int number, List<string> options)> s_monthList = new()
        {
            { BudgetSheetConstants.SheetNameJanuary, (1, new() { "1", "jan", "januari", "janaury" })},
            { BudgetSheetConstants.SheetNameFebruary, (2, new() { "2", "feb", "february", "februari" })},
            { BudgetSheetConstants.SheetNameMarch, (3, new() { "3", "mar", "march", "mars" })},
            { BudgetSheetConstants.SheetNameApril, (4, new() { "4", "apr", "april" })},
            { BudgetSheetConstants.SheetNameMay, (5, new() { "5", "may", "maj" })},
            { BudgetSheetConstants.SheetNameJune, (6, new() { "6", "jun", "june", "juni" })},
            { BudgetSheetConstants.SheetNameJuly, (7, new() { "7", "jul", "july", "juli" })},
            { BudgetSheetConstants.SheetNameAugust, (8, new() { "8", "aug", "august", "augusti" })},
            { BudgetSheetConstants.SheetNameSeptember, (9, new() { "9", "sep", "september" })},
            { BudgetSheetConstants.SheetNameOctober, (10, new() { "10", "oct", "okt", "october", "oktober" })},
            { BudgetSheetConstants.SheetNameNovember, (11, new() { "11", "nov", "november" })},
            { BudgetSheetConstants.SheetNameDecember, (12, new() { "12", "dec", "december" })}
        };

        public enum Emphasis
        {
            Italic,
            Bold
        }

        public static string FormatException(this Exception e)
        {
            if (e is SocketException socketException)
                return $"{socketException.GetType()} (socket error code: {socketException.ErrorCode}): {e.Message}";
            else
                return $"{e.GetType()}: {e.Message}";
        }

        public static string FormatHttpResponse(this HttpResponseMessage response)
            => $"HTTP status code {response.StatusCode}: {response.ReasonPhrase}";

        public static string AddMarkdownEmphasisIfTrue(this string s, bool addMarkdown, Emphasis emphasis, bool addSpaceAfter = false)
            => addMarkdown ? s.AddMarkdownEmphasis(emphasis, addSpaceAfter) : s;

        public static string AddMarkdownEmphasis(this string s, Emphasis emphasis, bool addSpaceAfter = false)
           => emphasis switch
           {
               Emphasis.Italic => $"*{s}*{(addSpaceAfter ? " " : "")}",
               Emphasis.Bold => $"**{s}**{(addSpaceAfter ? " " : "")}",
               _ => throw new NotImplementedException(),
           };

        public static string TryMapToMonth(this string month)
        {
            foreach (var entry in s_monthList)
            {
                if (month.MatchesAny(entry.Value.options.ToArray()))
                    return entry.Key;
            }
            return string.Empty;
        }

        public static int TryGetMonthNumberFromMonth(this string month)
        {
            if (s_monthList.TryGetValue(month, out var match))
                return match.number;
            return -1;
        }

        public static string FormatLabelAndCurrencyValue(this string label, decimal value, CultureInfo c, int labelWidth)
            => (labelWidth > 0 ? $"{label}:".PadRight(labelWidth) : $"{label}: ") + value.GetFormattedValueAsCurrency(c);

        public static int LengthOfLongestString(this IEnumerable<string> s)
            => s.Select(x => x.Length).Max();

        public static string GetFormattedValueAsCurrency(this decimal d, CultureInfo c)
            => d.ToString("C", c);

        private static bool MatchesAny(this string s, params string[] candidates)
        {
            foreach (var candidate in candidates)
            {
                if (s.Equals(candidate, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }
    }
}
