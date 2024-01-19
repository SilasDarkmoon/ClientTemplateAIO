using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class UniversalRenderFeature : ScriptableRendererFeature
{
    protected static readonly Dictionary<Camera, List<ComponentBasedRenderFeature>> _RegisteredFeatures = new Dictionary<Camera, List<ComponentBasedRenderFeature>>();
#if UNITY_EDITOR
    protected static readonly List<ComponentBasedRenderFeature>[] _RegisteredTypedFeatures = new List<ComponentBasedRenderFeature>[32];
#endif

    public static void RegRenderFeature(ComponentBasedRenderFeature feature)
    {
        var cam = feature.Camera;
        if (cam)
        {
            List<ComponentBasedRenderFeature> list;
            if (!_RegisteredFeatures.TryGetValue(cam, out list))
            {
                list = new List<ComponentBasedRenderFeature>();
                _RegisteredFeatures.Add(cam, list);
            }
            if (!list.Contains(feature))
            {
                list.Add(feature);
                if (list.Count > 1)
                {
                    // need sort.
                    HashSet<ComponentBasedRenderFeature> existing = new HashSet<ComponentBasedRenderFeature>(list);
                    list.Clear();
                    var allcomps = cam.gameObject.GetComponents<ComponentBasedRenderFeature>();
                    for (int i = 0; i < allcomps.Length; ++i)
                    {
                        var comp = allcomps[i];
                        if (existing.Remove(comp))
                        {
                            list.Add(comp);
                        }
                    }
                    if (existing.Count > 0)
                    {
                        list.AddRange(existing); // NOTICE: should we sort the rest in existing?
                    }
                }
            }
        }
#if UNITY_EDITOR
        else
        {
            var type = feature.TargetCameraType;
            uint tindex = (uint)type;
            BitArray32 arrtypes = new BitArray32(tindex);
            for (uint i = 0; i < arrtypes.capacity; ++i)
            {
                if (arrtypes[i])
                {
                    var list = _RegisteredTypedFeatures[i];
                    if (list == null)
                    {
                        list = new List<ComponentBasedRenderFeature>();
                        _RegisteredTypedFeatures[i] = list;
                    }
                    if (!list.Contains(feature))
                    {
                        list.Add(feature);
                    }
                }
            }
        }
#endif
    }
    public static void UnregRenderFeature(ComponentBasedRenderFeature feature)
    {
        {
            List<ComponentBasedRenderFeature> list;
            var cam = feature.Camera;
            if (_RegisteredFeatures.TryGetValue(cam, out list))
            {
                list.Remove(feature);
                if (list.Count == 0)
                {
                    _RegisteredFeatures.Remove(cam);
                }
            }
        }

#if UNITY_EDITOR
        var type = feature.TargetCameraType;
        uint tindex = (uint)type;
        BitArray32 arrtypes = new BitArray32(tindex);
        for (uint i = 0; i < arrtypes.capacity; ++i)
        {
            if (arrtypes[i])
            {
                var list = _RegisteredTypedFeatures[i];
                if (list != null)
                {
                    list.Remove(feature);
                    if (list.Count == 0)
                    {
                        _RegisteredTypedFeatures[i] = null;
                    }
                }
            }
        }
#endif

        RemoveDeadRenderFeatures();
    }
    public static void RemoveRenderFeature(ComponentBasedRenderFeature feature)
    {
        var cams = _RegisteredFeatures.Keys.ToArray();
        for (int i = 0; i < cams.Length; ++i)
        {
            var cam = cams[i];
            if (!cam)
            {
                _RegisteredFeatures.Remove(cam);
            }
            else
            {
                var list = _RegisteredFeatures[cam];
                for (int j = list.Count - 1; j >= 0; --j)
                {
                    if (feature == list[j])
                    {
                        list.RemoveAt(j);
                    }
                }
                if (list.Count == 0)
                {
                    _RegisteredFeatures.Remove(cam);
                }
            }
        }
#if UNITY_EDITOR
        for (int i = 0; i < _RegisteredTypedFeatures.Length; ++i)
        {
            var list = _RegisteredTypedFeatures[i];
            if (list != null)
            {
                for (int j = list.Count - 1; j >= 0; --j)
                {
                    if (feature == list[j])
                    {
                        list.RemoveAt(j);
                    }
                }
                if (list.Count == 0)
                {
                    _RegisteredTypedFeatures[i] = null;
                }
            }
        }
#endif
    }
    public static void RemoveDeadRenderFeatures()
    {
        var cams = _RegisteredFeatures.Keys.ToArray();
        for (int i = 0; i < cams.Length; ++i)
        {
            var cam = cams[i];
            if (!cam)
            {
                _RegisteredFeatures.Remove(cam);
            }
            else
            {
                var list = _RegisteredFeatures[cam];
                for (int j = list.Count - 1; j >= 0; --j)
                {
                    var feature = list[j];
                    if (!feature)
                    {
                        list.RemoveAt(j);
                    }
                }
                if (list.Count == 0)
                {
                    _RegisteredFeatures.Remove(cam);
                }
            }
        }
#if UNITY_EDITOR
        for (int i = 0; i < _RegisteredTypedFeatures.Length; ++i)
        {
            var list = _RegisteredTypedFeatures[i];
            if (list != null)
            {
                for (int j = list.Count - 1; j >= 0; --j)
                {
                    var feature = list[j];
                    if (!feature)
                    {
                        list.RemoveAt(j);
                    }
                }
                if (list.Count == 0)
                {
                    _RegisteredTypedFeatures[i] = null;
                }
            }
        }
#endif
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        {
            var cam = renderingData.cameraData.camera;
            List<ComponentBasedRenderFeature> list;
            if (_RegisteredFeatures.TryGetValue(cam, out list))
            {
                for (int i = 0; i < list.Count; ++i)
                {
                    var feature = list[i];
                    feature.AddRenderPasses(renderer, ref renderingData);
                }
            }
        }

#if UNITY_EDITOR
        var type = renderingData.cameraData.camera.cameraType;
        uint tindex = (uint)type;
        if (tindex > 0)
        {
            int bitpos = log2(tindex);
            var list = _RegisteredTypedFeatures[bitpos];
            if (list != null)
            {
                for (int i = 0; i < list.Count; ++i)
                {
                    var feature = list[i];
                    feature.AddRenderPasses(renderer, ref renderingData);
                }
            }
        }
#endif
    }
    public static int log2(uint n)
    {
        int result = 0;
        if ((n & 0xffff0000U) != 0) { result += 16; n >>= 16; }
        if ((n & 0x0000ff00U) != 0) { result += 8; n >>= 8; }
        if ((n & 0x000000f0U) != 0) { result += 4; n >>= 4; }
        if ((n & 0x0000000cU) != 0) { result += 2; n >>= 2; }
        if ((n & 0x00000002U) != 0) { result += 1; n >>= 1; }
        return result;
    }

    public override void Create()
    {
    }
}