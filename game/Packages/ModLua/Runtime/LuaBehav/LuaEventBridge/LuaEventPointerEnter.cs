using UnityEngine.EventSystems;

namespace LuaLib.UI
{
    public class LuaEventPointerEnter : LuaBehavEx, IPointerEnterHandler
    {
        public void OnPointerEnter(PointerEventData eventData)
        {
            this.CallLuaFunc("onPointerEnter", eventData);
        }
    }
}