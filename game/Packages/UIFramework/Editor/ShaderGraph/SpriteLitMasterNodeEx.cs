#if !UNITY_2020_1_OR_NEWER
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using UnityEditor;
using UnityEditor.Experimental.Rendering.Universal;
using UnityEditor.Rendering.Universal;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.Rendering;
using static System.Linq.Expressions.Expression;
using UnityEditor.Graphing;
using System.Linq;
using UnityEngine.UIElements;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.Graphing.Util;
using UnityEditor.UIElements;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    [Title("Master", "Sprite Lit (Ex)")]
    class SpriteLitMasterNodeEx : SpriteLitMasterNode, ISerializationCallbackReceiver
    {
        [MenuItem("Assets/Create/Shader/2D Renderer/Sprite Lit Graph (Ex)", false, 209)]
        public static void CreateMaterialGraph()
        {
            GraphUtil.CreateNewGraph(new SpriteLitMasterNodeEx());
        }

        [SerializeField] public RenderStateOverride RenderState = new RenderStateOverride();

        private static FieldInfo _fi_m_LitPass = typeof(UniversalSpriteLitSubShader).GetField("m_LitPass", BindingFlags.NonPublic | BindingFlags.Instance);
        private static Func<UniversalSpriteLitSubShader, ShaderPass> Func_GetLitPass;
        private static Action<UniversalSpriteLitSubShader, ShaderPass> Func_SetLitPass;

        private UniversalSpriteLitSubShader SubShader
        {
            get
            {
                return (UniversalSpriteLitSubShader)subShaders.FirstOrDefault();
            }
        }
        public ShaderPass LitPass
        {
            get
            {
                if (Func_GetLitPass == null)
                {
                    var tar = Parameter(typeof(UniversalSpriteLitSubShader));
                    Func_GetLitPass = Lambda<Func<UniversalSpriteLitSubShader, ShaderPass>>(Field(tar, _fi_m_LitPass), tar).Compile();
                }
                return Func_GetLitPass(SubShader);
            }
            set
            {
                if (Func_SetLitPass == null)
                {
                    var tar = Parameter(typeof(UniversalSpriteLitSubShader));
                    var val = Parameter(typeof(ShaderPass));
                    Func_SetLitPass = Lambda<Action<UniversalSpriteLitSubShader, ShaderPass>>(Assign(Field(tar, _fi_m_LitPass), val), tar, val).Compile();
                }
                Func_SetLitPass(SubShader, value);
            }
        }

        public SpriteLitMasterNodeEx()
        {
            RenderState.OnValueChanged += FixNodeAfterDeserialization;
            UpdateNodeAfterDeserialization();
        }

        public virtual void FixNodeAfterDeserialization()
        {
            var pass = LitPass;
            pass.StencilOverride = RenderState.GetStencilOverrideString();
            pass.CullOverride = RenderState.GetCullOverrideString();
            pass.ZWriteOverride = RenderState.GetZWriteOverrideString();
            pass.ZTestOverride = RenderState.GetZTestOverrideString();
            pass.BlendOverride = RenderState.GetBlendOverrideString();
            pass.ColorMaskOverride = RenderState.GetColorMaskOverrideString();
            LitPass = pass;
        }

        public new virtual void UpdateNodeAfterDeserialization()
        {
            base.UpdateNodeAfterDeserialization();
            FixNodeAfterDeserialization();
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            base.OnAfterDeserialize();
            FixNodeAfterDeserialization();
        }

        protected override VisualElement CreateCommonSettingsElement()
        {
            return RenderState.CreateSettingsElement();
        }
    }
}
#endif

#if UNITY_2020_1_OR_NEWER
using System;
using System.Collections.Generic;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph.Legacy
{
    [FormerName("UnityEditor.ShaderGraph.SpriteLitMasterNodeEx")]
    class SpriteLitMasterNodeEx : AbstractMaterialNode, IMasterNodeEx1
    {
        public string m_ShaderGUIOverride;
        public bool m_OverrideEnabled;

        public RenderStateOverride RenderState;
        public RenderStateOverride GetRenderStateOverride()
        {
            return RenderState;
        }
    }
}
#endif