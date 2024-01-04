using UnityEngine.EventSystems;

namespace LuaLib.UI
{
    public class LuaEventDrag : LuaBehavEx, IDragHandler
    {
        public void OnDrag(PointerEventData eventData)
        {
            this.CallLuaFunc("onDrag", eventData);
        }
    }
}