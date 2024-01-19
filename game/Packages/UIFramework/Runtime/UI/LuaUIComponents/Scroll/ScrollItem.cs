using UnityEngine;
using UnityEngine.UI;

namespace UnityEngineEx.UI
{
    /// <summary>
    /// ScrollRect中元素的基类
    /// </summary>
    public class ScrollItemBase
    {
        /// <summary>
        /// 元素对象
        /// </summary>
        protected GameObject m_GameObject;
        /// <summary>
        /// position的内部属性
        /// </summary>
        private Vector2 m_Position;
        /// <summary>
        /// 元素的位置
        /// </summary>
        public Vector2 position
        {
            get { return m_Position; }
        }

        /// <summary>
        /// 只更新位置的记录值，并不改变anchoredPosition
        /// </summary>
        /// <param name="position"></param>
        public void SetPositionRecord(Vector2 position)
        {
            m_Position = position;
        }
        /// <summary>
        /// 将元素的位置按照position属性当前的值进行更新
        /// </summary>
        public void RefreshPosition()
        {
            if (m_GameObject != null && position != m_GameObject.GetComponent<RectTransform>().anchoredPosition)
            {
                m_GameObject.GetComponent<RectTransform>().anchoredPosition = position;
            }
        }
        /// <summary>
        /// 更新当前元素的位置，同时设置anchoredPosition
        /// </summary>
        /// <param name="position">新的位置</param>
        public void SetPosition(Vector2 position)
        {
            SetPositionRecord(position);
            RefreshPosition();
        }
    }
    /// <summary>
    /// ScrollRect中的元素
    /// </summary>
    public class ScrollItem : ScrollItemBase
    {
        /// <summary>
        /// 是否初始化过了
        /// </summary>
        private bool m_Init;
        /// <summary>
        /// 实际创建的GameObject，null代表对象已经销毁或进入对象池中
        /// </summary>
        public GameObject gameObject
        {
            get
            {
                return m_GameObject;
            }
            set
            {
                m_GameObject = value;
                if (!m_Init && m_GameObject != null)
                {
                    m_Size = GetItemSize();
                    m_Init = true;
                }
            }
        }
        /// <summary>
        /// 指代元素的prefab的标识，相同prefab的实例的tag字段应该相同
        /// </summary>
        public string tag;
        /// <summary>
        /// 这个元素是否重用，这个字段先不考虑
        /// </summary>
        public bool reusable = true;
        /// <summary>
        /// 内部属性，元素创建时记录元素的大小
        /// </summary>
        private Vector2 m_Size;
        /// <summary>
        /// 只读属性，元素的大小
        /// </summary>
        public Vector2 size { get { return m_Size; } }
        /// <summary>
        /// 元素所在的行号
        /// </summary>
        public int groupIndex = -1;
        /// <summary>
        /// 元素的实际索引
        /// </summary>
        public int index;

        public ScrollItem() { }
        public ScrollItem(Vector2 size)
        {
            m_Size = size;
        }
        public ScrollItem(GameObject gameObject)
        {
            this.gameObject = gameObject;
        }

        private Vector2 GetItemSize()
        {
            var contentSizeFitter = m_GameObject.GetComponent<ContentSizeFitter>();
            if (contentSizeFitter != null)
            {
                contentSizeFitter.SetLayoutHorizontal();
                contentSizeFitter.SetLayoutVertical();
            }
            return m_GameObject.GetComponent<RectTransform>().rect.size;
        }
    }
    /// <summary>
    /// 处在一行或一列中的ScrollItem组的信息
    /// </summary>
    public class ScrollGroup : ScrollItemBase
    {
        public static string Tag = "line";
        /// <summary>
        /// 分隔线的对象
        /// </summary>
        public GameObject gameObject
        {
            get { return m_GameObject; }
            set { m_GameObject = value; }
        }
        /// <summary>
        /// 起始元素的索引
        /// </summary>
        public int startIndex;
        /// <summary>
        /// 最后元素的索引
        /// </summary>
        public int endIndex;
        /// <summary>
        /// 行高或列高，考虑cellSpace
        /// </summary>
        public float groupHeight;
        /// <summary>
        /// 行或列索引
        /// </summary>
        public int groupIndex;
    }
}
