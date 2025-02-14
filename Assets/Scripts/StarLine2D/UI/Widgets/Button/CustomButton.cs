using UnityEngine;
using UnityEngine.Events;

namespace StarLine2D.UI.Widgets.Button
{
    public class CustomButton : UnityEngine.UI.Button
    {
        [SerializeField] private GameObject normal;
        [SerializeField] private GameObject selected;
        [SerializeField] private GameObject pressed;
        [SerializeField] private GameObject disabled;

        [SerializeField] private UnityEvent onState;

        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            base.DoStateTransition(state, instant);

            switch (state)
            {
                case SelectionState.Disabled:
                    DisableAll();
                    if (disabled != null) disabled.SetActive(true);        
                    break;
                case SelectionState.Normal:
                    DisableAll();
                    if (normal != null) normal.SetActive(true);
                    break;
                case SelectionState.Selected:
                    DisableAll();
                    if (selected != null) selected.SetActive(true);
                    break;
                case SelectionState.Pressed:
                    DisableAll();
                    if (pressed != null) pressed.SetActive(true);
                    break;
                case SelectionState.Highlighted:
                    break;
            }
        }

        private void DisableAll()
        {
            if (normal != null) normal.SetActive(false);
            if (pressed != null) pressed.SetActive(false);
            if (selected != null) selected.SetActive(false);
            if (disabled != null) disabled.SetActive(false);
        }
    }
}