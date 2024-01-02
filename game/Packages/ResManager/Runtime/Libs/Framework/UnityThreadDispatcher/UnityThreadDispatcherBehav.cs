using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnityThreadDispatcherBehav : MonoBehaviour
{
	void Update ()
    {
		if (UnityEngineEx.UnityThreadDispatcher._RunningObj != gameObject)
        {
            Destroy(gameObject);
            return;
        }

        UnityEngineEx.UnityThreadDispatcher.HandleEvents();
    }
}
