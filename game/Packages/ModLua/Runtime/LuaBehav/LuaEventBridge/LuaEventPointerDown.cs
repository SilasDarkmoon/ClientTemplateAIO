using UnityEngine.EventSystems;

namespace LuaLib.UI
{
    public class LuaEventPointerDown : LuaBehavEx, IPointerDownHandler
    {
        public void OnPointerDown(PointerEventData eventData)
        {
            this.CallLuaFunc("onPointerDown", eventData);
        }
    }
}