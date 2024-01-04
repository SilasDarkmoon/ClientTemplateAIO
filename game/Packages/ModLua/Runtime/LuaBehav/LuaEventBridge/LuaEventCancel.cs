using UnityEngine.EventSystems;

namespace LuaLib.UI
{
    public class LuaEventCancel : LuaBehavEx, ICancelHandler
    {
       
        public void OnCancel(BaseEventData eventData)
        {
            this.CallLuaFunc("onCancel", eventData);
        }
    }
}