using UnityEngine;

namespace StarLine2D.Components {
    [ExecuteInEditMode]
    public class FixScaleComponent : MonoBehaviour
    {
        [SerializeField] private Vector3 scale = new Vector3(1, 1, 1);

        private void LateUpdate()
        {
            var newScale = new Vector3(
                scale.x / transform.parent.lossyScale.x,
                scale.y / transform.parent.lossyScale.y,
                scale.z / transform.parent.lossyScale.z
            );
            transform.localScale = newScale;
        }
    }
}