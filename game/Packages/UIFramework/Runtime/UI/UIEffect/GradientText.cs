using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.UI
{
#if UNITY_5 || UNITY_5_3_OR_NEWER
    public class GradientText : BaseMeshEffect
    {
        public static int elementStep = 6;
#else
    public class GradientText : BaseVertexEffect
    {
        public static int elementStep = 4;
#endif
        [System.Serializable]
        public struct KeyPoint
        {
#if UNITY_EDITOR
            [Range(0, 1)]
#endif
            public float percent;
            public Color color;
            public KeyPoint(float percent, Color color)
            {
                this.percent = percent;
                this.color = color;
            }
        }
        [SerializeField]
        private KeyPoint[] m_keyPointsColor = new KeyPoint[] { };
        [SerializeField]
        private bool m_useGraphicAlpha = true;
        [SerializeField]
        private bool m_horizontal = false;
        private int m_startPointIndex = 0;

        protected GradientText()
        { }

        public KeyPoint[] keyPointColors
        {
            get { return m_keyPointsColor; }
            set
            {
                m_keyPointsColor = value;
                if (graphic != null)
                    graphic.SetVerticesDirty();
            }
        }

        public void ResetPointColors(int size)
        {
            m_keyPointsColor = new KeyPoint[size];
            m_startPointIndex = 0;
            if (graphic != null)
                graphic.SetVerticesDirty();
        }

        public void AddPointColors(float percent, Color color)
        {
            var keyPoint = new KeyPoint(percent, color);
            m_keyPointsColor[m_startPointIndex] = keyPoint;
            m_startPointIndex++;
        }

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

        public bool Horizontal
        {
            get { return m_horizontal; }
            set
            {
                m_horizontal = value;
                if (graphic != null)
                    graphic.SetVerticesDirty();
            }
        }

        public void UpdateKeyPointColor(int index, Color color)
        {
            if (index < m_keyPointsColor.Length)
            {
                m_keyPointsColor[index].color = color;
                if (graphic != null)
                    graphic.SetVerticesDirty();
            }
            
        }

#if UNITY_5 || UNITY_5_3_OR_NEWER
        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive())
                return;

            List<UIVertex> output = new List<UIVertex>();
            vh.GetUIVertexStream(output);
            if(m_horizontal)
            {
                ApplyGradientColorHorizontal(m_keyPointsColor, output);
            }
            else
            {
                ApplyGradientColorVertial(m_keyPointsColor, output);
            }
            vh.Clear();
            vh.AddUIVertexTriangleStream(output);
            output.Clear();
        }
#else
        public override void ModifyVertices(List<UIVertex> vh)
        {
            int count = vh.Count;
            if (!IsActive() || count == 0)
            {
                return;
            }

            ApplyGradientColor(m_keyPointsColor, vh);
        }
#endif

        private void ApplyGradientColorVertial(KeyPoint[] keyPointsColor, List<UIVertex> verts)
        {
            if (verts.Count <= 0 || keyPointColors.Length <= 0) return;

            int count = verts.Count;
            int vertexNum = (keyPointsColor.Length - 1) * count;
            if (verts.Capacity < vertexNum)
                verts.Capacity = vertexNum;

            for (int i = 0; i < count; )
            {
                // 修改并增加UIVertex
                // ！！！ Unity5.5中是6个UIVertex(两个三角形)组成一个字，在Unity4.7中是4个UIVertex（一个四边形）组成一个字
                //      0----1
                //      | \  |
                //      |  \ |
                //      3----2
                // 顶点顺序为：unity5.5 : 0-1-2==2-3-0,  unity4.7 : 0-1-2-3
                // 以左下角为原点
                UIVertex leftTopPos = verts[i + 0];
                UIVertex rightTopPos = verts[i + 1];
                UIVertex rightBottomPos = verts[i + 2];
#if UNITY_5 || UNITY_5_3_OR_NEWER
                UIVertex leftBottomPos = verts[i + 4];
#else
                UIVertex leftBottomPos = verts[i + 3];
#endif

                UIVertex lastLerpLeftPos = leftTopPos;
                lastLerpLeftPos.color = (Color32)keyPointColors[0].color;
                UIVertex lastLerpRightPos = rightTopPos;
                lastLerpRightPos.color = (Color32)keyPointColors[0].color;

                float originAlpha = verts[i + 0].color.a;
                for (int j = 0; j < keyPointsColor.Length - 1; j++)
                {
                    Color32 bottomColor = (Color32)keyPointsColor[j + 1].color;
                    if (m_useGraphicAlpha)
                    {
                        bottomColor.a = (byte)((bottomColor.a * originAlpha) / 255);
                    }
                    // left top (0)
                    UIVertex vt0 = lastLerpLeftPos;
                    // right top (1)
                    UIVertex vt1 = lastLerpRightPos;

                    lastLerpLeftPos.position = Vector3.Lerp(leftTopPos.position, leftBottomPos.position, keyPointsColor[j + 1].percent);
                    lastLerpLeftPos.uv0 = Vector2.Lerp(leftTopPos.uv0, leftBottomPos.uv0, keyPointsColor[j + 1].percent);
                    lastLerpLeftPos.color = bottomColor;
                    lastLerpRightPos.position = Vector3.Lerp(rightTopPos.position, rightBottomPos.position, keyPointsColor[j + 1].percent);
                    lastLerpRightPos.uv0 = Vector2.Lerp(rightTopPos.uv0, rightBottomPos.uv0, keyPointsColor[j + 1].percent);
                    lastLerpRightPos.color = bottomColor;

                    // right bottom (2)
                    UIVertex vt2 = lastLerpRightPos;
                    // left bottom (3)
                    UIVertex vt3 = lastLerpLeftPos;

                    // 修改原始的UIVertex的顶点数据
                    if (j == 0)
                    {
                        // 修改原有的UIVertex数据
                        verts[i + 0] = vt0;
                        verts[i + 1] = vt1;
                        verts[i + 2] = vt2;
#if UNITY_5 || UNITY_5_3_OR_NEWER
                        verts[i + 3] = vt2;
                        verts[i + 4] = vt3;
                        verts[i + 5] = vt0;
#else
                        verts[i + 3] = vt3;
#endif
                    }
                    else
                    {
                        // 增加新的UIVertex数据
                        verts.Add(vt0);
                        verts.Add(vt1);
                        verts.Add(vt2);
#if UNITY_5 || UNITY_5_3_OR_NEWER
                        verts.Add(vt2);
                        verts.Add(vt3);
                        verts.Add(vt0);
#else
                        verts.Add(vt3);
#endif
                    }
                }

                i += elementStep;
            }
        }

        private void ApplyGradientColorHorizontal(KeyPoint[] keyPointsColor, List<UIVertex> verts)
        {
            if (verts.Count <= 0 || keyPointColors.Length <= 1) return;

            int count = verts.Count;
            int vertexNum = (keyPointsColor.Length - 1) * count;
            if (verts.Capacity < vertexNum)
                verts.Capacity = vertexNum;

            int colorStep = Mathf.CeilToInt(1f * Mathf.FloorToInt(count / elementStep) / (keyPointsColor.Length - 1));

            float startX = 0, endX = 0;
            for (int i = 0; i < count; i++)
            {
                UIVertex pos = verts[i];
                if (pos.position.x < startX)
                {
                    startX = pos.position.x;
                }
                if (pos.position.x > endX)
                {
                    endX = pos.position.x;
                }
            }
            float xLen = endX - startX;
            if (xLen == 0)
            {
                return;
            }

            // 修改并增加UIVertex
            // ！！！ Unity5.5中是6个UIVertex(两个三角形)组成一个字，在Unity4.7中是4个UIVertex（一个四边形）组成一个字
            //      0----1
            //      | \  |
            //      |  \ |
            //      3----2
            // 顶点顺序为：unity5.5 : 0-1-2==2-3-0,  unity4.7 : 0-1-2-3
            // 以左下角为原点
            for (int i = 0; i < count; i += elementStep)
            {
                UIVertex leftTopPos = verts[i + 0];
                UIVertex rightTopPos = verts[i + 1];
                UIVertex rightBottomPos = verts[i + 2];
#if UNITY_5 || UNITY_5_3_OR_NEWER
                UIVertex leftBottomPos = verts[i + 4];
#else
                UIVertex leftBottomPos = verts[i + 3];
#endif
                Color leftLerpColor = LerpColor((leftTopPos.position.x - startX) / xLen, keyPointsColor);
                Color rightLerpColor = LerpColor((rightTopPos.position.x - startX) / xLen, keyPointsColor);

                if (m_useGraphicAlpha)
                {
                    leftLerpColor.a = ((leftLerpColor.a * leftTopPos.color.a) / 255);
                    rightLerpColor.a = ((rightLerpColor.a * rightTopPos.color.a) / 255);
                }

                leftTopPos.color = leftLerpColor;
                leftBottomPos.color = leftLerpColor;
                rightTopPos.color = rightLerpColor;
                rightBottomPos.color = rightLerpColor;

                verts[i + 0] = leftTopPos;
                verts[i + 1] = rightTopPos;
                verts[i + 2] = rightBottomPos;
#if UNITY_5 || UNITY_5_3_OR_NEWER
                verts[i + 3] = rightBottomPos;
                verts[i + 4] = leftBottomPos;
                verts[i + 5] = leftTopPos;
#else
                verts[i + 3] = leftBottomPos;
#endif
            }
        }

        Color LerpColor(float xPercent, KeyPoint[] keyPointsColor)
        {
            KeyPoint startColor = keyPointsColor[0], endColor = keyPointsColor[keyPointsColor.Length - 1];
            for (int j = 0; j < keyPointsColor.Length - 1; j++)
            {
                KeyPoint color = keyPointsColor[j];
                if (xPercent >= color.percent)
                {
                    startColor = color;
                    endColor = keyPointsColor[j + 1];
                }
            }
            float colorPercent = (xPercent - startColor.percent) / (endColor.percent - startColor.percent);

            return Color.Lerp(startColor.color, endColor.color, colorPercent);
        }
    }
}
