using System;
using System.Collections.Generic;
using System.Linq;
using StarLine2D.UI.Widgets.Palette;
using UnityEngine;

namespace StarLine2D.Components
{
    [ExecuteInEditMode]
    public class SpriteCompoundComponent : MonoBehaviour
    {
        [SerializeField] private List<SpriteRenderer> sprites = new();
        [SerializeField] private string current;
        [SerializeField] private List<Profile> profiles = new();

        private bool _updated = true;

        public string GetCurrentProfile()
        {
            return current;
        }

        public bool HasProfile(string profileId)
        {
            return profiles.Any(item => item.Id == profileId);
        }

        public bool SetProfile(string profileId)
        {
            if (!HasProfile(profileId)) return false;
            current = profileId;

            _updated = true;

            return true;
        }

        public void UpdateSprites()
        {
            sprites = new List<SpriteRenderer>(GetComponentsInChildren<SpriteRenderer>());
            _updated = true;
        }

        private void Update()
        {
            if (!_updated) return;
            _updated = false;

            for (var i = 0; i < sprites.Count; i++)
            {
                sprites[i].color = GetColor(i);
            }
        }

        private Color GetColor(int index)
        {
            var profileIndex = profiles.FindIndex(item => item.Id == current);
            if (profileIndex == -1) return Color.white;

            var profile = profiles[profileIndex];
            if (index < 0) return Color.white;
            if (index >= profile.Colors.Count) return Color.white;

            return profile.Colors[index];
        }

        private void OnValidate()
        {
            _updated = true;
        }

        [Serializable]
        private struct ProfileColor
        {
            [SerializeField] [Palette] public Color color;
        }

        [Serializable]
        private struct Profile
        {
            [SerializeField] private string id;
            [SerializeField] private List<ProfileColor> colors;

            public string Id => id;
            public List<Color> Colors => colors.ConvertAll(item => item.color);
        }
    }
}