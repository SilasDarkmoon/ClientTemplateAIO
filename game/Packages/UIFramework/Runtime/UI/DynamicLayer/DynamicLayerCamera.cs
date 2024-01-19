using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngineEx.UI
{
    [ExecuteInEditMode]
    public class DynamicLayerCamera : DynamicLayer
    {
        private Camera _Camera;
        public bool Reverse;

        protected override void OnEnable()
        {
            if (AssignedLayer > 0)
            {
                if (!_Camera)
                {
                    _Camera = GetComponent<Camera>();
                }
                if (_Camera)
                {
                    if (Reverse)
                    {
                        _Camera.cullingMask |= (1 << AssignedLayer);
                    }
                    else
                    {
                        _Camera.cullingMask &= ~(1 << AssignedLayer);
                    }
                }
            }
            base.OnEnable();
        }

        protected override void ApplyLayer()
        {
            base.ApplyLayer();
            if (AssignedLayer > 0)
            {
                if (!_Camera)
                {
                    _Camera = GetComponent<Camera>();
                }
                if (_Camera)
                {
                    if (Reverse)
                    {
                        _Camera.cullingMask &= ~(1 << AssignedLayer);
                    }
                    else
                    {
                        _Camera.cullingMask |= (1 << AssignedLayer);
                    }
                }
            }
        }
        protected override void RestoreLayer()
        {
            if (AssignedLayer > 0 && _Camera)
            {
                var mask = 1 << AssignedLayer;
                if (Reverse)
                {
                    _Camera.cullingMask |= mask;
                }
                else
                {
                    _Camera.cullingMask &= ~mask;
                }
            }
            base.RestoreLayer();
        }
    }
}