#if UNITY_2020_1_OR_NEWER
using System.Collections.Generic;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Legacy;

namespace UnityEditor.Rendering.Universal.ShaderGraph
{
    sealed class UniversalSpriteUnlitSubTargetEx : SubTargetExBase<UniversalTarget, UniversalSpriteUnlitSubTarget>.SubTargetExCanUpgrade<SpriteUnlitMasterNodeEx, SpriteUnlitMasterNode1>
    {
        public override bool TryUpgradeFromMasterNode(IMasterNode1 masterNode, out Dictionary<BlockFieldDescriptor, int> blockMap)
        {
            if (base.TryUpgradeFromMasterNode(masterNode, out blockMap))
            {
                var old = (SpriteUnlitMasterNodeEx)masterNode;
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