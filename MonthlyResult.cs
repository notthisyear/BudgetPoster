using System.Collections.Generic;

namespace BudgetPoster
{
    internal class MonthlyResult
    {
        public string Month { get; }

        private readonly BudgetSheetArea _revenue;

        private readonly BudgetSheetArea _expenses;

        public MonthlyResult(string month, BudgetSheetArea revenue, BudgetSheetArea expenses)
        {
            Month = month;
            _revenue = revenue;
            _expenses = expenses;
        }

        public decimal GetTotalRevenue()
            => _revenue.GetTotal();

        public decimal GetTotalExpenses()
            => _expenses.GetTotal();

        public IEnumerable<(string name, decimal value)> RevenueItems()
        {
            foreach (var (name, value) in _revenue)
                yield return (name, value);
        }

        public IEnumerable<(string name, decimal value)> ExpenseItems()
        {
            foreach (var (name, value) in _expenses)
                yield return (name, value);
        }
    }
}