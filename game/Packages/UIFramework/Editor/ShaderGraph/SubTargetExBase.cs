#if UNITY_2020_1_OR_NEWER
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.ShaderGraph;
using UnityEngine.Rendering;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEditor.ShaderGraph.Legacy;

namespace UnityEditor.Rendering.Universal.ShaderGraph
{
    abstract class SubTargetExBase<TTarget, TInner> : SubTarget<TTarget>, ILegacyTarget where TTarget : Target where TInner : SubTarget<TTarget>, new()
    {
        [SerializeField] internal TInner _Inner;
        [SerializeField] public RenderStateOverride RenderState;

        public SubTargetExBase()
        {
            _Inner = new TInner();
            RenderState = new RenderStateOverride();
            displayName = _Inner.displayName + " Ex";
        }

        public override void OnBeforeSerialize()
        {
            base.OnBeforeSerialize();
            if (_Inner == null)
            {
                _Inner = new TInner();
                if (target != null)
                {
                    _Inner.target = target;
                }
            }
            if (RenderState == null)
            {
                RenderState = new RenderStateOverride();
            }
        }
        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();
            if (_Inner == null)
            {
                _Inner = new TInner();
                if (target != null)
                {
                    _Inner.target = target;
                }
            }
            if (RenderState == null)
            {
                RenderState = new RenderStateOverride();
            }
        }
        public override void OnAfterDeserialize(string json)
        {
            base.OnAfterDeserialize(json);
            if (_Inner == null)
            {
                _Inner = new TInner();
                if (target != null)
                {
                    _Inner.target = target;
                }
            }
            if (RenderState == null)
            {
                RenderState = new RenderStateOverride();
            }
        }
        public override void OnAfterMultiDeserialize(string json)
        {
            base.OnAfterMultiDeserialize(json);
            if (_Inner == null)
            {
                _Inner = new TInner();
                if (target != null)
                {
                    _Inner.target = target;
                }
            }
            if (RenderState == null)
            {
                RenderState = new RenderStateOverride();
            }
        }

        public override void GetActiveBlocks(ref TargetActiveBlockContext context)
        {
            if (_Inner.target == null)
            {
                _Inner.target = target;
            }
            _Inner.GetActiveBlocks(ref context);
        }
        public override void GetFields(ref TargetFieldContext context)
        {
            if (_Inner.target == null)
            {
                _Inner.target = target;
            }
            _Inner.GetFields(ref context);
        }
        public override void GetPropertiesGUI(ref TargetPropertyGUIContext context, Action onChange, Action<string> registerUndo)
        {
            if (_Inner.target == null)
            {
                _Inner.target = target;
            }
            _Inner.GetPropertiesGUI(ref context, onChange, registerUndo);

            context.Add(RenderState.CreateSettingsElement());
        }
        public override bool IsActive()
        {
            if (_Inner.target == null)
            {
                _Inner.target = target;
            }
            return _Inner.IsActive();
        }
        public override void Setup(ref TargetSetupContext context)
        {
            if (_Inner.target == null)
            {
                _Inner.target = target;
            }
            _Inner.Setup(ref context);

            for (int i = 0; i < context.subShaders.Count; ++i)
            {
                var sub = context.subShaders[i];
                var passes = new PassCollection();
                int passindex = 0;
                foreach (var pass in sub.passes)
                {
                    if (passindex == 0
						|| pass.descriptor.lightMode == "Universal2D"
						|| pass.descriptor.lightMode == "UniversalForward")
					{
                        var newpass = pass.descriptor;
                        newpass.renderStates = RenderState.ModifyRenderStateCollection(newpass.renderStates);
                        passes.Add(newpass, pass.fieldConditions);
                    }
                    else
                    {
                        passes.Add(pass.descriptor, pass.fieldConditions);
                    }
                    ++passindex;
                }
                sub.passes = passes;
                context.subShaders[i] = sub;
            }
        }

        public override void CollectShaderProperties(PropertyCollector collector, GenerationMode generationMode)
        {
            if (_Inner.target == null)
            {
                _Inner.target = target;
            }
            _Inner.CollectShaderProperties(collector, generationMode);
        }
        public override void ProcessPreviewMaterial(Material material)
        {
            if (_Inner.target == null)
            {
                _Inner.target = target;
            }
            _Inner.ProcessPreviewMaterial(material);
        }
        public override object saveContext
        {
            get
            {
                if (_Inner.target == null)
                {
                    _Inner.target = target;
                }
                return _Inner.saveContext;
            }
        }

        public virtual bool TryUpgradeFromMasterNode(IMasterNode1 masterNode, out Dictionary<BlockFieldDescriptor, int> blockMap)
        {
            blockMap = null;
            return false;
        }

        public abstract class SubTargetExCanUpgrade<TMasterNode, TMasterNodeInner> : SubTargetExBase<TTarget, TInner> where TMasterNode : IMasterNodeEx1 where TMasterNodeInner : IMasterNode1, new()
        {
            public override bool TryUpgradeFromMasterNode(IMasterNode1 masterNode, out Dictionary<BlockFieldDescriptor, int> blockMap)
            {
                if (masterNode is TMasterNode oldex && _Inner is ILegacyTarget inner)
                {
                    var oldnorm = new TMasterNodeInner();
                    inner.TryUpgradeFromMasterNode(oldnorm, out blockMap);
                    RenderState = oldex.GetRenderStateOverride() ?? new RenderStateOverride();
                    return true;
                }
                else
                {
                    return base.TryUpgradeFromMasterNode(masterNode, out blockMap);
                }
            }
        }
    }

    internal static class SubTargetExUtils
    {
        public static void UpgradeAlphaClip(UniversalTarget target, AbstractMaterialNode node)
        {
            var clipThresholdId = 8;
            var clipThresholdSlot = node.FindSlot<Vector1MaterialSlot>(clipThresholdId);
            if (clipThresholdSlot == null)
                return;

            clipThresholdSlot.owner = node;
            if (clipThresholdSlot.isConnected || clipThresholdSlot.value > 0.0f)
            {
                target.alphaClip = true;
            }
        }
    }
}
#endif
