using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UnityEngineEx.UI
{
    public class GradientImage : BaseMeshEffect
    {
        public static int elementStep = 6;

        [SerializeField]
        private Color m_topLeft = Color.white;
        [SerializeField]
        private Color m_topRight = Color.white;
        [SerializeField]
        private Color m_bottomLeft = Color.white;
        [SerializeField]
        private Color m_bottomRight = Color.white;
        [SerializeField]
        private bool m_useGraphicAlpha = true;

        protected GradientImage()
        { }

        public bool useGraphicAlpha
        {
            get { return m_useGraphicAlpha; }
            set
            {
                m_useGraphicAlpha = value;
                if (graphic != null)
                    graphic.SetVerticesDirty();
            }
        }

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive())
                return;

            List<UIVertex> output = new List<UIVertex>();
            vh.GetUIVertexStream(output);
            ApplyGradientColor(output);
            vh.Clear();
            vh.AddUIVertexTriangleStream(output);
            output.Clear();
        }

        public void modifyColors(Color topLeft, Color topRight, Color bottomLeft, Color bottomRight)
        {
            m_topLeft = topLeft;
            m_topRight = topRight;
            m_bottomLeft = bottomLeft;
            m_bottomRight = bottomRight;
            if (graphic != null)
                graphic.SetVerticesDirty();
        }

        private void ApplyGradientColor(List<UIVertex> verts)
        {
            if (verts.Count != 6) return;

            // 修改原有的UIVertex数据
            //      1----2
            //      |  / |
            //      | /  |
            //      0----3
            // 顶点顺序为：unity5 : 0-1-2==2-3-0,
            UIVertex leftTopPos = verts[1];
            UIVertex rightTopPos = verts[2];
            UIVertex rightBottomPos = verts[4];
            UIVertex leftBottomPos = verts[0];
            leftTopPos.color = m_topLeft;
            rightTopPos.color = m_topRight;
            leftBottomPos.color = m_bottomLeft;
            rightBottomPos.color = m_bottomRight;

            verts[0] = leftBottomPos;
            verts[1] = leftTopPos;
            verts[2] = rightTopPos;
            verts[3] = rightTopPos;
            verts[4] = rightBottomPos;
            verts[5] = leftBottomPos;
        }
    }
}
