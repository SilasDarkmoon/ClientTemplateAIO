using UnityEngine.EventSystems;

namespace LuaLib.UI
{
    public class LuaEventBeginDrag : LuaBehavEx, IBeginDragHandler
    {
        public void OnBeginDrag(PointerEventData eventData)
        {
            this.CallLuaFunc("onBeginDrag", eventData);
        }
    }
}
