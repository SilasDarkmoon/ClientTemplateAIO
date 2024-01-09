using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEngineEx
{
    [CreateAssetMenu]
    public sealed class ResAlias : ScriptableObject
    {
        public string Target;
    }
}