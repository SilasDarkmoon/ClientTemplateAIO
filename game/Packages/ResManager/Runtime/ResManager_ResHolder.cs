using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Object = UnityEngine.Object;

namespace UnityEngineEx
{
    public static partial class ResManager
    {
        public static void HoldRes(this GameObject parent, Object res)
        {
            if (parent && res)
            {
                var holder = parent.GetComponent<ResHolder>();
                if (!holder)
                {
                    holder = parent.AddComponent<ResHolder>();
                }
                if (holder.ResList == null)
                {
                    holder.ResList = new List<Object>();
                }
                holder.ResList.Add(res);
            }
        }
        public static void HoldRes(this Component parent, Object res)
        {
            if (parent && res)
            {
                HoldRes(parent.gameObject, res);
            }
        }
        private static ResHolderEx _ExHolder;
        public static void HoldRes(this object parent, object res)
        {
            if (!_ExHolder)
            {
                var holderobj = new GameObject();
                holderobj.hideFlags = HideFlags.HideAndDontSave;
                Object.DontDestroyOnLoad(holderobj);
                _ExHolder = holderobj.AddComponent<ResHolderEx>();
            }
            _ExHolder.AddHolder(parent, res);
        }
    }
}