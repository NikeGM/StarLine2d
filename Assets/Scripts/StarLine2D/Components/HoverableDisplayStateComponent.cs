using System;
using StarLine2D.Controllers;
using StarLine2D.Utils;
using UnityEngine;

namespace StarLine2D.Components
{
    public class HoverableDisplayStateComponent : MonoBehaviour, IHoverable
    {
        private bool _hover = false;
        private CellsStateController _cellsStateController;

        private void Awake()
        {
            // Попытка найти CellsStateController на родительском объекте
            var fieldController = GetComponentInParent<FieldController>();
            if (fieldController != null)
            {
                _cellsStateController = fieldController.GetComponent<CellsStateController>();
            }

            if (_cellsStateController == null)
            {
                Debug.LogError("CellsStateController not found on parent Field GameObject.");
            }
        }

        public void OnHoverStarted(GameObject target)
        {
            _hover = true;

            if (_cellsStateController != null)
            {
                var cellController = GetComponent<CellController>();
                if (cellController != null)
                {
                    _cellsStateController.SetHoveredCell(cellController);
                }
            }
        }

        public void OnHoverFinished(GameObject target)
        {
            _hover = false;

            if (_cellsStateController != null)
            {
                var cellController = GetComponent<CellController>();
                if (cellController != null)
                {
                    _cellsStateController.SetHoveredCell(null);
                }
            }
        }

        public bool IsHovered()
        {
            return _hover;
        }
    }
}