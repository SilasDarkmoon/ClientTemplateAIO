using UnityEngine;
using UnityEditor;
using UnityEngineEx;

[ExecuteInEditMode]
public class DynamicImageSaver : UnityEditor.AssetModificationProcessor
{
    static void OnWillSaveAssets (string[] paths)
    {
        var objs = Object.FindObjectsOfType<DynamicImage>();
        if (objs != null)
        {
            foreach(var obj in objs)
            {
                if (obj)
                {
                    obj.DestroyDynamicChild();
                }
            }
        }
    }
}
