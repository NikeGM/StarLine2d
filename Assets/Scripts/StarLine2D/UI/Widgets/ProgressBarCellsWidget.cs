using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace StarLine2D.UI.Widgets
{
    [ExecuteInEditMode]
    public class ProgressBarCellsWidget : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private Sprite cellSprite;
        [SerializeField] private Sprite filledCellSprite;
        [SerializeField] private int cellsCount = 15;
        [SerializeField] private float cellsOffset = 10;

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
            if (_cells.Count != cellsCount) _needToGenerate = true;
            if (_lastFilledInt != FilledInt) _needToFill = true;
        }

        public void SetProgress(float value)
        {
            value = Mathf.Clamp01(value);
            filled = value;
            _needToFill = true;
        }

        [ContextMenu("Fill")]
        private void Fill()
        {
            for (var i = 0; i < cellsCount; i++)
            {
                _cells[i].sprite = i < FilledInt ? filledCellSprite : cellSprite;
            }

            _lastFilledInt = FilledInt;
        }

        [ContextMenu("Generate")]
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