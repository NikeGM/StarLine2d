using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StarLine2D.Libraries.Icon
{
    [CreateAssetMenu(menuName = "Custom/IconsLibrary", fileName = "IconsLibrary")]
    public class IconsLibrary : ScriptableObject
    {
        [SerializeField] private List<Sprite> icons = new();

        public int Count => icons.Count;

        public bool Has(string iconName)
        {
            return icons.Any(item => item.texture.name == iconName);
        }

        public Sprite GetSprite(string iconName)
        {
            return icons.Find(item => item.texture.name == iconName);
        }

        public Sprite GetSprite(int index)
        {
            return icons[index];
        }

        public int GetIndex(Sprite sprite)
        {
            return icons.FindIndex(item => item == sprite);
        }

        public List<string> GetAllNames()
        {
            return icons.ConvertAll(item => item.texture.name);
        }
        
        private static IconsLibrary _instance;
        public static IconsLibrary I => _instance == null ? Load() : _instance;

        private static IconsLibrary Load()
        {
            _instance = Resources.Load<IconsLibrary>("IconsLibrary");
            return _instance;
        }
    }
}