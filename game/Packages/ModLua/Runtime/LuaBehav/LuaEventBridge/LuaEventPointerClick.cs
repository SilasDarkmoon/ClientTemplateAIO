using UnityEngine.EventSystems;

namespace LuaLib.UI
{
    public class LuaEventPointerClick : LuaBehavEx, IPointerClickHandler
    {
        public void OnPointerClick(PointerEventData eventData)
        {
            this.CallLuaFunc("onPointerClick", eventData);
        }
    }
}