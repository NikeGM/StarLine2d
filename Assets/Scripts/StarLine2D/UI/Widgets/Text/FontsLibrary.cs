using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace StarLine2D.UI.Widgets.Text
{
    [CreateAssetMenu(menuName = "Custom/FontsLibrary", fileName = "FontsLibrary")]
    public class FontsLibrary : ScriptableObject
    {
        [SerializeField] private List<FontItem> fonts = new();
        
        public int Count => fonts.Count;
        public TMP_FontAsset DefaultFont => GetDefaultFont();

        public TMP_FontAsset GetDefaultFont()
        {
            return Count > 0 ? GetFont(0) : null;
        }

        public bool Has(string fontName)
        {
            return fonts.Any(item => item.Name == fontName);
        }

        public TMP_FontAsset GetFont(string fontName)
        {
            return fonts.Find(item => item.Name == fontName).Font;
        }

        public TMP_FontAsset GetFont(int index)
        {
            index = Math.Clamp(index, 0, Count - 1);
            return fonts[index].Font;
        }

        public int GetIndex(TMP_FontAsset font)
        {
            return fonts.FindIndex(item => item.Font.Equals(font));
        }

        public List<string> GetAllNames()
        {
            return fonts.ConvertAll(item => item.Name);
        }

        public List<FontItem> GetAll()
        {
            return new List<FontItem>(fonts);
        }

        
        private static FontsLibrary _instance;
        public static FontsLibrary I => _instance == null ? Load() : _instance;

        private static FontsLibrary Load()
        {
            _instance = Resources.Load<FontsLibrary>("FontsLibrary");
            return _instance;
        }
    }

    [Serializable]
    public struct FontItem
    {
        [SerializeField] private string fontName;
        [SerializeField] private TMP_FontAsset font;

        public string Name => fontName;
        public TMP_FontAsset Font => font;
    }
}