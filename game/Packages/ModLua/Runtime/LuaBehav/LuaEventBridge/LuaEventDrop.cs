using UnityEngine.EventSystems;

namespace LuaLib.UI
{
    public class LuaEventDrop : LuaBehavEx, IDropHandler
    {
        public void OnDrop(PointerEventData eventData)
        {
            this.CallLuaFunc("onDrop", eventData);
        }
    }
}