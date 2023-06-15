namespace BudgetPoster
{
    // Note: These are specific to the targeted budget sheet
    internal static class BudgetSheetConstants
    {
        public static readonly (int start, int end) RevenueColumns = (0, 2);

        public static readonly (int start, int end) ExpenseColumns = (3, 5);

        public static readonly (int start, int end) RevenueAndExpenseRows = (10, 20);

        public static readonly (int column, int row) BankBalanceStartOfYearCell = (2, 6);

        public const string BankBalanceSheetName = "Kassa";

        public const string ExpenseLabelPrefixToExclude = "...";

        public const string SheetNameJanuary = "Januari";

        public const string SheetNameFebruary = "Februari";

        public const string SheetNameMarch = "Mars";

        public const string SheetNameApril = "April";

        public const string SheetNameMay = "Maj";

        public const string SheetNameJune = "Juni";

        public const string SheetNameJuly = "Juli";

        public const string SheetNameAugust = "Augusti";

        public const string SheetNameSeptember = "September";

        public const string SheetNameOctober = "Oktober";

        public const string SheetNameNovember = "November";

        public const string SheetNameDecember = "December";
    }
}