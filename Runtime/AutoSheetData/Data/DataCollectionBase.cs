using System.Collections.Generic;
using Ared.AutoSheetData.Abstraction;
using UnityEngine;

namespace Ared.AutoSheetData.Data
{
    public abstract class DataCollectionBase : ScriptableObject
    {
        public abstract int Count { get; }
    }

    public class DataCollection<T> : DataCollectionBase, IAutoDataCollection<T>
    {
        [field: SerializeField] public List<T> Items { get; set; } = new();
        public override int Count => Items?.Count ?? 0;
        
        public List<T> GetItems() => Items;
    }
}