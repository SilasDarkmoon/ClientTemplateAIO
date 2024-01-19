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
    [Title("Master", "Sprite Unlit (Ex)")]
    class SpriteUnlitMasterNodeEx : SpriteUnlitMasterNode, ISerializationCallbackReceiver
    {
        [MenuItem("Assets/Create/Shader/2D Renderer/Sprite Unlit Graph (Ex)", false, 209)]
        public static void CreateMaterialGraph()
        {
            GraphUtil.CreateNewGraph(new SpriteUnlitMasterNodeEx());
        }

        [SerializeField] public RenderStateOverride RenderState = new RenderStateOverride();

        private static FieldInfo _fi_m_UnlitPass = typeof(UniversalSpriteUnlitSubShader).GetField("m_UnlitPass", BindingFlags.NonPublic | BindingFlags.Instance);
        private static Func<UniversalSpriteUnlitSubShader, ShaderPass> Func_GetUnlitPass;
        private static Action<UniversalSpriteUnlitSubShader, ShaderPass> Func_SetUnlitPass;

        private UniversalSpriteUnlitSubShader SubShader
        {
            get
            {
                return (UniversalSpriteUnlitSubShader)subShaders.FirstOrDefault();
            }
        }
        public ShaderPass UnlitPass
        {
            get
            {
                if (Func_GetUnlitPass == null)
                {
                    var tar = Parameter(typeof(UniversalSpriteUnlitSubShader));
                    Func_GetUnlitPass = Lambda<Func<UniversalSpriteUnlitSubShader, ShaderPass>>(Field(tar, _fi_m_UnlitPass), tar).Compile();
                }
                return Func_GetUnlitPass(SubShader);
            }
            set
            {
                if (Func_SetUnlitPass == null)
                {
                    var tar = Parameter(typeof(UniversalSpriteUnlitSubShader));
                    var val = Parameter(typeof(ShaderPass));
                    Func_SetUnlitPass = Lambda<Action<UniversalSpriteUnlitSubShader, ShaderPass>>(Assign(Field(tar, _fi_m_UnlitPass), val), tar, val).Compile();
                }
                Func_SetUnlitPass(SubShader, value);
            }
        }

        public SpriteUnlitMasterNodeEx()
        {
            RenderState.OnValueChanged += FixNodeAfterDeserialization;
            UpdateNodeAfterDeserialization();
        }

        public virtual void FixNodeAfterDeserialization()
        {
            var pass = UnlitPass;
            pass.StencilOverride = RenderState.GetStencilOverrideString();
            pass.CullOverride = RenderState.GetCullOverrideString();
            pass.ZWriteOverride = RenderState.GetZWriteOverrideString();
            pass.ZTestOverride = RenderState.GetZTestOverrideString();
            pass.BlendOverride = RenderState.GetBlendOverrideString();
            pass.ColorMaskOverride = RenderState.GetColorMaskOverrideString();
            UnlitPass = pass;
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
    [FormerName("UnityEditor.ShaderGraph.SpriteUnlitMasterNodeEx")]
    class SpriteUnlitMasterNodeEx : AbstractMaterialNode, IMasterNodeEx1
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