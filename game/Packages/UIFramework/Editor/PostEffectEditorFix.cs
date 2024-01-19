using System;
using System.Linq;
using UnityEditor;

namespace UnityEditorEx
{
    [InitializeOnLoad]
    sealed class PostEffectEditorFix
    {
        const string k_Define = "UNITY_POST_PROCESSING_STACK_V2";

        static PostEffectEditorFix()
        {
            var asms = AppDomain.CurrentDomain.GetAssemblies();
            if (asms.Any(asm => asm.GetName().Name == "Unity.Postprocessing.Runtime"))
            {
                var targets = from field in typeof(BuildTargetGroup).GetFields()
                              let attrs = field.GetCustomAttributes(typeof(ObsoleteAttribute), false)
                              where field.IsPublic && field.IsStatic && (attrs == null || attrs.Length == 0)
                              let val = (BuildTargetGroup)field.GetValue(null)
                              where val != BuildTargetGroup.Unknown
                              select (BuildTargetGroup)field.GetValue(null);

                foreach (var target in targets)
                {
                    var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(target).Trim();

                    var list = defines.Split(';', ' ')
                        .Where(x => !string.IsNullOrEmpty(x))
                        .ToList();

                    if (list.Contains(k_Define))
                        continue;

                    list.Add(k_Define);
                    defines = list.Aggregate((a, b) => a + ";" + b);

                    PlayerSettings.SetScriptingDefineSymbolsForGroup(target, defines);
                }
            }
        }
    }
}
