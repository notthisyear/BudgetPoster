using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BudgetPoster.Common;
using BudgetPoster.Discord;
using BudgetPoster.GoogleSheet;
using static BudgetPoster.Common.GeneralExtensionMethods;

namespace BudgetPoster
{
    internal static class EntryPoint
    {
        public static async Task StartProgram(string applicationName, GitInformation gitInformation, TaskCompletionSource tcs, BudgetPosterArguments arguments)
        {
            LoggerType.Internal.Log(LoggingLevel.Info, $"{applicationName} started [{gitInformation}]");

            GoogleSheetClient googleClient = new(arguments.CredentialsPath!, arguments.AccountName!);
            var monthNumber = arguments.Month!.TryGetMonthNumberFromMonth();
            if (monthNumber == -1)
                throw new InvalidOperationException($"Could not get month number for month '{arguments.Month}'");

            await googleClient.Initialize(applicationName);

            var balanceAtStartOfYear = await GetBalanceAtStartOfYear(googleClient, arguments.SheetId!);
            if (balanceAtStartOfYear == decimal.MinValue)
            {
                LoggerType.Internal.Log(LoggingLevel.Warning, "Could not find bank balance at start of the year");
                balanceAtStartOfYear = decimal.Zero;
            }

            List<MonthlyResult> entriesPerMonth = new();
            var success = true;
            for (var i = 1; i <= monthNumber; i++)
            {
                var currentMonth = i.ToString().TryMapToMonth();
                LoggerType.GoogleCommunication.Log(LoggingLevel.Info, $"Getting data for '{currentMonth}'...");
                var result = await GetResultForMonth(googleClient, arguments.SheetId!, currentMonth);
                if (result == default)
                {
                    LoggerType.Internal.Log(LoggingLevel.Error, $"Failed to get all monthly results");
                    success = false;
                    break;
                }
                entriesPerMonth.Add(result);
            }

            if (success)
            {
                var totalRevenue = entriesPerMonth.Select(x => x.GetTotalRevenue()).Sum();
                var totalExpenses = entriesPerMonth.Select(x => x.GetTotalExpenses()).Sum();
                var postToDiscord = !string.IsNullOrEmpty(arguments.DiscordWebhookUrl);
                var post = BuildPost(balanceAtStartOfYear, totalRevenue, totalExpenses, arguments.CurrenyCulture!, entriesPerMonth.First().Month, arguments.TabWidth, postToDiscord, entriesPerMonth.FirstOrDefault(x => x.Month == arguments.Month!)!);

                if (postToDiscord)
                {
                    var discordClient = new DiscordClient(arguments.DiscordWebhookUrl!);
                    var result = await discordClient.TryPostUpdateToDiscord(post);

                    if (!result)
                        LoggerType.DiscordCommunication.Log(LoggingLevel.Error, "Posting to Discord failed");
                    else
                        LoggerType.DiscordCommunication.Log(LoggingLevel.Info, "Posted budget update to Discord");
                }
                else
                {
                    Console.Write(post);
                }
            }

            LoggerType.Internal.Log(LoggingLevel.Info, "Application closing");
            tcs.SetResult();
        }

        private static async Task<decimal> GetBalanceAtStartOfYear(GoogleSheetClient googleClient, string mainSheetId)
        {
            var sheetId = await googleClient.GetIdOfSpecificSheet(mainSheetId, BudgetSheetConstants.BankBalanceSheetName);
            if (sheetId == -1)
            {
                LoggerType.GoogleCommunication.Log(LoggingLevel.Error, $"Could not find sheet Id matching month '{BudgetSheetConstants.BankBalanceSheetName}'");
                return decimal.MinValue;
            }

            var bankBalance = await googleClient.GetDataInSpecificSheet(mainSheetId, sheetId,
                false,
                (BudgetSheetConstants.BankBalanceStartOfYearCell.column, BudgetSheetConstants.BankBalanceStartOfYearCell.column + 1),
                (BudgetSheetConstants.BankBalanceStartOfYearCell.row, BudgetSheetConstants.BankBalanceStartOfYearCell.row + 1));

            if (!bankBalance.Any() || bankBalance.First() == default || !bankBalance.First().Value.IsDecimalValue)
                return decimal.Zero;
            return bankBalance.First().Value.ValueAsDecimal;
        }

        private static async Task<MonthlyResult?> GetResultForMonth(GoogleSheetClient googleClient, string mainSheetId, string month)
        {
            var sheetId = await googleClient.GetIdOfSpecificSheet(mainSheetId, month);
            if (sheetId == -1)
            {
                LoggerType.GoogleCommunication.Log(LoggingLevel.Error, $"Could not find sheet Id matching month '{month}'");
                return default;
            }

            var revenueResult = await googleClient.GetDataInSpecificSheet(mainSheetId, sheetId,
                true,
                (BudgetSheetConstants.RevenueColumns.start, BudgetSheetConstants.RevenueColumns.end),
                (BudgetSheetConstants.RevenueAndExpenseRows.start, BudgetSheetConstants.RevenueAndExpenseRows.end));

            if (!revenueResult.Any())
            {
                LoggerType.GoogleCommunication.Log(LoggingLevel.Error, $"Could not find any revenue data in sheet '{month}'");
                return default;
            }

            var expenseResult = await googleClient.GetDataInSpecificSheet(mainSheetId, sheetId,
                true,
                (BudgetSheetConstants.ExpenseColumns.start, BudgetSheetConstants.ExpenseColumns.end),
                (BudgetSheetConstants.RevenueAndExpenseRows.start, BudgetSheetConstants.RevenueAndExpenseRows.end));

            if (!expenseResult.Any())
            {
                LoggerType.GoogleCommunication.Log(LoggingLevel.Error, $"Could not find any expense data in sheet '{month}'");
                return default;
            }

            return new(month, BudgetSheetArea.CreateRevenueArea(revenueResult), BudgetSheetArea.CreateExpenseArea(expenseResult));
        }

        private static string BuildPost(decimal bankBalanceAtStartOfYear, decimal totalRevenue, decimal totalExpenses, string cultureFormat, string firstMonthInTotal, uint tabWidth, bool isDiscordPost, MonthlyResult result)
        {
            StringBuilder sb = new();
            var culture = new CultureInfo(cultureFormat);
            sb.AppendLine($"--- {result.Month.ToUpper()} ---".AddMarkdownEmphasisIfTrue(isDiscordPost, Emphasis.Bold));
            sb.AppendLine();

            var longestLabel = Math.Max(result.RevenueItems().Select(x => x.name).LengthOfLongestString(), result.ExpenseItems().Select(x => x.name).LengthOfLongestString());
            // Discord font is not monospaced, so results are inconsistent using spaces
            var labelWidthIncludingSpace = isDiscordPost ? int.MinValue : longestLabel + 2;

            sb.AppendLine("Result"
            .FormatLabelAndCurrencyValue(result.GetTotalRevenue() - result.GetTotalExpenses(), culture, labelWidthIncludingSpace + (int)tabWidth)
            .AddMarkdownEmphasisIfTrue(isDiscordPost, Emphasis.Bold));

            sb.AppendLine("\nRevenue");
            foreach (var (name, value) in result.RevenueItems())
                sb.AppendLine(string.Empty.PadLeft((int)tabWidth) + name.FormatLabelAndCurrencyValue(value, culture, labelWidthIncludingSpace).AddMarkdownEmphasisIfTrue(isDiscordPost, Emphasis.Italic));
            sb.AppendLine("Total revenue".FormatLabelAndCurrencyValue(result.GetTotalRevenue(), culture, labelWidthIncludingSpace + (int)tabWidth).AddMarkdownEmphasisIfTrue(isDiscordPost, Emphasis.Bold));

            sb.AppendLine("\nExpenses");
            foreach (var (name, value) in result.ExpenseItems())
                sb.AppendLine(string.Empty.PadLeft((int)tabWidth) + name.FormatLabelAndCurrencyValue(value, culture, labelWidthIncludingSpace).AddMarkdownEmphasisIfTrue(isDiscordPost, Emphasis.Italic));
            sb.AppendLine("Total expenses".FormatLabelAndCurrencyValue(result.GetTotalExpenses(), culture, labelWidthIncludingSpace + (int)tabWidth).AddMarkdownEmphasisIfTrue(isDiscordPost, Emphasis.Bold));

            sb.AppendLine($"\n--- Total{(firstMonthInTotal.Equals(result.Month, StringComparison.Ordinal) ? string.Empty : $" {firstMonthInTotal.ToUpper()} - {result.Month.ToUpper()}")} ---");
            sb.AppendLine("Revenue".FormatLabelAndCurrencyValue(totalRevenue, culture, labelWidthIncludingSpace + (int)tabWidth).AddMarkdownEmphasisIfTrue(isDiscordPost, Emphasis.Italic));
            sb.AppendLine("Expenses".FormatLabelAndCurrencyValue(totalExpenses, culture, labelWidthIncludingSpace + (int)tabWidth).AddMarkdownEmphasisIfTrue(isDiscordPost, Emphasis.Italic));
            var yearlyResult = totalRevenue - totalExpenses;
            sb.AppendLine("Result".FormatLabelAndCurrencyValue(yearlyResult, culture, labelWidthIncludingSpace + (int)tabWidth).AddMarkdownEmphasisIfTrue(isDiscordPost, Emphasis.Bold));
            sb.AppendLine();
            sb.AppendLine("Current bank balance".FormatLabelAndCurrencyValue(bankBalanceAtStartOfYear + yearlyResult, culture, labelWidthIncludingSpace + (int)tabWidth).AddMarkdownEmphasisIfTrue(isDiscordPost, Emphasis.Bold));

            return sb.ToString();
        }
    }
}
