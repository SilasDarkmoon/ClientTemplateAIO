using UnityEngine;
using UnityEngine.EventSystems;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngineEx
{
    public enum ResManifestItemType
    {
        None = 0,
        Normal = 1,
        Prefab = 2,
        Scene = 3,
        Redirect = 4,
        //PackedTex = 5,
        //DynTex = 6,
        //DynSprite = 7,
    }
    [Serializable]
    public sealed class ResOnDiskManifestItem
    {
        public int Type; // ResManifestItemType and Defined by Other Modules
        public int BRef;
        public int Ref;
        public ScriptableObject ExInfo;
    }
    [Serializable]
    public class ResOnDiskManifestNode
    {
        public int Level;
        public string PPath;
        public ResOnDiskManifestItem Item;
    }
    public class ResOnDiskManifest : ScriptableObject
    {
        public string MFlag;
        public string DFlag;
        public bool InMain;

        public string[] Bundles;
        public ResOnDiskManifestNode[] Assets;
    }
}
