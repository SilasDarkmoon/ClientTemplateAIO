using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Object = UnityEngine.Object;

namespace UnityEngineEx
{
    public interface IDataReceiver
    {
        void Receive(IDictionary<string, object> data);
    }
}