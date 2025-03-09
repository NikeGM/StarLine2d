using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StarLine2D.Libraries.Palette
{
    [CreateAssetMenu(menuName = "Custom/PaletteLibrary", fileName = "PaletteLibrary")]
    public class PaletteLibrary : ScriptableObject
    {
        [SerializeField] private List<ColorItem> colors;

        public int Count => colors.Count;
        public Color DefaultColor => GetDefaultColor();

        public Color GetDefaultColor()
        {
            return Count > 0 ? GetColor(0) : Color.white;
        }

        public bool Has(string colorName)
        {
            return colors.Any(item => item.Name == colorName);
        }

        public Color GetColor(string colorName)
        {
            return colors.Find(item => item.Name == colorName).Color;
        }

        public Color GetColor(int index)
        {
            index = Math.Clamp(index, 0, Count - 1);
            return colors[index].Color;
        }

        public int GetIndex(Color color)
        {
            return colors.FindIndex(item => item.Color.Equals(color));
        }

        public List<string> GetAllNames()
        {
            return colors.ConvertAll(item => item.Name);
        }

        public List<ColorItem> GetAll()
        {
            return new List<ColorItem>(colors);
        }

        
        private static PaletteLibrary _instance;
        public static PaletteLibrary I => _instance == null ? Load() : _instance;

        private static PaletteLibrary Load()
        {
            _instance = Resources.Load<PaletteLibrary>("PaletteLibrary");
            return _instance;
        }
    }

    [Serializable]
    public struct ColorItem
    {
        [SerializeField] private string colorName;
        [SerializeField] private Color color;

        public string Name => colorName;
        public Color Color => color;
    }
}