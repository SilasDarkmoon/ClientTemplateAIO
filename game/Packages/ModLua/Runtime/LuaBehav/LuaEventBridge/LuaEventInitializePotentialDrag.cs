using UnityEngine.EventSystems;

namespace LuaLib.UI
{
    public class LuaEventInitializePotentialDrag : LuaBehavEx, IInitializePotentialDragHandler
    {
        public void OnInitializePotentialDrag(PointerEventData eventData)
        {
            this.CallLuaFunc("onInitializePotentialDrag", eventData);
        }
    }
}