using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Object = UnityEngine.Object;

namespace UnityEngineEx
{
    public class PrefabHolder : MonoBehaviour, ISerializationCallbackReceiver
    {
        [NonSerialized]
        public ResManagerAB.ClientResLoader.AssetInfo_Prefab AssetInfo;
        [SerializeField]
        private PrefabRef RefHandle;
        [SerializeField]
        private bool IsInstantiated;

        public object GetHandle()
        {
            if (AssetInfo != null)
            {
                var handle = AssetInfo.Hold();
                return handle;
            }
            return null;
        }

        public void OnAfterDeserialize()
        {
            if (!IsInstantiated)
            {
                IsInstantiated = true;
                var old = RefHandle;
                RefHandle = ScriptableObject.CreateInstance<PrefabRef>();
                RefHandle.RefHandle = old.RefHandle;
                old.RefHandle = null;
            }
        }

        public void OnBeforeSerialize()
        {
            if (!IsInstantiated)
            {
                var handle = GetHandle();
                if (!RefHandle)
                {
                    RefHandle = ScriptableObject.CreateInstance<PrefabRef>();
                }
                RefHandle.RefHandle = handle;
            }
        }
    }
}