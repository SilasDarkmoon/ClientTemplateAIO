using UnityEngine.EventSystems;

namespace LuaLib.UI
{
    public class LuaEventPointerExit : LuaBehavEx, IPointerExitHandler
    {
        public void OnPointerExit(PointerEventData eventData)
        {
            this.CallLuaFunc("onPointerExit", eventData);
        }
    }
}