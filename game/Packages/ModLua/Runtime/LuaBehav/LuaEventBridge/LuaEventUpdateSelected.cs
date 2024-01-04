using UnityEngine.EventSystems;

namespace LuaLib.UI
{
    public class LuaEventUpdateSelected : LuaBehavEx, IUpdateSelectedHandler
    {
        public void OnUpdateSelected(BaseEventData eventData)
        {
            this.CallLuaFunc("onUpdateSelected", eventData);
        }
    }
}