using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Object = UnityEngine.Object;

namespace UnityEngineEx
{
    public class ResHolderEx : MonoBehaviour
    {
        public struct ExHolderInfo
        {
            public WeakReference Parent;
            public object Res;
        }

        public LinkedList<ExHolderInfo> ExList;

        void Update()
        {
            if (ExList != null)
            {
                var node = ExList.First;
                while (node != null)
                {
                    var nxt = node.Next;
                    var info = node.Value;
                    bool dead = false;
                    object parent;
                    if ((parent = info.Parent.GetWeakReference<object>()) == null)
                    {
                        dead = true;
                    }
                    else
                    {
                        if (parent is Object && !((Object)parent))
                        {
                            dead = true;
                        }
                    }
                    if (dead)
                    {
                        ExList.Remove(node);
                    }
                    node = nxt;
                }
            }
        }

        public void AddHolder(object parent, object res)
        {
            if (parent != null && res != null)
            {
                if (ExList == null)
                {
                    ExList = new LinkedList<ExHolderInfo>();
                }
                ExList.AddLast(new ExHolderInfo() { Parent = new WeakReference(parent), Res = res });
            }
        }
    }
}