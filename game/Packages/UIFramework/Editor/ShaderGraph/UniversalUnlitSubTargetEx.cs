#if UNITY_2020_1_OR_NEWER
using System.Collections.Generic;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Legacy;

namespace UnityEditor.Rendering.Universal.ShaderGraph
{
    sealed class UniversalUnlitSubTargetEx : SubTargetExBase<UniversalTarget, UniversalUnlitSubTarget>.SubTargetExCanUpgrade<UnlitMasterNodeEx, UnlitMasterNode1>
    {
        public override bool TryUpgradeFromMasterNode(IMasterNode1 masterNode, out Dictionary<BlockFieldDescriptor, int> blockMap)
        {
            if (base.TryUpgradeFromMasterNode(masterNode, out blockMap))
            {
                var old = (UnlitMasterNodeEx)masterNode;
                target.surfaceType = (SurfaceType)old.m_SurfaceType;
                target.alphaMode = (AlphaMode)old.m_AlphaMode;
                target.renderFace = old.m_TwoSided ? RenderFace.Both : RenderFace.Front;
                SubTargetExUtils.UpgradeAlphaClip(target, old);
                target.customEditorGUI = old.m_OverrideEnabled ? old.m_ShaderGUIOverride : "";
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
#endif