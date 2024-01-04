using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;

namespace LuaLib.UI
{
    public class LuaEventDeselect : LuaBehavEx, IDeselectHandler
    {
        public void OnDeselect(BaseEventData eventData)
        {
            this.CallLuaFunc("onDeselect", eventData);
        }
    }
}