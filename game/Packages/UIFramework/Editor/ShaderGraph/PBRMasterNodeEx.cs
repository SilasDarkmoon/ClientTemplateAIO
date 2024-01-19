#if !UNITY_2020_1_OR_NEWER
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.UIElements;
using System.Reflection;
using UnityEditor.Rendering.Universal;

using static System.Linq.Expressions.Expression;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    [Title("Master", "PBR (Ex)")]
    class PBRMasterNodeEx : PBRMasterNode, ISerializationCallbackReceiver
    {
        [MenuItem("Assets/Create/Shader/PBR Graph (Ex)", false, 209)]
        public static void CreateMaterialGraph()
        {
            GraphUtil.CreateNewGraph(new PBRMasterNodeEx());
        }

        [SerializeField] public RenderStateOverride RenderState = new RenderStateOverride();

        private static FieldInfo _fi_m_ForwardPass = typeof(UniversalPBRSubShader).GetField("m_ForwardPass", BindingFlags.NonPublic | BindingFlags.Instance);
        private static Func<UniversalPBRSubShader, ShaderPass> Func_GetForwardPass;
        private static Action<UniversalPBRSubShader, ShaderPass> Func_SetForwardPass;

        private UniversalPBRSubShader SubShader
        {
            get
            {
                return (UniversalPBRSubShader)subShaders.FirstOrDefault();
            }
        }
        public ShaderPass ForwardPass
        {
            get
            {
                if (Func_GetForwardPass == null)
                {
                    var tar = Parameter(typeof(UniversalPBRSubShader));
                    Func_GetForwardPass = Lambda<Func<UniversalPBRSubShader, ShaderPass>>(Field(tar, _fi_m_ForwardPass), tar).Compile();
                }
                return Func_GetForwardPass(SubShader);
            }
            set
            {
                if (Func_SetForwardPass == null)
                {
                    var tar = Parameter(typeof(UniversalPBRSubShader));
                    var val = Parameter(typeof(ShaderPass));
                    Func_SetForwardPass = Lambda<Action<UniversalPBRSubShader, ShaderPass>>(Assign(Field(tar, _fi_m_ForwardPass), val), tar, val).Compile();
                }
                Func_SetForwardPass(SubShader, value);
            }
        }

        public PBRMasterNodeEx()
        {
            RenderState.OnValueChanged += FixNodeAfterDeserialization;
            UpdateNodeAfterDeserialization();
        }

        public virtual void FixNodeAfterDeserialization()
        {
            var pass = ForwardPass;
            pass.StencilOverride = RenderState.GetStencilOverrideString();
            pass.CullOverride = RenderState.GetCullOverrideString();
            pass.ZWriteOverride = RenderState.GetZWriteOverrideString();
            pass.ZTestOverride = RenderState.GetZTestOverrideString();
            pass.BlendOverride = RenderState.GetBlendOverrideString();
            pass.ColorMaskOverride = RenderState.GetColorMaskOverrideString();
            ForwardPass = pass;
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
            var ele = base.CreateCommonSettingsElement();
            ele.Add(RenderState.CreateSettingsElement());
            return ele;
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
    [FormerName("UnityEditor.ShaderGraph.PBRMasterNodeEx")]
    class PBRMasterNodeEx : AbstractMaterialNode, IMasterNodeEx1
    {
        public PBRMasterNode1.Model m_Model;
        public PBRMasterNode1.SurfaceType m_SurfaceType;
        public PBRMasterNode1.AlphaMode m_AlphaMode;
        public bool m_TwoSided;
        public NormalDropOffSpace m_NormalDropOffSpace;
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