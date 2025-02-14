using System.Collections.Generic;
using StarLine2D.UI.Widgets.Palette;
using UnityEngine;
using UnityEngine.UI;

namespace StarLine2D.UI.Widgets.DiscreteProgressBar
{
    [ExecuteInEditMode]
    public class DiscreteProgressBarWidget : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private Sprite cellSprite;
        [SerializeField] private Sprite filledCellSprite;
        [SerializeField] private int cellsCount = 15;
        [SerializeField] private float cellsOffset = 10;
        [SerializeField] [Palette] private Color color;

        [Header("Value")] 
        [Range(0f, 1f)]
        [SerializeField] private float filled = 0;

        private int FilledInt => Mathf.RoundToInt(cellsCount * filled);
        private int _lastFilledInt = -1;

        private bool _needToGenerate = false;
        private bool _needToFill = false;
        
        private readonly List<Image> _cells = new List<Image>();

        private void Update()
        {
            if (_needToGenerate)
            {
                _cells.Clear();
                _cells.AddRange(GenerateCells());
                _needToGenerate = false;
            }

            if (_needToFill)
            {
                Fill();
                _needToFill = false;
            }
        }

        private void OnValidate()
        {
            _needToGenerate = true;
            _needToFill = true;
        }

        public void SetProgress(float value)
        {
            value = Mathf.Clamp01(value);
            filled = value;
            _needToFill = true;
        }

        private void Fill()
        {
            for (var i = 0; i < cellsCount; i++)
            {
                if (i < FilledInt)
                {
                    _cells[i].sprite = filledCellSprite;
                    _cells[i].color = color;
                }
                else
                {
                    _cells[i].sprite = cellSprite;
                    _cells[i].color = Color.white;
                }
            }

            _lastFilledInt = FilledInt;
        }

        [ContextMenu("Generate")]
        private void Regenerate()
        {
            _needToGenerate = true;
            _needToFill = true;
        }
        
        private List<Image> GenerateCells()
        {
            for (var i = transform.childCount - 1; i >= 0; i--) DestroyImmediate(transform.GetChild(i).gameObject);
            
            var result = new List<Image>();
            
            var x = 0f;
            
            for (var i = 0; i < cellsCount; i++)
            {
                var newGo = new GameObject($"cell#{i}");
                newGo.transform.SetParent(transform, false);

                var rectTransform = newGo.AddComponent<RectTransform>();
                rectTransform.sizeDelta = new Vector2(50, 50);
                rectTransform.anchoredPosition = new Vector2(x, 0);
                x += cellsOffset;

                var image = newGo.AddComponent<Image>();
                image.sprite = cellSprite;
                result.Add(image);
            }

            return result;
        }
    }
}