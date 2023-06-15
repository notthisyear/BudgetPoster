using System;
using System.Collections;
using System.Collections.Generic;
using BudgetPoster.GoogleSheet;

namespace BudgetPoster
{
    internal record BudgetSheetArea : IEnumerable<(string name, decimal value)>
    {
        private readonly List<SheetEntry> _entries;

        private readonly bool _areExpenses;

        private BudgetSheetArea(List<SheetEntry> entries, bool areExpenses)
        {
            _entries = entries;
            _areExpenses = areExpenses;
        }

        public decimal GetTotal()
        {
            var total = decimal.Zero;
            foreach (var entry in _entries)
            {
                if (_areExpenses)
                {
                    if (entry.Name.StartsWith(BudgetSheetConstants.ExpenseLabelPrefixToExclude))
                        continue;
                }

                if (entry.Value.IsDecimalValue)
                    total += entry.Value.ValueAsDecimal;
            }
            return total;
        }

        public static BudgetSheetArea CreateRevenueArea(List<SheetEntry> entries)
            => new(entries, false);

        public static BudgetSheetArea CreateExpenseArea(List<SheetEntry> entries)
            => new(entries, true);

        public IEnumerator<(string name, decimal value)> GetEnumerator()
             => new AreaEnumerator(_areExpenses, _entries);

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();
    }

    internal class AreaEnumerator : IEnumerator<(string name, decimal value)>
    {
        private readonly List<SheetEntry> _entries;
        private readonly bool _areExpenses;

        private int _position = -1;
        public AreaEnumerator(bool areExpenses, List<SheetEntry> entries)
        {
            _areExpenses = areExpenses;
            if (_areExpenses)
            {
                _entries = new();
                foreach (var entry in entries)
                {
                    if (!entry.Name.StartsWith(BudgetSheetConstants.ExpenseLabelPrefixToExclude))
                        _entries.Add(entry);
                }
            }
            else
            {
                _entries = entries;
            }
        }

        public (string name, decimal value) Current
        {
            get
            {
                try
                {
                    var v = _entries[_position];
                    return (v.Name, v.Value.IsDecimalValue ? v.Value.ValueAsDecimal : decimal.Zero);
                }
                catch (IndexOutOfRangeException)
                {
                    throw new InvalidOperationException();
                }
            }
        }

        object IEnumerator.Current => Current;

        public bool MoveNext()
        {
            _position++;
            return _position < _entries.Count;
        }

        public void Reset()
        {
            _position = -1;
        }

        public void Dispose()
        { }
    }
}