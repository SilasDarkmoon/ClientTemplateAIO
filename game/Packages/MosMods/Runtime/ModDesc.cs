using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngineEx
{
    [CreateAssetMenu(fileName = "resdesc.asset", menuName = "Module Desc", order = 2000)]
    public class ModDesc : ScriptableObject
#if UNITY_EDITOR
        , ISerializationCallbackReceiver
#endif
    {
        [SerializeField, HideInInspector]
        private string _Mod;
        public bool InMain;
        public string[] Deps;

        public string Mod
        {
            get
            {
#if UNITY_EDITOR
                CheckSelfMod();
#endif
                return _Mod;
            }
#if UNITY_EDITOR
            set
            {
                _Mod = value;
            }
#endif
        }
        public bool IsOptional
        {
            get { return !InMain || (Deps != null && Deps.Length > 0); }
        }

#if UNITY_EDITOR
        public void OnAfterDeserialize()
        {
        }
        public void OnBeforeSerialize()
        {
            CheckSelfMod();
        }
        private void CheckSelfMod()
        {
            _Mod = null;
            var path = UnityEditor.AssetDatabase.GetAssetPath(this);
            if (!string.IsNullOrEmpty(path))
            {
                _Mod = "";
                if (path.StartsWith("Assets/Mods/"))
                {
                    path = path.Substring("Assets/Mods/".Length);
                    var im = path.IndexOf("/");
                    if (im > 0)
                    {
                        path = path.Substring(0, im);
                        _Mod = path;
                    }
                }
                else if (path.StartsWith("Packages/"))
                {
                    path = path.Substring("Packages/".Length);
                    var im = path.IndexOf("/");
                    if (im > 0)
                    {
                        path = path.Substring(0, im);
                        _Mod = EditorToClientUtils.GetModNameFromPackageName(path);
                    }
                }
            }
        }
#endif
    }
}
