namespace BudgetPoster.GoogleSheet
{
    internal record SheetValue
    {
        public string ValueAsString { get; init; }

        public decimal ValueAsDecimal { get; init; }

        public bool IsStringValue { get; init; }

        public bool IsDecimalValue { get; init; }

        private SheetValue(string valueAsString, decimal valueAsDecimal, bool isStringValue, bool isDecimalValue)
        {
            ValueAsString = valueAsString;
            ValueAsDecimal = valueAsDecimal;
            IsStringValue = isStringValue;
            IsDecimalValue = isDecimalValue;
        }

        public static SheetValue FromString(string value)
            => new(value, decimal.Zero, true, false);

        public static SheetValue FromNumber(double number)
            => new(string.Empty, (decimal)number, false, true);
    }
}