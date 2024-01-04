using UnityEngine.EventSystems;

namespace LuaLib.UI
{
    public class LuaEventSelect : LuaBehavEx, ISelectHandler
    {
        public void OnSelect(BaseEventData eventData)
        {
             this.CallLuaFunc("onSelect", eventData);
        }
    }
}