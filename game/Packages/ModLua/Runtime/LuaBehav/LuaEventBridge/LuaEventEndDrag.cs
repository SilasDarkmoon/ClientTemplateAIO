using UnityEngine.EventSystems;

namespace LuaLib.UI
{
    public class LuaEventEndDrag : LuaBehavEx, IEndDragHandler
    {
        public void OnEndDrag(PointerEventData eventData)
        {
            this.CallLuaFunc("onEndDrag", eventData);
        }
    }
}