using UnityEngine.EventSystems;

namespace LuaLib.UI
{
    public class LuaEventPointerUp : LuaBehavEx, IPointerUpHandler
    {
        public void OnPointerUp(PointerEventData eventData)
        {
            this.CallLuaFunc("onPointerUp", eventData);
        }
    }
}