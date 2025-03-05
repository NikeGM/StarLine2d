using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StarLine2D.UI.Widgets.Text
{
    [CreateAssetMenu(menuName = "Custom/FontSizesLibrary", fileName = "FontSizesLibrary")]
    public class FontSizesLibrary : ScriptableObject
    {
        [SerializeField] private List<FontSizeItem> sizes = new();
        
        public int Count => sizes.Count;
        public int DefaultSize => GetDefaultSize();

        public int GetDefaultSize()
        {
            return Count > 0 ? GetSize(0) : 10;
        }

        public bool Has(string sizeName)
        {
            return sizes.Any(item => item.Name == sizeName);
        }

        public int GetSize(string sizeName)
        {
            return sizes.Find(item => item.Name == sizeName).Size;
        }

        public int GetSize(int index)
        {
            index = Math.Clamp(index, 0, Count - 1);
            return sizes[index].Size;
        }

        public int GetIndex(int size)
        {
            return sizes.FindIndex(item => item.Size == size);
        }

        public List<string> GetAllNames()
        {
            return sizes.ConvertAll(item => item.Name);
        }

        public List<FontSizeItem> GetAll()
        {
            return new List<FontSizeItem>(sizes);
        }

        
        private static FontSizesLibrary _instance;
        public static FontSizesLibrary I => _instance == null ? Load() : _instance;

        private static FontSizesLibrary Load()
        {
            _instance = Resources.Load<FontSizesLibrary>("FontSizesLibrary");
            return _instance;
        }
    }
    
    [Serializable]
    public struct FontSizeItem
    {
        [SerializeField] private string sizeName;
        [SerializeField] private int size;

        public string Name => sizeName;
        public int Size => size;
    }
}