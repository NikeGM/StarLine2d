using System;
using UnityEngine;

namespace StarLine2D.Components
{
    [ExecuteInEditMode]
    public class FixRotationComponent : MonoBehaviour
    {
        [SerializeField] private Vector3 angle = Vector3.zero;

        private void LateUpdate()
        {
            transform.rotation = Quaternion.Euler(angle);
        }
    }
}