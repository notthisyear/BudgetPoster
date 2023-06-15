using System.Collections.Generic;
using System.Linq;

namespace BudgetPoster.GoogleSheet
{
    internal record SheetEntry(string Name, List<SheetValue> Values)
    {
        public SheetValue Value => Values.First();
    }
}