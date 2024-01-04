using UnityEngine.EventSystems;

namespace LuaLib.UI
{
    public class LuaEventSubmit : LuaBehavEx, ISubmitHandler
    {
        public void OnSubmit(BaseEventData eventData)
        {
            this.CallLuaFunc("onSubmit", eventData);
        }
    }
}