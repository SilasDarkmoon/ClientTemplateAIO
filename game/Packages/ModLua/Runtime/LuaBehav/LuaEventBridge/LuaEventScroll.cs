using UnityEngine.EventSystems;

namespace LuaLib.UI
{
    public class LuaEventScroll : LuaBehavEx, IScrollHandler
    {
        public void OnScroll(PointerEventData eventData)
        {
            this.CallLuaFunc("onScroll", eventData);
        }
    }
}