using System;
using StarLine2D.Controllers;
using StarLine2D.Utils;
using UnityEngine;

namespace StarLine2D.Components
{
    public class HoverableDisplayStateComponent : MonoBehaviour, IHoverable
    {
        private bool _hover = false;
        private CellsStateManager _cellsStateManager;

        private void Awake()
        {
            // Попытка найти CellsStateManager на родительском объекте
            var fieldController = GetComponentInParent<FieldController>();
            if (fieldController != null)
            {
                _cellsStateManager = fieldController.CellStateManager;
            }

            if (_cellsStateManager == null)
            {
                Debug.LogError("CellsStateManager not found on parent Field GameObject.");
            }
        }

        public void OnHoverStarted(GameObject target)
        {
            _hover = true;

            if (_cellsStateManager != null)
            {
                var cellController = GetComponent<CellController>();
                if (cellController != null)
                {
                    _cellsStateManager.SetHoveredCell(cellController);
                }
            }
        }

        public void OnHoverFinished(GameObject target)
        {
            _hover = false;

            if (_cellsStateManager != null)
            {
                var cellController = GetComponent<CellController>();
                if (cellController != null)
                {
                    _cellsStateManager.SetHoveredCell(null);
                }
            }
        }

        public bool IsHovered()
        {
            return _hover;
        }
    }
}