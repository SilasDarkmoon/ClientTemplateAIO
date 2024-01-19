#if UNITY_2020_1_OR_NEWER
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Rendering.BuiltIn;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.Rendering.Universal.ShaderGraph
{
    sealed class UniversalUISubTarget : SubTargetExBase<UniversalTarget, UniversalSpriteUnlitSubTarget>
    {
		public UniversalUISubTarget()
		{
			displayName = "UI";

			RenderState.OverrideStencil = true;
			RenderState.StencilRefString = "[_Stencil]";
			RenderState.StencilCompString = "[_StencilComp]";
			RenderState.StencilPassString = "[_StencilOp]";
			RenderState.StencilReadMaskString = "[_StencilReadMask]";
			RenderState.StencilWriteMaskString = "[_StencilWriteMask]";

			RenderState.OverrideCull = true;
			RenderState.Cull = UnityEngine.Rendering.CullMode.Off;

			RenderState.OverrideZWrite = true;
			RenderState.ZWrite = false;

			RenderState.OverrideZTest = true;
			RenderState.ZTestString = "[unity_GUIZTestMode]";

			RenderState.OverrideBlend = true;
			RenderState.BlendEnabled = true;
			RenderState.SrcFactor = UnityEngine.Rendering.BlendMode.One;
			RenderState.DstFactor = UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha;

			RenderState.OverrideColorMask = true;
			RenderState.ColorMaskString = "[_ColorMask]";
		}
		public override void Setup(ref TargetSetupContext context)
		{
			base.Setup(ref context);
			var subshader = context.subShaders[0];
			var passes = subshader.passes;
			var lastpass = passes.Last();
			passes = new PassCollection();
			passes.Add(lastpass.descriptor, lastpass.fieldConditions);
			subshader.passes = passes;
			context.subShaders[0] = subshader;
			_ExistingProperties = null;
		}
		private Dictionary<string, int> _ExistingProperties;
		public override void CollectShaderProperties(PropertyCollector collector, GenerationMode generationMode)
		{
			base.CollectShaderProperties(collector, generationMode);
			HashSet<string> exstingProps = new HashSet<string>();
			if (_ExistingProperties == null)
			{
				_ExistingProperties = new Dictionary<string, int>();
				int i = 0;
				foreach (var prop in collector.properties)
				{
					exstingProps.Add(prop.referenceName);
					_ExistingProperties[prop.referenceName] = i;
					++i;
				}
			}
			else
			{
				exstingProps.UnionWith(_ExistingProperties.Keys);
			}
			if (!exstingProps.Contains("_MainTex"))
			{
				collector.AddShaderProperty(new Texture2DShaderProperty()
				{
					displayName = "Sprite Texture",
					overrideReferenceName = "_MainTex",
					useTilingAndOffset = true,
				});
			}
			if (!exstingProps.Contains("_Color"))
			{
				collector.AddShaderProperty(new ColorShaderProperty()
				{
					displayName = "Tint",
					overrideReferenceName = "_Color",
					value = new UnityEngine.Color(1, 1, 1, 1),
				});
			}
			if (!exstingProps.Contains("_StencilComp"))
			{
				collector.AddShaderProperty(new Vector1ShaderProperty()
				{
					displayName = "Stencil Comparison",
					overrideReferenceName = "_StencilComp",
					value = 8,
				});
			}
			if (!exstingProps.Contains("_Stencil"))
			{
				collector.AddShaderProperty(new Vector1ShaderProperty()
				{
					displayName = "Stencil ID",
					overrideReferenceName = "_Stencil",
					value = 0,
				});
			}
			if (!exstingProps.Contains("_StencilOp"))
			{
				collector.AddShaderProperty(new Vector1ShaderProperty()
				{
					displayName = "Stencil Operation",
					overrideReferenceName = "_StencilOp",
					value = 0,
				});
			}
			if (!exstingProps.Contains("_StencilWriteMask"))
			{
				collector.AddShaderProperty(new Vector1ShaderProperty()
				{
					displayName = "Stencil Write Mask",
					overrideReferenceName = "_StencilWriteMask",
					value = 255,
				});
			}
			if (!exstingProps.Contains("_StencilReadMask"))
			{
				collector.AddShaderProperty(new Vector1ShaderProperty()
				{
					displayName = "Stencil Read Mask",
					overrideReferenceName = "_StencilReadMask",
					value = 255,
				});
			}
			if (!exstingProps.Contains("_ColorMask"))
			{
				collector.AddShaderProperty(new Vector1ShaderProperty()
				{
					displayName = "Color Mask",
					overrideReferenceName = "_ColorMask",
					value = 15,
				});
			}
			if (!exstingProps.Contains("_UseUIAlphaClip"))
			{
				collector.AddShaderProperty(new Vector1ShaderProperty()
				{
					displayName = "Use Alpha Clip",
					overrideReferenceName = "_UseUIAlphaClip",
					value = 0,
				});
			}
		}
	}
}
#endif
