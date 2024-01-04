using UnityEngine.EventSystems;

namespace LuaLib.UI
{
    public class LuaEventMove : LuaBehavEx, IMoveHandler
    {
        public void OnMove(AxisEventData eventData)
        {
            this.CallLuaFunc("onMove", eventData);
        }
    }
}