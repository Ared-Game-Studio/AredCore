using System.Collections.Generic;

namespace Ared.Core.AutoSheetData.Abstraction
{
    public interface IAutoDataCollection<T>
    {
        public List<T> Items { get; }
        public List<T> GetItems();
    }
}