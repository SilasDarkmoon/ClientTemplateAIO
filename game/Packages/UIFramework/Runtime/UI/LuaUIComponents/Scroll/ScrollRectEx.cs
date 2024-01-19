using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngineEx;

namespace UnityEngineEx.UI
{
    [AddComponentMenu("UI/Scroll Rect Ex", 33)]
    [SelectionBase]
    //[ExecuteInEditMode]
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(LuaBehav))]
    [DisallowMultipleComponent]
    public class ScrollRectEx : UIBehaviour, IInitializePotentialDragHandler, IBeginDragHandler, IEndDragHandler, IDragHandler, IScrollHandler, ICanvasElement, ILuaBehavEx
    {
        public enum MovementType
        {
            Unrestricted, // Unrestricted movement -- can scroll forever
            Elastic, // Restricted but flexible -- can go past the edges, but springs back in place
            Clamped, // Restricted movement where it's not possible to go past the edges
        }

        public enum Direction
        {
            Horizontal,
            Vertical
        }
        [Serializable]
        public class ScrollRectExEvent : UnityEvent<Vector2> { }

        [SerializeField]
        private RectTransform m_Content;
        public RectTransform content { get { return m_Content; } set { m_Content = value; } }

        public Direction direction = Direction.Horizontal;

        [SerializeField]
        private MovementType m_MovementType = MovementType.Elastic;
        public MovementType movementType { get { return m_MovementType; } set { m_MovementType = value; } }

        [SerializeField]
        private float m_Elasticity = 0.1f; // Only used for MovementType.Elastic
        public float elasticity { get { return m_Elasticity; } set { m_Elasticity = value; } }

        [SerializeField]
        private bool m_Inertia = true;
        public bool inertia { get { return m_Inertia; } set { m_Inertia = value; } }

        [SerializeField]
        private float m_DecelerationRate = 0.135f; // Only used when inertia is enabled
        public float decelerationRate { get { return m_DecelerationRate; } set { m_DecelerationRate = value; } }

        [SerializeField]
        private float m_ScrollSensitivity = 1.0f;
        public float scrollSensitivity { get { return m_ScrollSensitivity; } set { m_ScrollSensitivity = value; } }

        [SerializeField]
        private Scrollbar m_HorizontalScrollbar;
        public Scrollbar horizontalScrollbar
        {
            get
            {
                return m_HorizontalScrollbar;
            }
            set
            {
                if (m_HorizontalScrollbar)
                    m_HorizontalScrollbar.onValueChanged.RemoveListener(SetHorizontalNormalizedPosition);
                m_HorizontalScrollbar = value;
                if (m_HorizontalScrollbar)
                    m_HorizontalScrollbar.onValueChanged.AddListener(SetHorizontalNormalizedPosition);
            }
        }

        [SerializeField]
        private Scrollbar m_VerticalScrollbar;
        public Scrollbar verticalScrollbar
        {
            get
            {
                return m_VerticalScrollbar;
            }
            set
            {
                if (m_VerticalScrollbar)
                    m_VerticalScrollbar.onValueChanged.RemoveListener(SetVerticalNormalizedPosition);
                m_VerticalScrollbar = value;
                if (m_VerticalScrollbar)
                    m_VerticalScrollbar.onValueChanged.AddListener(SetVerticalNormalizedPosition);
            }
        }

        [SerializeField]
        private ScrollRectExEvent m_OnValueChanged = new ScrollRectExEvent();
        public ScrollRectExEvent onValueChanged { get { return m_OnValueChanged; } set { m_OnValueChanged = value; } }

        // The offset from handle position to mouse down position
        private Vector2 m_PointerStartLocalCursor = Vector2.zero;
        private Vector2 m_ContentStartPosition = Vector2.zero;

        private RectTransform m_ViewRect;

        protected RectTransform viewRect
        {
            get
            {
                if (m_ViewRect == null)
                    m_ViewRect = (RectTransform)transform;
                return m_ViewRect;
            }
        }

        private Bounds m_ContentBounds;
        private Bounds m_ViewBounds;

        private Vector2 m_Velocity;
        public Vector2 velocity { get { return m_Velocity; } set { m_Velocity = value; } }

        private bool m_Dragging;

        private Vector2 m_PrevPosition = Vector2.zero;
        private Bounds m_PrevContentBounds;
        private Bounds m_PrevViewBounds;
        [NonSerialized]
        private bool m_HasRebuiltLayout = false;

        protected ScrollRectEx()
        { }

        public virtual void Rebuild(CanvasUpdate executing)
        {
            if (executing != CanvasUpdate.PostLayout)
                return;

            UpdateBounds();
            UpdateScrollbars(Vector2.zero);
            UpdatePrevData();
            m_HasRebuiltLayout = true;
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            if (m_HorizontalScrollbar)
                m_HorizontalScrollbar.onValueChanged.AddListener(SetHorizontalNormalizedPosition);
            if (m_VerticalScrollbar)
                m_VerticalScrollbar.onValueChanged.AddListener(SetVerticalNormalizedPosition);

            CanvasUpdateRegistry.RegisterCanvasElementForLayoutRebuild(this);
        }

        protected override void OnDisable()
        {
            CanvasUpdateRegistry.UnRegisterCanvasElementForRebuild(this);

            if (m_HorizontalScrollbar)
                m_HorizontalScrollbar.onValueChanged.RemoveListener(SetHorizontalNormalizedPosition);
            if (m_VerticalScrollbar)
                m_VerticalScrollbar.onValueChanged.RemoveListener(SetVerticalNormalizedPosition);

            m_HasRebuiltLayout = false;
            base.OnDisable();
        }

        public override bool IsActive()
        {
            return base.IsActive() && m_Content != null;
        }

        private void EnsureLayoutHasRebuilt()
        {
            if (!m_HasRebuiltLayout && !CanvasUpdateRegistry.IsRebuildingLayout())
                Canvas.ForceUpdateCanvases();
        }

        public virtual void StopMovement()
        {
            m_Velocity = Vector2.zero;
            m_SnapOffset = Vector2.zero;
            m_ManualScroll = false;
        }

        public virtual void OnScroll(PointerEventData data)
        {
            if (!IsActive())
                return;

            EnsureLayoutHasRebuilt();
            UpdateBounds();

            Vector2 delta = data.scrollDelta;
            // Down is positive for scroll events, while in UI system up is positive.
            delta.y *= -1;
            if (direction == Direction.Vertical)
            {
                if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                    delta.y = delta.x;
                delta.x = 0;
            }
            if (direction == Direction.Horizontal)
            {
                if (Mathf.Abs(delta.y) > Mathf.Abs(delta.x))
                    delta.x = delta.y;
                delta.y = 0;
            }

            Vector2 position = m_Content.anchoredPosition;
            position += delta * m_ScrollSensitivity;
            if (m_MovementType == MovementType.Clamped)
                position += CalculateOffset(position - m_Content.anchoredPosition);

            SetContentAnchoredPosition(position);
            UpdateBounds();
        }

        public virtual void OnInitializePotentialDrag(PointerEventData eventData)
        {
            m_Velocity = Vector2.zero;
        }

        public virtual void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            if (!IsActive())
                return;

            if (m_ParentScrollRect != null && Mathf.Abs(eventData.delta[otherDirAxisIndex]) > Mathf.Abs(eventData.delta[dirAxisIndex]))
            {
                m_ParentScrollRect.OnBeginDrag(eventData);
                m_CrossDragging = true;
            }
            else
            {
                UpdateBounds();

                m_PointerStartLocalCursor = Vector2.zero;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(viewRect, eventData.position, eventData.pressEventCamera, out m_PointerStartLocalCursor);
                m_ContentStartPosition = m_Content.anchoredPosition;
                m_Dragging = true;
            }
        }

        public virtual void OnEndDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            if (m_ParentScrollRect != null && m_CrossDragging)
            {
                m_ParentScrollRect.OnEndDrag(eventData);
                m_CrossDragging = false;
            }
            else
            {
                if (snapToGrid && m_ItemList.Count > 0)
                {
                    Vector2 localCursor;
                    if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(viewRect, eventData.position, eventData.pressEventCamera, out localCursor))
                        return;
                    var pointerDelta = localCursor - m_PointerStartLocalCursor;
                    SetSnapDestination(pointerDelta);
                }
                m_Dragging = false;
            }
        }

        public virtual void OnDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            if (!IsActive())
                return;

            if (m_ParentScrollRect != null && m_CrossDragging)
            {
                m_ParentScrollRect.OnDrag(eventData);
            }
            else
            {
                Vector2 localCursor;
                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(viewRect, eventData.position, eventData.pressEventCamera, out localCursor))
                    return;

                var pointerDelta = localCursor - m_PointerStartLocalCursor;
                Vector2 position = m_ContentStartPosition + pointerDelta;

                UpdateBounds();

                // Offset to get content into place in the view.
                Vector2 offset = CalculateOffset(position - m_Content.anchoredPosition);
                position += offset;
                if (m_MovementType == MovementType.Elastic)
                {
                    if (offset.x != 0)
                        position.x = position.x - RubberDelta(offset.x, m_ViewBounds.size.x);
                    if (offset.y != 0)
                        position.y = position.y - RubberDelta(offset.y, m_ViewBounds.size.y);
                }

                SetContentAnchoredPosition(position);
            }
        }

        protected virtual void SetContentAnchoredPosition(Vector2 position)
        {
            if (direction == Direction.Vertical)
                position.x = m_Content.anchoredPosition.x;
            else if (direction == Direction.Horizontal)
                position.y = m_Content.anchoredPosition.y;

            if (position != m_Content.anchoredPosition)
            {
                var delta = (position - m_Content.anchoredPosition)[dirAxisIndex];
                if (Mathf.Abs(delta) > 0.01f) CheckVisibility(delta);

                m_Content.anchoredPosition = position;
                UpdateBounds();
                float offset = dirSign * position[dirAxisIndex];
                if (m_IsLoop && IsCreatedAll)
                {
                    offset = offset % TotalLength;
                    if (offset > 0) offset -= TotalLength;
                }
                OnScrollPositionChanged(-offset);
            }
        }

        protected virtual void LateUpdate()
        {
            if (!m_Content) return;

            EnsureLayoutHasRebuilt();
            UpdateBounds();
            float deltaTime = Time.unscaledDeltaTime;
            Vector2 offset = CalculateOffset(Vector2.zero);
            if (!m_Dragging && (offset != Vector2.zero || m_Velocity != Vector2.zero))
            {
                Vector2 position = m_Content.anchoredPosition;
                for (int axis = 0; axis < 2; axis++)
                {
                    // Apply spring physics if movement is elastic and content has an offset from the view.
                    if (m_MovementType == MovementType.Elastic && Mathf.Abs(offset[axis]) > 0.01f)
                    {
                        float speed = m_Velocity[axis];
                        position[axis] = Mathf.SmoothDamp(m_Content.anchoredPosition[axis], m_Content.anchoredPosition[axis] + offset[axis], ref speed, m_Elasticity, Mathf.Infinity, deltaTime);
                        m_Velocity[axis] = speed;
                        m_ManualScroll = false;
                        m_SnapOffset = Vector2.zero;
                    }
                    // 处理元素归位
                    else if (snapToGrid || m_ManualScroll)
                    {
                        if (m_SnapOffset[axis] != 0)
                        {
                            float speed = m_Velocity[axis];
                            position[axis] = Mathf.SmoothDamp(m_Content.anchoredPosition[axis], m_Content.anchoredPosition[axis] + m_SnapOffset[axis], ref speed, m_Elasticity, Mathf.Infinity, deltaTime);
                            m_Velocity[axis] = speed;
                            m_SnapOffset[axis] -= position[axis] - m_Content.anchoredPosition[axis];
                            if (Mathf.Abs(m_SnapOffset[dirAxisIndex]) < 0.01f) m_ManualScroll = false;
                        }
                    }
                    // Else move content according to velocity with deceleration applied.
                    else if (m_Inertia)
                    {
                        m_Velocity[axis] *= Mathf.Pow(m_DecelerationRate, deltaTime);
                        if (Mathf.Abs(m_Velocity[axis]) < 1)
                            m_Velocity[axis] = 0;
                        position[axis] += m_Velocity[axis] * deltaTime;
                    }
                    // If we have neither elaticity or friction, there shouldn't be any velocity.
                    else
                    {
                        m_Velocity[axis] = 0;
                    }
                }

                if (m_Velocity != Vector2.zero || position[dirAxisIndex] != content.anchoredPosition[dirAxisIndex])
                {
                    if (m_MovementType == MovementType.Clamped)
                    {
                        offset = CalculateOffset(position - m_Content.anchoredPosition);
                        position += offset;
                    }

                    SetContentAnchoredPosition(position);
                }
            }

            if (m_Dragging && m_Inertia)
            {
                m_ManualScroll = false;
                Vector3 newVelocity = (m_Content.anchoredPosition - m_PrevPosition) / deltaTime;
                m_Velocity = Vector3.Lerp(m_Velocity, newVelocity, deltaTime * 10);
            }

            if (m_ViewBounds != m_PrevViewBounds || m_ContentBounds != m_PrevContentBounds || m_Content.anchoredPosition != m_PrevPosition)
            {
                UpdateScrollbars(offset);
                m_OnValueChanged.Invoke(normalizedPosition);
                UpdatePrevData();
            }
        }

        private void UpdatePrevData()
        {
            if (m_Content == null)
                m_PrevPosition = Vector2.zero;
            else
                m_PrevPosition = m_Content.anchoredPosition;
            m_PrevViewBounds = m_ViewBounds;
            m_PrevContentBounds = m_ContentBounds;
        }

        private void UpdateScrollbars(Vector2 offset)
        {
            if (m_HorizontalScrollbar)
            {
                if (m_ContentBounds.size.x > 0)
                    m_HorizontalScrollbar.size = Mathf.Clamp01((m_ViewBounds.size.x - Mathf.Abs(offset.x)) / m_ContentBounds.size.x);
                else
                    m_HorizontalScrollbar.size = 1;

                m_HorizontalScrollbar.value = horizontalNormalizedPosition;
            }

            if (m_VerticalScrollbar)
            {
                if (m_ContentBounds.size.y > 0)
                    m_VerticalScrollbar.size = Mathf.Clamp01((m_ViewBounds.size.y - Mathf.Abs(offset.y)) / m_ContentBounds.size.y);
                else
                    m_VerticalScrollbar.size = 1;

                m_VerticalScrollbar.value = verticalNormalizedPosition;
            }
        }

        public Vector2 normalizedPosition
        {
            get
            {
                return new Vector2(horizontalNormalizedPosition, verticalNormalizedPosition);
            }
            set
            {
                SetNormalizedPosition(value.x, 0);
                SetNormalizedPosition(value.y, 1);
            }
        }

        public float horizontalNormalizedPosition
        {
            get
            {
                UpdateBounds();
                if (m_ContentBounds.size.x <= m_ViewBounds.size.x)
                    return (m_ViewBounds.min.x > m_ContentBounds.min.x) ? 1 : 0;
                return (m_ViewBounds.min.x - m_ContentBounds.min.x) / (m_ContentBounds.size.x - m_ViewBounds.size.x);
            }
            set
            {
                SetNormalizedPosition(value, 0);
            }
        }

        public float verticalNormalizedPosition
        {
            get
            {
                UpdateBounds();
                if (m_ContentBounds.size.y <= m_ViewBounds.size.y)
                    return (m_ViewBounds.min.y > m_ContentBounds.min.y) ? 1 : 0;
                ;
                return (m_ViewBounds.min.y - m_ContentBounds.min.y) / (m_ContentBounds.size.y - m_ViewBounds.size.y);
            }
            set
            {
                SetNormalizedPosition(value, 1);
            }
        }

        private void SetHorizontalNormalizedPosition(float value) { SetNormalizedPosition(value, 0); }
        private void SetVerticalNormalizedPosition(float value) { SetNormalizedPosition(value, 1); }

        private void SetNormalizedPosition(float value, int axis)
        {
            EnsureLayoutHasRebuilt();
            UpdateBounds();
            // How much the content is larger than the view.
            float hiddenLength = m_ContentBounds.size[axis] - m_ViewBounds.size[axis];
            // Where the position of the lower left corner of the content bounds should be, in the space of the view.
            float contentBoundsMinPosition = m_ViewBounds.min[axis] - value * hiddenLength;
            // The new content localPosition, in the space of the view.
            float newLocalPosition = m_Content.localPosition[axis] + contentBoundsMinPosition - m_ContentBounds.min[axis];

            Vector3 localPosition = m_Content.localPosition;
            if (Mathf.Abs(localPosition[axis] - newLocalPosition) > 0.01f)
            {
                localPosition[axis] = newLocalPosition;
                m_Content.localPosition = localPosition;
                m_Velocity[axis] = 0;
                UpdateBounds();
            }
        }

        private static float RubberDelta(float overStretching, float viewSize)
        {
            return (1 - (1 / ((Mathf.Abs(overStretching) * 0.55f / viewSize) + 1))) * viewSize * Mathf.Sign(overStretching);
        }

        private void UpdateBounds()
        {
            m_ViewBounds = new Bounds(viewRect.rect.center, viewRect.rect.size);
            m_ContentBounds = GetBounds();

            if (m_Content == null)
                return;

            // Make sure content bounds are at least as large as view by adding padding if not.
            // One might think at first that if the content is smaller than the view, scrolling should be allowed.
            // However, that's not how scroll views normally work.
            // Scrolling is *only* possible when content is *larger* than view.
            // We use the pivot of the content rect to decide in which directions the content bounds should be expanded.
            // E.g. if pivot is at top, bounds are expanded downwards.
            // This also works nicely when ContentSizeFitter is used on the content.
            Vector3 contentSize = m_ContentBounds.size;
            Vector3 contentPos = m_ContentBounds.center;
            Vector3 excess = m_ViewBounds.size - contentSize;
            if (excess.x > 0)
            {
                contentPos.x -= excess.x * (m_Content.pivot.x - 0.5f);
                contentSize.x = m_ViewBounds.size.x;
            }
            if (excess.y > 0)
            {
                contentPos.y -= excess.y * (m_Content.pivot.y - 0.5f);
                contentSize.y = m_ViewBounds.size.y;
            }

            m_ContentBounds.size = contentSize;
            m_ContentBounds.center = contentPos;
        }

        private readonly Vector3[] m_Corners = new Vector3[4];
        private Bounds GetBounds()
        {
            if (m_Content == null)
                return new Bounds();

            var vMin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            var vMax = new Vector3(float.MinValue, float.MinValue, float.MinValue);

            var toLocal = viewRect.worldToLocalMatrix;
            m_Content.GetWorldCorners(m_Corners);
            for (int j = 0; j < 4; j++)
            {
                Vector3 v = toLocal.MultiplyPoint3x4(m_Corners[j]);
                vMin = Vector3.Min(v, vMin);
                vMax = Vector3.Max(v, vMax);
            }

            var bounds = new Bounds(vMin, Vector3.zero);
            bounds.Encapsulate(vMax);
            return bounds;
        }

        private Vector2 CalculateOffset(Vector2 delta)
        {
            Vector2 offset = Vector2.zero;
            if (m_MovementType == MovementType.Unrestricted)
                return offset;

            Vector2 min = m_ContentBounds.min;
            Vector2 max = m_ContentBounds.max;

            if (direction == Direction.Horizontal)
            {
                min.x += delta.x;
                max.x += delta.x;
                if (min.x > m_ViewBounds.min.x)
                    offset.x = m_ViewBounds.min.x - min.x;
                else if (max.x < m_ViewBounds.max.x)
                    offset.x = m_ViewBounds.max.x - max.x;
            }
            else if (direction == Direction.Vertical)
            {
                min.y += delta.y;
                max.y += delta.y;
                if (max.y < m_ViewBounds.max.y)
                    offset.y = m_ViewBounds.max.y - max.y;
                else if (min.y > m_ViewBounds.min.y)
                    offset.y = m_ViewBounds.min.y - min.y;
            }

            return offset;
        }

        public virtual void LayoutComplete()
        { }

        public virtual void GraphicUpdateComplete()
        { }

        #region Extension
        #region Public Interface
        /// <summary>
        /// 初始化所有元素显示
        /// </summary>
        /// <param name="itemCount">元素数量</param>
        public void RefreshWithItemCount(int itemCount)
        {
            if (itemCount < 0) { PlatDependant.LogError("Item Count cann't be negative!"); }

            InitConfig(itemCount);

            CheckVisibility(0);
            UpdateBounds();

            if (snapToGrid && itemCount > 0) OnItemIndexChanged(0);
            OnScrollPositionChanged(0);
        }
        /// <summary>
        /// 删除一个元素
        /// </summary>
        /// <param name="index">删除元素的索引</param>
        public void RemoveItem(int index)
        {
            RemoveItems(index, 1);
        }
        /// <summary>
        /// 删除任意数量的元素
        /// </summary>
        /// <param name="index">删除的起始位置索引</param>
        /// <param name="count">删除的元素个数</param>
        public void RemoveItems(int index, int count)
        {
            if (count <= 0)
            {
                PlatDependant.LogInfo("Count should be positive");
                return;
            }
            if (index >= m_ItemList.Count)
            {
                PlatDependant.LogInfo("Wrong Index To Remove");
                return;
            }
            count = Mathf.Min(count, m_ItemList.Count - index);

            StopMovement();

            if (IsCreatedAll && TotalLength > 0)
            {
                m_Content.anchoredPosition = GetVector2WithDir(m_Content.anchoredPosition[dirAxisIndex] % TotalLength, m_Content.anchoredPosition[otherDirAxisIndex]);
                UpdateBounds();
            }
            for (int i = m_ItemList.Count - 1; i >= index + count; i--)
            {
                var item = m_ItemList[i];
                if (item != null) item.index -= count;
            }
            InitRecord(m_ItemList.Count - count, isLoop);
            m_ItemList.RemoveRange(index, count);
            CheckVisibility(0);
        }
        /// <summary>
        /// 添加一个元素
        /// </summary>
        /// <param name="index">添加的位置</param>
        public void AddItem(int index)
        {
            AddItems(index, 1);
        }
        /// <summary>
        /// 添加一个元素，并指定元素大小
        /// </summary>
        /// <param name="index">添加的位置</param>
        /// <param name="width">元素的宽度</param>
        /// <param name="height">元素的高度</param>
        public void AddItem(int index, float width, float height)
        {
            AddItems(index, 1, width, height);
        }
        /// <summary>
        /// 添加任意数量的元素
        /// </summary>
        /// <param name="index">添加的位置</param>
        /// <param name="count">添加的元素数量</param>
        public void AddItems(int index, int count)
        {
            if (count <= 0)
            {
                PlatDependant.LogInfo("Count should be positive");
                return;
            }

            StopMovement();

            if (IsCreatedAll && TotalLength > 0)
            {
                m_Content.anchoredPosition = GetVector2WithDir(m_Content.anchoredPosition[dirAxisIndex] % TotalLength, m_Content.anchoredPosition[otherDirAxisIndex]);
                UpdateBounds();
            }
            for (int i = m_ItemList.Count - 1; i >= Mathf.Max(m_EndCreatedItemIndex, index); i--) m_ItemList[i].index += count;
            InitRecord(m_ItemList.Count + count, isLoop);
            m_ItemList.InsertRange(index, Enumerable.Repeat<ScrollItem>(null, count));
            CheckVisibility(0);
        }
        /// <summary>
        /// 添加任意数量的元素，并指定元素大小
        /// </summary>
        /// <param name="index">添加的位置</param>
        /// <param name="count">添加的元素数量</param>
        /// <param name="width">元素的宽度</param>
        /// <param name="height">元素的高度</param>
        public void AddItems(int index, int count, float width, float height)
        {
            if (count <= 0)
            {
                PlatDependant.LogInfo("Count should be positive");
                return;
            }

            StopMovement();

            if (IsCreatedAll && TotalLength > 0)
            {
                m_Content.anchoredPosition = GetVector2WithDir(m_Content.anchoredPosition[dirAxisIndex] % TotalLength, m_Content.anchoredPosition[otherDirAxisIndex]);
                UpdateBounds();
            }
            for (int i = m_ItemList.Count - 1; i >= Mathf.Max(m_EndCreatedItemIndex, index); i--) m_ItemList[i].index += count;
            InitRecord(m_ItemList.Count + count, isLoop);
            m_ItemList.InsertRange(index, Enumerable.Repeat<ScrollItem>(null, count));
            for (int i = 0; i < count; i++) m_ItemList[index + i] = new ScrollItem(new Vector2(width, height));
            CheckVisibility(0);
        }
        /// <summary>
        /// 滚动到指定位置
        /// </summary>
        /// <param name="index">元素索引</param>
        public void ScrollToCell(int index)
        {
            float offset = CalcScrollToOffset(index);
            if (offset != 0)
            {
                m_SnapOffset = GetVector2WithDir(offset, 0);
                m_ManualScroll = true;
                m_Velocity[dirAxisIndex] = 1000f * Mathf.Sign(offset);
            }
        }
        /// <summary>
        /// 立刻滚动到指定位置
        /// </summary>
        /// <param name="index">元素索引</param>
        public void ScrollToCellImmediate(int index)
        {
            float offset = CalcScrollToOffset(index);
            if (offset != 0)
            {
                SetContentAnchoredPosition(content.anchoredPosition + GetVector2WithDir(offset, 0));
                StopMovement();
            }
        }
        /// <summary>
        /// 滚动到下一组元素
        /// </summary>
        public void ScrollToNextGroup()
        {
            float groupEdge1 = 0;
            float groupEdge2 = 0;
            int currentItemIndex = GetCurrentItemIndex(out groupEdge1, out groupEdge2);
            var item = m_ItemList[currentItemIndex];
            
            int groupIndex = item.groupIndex;
            var nextGroupInfo = GetGroupInfo(groupIndex + 1);
            if (nextGroupInfo != null) ScrollToCell(nextGroupInfo.startIndex);
        }
        /// <summary>
        /// 滚动到上一组元素
        /// </summary>
        public void ScrollToPreviousGroup()
        {
            float groupEdge1 = 0;
            float groupEdge2 = 0;
            int currentItemIndex = GetCurrentItemIndex(out groupEdge1, out groupEdge2);
            var item = m_ItemList[currentItemIndex];
            if (groupEdge1 < - Mathf.Min(0.1f * item.size[dirAxisIndex], 10f))
            {
                ScrollToCell(currentItemIndex);
            }
            else
            {
                int groupIndex = item.groupIndex;
                var previousGroupInfo = GetGroupInfo(groupIndex - 1);
                if (previousGroupInfo != null) ScrollToCell(previousGroupInfo.startIndex);
            }
        }
        /// <summary>
        /// 获取指定索引处的元素
        /// </summary>
        /// <param name="index">带获取的元素索引</param>
        /// <returns>元素</returns>
        public ScrollItem GetItem(int index)
        {
            if (index >= 0 && index < m_ItemList.Count) return m_ItemList[index];
            else return null;
        }
        /// <summary>
        /// 第一个可见元素的索引
        /// </summary>
        /// <returns>索引</returns>
        public int GetStartVisibleItemIndex()
        {
            return m_StartVisibleItemIndex;
        }
        /// <summary>
        /// 最后一个可见元素的索引
        /// </summary>
        /// <returns>索引</returns>
        public int GetEndVisibleItemIndex()
        {
            return m_EndVisibleItemIndex;
        }
        /// <summary>
        /// 获取所有可见元素，不过这个是考虑到showOffset的
        /// </summary>
        /// <returns>所有可见元素的数组</returns>
        public ScrollItem[] GetVisibleItems()
        {
            int count = m_EndVisibleItemIndex - m_StartVisibleItemIndex + 1;
            if (m_StartVisibleItemIndex <= m_EndVisibleItemIndex)
            {
                ScrollItem[] items = new ScrollItem[count];
                m_ItemList.CopyTo(m_StartVisibleItemIndex, items, 0, count);
                return items;
            }
            else
            {
                count += m_ItemList.Count;
                ScrollItem[] items = new ScrollItem[count];
                m_ItemList.CopyTo(m_StartVisibleItemIndex, items, 0, m_ItemList.Count - m_StartVisibleItemIndex);
                m_ItemList.CopyTo(0, items, m_ItemList.Count - m_StartVisibleItemIndex, m_EndVisibleItemIndex + 1);
                return items;
            }

        }
        #endregion

        #region Editor Fields
        /// <summary>
        /// 元素的间隔
        /// </summary>
        [SerializeField]
        private Vector2 cellSpace = Vector2.zero;
        /// <summary>
        /// 线的宽度
        /// </summary>
        [SerializeField]
        private float lineSpace = 0f;
        /// <summary>
        /// 实际显示相对于可视区的偏移，小于等于0时不创建
        /// </summary>
        [SerializeField]
        private float showOffset = 100f;
        /// <summary>
        /// 使用对象池
        /// </summary>
        [SerializeField]
        private bool usePool = true;
        /// <summary>
        /// 循环滚动
        /// </summary>
        [SerializeField]
        private bool isLoop = false;
        /// <summary>
        /// 按元素滚动
        /// </summary>
        [SerializeField]
        private bool snapToGrid = false;
        #endregion

        #region Private Fields
        /// <summary>
        /// 当前是否正在交叉滚动，拖拽处理已有上层ScrollRectEx接管
        /// </summary>
        private bool m_CrossDragging = false;
        /// <summary>
        /// 距离最近的祖先ScrollRectExtension，在交叉滚动的判定时会用到
        /// </summary>
        private ScrollRectEx m_ParentScrollRect;
        /// <summary>
        /// 内部维护的当前是否可循环状态，如果isLoop == true，但是总元素数量不足，这个字段就是false
        /// </summary>
        private bool m_IsLoop;
        /// <summary>
        /// 是否是通过代码调用ScrollToXXX来触发的自动滚动
        /// </summary>
        private bool m_ManualScroll = false;
        /// <summary>
        /// 元素归位时待滚动的偏移
        /// </summary>
        private Vector2 m_SnapOffset;
        /// <summary>
        /// 从最前面开始往后找到的最后一个创建过的元素的索引
        /// </summary>
        private int m_StartCreatedItemIndex = -1;
        /// <summary>
        /// 从最后开始往前找到的最后一个创建过的元素的索引
        /// </summary>
        private int m_EndCreatedItemIndex = -1;
        /// <summary>
        /// m_ItemList中已显示的第一个元素
        /// </summary>
        private int m_StartVisibleItemIndex = -1;
        /// <summary>
        /// m_ItemList中已显示的最后一个元素
        /// </summary>
        private int m_EndVisibleItemIndex = -1;
        /// <summary>
        /// 前一次m_ItemList中已显示的第一个元素
        /// </summary>
        private int m_PreStartVisibleItemIndex = -1;
        /// <summary>
        /// 前一次m_ItemList中已显示的最后一个元素
        /// </summary>
        private int m_PreEndVisibleItemIndex = -1;
        /// <summary>
        /// 上一次的起始元素索引1
        /// </summary>
        private int m_PreItemIndex = -1; 
        /// <summary>
        /// 所有已创建过的元素的信息
        /// </summary>
        private List<ScrollItem> m_ItemList = new List<ScrollItem>();
        /// <summary>
        /// 正向线的信息
        /// </summary>
        private List<ScrollGroup> m_GroupList = new List<ScrollGroup>();
        /// <summary>
        /// 反向线的信息，只在循环滚动时才会用到
        /// </summary>
        private List<ScrollGroup> m_ReverseGroupList = new List<ScrollGroup>();
        /// <summary>
        /// 在滚动方向上的运算符号
        /// </summary>
        private int dirSign
        {
            get
            {
                if (direction == Direction.Horizontal) { return 1; }
                else { return -1; }
            }
        }
        /// <summary>
        /// 在非滚动方向上的运算符号
        /// </summary>
        private int otherDirSign { get { return -dirSign; } }
        /// <summary>
        /// 滚动方向上的坐标索引，横向滚动时返回0，纵向滚动时返回1
        /// </summary>
        private int dirAxisIndex { get { return (int)direction; } }
        /// <summary>
        /// 非滚动方向上的坐标索引，横向滚动时返回1，纵向滚动时返回0
        /// </summary>
        private int otherDirAxisIndex { get { return 1 - (int)direction; } }
        /// <summary>
        /// 根据滚动方向创建Vector2
        /// </summary>
        /// <param name="x">水平滚动时的横坐标，竖直滚动时的纵坐标</param>
        /// <param name="y">水平滚动时的纵坐标，竖直滚动时的横坐标</param>
        /// <returns>Vector2</returns>
        private Vector2 GetVector2WithDir(float x, float y)
        {
            if (direction == Direction.Horizontal) { return new Vector2(x, y); }
            else { return new Vector2(y, x); }
        }
        /// <summary>
        /// 可见分隔线的起始位置，为-1时没有任何分隔线是可见的
        /// </summary>
        private int startVisibleGroupIndex
        {
            get
            {
                if (m_StartVisibleItemIndex < 0) return -1;
                return m_ItemList[m_StartVisibleItemIndex].groupIndex;
            }
        }
        /// <summary>
        /// 可见分割线的结束位置，为-1时没有任何分隔线是可见的
        /// </summary>
        private int endVisibleGroupIndex
        {
            get
            {
                if (m_EndVisibleItemIndex < 0) return -1;
                return m_ItemList[m_EndVisibleItemIndex].groupIndex;
            }
        }
        /// <summary>
        /// 所有元素是否都被创建过
        /// </summary>
        private bool IsCreatedAll
        {
            get
            {
                if (m_GroupList.Count > 0)
                {
                    if (m_ReverseGroupList.Count > 0)
                    {
                        return m_GroupList.Last<ScrollGroup>().endIndex + 1 == m_ReverseGroupList.Last<ScrollGroup>().startIndex;
                    }
                    else
                    {
                        return m_GroupList.Last<ScrollGroup>().endIndex == m_ItemList.Count - 1;
                    }
                }
                else
                {
                    return false;
                }
            }
        }
        /// <summary>
        /// 总分组数量，只有在所有元素都创建完毕的情况下调用才有意义，否则直接返回null
        /// </summary>
        private int? GroupCount
        {
            get
            {
                if (IsCreatedAll) return m_GroupList.Count + m_ReverseGroupList.Count;
                else return null;
            }
        }
        /// <summary>
        /// 滚动方向的总长度
        /// </summary>
        private float TotalLength
        {
            get
            {
                float length = 0f;
                foreach(var group in m_GroupList)
                {
                    length += group.groupHeight;
                }
                foreach(var group in m_ReverseGroupList)
                {
                    length += group.groupHeight;
                }
                return length;
            }
        }
        /// <summary>
        /// 获取最大的GroupHeight
        /// </summary>
        private float MaxGroupHeight
        {
            get
            {
                float length = 0f;
                foreach(var group in m_GroupList)
                {
                    length = Mathf.Max(group.groupHeight, length);
                }
                foreach(var group in m_ReverseGroupList)
                {
                    length = Mathf.Max(group.groupHeight, length);
                }
                return length;
            }
        }
        /// <summary>
        /// 对象池
        /// </summary>
        private GameObjectPool m_Pool;
        /// <summary>
        /// 对象池
        /// </summary>
        private GameObjectPool pool
        {
            get
            {
                if (m_Pool == null) m_Pool = content.GetComponent<GameObjectPool>();
                return m_Pool;
            }
        }
        #endregion

        #region LuaCallback
        /// <summary>
        /// 通过Lua方法获取index处元素的tag
        /// </summary>
        /// <param name="index">元素的索引</param>
        /// <returns>元素的tag</returns>
        private string GetItemTagLuaFunc(int index)
        {
            string tag;
            this.CallLuaFunc("getItemTag", out tag, index + 1);
            return tag;
        }
        /// <summary>
        /// 通过Lua方法创建index处的元素，返回的ScrollItem的tag是default
        /// </summary>
        /// <param name="index">元素的索引</param>
        /// <returns>index处的元素</returns>
        private GameObject CreateItemLuaFunc(int index)
        {
            GameObject obj;
            this.CallLuaFunc("createItem", out obj, index + 1);
            return obj;
        }
        /// <summary>
        /// 通过Lua方法创建一个换行分割线
        /// </summary>
        /// <returns>新创建的换行分割线</returns>
        private GameObject CreateLineLuaFunc()
        {
            GameObject obj;
            this.CallLuaFunc("createLine", out obj);
            return obj;
        }
        /// <summary>
        /// 通过Lua方法重新更新一个已经实例化的元素的状态
        /// </summary>
        /// <param name="go">已经实例化的元素</param>
        /// <param name="index">元素的索引</param>
        private void ResetItemLuaFunc(GameObject go, int index)
        {
            var itemLuaBehaviour = go.GetComponent<LuaBehav>();
            //resetItemLuaFunc(luaBehaviour.lua, itemLuaBehaviour ? itemLuaBehaviour.lua : null, index + 1);
            this.CallLuaFunc("resetItem", itemLuaBehaviour, index + 1);
        }
        /// <summary>
        /// 如果是按元素滚动，当前元素变化时被调用
        /// </summary>
        /// <param name="index">变化之后的元素索引</param>
        private void OnItemIndexChanged(int index)
        {
            if (m_PreItemIndex != index)
            {
                m_PreItemIndex = index;
                //onItemIndexChanged(luaBehaviour.lua, index + 1);
                this.CallLuaFunc("onItemIndexChanged", index + 1);
            }
        }
        /// <summary>
        /// 当滚动位置变化时被调用
        /// </summary>
        /// <param name="position">含义有待确定</param>
        private void OnScrollPositionChanged(float position)
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
#endif
            {
                //onScrollPositionChanged(luaBehaviour.lua, position);
                this.CallLuaFunc("onScrollPositionChanged", position);
            }
        }
        #endregion

        LuaBehav ILuaBehavEx.Major { get; set; }
        bool ILuaBehavEx.RouteToParents { get { return false; } }

        #region Unity Methods
        //public delegate void ScrollRectExDelegate(LuaTable self, int param1);
        //public delegate void ScrollRectExDelegate1(LuaTable self, LuaTable param1, int param2);
        //public delegate void ScrollRectExDelegate2(LuaTable self, float param1);
        //public delegate GameObject ScrollRectExDelegate3(LuaTable self, int param1);
        //public delegate GameObject ScrollRectExDelegate4(LuaTable self);
        //public delegate string ScrollRectExDelegate5(LuaTable self, int param1);

        //private ScrollRectExDelegate3 createItemLuaFunc;
        //private ScrollRectExDelegate4 createLineLuaFunc;
        //private ScrollRectExDelegate1 resetItemLuaFunc;

        //private ScrollRectExDelegate5 getItemTagLuaFunc;

        //private ScrollRectExDelegate onItemIndexChanged;
        //private ScrollRectExDelegate2 onScrollPositionChanged;

        protected override void Start()
        {
            // 获取到最近祖先节点中的ScrollRectExtension，留待多个ScrollRectExtension交错滚动时使用
            if (transform.parent != null) { m_ParentScrollRect = transform.parent.GetComponentInParent<ScrollRectEx>(); }
        }
        protected override void Awake()
        {
            //if (luaBehaviour != null && luaBehaviour.lua != null)
            //{
            //    if (createItemLuaFunc == null)
            //    {
            //        luaBehaviour.lua.Get("createItem", out createItemLuaFunc);
            //    }
            //    if (createLineLuaFunc == null)
            //    {
            //        luaBehaviour.lua.Get("createLine", out createLineLuaFunc);
            //    }
            //    if (resetItemLuaFunc == null)
            //    {
            //        luaBehaviour.lua.Get("resetItem", out resetItemLuaFunc);
            //    }
            //    if (getItemTagLuaFunc == null)
            //    {
            //        luaBehaviour.lua.Get("getItemTag", out getItemTagLuaFunc);
            //    }
            //    if (onItemIndexChanged == null)
            //    {
            //        luaBehaviour.lua.Get("onItemIndexChanged", out onItemIndexChanged);
            //    }
            //    if (onScrollPositionChanged == null)
            //    {
            //        luaBehaviour.lua.Get("onScrollPositionChanged", out onScrollPositionChanged);
            //    }
            //}

            RefreshWithItemCount(0);
        }
        #endregion

        #region Private Method
        private void InitRecord(int count, bool isLoop)
        {
            m_StartVisibleItemIndex = -1;
            m_EndVisibleItemIndex = -1;
            m_StartCreatedItemIndex = -1;
            m_EndCreatedItemIndex = count;
            UpdatePreVisibleIndex();

            RecycleAllToPool();

            m_GroupList.Clear();
            m_ReverseGroupList.Clear();

            m_IsLoop = isLoop;
            if (m_IsLoop) m_MovementType = MovementType.Unrestricted;

            content.anchorMax = new Vector2(0, 1);
            content.anchorMin = new Vector2(0, 1);
            content.pivot = new Vector2(0, 1);
            content.sizeDelta = Vector2.zero;
        }
        /// <summary>
        /// 回到初始化的状态
        /// </summary>
        private void InitConfig(int count = 0)
        {
            m_CrossDragging = false;
            StopMovement();

            InitRecord(count, isLoop);

            m_ItemList.Clear();
            m_ItemList.AddRange(Enumerable.Repeat<ScrollItem>(null, count));

            content.anchoredPosition = Vector2.zero;
            UpdateBounds();
            OnScrollPositionChanged(0);
        }
        /// <summary>
        /// 根据当前可见区域为对象池中预留元素，应该只在add，remove和init的时候会调用到
        /// </summary>
        private void InitPool()
        {
            pool.Clear();
            for (int i = m_StartVisibleItemIndex; i != m_EndVisibleItemIndex; i++)
            {
                if (i == m_ItemList.Count) i = 0;
                var item = m_ItemList[i];
                if (item.tag != null)
                {
                    var gameObject = CreateItemLuaFunc(i);
                    var rt = gameObject.GetComponent<RectTransform>();
                    rt.anchorMin = new Vector2(0, 1);
                    rt.anchorMax = new Vector2(0, 1);
                    rt.pivot = new Vector2(0, 1);
                    rt.SetParent(content, false);
                    rt.anchoredPosition = GetVector2WithDir(0, otherDirSign * (viewRect.rect.size[otherDirAxisIndex] + (dirAxisIndex == 0 ? Screen.height : Screen.width)));
                    pool.ReturnToPool(gameObject, item.tag);
                }
            }
        }
        /// <summary>
        /// 获取计算了Space之后的元素大小
        /// </summary>
        /// <param name="cellSize">元素原始大小</param>
        /// <returns>计算了Space之后的元素大小</returns>
        private Vector2 GetCellSizeWithSpace(Vector2 cellSize)
        {
            return cellSize + cellSpace;
        }
        /// <summary>
        /// 从对象池中获取一个对象
        /// </summary>
        /// <param name="tag">代表对象类型的标识</param>
        /// <returns>获取到的对象</returns>
        private GameObject GetFromPool(string tag)
        {
            if (usePool)
            {
                var go = pool.GetFromPool(tag);
                return go;
            }
            else
            {
                return null;
            }
        }
        /// <summary>
        /// 把一个Group对象返回到对象池中
        /// </summary>
        /// <param name="item">要回收的对象</param>
        private void ReturnToPool(ScrollGroup item)
        {
            if (usePool)
            {
                if (item.gameObject != null)
                {
                    pool.ReturnToPool(item.gameObject, ScrollGroup.Tag);
                    item.gameObject.GetComponent<RectTransform>().anchoredPosition = GetVector2WithDir(0, otherDirSign * (viewRect.rect.size[otherDirAxisIndex] + (dirAxisIndex == 0 ? Screen.height : Screen.width)));
                    item.gameObject = null;
                }
            }
            else
            {
                UnityEngine.Object.Destroy(item.gameObject);
                item.gameObject = null;
            }
        }
        /// <summary>
        /// 把一个对象返回到对象池中
        /// </summary>
        /// <param name="item">要回收的对象</param>
        private void ReturnToPool(ScrollItem item)
        {
            if (usePool)
            {
                if (item.gameObject != null)
                {
                    pool.ReturnToPool(item.gameObject, item.tag);
                    item.gameObject.GetComponent<RectTransform>().anchoredPosition = GetVector2WithDir(0, otherDirSign * (viewRect.rect.size[otherDirAxisIndex] + (dirAxisIndex == 0 ? Screen.height : Screen.width)));
                    item.gameObject = null;
                }
            }
            else
            {
                UnityEngine.Object.Destroy(item.gameObject);
                item.gameObject = null;
            }
        }
        /// <summary>
        /// 将所有元素都回收到池中
        /// </summary>
        private void RecycleAllToPool()
        {
            for(int i = 0; i < m_ItemList.Count; i++)
            {
                HideItem(i);
            }
            for(int i = 0; i < m_GroupList.Count; i++)
            {
                HideGroup(i);
            }
            for(int i = 0; i < m_ReverseGroupList.Count; i++)
            {
                HideGroup(-i - 1);
            }
        }
        /// <summary>
        /// 显示index处的元素，如果对象池中已经存在元素，则直接从对象池中获取
        /// </summary>
        /// <param name="index">元素索引，从0开始</param>
        /// <returns>创建或更新的元素</returns>
        private ScrollItem ShowItem(int index)
        {
            ScrollItem item;
            if (IsItemVisible(index))
            {
                item = m_ItemList[index];
                item.RefreshPosition();
                return item;
            }

            // if (IsItemCreated(index)) DropItem(index);

            string tag = GetItemTagLuaFunc(index);
            GameObject go = GetFromPool(tag);

            if (go == null)
            {
                go = CreateItemLuaFunc(index);
                go.transform.SetParent(content, false);

            }
            ResetItemLuaFunc(go, index);
            

            if (IsItemCreated(index))
            {
                item = m_ItemList[index];
                item.gameObject = go;
                item.RefreshPosition();
            }
            else
            {
                item = new ScrollItem(go);
                item.index = index;
            }
            item.tag = tag;

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            // if (!go.activeSelf) go.SetActiveWithValidation(true);
            return item;
        }
        /// <summary>
        /// 取消一个显示中的元素，如果使用对象池，则把它放到池里去
        /// </summary>
        /// <param name="index">元素索引，从0开始</param>
        private void HideItem(int index)
        {
            if (IsItemCreated(index))
            {
                var item = m_ItemList[index];
                ReturnToPool(item);
            }
        }
        /// <summary>
        /// 创建或从池中取出一条分隔线
        /// </summary>
        /// <returns>分隔线</returns>
        private ScrollGroup ShowGroup(int groupIndex)
        {
            ScrollGroup item = null;
            if (groupIndex >= 0 && groupIndex < m_GroupList.Count && m_GroupList[groupIndex].gameObject != null
                || groupIndex < 0 && -groupIndex - 1 < m_ReverseGroupList.Count && m_ReverseGroupList[-groupIndex - 1].gameObject != null)
            {
                item = GetGroupInfo(groupIndex);
                item.RefreshPosition();
                return null;
            }

            string tag = ScrollGroup.Tag;

            if (groupIndex >= 0 && groupIndex < m_GroupList.Count)
            {
                item = m_GroupList[groupIndex];
            }
            else if (groupIndex < 0 && -groupIndex - 1 < m_ReverseGroupList.Count)
            {
                item = m_ReverseGroupList[-groupIndex - 1];
            }
            else
            {
                item = new ScrollGroup();
                item.groupIndex = groupIndex;
            }

            if (lineSpace > 0)
            {
                var go = GetFromPool(tag);
                RectTransform rt;
                if (go == null)
                {
                    go = CreateLineLuaFunc();
                    rt = go.GetComponent<RectTransform>();
                    rt.SetParent(content, false);
                }
                else
                {
                    rt = go.GetComponent<RectTransform>();
                }
                rt.anchorMin = new Vector2(0, 1);
                rt.anchorMax = new Vector2(0, 1);
                rt.pivot = GetVector2WithDir(0.5f, 1.0f - dirAxisIndex);
                rt.sizeDelta = GetVector2WithDir(
                    lineSpace,
                    viewRect.rect.size[otherDirAxisIndex] - cellSpace[otherDirAxisIndex]
                );
                //if (!go.activeSelf) go.SetActiveWithValidation(true);
                item.gameObject = go;
                item.RefreshPosition();
            }
            return item;
        }
        /// <summary>
        /// 取消一个显示中的分隔线，如果使用对象池，则把它放到池里去
        /// </summary>
        /// <param name="groupIndex">分隔线的索引，从0开始</param>
        private void HideGroup(int groupIndex)
        {
            if (lineSpace > 0)
            {
                ScrollGroup line = null;
                if (groupIndex >= 0 && groupIndex < m_GroupList.Count)
                {
                    line = m_GroupList[groupIndex];
                }
                else if (groupIndex < 0 && -groupIndex - 1 < m_ReverseGroupList.Count)
                {
                    line = m_ReverseGroupList[-groupIndex - 1];
                }

                if (line != null)
                {
                    ReturnToPool(line);
                }
            }
        }
        /// <summary>
        /// 不考虑换行的情况下下一个元素的位置
        /// </summary>
        /// <param name="item">当前元素</param>
        /// <returns>下一个元素的位置</returns>
        private Vector2 GetNextItemPosition(ScrollItem item)
        {
            var position = item.position;
            Vector2 nextItemPosition = GetVector2WithDir(
                position[dirAxisIndex],
                position[otherDirAxisIndex] + otherDirSign * GetCellSizeWithSpace(item.size)[otherDirAxisIndex]
            );
            return nextItemPosition;
        }
        /// <summary>
        /// 不考虑换行的情况下上一个元素右下角的位置，注意是右下角，所以这个坐标是不能直接被用来当做anchoredPosition的
        /// </summary>
        /// <param name="item">当前元素</param>
        /// <returns>上一个元素的位置</returns>
        private Vector2 GetPreviousItemPosition(ScrollItem item)
        {
            var position = item.position;
            Vector2 nextItemPosition = GetVector2WithDir(
                position[dirAxisIndex] + dirSign * item.size[dirAxisIndex],
                position[otherDirAxisIndex] - otherDirSign * cellSpace[otherDirAxisIndex]
            );
            return nextItemPosition;
        }
        /// <summary>
        /// 获取分组的边缘坐标
        /// </summary>
        /// <param name="groupIndex">分组索引</param>
        /// <param name="groupEdge1">左或上边缘的位置</param>
        /// <param name="groupEdge2">右或下边缘的位置</param>
        private void GetGroupEdges(int groupIndex, out float groupEdge1, out float groupEdge2)
        {
            ScrollGroup groupItem = GetGroupInfo(groupIndex);
            var groupHeight = groupItem.groupHeight;
            groupEdge2 = groupItem.position[dirAxisIndex];
            groupEdge1 = groupEdge2 - dirSign * groupHeight;
        }
        /// <summary>
        /// 是否需要换行
        /// </summary>
        /// <param name="size">待计算的元素的大小</param>
        /// <param name="anchoredPosition">不考虑换行的情况下元素的anchoredPosition，反向的情况是右下角的anchoredPosition</param>
        /// <param name="reverse">是否是反向</param>
        /// <returns>是否需要换行</returns>
        private bool NeedChangeLine(Vector2 size, Vector2 anchoredPosition, bool reverse = false)
        {
            if (!reverse && otherDirSign * anchoredPosition[otherDirAxisIndex] + GetCellSizeWithSpace(size)[otherDirAxisIndex] - cellSpace[otherDirAxisIndex] / 2 > viewRect.rect.size[otherDirAxisIndex])
            {
                return true;
            }
            else if (reverse && otherDirSign * anchoredPosition[otherDirAxisIndex] - GetCellSizeWithSpace(size)[otherDirAxisIndex] + cellSpace[otherDirAxisIndex] / 2 < 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        /// <summary>
        /// 当groupIndex为非负值时，计算指定分组的起始位置元素的anchoredPosition
        /// 当groupInfex为负值时，计算指定分组的结束位置元素的右下角坐标，注意这是在调用AdjustGroupPosition之前的位置
        /// </summary>
        /// <param name="groupIndex">行或列索引</param>
        /// <returns>起始位置</returns>
        private Vector2 CalcGroupStartPosition(int groupIndex)
        {
            if (groupIndex == 0)
            {
                return new Vector2(cellSpace.x / 2, -cellSpace.y / 2);
            }
            else if (groupIndex > 0)
            {
                var preGroup = m_GroupList[groupIndex - 1];
                var preGroupHeight = preGroup.groupHeight;
                var preGroupStartItem = m_ItemList[preGroup.startIndex];
                var preGroupStartItemPosition = preGroupStartItem.position;
                return preGroupStartItemPosition + GetVector2WithDir(preGroupHeight * dirSign, 0);
            }
            else
            {
                ScrollGroup nextGroup;
                if (groupIndex == -1) nextGroup = m_GroupList[0];
                else nextGroup = m_ReverseGroupList[-groupIndex - 2];

                var nextGroupStartItem = m_ItemList[nextGroup.startIndex];
                return GetVector2WithDir(
                    nextGroupStartItem.position[dirAxisIndex] - dirSign * cellSpace[dirAxisIndex],
                    otherDirSign * (viewRect.rect.size[otherDirAxisIndex] - cellSpace[otherDirAxisIndex] / 2)
                );
            }
        }
        /// <summary>
        /// 更新之前的可视区边界索引值为当前值
        /// </summary>
        private void UpdatePreVisibleIndex()
        {
            m_PreStartVisibleItemIndex = m_StartVisibleItemIndex;
            m_PreEndVisibleItemIndex = m_EndVisibleItemIndex;
        }
        /// <summary>
        /// 元素当前是否是显示状态的
        /// </summary>
        /// <param name="index">元素索引</param>
        /// <returns>是否显示</returns>
        private bool IsItemVisible(int index)
        {
            if (index < 0 || index >= m_ItemList.Count)
            {
                PlatDependant.LogError("判断元素是否是显示状态时索引错误");
                return false;
            }

            var item = m_ItemList[index];

            if (item != null && item.gameObject != null) return true;
            else return false;
        }
        /// <summary>
        /// 判断指定位置的元素是否创建过，即使创建过也可能当前处于位置无效状态
        /// </summary>
        /// <param name="index">待判断元素的索引</param>
        /// <returns>是否创建过</returns>
        private bool IsItemCreated(int index)
        {
            // return (index >= 0 && index <= m_StartCreatedItemIndex) || (index < m_ItemList.Count && index >= m_EndCreatedItemIndex);
            return index >= 0 && index < m_ItemList.Count && m_ItemList[index] != null;
        }
        /// <summary>
        /// 判断指定位置的元素位置是否是可靠的
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        private bool IsItemValid(int index)
        {
            return (index >= 0 && index <= m_StartCreatedItemIndex) || (index < m_ItemList.Count && index >= m_EndCreatedItemIndex);
        }
        /// <summary>
        /// 分组的可见状态
        /// </summary>
        /// <param name="offset">content的额外偏移量</param>
        /// <param name="groupIndex">行或列索引</param>
        /// <returns>0表示可见，-1表示在可视区的左侧或上侧，1表示在可视区的右侧或下侧</returns>
        private int GroupVisibleStatus(float offset, int groupIndex)
        {
            float groupEdge1 = 0;
            float groupEdge2 = 0;
            GetGroupEdges(groupIndex, out groupEdge1, out groupEdge2);

            float contentOffset = offset + content.anchoredPosition[dirAxisIndex];

            if (dirSign * (groupEdge2 + contentOffset) <= -showOffset) return -1;
            else if (dirSign * (groupEdge1 + contentOffset) >= viewRect.rect.size[dirAxisIndex] + showOffset) return 1;
            else return 0;
        }
        /// <summary>
        /// 获取行或列的宽度
        /// </summary>
        /// <param name="groupIndex">待计算元素</param>
        /// <returns>行或列的宽度</returns>
        private float CalcGroupHeight(int groupIndex)
        {
            ScrollGroup groupItem = GetGroupInfo(groupIndex);

            float maxHeight = 0;
            for(int i = groupItem.startIndex; i <= groupItem.endIndex; i++)
            {
                var item = m_ItemList[i];
                var itemHeight = GetCellSizeWithSpace(item.size)[dirAxisIndex];
                maxHeight = Mathf.Max(maxHeight, itemHeight);
            }
            return maxHeight;
        }
        /// <summary>
        /// 获取当前元素的索引
        /// </summary>
        /// <param name="groupEdge1">相对于可见区左或上边缘的位置</param>
        /// <param name="groupEdge2">相对于可见区右或下边缘的位置</param>
        /// <returns>当前元素索引</returns>
        private int GetCurrentItemIndex(out float groupEdge1, out float groupEdge2)
        {
            var item = m_ItemList[m_StartVisibleItemIndex];
            var index = -1;
            for(int i = item.groupIndex; true; i++)
            {
                GetGroupEdges(i, out groupEdge1, out groupEdge2);

                groupEdge1 += content.anchoredPosition[dirAxisIndex];
                groupEdge2 += content.anchoredPosition[dirAxisIndex];

                float delta = Mathf.Min(10f, 0.1f * GetGroupInfo(i).groupHeight);
                if (dirSign * groupEdge1 <= delta && dirSign * groupEdge2 > delta)
                {
                    index = GetGroupInfo(i).startIndex;
                    break;
                }
                if (!m_IsLoop && dirSign * groupEdge1 >= -delta)
                {
                    index = m_StartVisibleItemIndex;
                    break;
                }
            }
            return index;
        }
        /// <summary>
        /// 将一个分组逻辑索引转换为内部使用的真实索引，返回null说明传入的索引值不正确
        /// </summary>
        /// <param name="groupIndex">逻辑索引</param>
        /// <returns>真实索引</returns>
        private int? GetRealGroupIndex(int groupIndex)
        {
            int? realGroupIndex = groupIndex;
            if (groupIndex >= 0)
            {
                if (groupIndex >= m_GroupList.Count)
                {
                    if (m_IsLoop && IsCreatedAll)
                    {
                        groupIndex = groupIndex % (int)GroupCount;
                        if (groupIndex >= m_GroupList.Count) realGroupIndex = groupIndex - GroupCount;
                        else realGroupIndex = groupIndex;
                    }
                    else if (groupIndex != m_GroupList.Count)
                    {
                        realGroupIndex = null;
                    }
                }
            }
            else
            {
                if (m_ReverseGroupList.Count == 0)
                {
                    if (m_IsLoop && IsCreatedAll)
                    {
                        groupIndex = groupIndex % (int)GroupCount;
                        if (groupIndex < 0) realGroupIndex = groupIndex + GroupCount;
                        else realGroupIndex = groupIndex;
                    }
                    else
                    {
                        realGroupIndex = null;
                    }
                }
                else
                {
                    if (-groupIndex - 1 >= m_ReverseGroupList.Count)
                    {
                        if (m_IsLoop && IsCreatedAll)
                        {
                            groupIndex = groupIndex % (int)GroupCount;
                            if (-groupIndex - 1 >= m_ReverseGroupList.Count) realGroupIndex = groupIndex + GroupCount;
                            else realGroupIndex = groupIndex;
                        }
                        else if (-groupIndex - 1 != m_ReverseGroupList.Count)
                        {
                            realGroupIndex = null;
                        }
                    }
                }
            }
            return realGroupIndex;
        }
        /// <summary>
        /// 获取分组信息
        /// </summary>
        /// <param name="groupIndex">分组索引，可能为负值</param>
        /// <returns>分组信息</returns>
        private ScrollGroup GetGroupInfo(int groupIndex)
        {
            ScrollGroup groupItem = null;
            int? realGroupIndex = GetRealGroupIndex(groupIndex);
            if (realGroupIndex != null)
            {
                if (realGroupIndex >= 0 && realGroupIndex < m_GroupList.Count)
                {
                    groupItem = m_GroupList[(int)realGroupIndex];
                }
                else if (realGroupIndex < 0 && -(int)realGroupIndex - 1 < m_ReverseGroupList.Count)
                {
                    groupItem = m_ReverseGroupList[-(int)realGroupIndex - 1];
                }
            }
            return groupItem;
        }
        /// <summary>
        /// 在反向加载时，用来调整元素的排布顺序和位置，效果为向上或向左对齐
        /// </summary>
        /// <param name="groupIndex">分组的索引，这个索引必须是负值才有意义</param>
        private void AdjustGroupPosition(int groupIndex)
        {
            if (groupIndex < 0)
            {
                var groupInfo = GetGroupInfo(groupIndex);
                if (groupInfo != null)
                {
                    float groupHeight = groupInfo.groupHeight;
                    int startIndex = groupInfo.startIndex;
                    int endIndex = groupInfo.endIndex;
                    var startItem = m_ItemList[startIndex];
                    float otherDirOffset = otherDirSign * cellSpace[otherDirAxisIndex] / 2 - startItem.position[otherDirAxisIndex];
                    for (int i = startIndex; i <= endIndex; i++)
                    {
                        var item = m_ItemList[i];
                        item.SetPosition(GetVector2WithDir(
                            groupInfo.position[dirAxisIndex] - dirSign * (groupHeight - cellSpace[dirAxisIndex] / 2),
                            //(GetCellSizeWithSpace(item.size)[dirAxisIndex] - groupHeight) * dirSign,
                            item.position[otherDirAxisIndex] + otherDirOffset
                        ));
                    }
                }
            }
        }
        /// <summary>
        /// 在反向加载时，用来调整元素的排布顺序和位置，效果为向下或向右对齐
        /// </summary>
        /// <param name="groupIndex">分组的索引，这个索引必须是负值才有意义</param>
        private void UnadjustGroupPosition(int groupIndex)
        {
            if (groupIndex < 0)
            {
                var groupInfo = GetGroupInfo(groupIndex);
                if (groupInfo != null)
                {
                    float groupHeight = groupInfo.groupHeight;
                    int startIndex = groupInfo.startIndex;
                    int endIndex = groupInfo.endIndex;
                    var endItem = m_ItemList[endIndex];
                    float otherDirOffset = otherDirSign * (viewRect.rect.size[otherDirAxisIndex] - cellSpace[otherDirAxisIndex] / 2 - endItem.size[otherDirAxisIndex]) - endItem.position[otherDirAxisIndex];
                    for (int i = startIndex; i <= endIndex; i++)
                    {
                        var item = m_ItemList[i];
                        item.SetPosition(item.position + GetVector2WithDir(
                            groupInfo.position[dirAxisIndex] - dirSign * (item.size[dirAxisIndex] + cellSpace[dirAxisIndex] / 2),
                            //-(cellSpace[dirAxisIndex] + item.size[dirAxisIndex] - groupHeight) * dirSign,
                            otherDirOffset
                        ));
                    }
                }
            }
        }
        /// <summary>
        /// 设置归位的目标位置
        /// </summary>
        /// <param name="pointerDelta">本次拖拽产生的偏移量</param>
        private void SetSnapDestination(Vector2 pointerDelta)
        {
            Vector2 offsetToEdge = CalculateOffset(Vector2.zero);
            if (offsetToEdge[dirAxisIndex] * dirSign > 0)
            {
                // 向右或下边缘归位
                // 此时必然是非循环滚动
                float length = 0f;
                for (int i = m_GroupList.Count - 1; i >= 0; i--)
                {
                    var groupItem = m_GroupList[i];
                    length += groupItem.groupHeight;
                    if (length >= viewRect.rect.size[dirAxisIndex])
                    {
                        OnItemIndexChanged(groupItem.startIndex);
                        break;
                    }
                }
            }
            else if (offsetToEdge[dirAxisIndex] * dirSign < 0)
            {
                // 向左或上边缘归位
                OnItemIndexChanged(0);
            }
            else
            {
                float groupEdge1 = 0;
                float groupEdge2 = 0;
                int currentItemIndex = GetCurrentItemIndex(out groupEdge1, out groupEdge2);

                bool next;
                if (-groupEdge1 * dirSign < 3 * EventSystem.current.pixelDragThreshold) next = false;
                else if (groupEdge2 * dirSign < 3 * EventSystem.current.pixelDragThreshold) next = true;
                else if (dirSign * pointerDelta[dirAxisIndex] >= 0) next = false;
                else next = true;

                float offset;
                int destinationItemIndex;
                if (next)
                {
                    offset = -groupEdge2;
                    int currentGroupIndex = m_ItemList[currentItemIndex].groupIndex;
                    currentGroupIndex++;
                    destinationItemIndex = GetGroupInfo(currentGroupIndex).startIndex;
                }
                else
                {
                    offset = -groupEdge1;
                    destinationItemIndex = currentItemIndex;
                }

                m_SnapOffset = GetVector2WithDir(offset, 0);
                OnItemIndexChanged(destinationItemIndex);

                // 这个属性就不在外面配了，配不好会很奇怪
                float maxScrollSpeed = 100f;
                if (m_Velocity[dirAxisIndex] > maxScrollSpeed) m_Velocity[dirAxisIndex] = maxScrollSpeed;
                else if (m_Velocity[dirAxisIndex] < -maxScrollSpeed) m_Velocity[dirAxisIndex] = -maxScrollSpeed;
            }
        }
        /// <summary>
        /// 首尾合并，将反向列表中的元素向正项列表中合并
        /// </summary>
        private void MergeTailToHeadGroups()
        {
            if (m_IsLoop && IsCreatedAll && m_ReverseGroupList.Count > 0)
            {
                var startCreatedGroup = m_GroupList[m_GroupList.Count - 1];
                var endCreatedGroup = m_ReverseGroupList[m_ReverseGroupList.Count - 1];

                Vector2 position = GetNextItemPosition(m_ItemList[startCreatedGroup.endIndex]);
                float descOtherDirLength = 0f;  // 反向列表移走元素在非滚动方向上的总长度
                
                for (int i = endCreatedGroup.startIndex; i <= endCreatedGroup.endIndex; i++)
                {
                    var item = m_ItemList[i];

                    if (NeedChangeLine(item.size, position)) break;
                    else
                    {
                        startCreatedGroup.endIndex = i;
                        endCreatedGroup.startIndex = (endCreatedGroup.startIndex + 1) % m_ItemList.Count;
                        var itemDirSize = GetCellSizeWithSpace(item.size)[dirAxisIndex];
                        descOtherDirLength += GetCellSizeWithSpace(item.size)[otherDirAxisIndex];
                        if (startCreatedGroup.groupHeight < itemDirSize)
                        {
                            var change = GetVector2WithDir(itemDirSize - startCreatedGroup.groupHeight, 0);
                            content.sizeDelta += change;
                            startCreatedGroup.SetPosition(startCreatedGroup.position + dirSign * change);
                            startCreatedGroup.groupHeight = itemDirSize;
                        }
                        if (endCreatedGroup.groupHeight == itemDirSize)
                        {
                            endCreatedGroup.groupHeight = CalcGroupHeight(endCreatedGroup.groupIndex);
                            var change = GetVector2WithDir(itemDirSize - endCreatedGroup.groupHeight, 0);
                            content.sizeDelta -= change;
                        }

                        item.groupIndex = startCreatedGroup.groupIndex;
                        item.SetPosition(position);
                        position = GetNextItemPosition(m_ItemList[i]);
                        if (m_StartVisibleItemIndex == i)
                        {
                            if (endCreatedGroup.startIndex > endCreatedGroup.endIndex)
                            {
                                m_StartVisibleItemIndex = GetGroupInfo(endCreatedGroup.groupIndex + 1).startIndex;
                            }
                            else
                            {
                                m_StartVisibleItemIndex++;
                            }
                        }
                        if (m_EndVisibleItemIndex == i && endCreatedGroup.startIndex > endCreatedGroup.endIndex)
                        {
                            m_EndVisibleItemIndex = GetGroupInfo(endCreatedGroup.groupIndex + 1).endIndex;
                        }

                        if (m_EndVisibleItemIndex == m_StartCreatedItemIndex) m_EndVisibleItemIndex = i;

                        m_StartCreatedItemIndex = i;
                        m_EndCreatedItemIndex = i + 1;

                        int visibility = GroupVisibleStatus(0, item.groupIndex);

                        if (visibility == 0)
                        {
                            ShowItem(i);
                            ShowGroup(item.groupIndex);
                        }
                        else
                        {
                            HideItem(i);
                            HideGroup(item.groupIndex);
                        }
                    }
                }
                // 首尾相接时正向列表最后一个元素之后的元素应该平移的距离，暂未用到
                float dirOffset = (startCreatedGroup.position - endCreatedGroup.position)[dirAxisIndex];
                if ((endCreatedGroup.endIndex + 1) % m_ItemList.Count == endCreatedGroup.startIndex)
                {
                    HideGroup(endCreatedGroup.groupIndex);
                    m_ReverseGroupList.RemoveAt(m_ReverseGroupList.Count - 1);
                }
                else
                {
                    float height = CalcGroupHeight(endCreatedGroup.groupIndex);
                    dirOffset += dirSign * height;
                    for (int i = endCreatedGroup.startIndex; i <= endCreatedGroup.endIndex; i++)
                    {
                        var item = m_ItemList[i];
                        item.SetPosition(item.position + dirSign * GetVector2WithDir(endCreatedGroup.groupHeight - height, descOtherDirLength));
                    }
                    endCreatedGroup.groupHeight = height;
                }
            }
        }
        /// <summary>
        /// 首尾合并，将正向列表中的元素向反项列表中合并
        /// </summary>
        private void MergeHeadToTailGroups()
        {
            if (m_IsLoop && IsCreatedAll && m_ReverseGroupList.Count > 0)
            {
                var startCreatedGroup = m_GroupList[m_GroupList.Count - 1];
                var endCreatedGroup = m_ReverseGroupList[m_ReverseGroupList.Count - 1];

                UnadjustGroupPosition(endCreatedGroup.groupIndex);
                Vector2 position = GetPreviousItemPosition(m_ItemList[endCreatedGroup.startIndex]);
                
                for (int i = startCreatedGroup.endIndex; i <= startCreatedGroup.startIndex; i++)
                {
                    var item = m_ItemList[i];

                    if (NeedChangeLine(item.size, position, true)) break;
                    else
                    {
                        endCreatedGroup.startIndex = i;
                        startCreatedGroup.endIndex = (startCreatedGroup.endIndex - 1) % m_ItemList.Count;
                        var itemDirSize = GetCellSizeWithSpace(item.size)[dirAxisIndex];
                        if (startCreatedGroup.groupHeight == itemDirSize)
                        {
                            startCreatedGroup.groupHeight = CalcGroupHeight(startCreatedGroup.groupIndex);
                            var change = GetVector2WithDir(itemDirSize - startCreatedGroup.groupHeight, 0);
                            content.sizeDelta -= change;
                            startCreatedGroup.SetPosition(startCreatedGroup.position - dirSign * change);
                        }
                        if (endCreatedGroup.groupHeight < itemDirSize)
                        {
                            endCreatedGroup.groupHeight = itemDirSize;
                            var change = GetVector2WithDir(itemDirSize - endCreatedGroup.groupHeight, 0);
                            content.sizeDelta += change;
                        }

                        position += new Vector2(-item.size.x, item.size.y);
                        item.groupIndex = endCreatedGroup.groupIndex;
                        item.SetPosition(position);
                        position = GetPreviousItemPosition(m_ItemList[i]);
                        if (m_StartVisibleItemIndex == i && startCreatedGroup.startIndex > startCreatedGroup.endIndex)
                        {
                            m_StartVisibleItemIndex = GetGroupInfo(startCreatedGroup.groupIndex - 1).startIndex;
                        }
                        if (m_EndVisibleItemIndex == i)
                        {
                            if (endCreatedGroup.startIndex > endCreatedGroup.endIndex)
                            {
                                m_EndVisibleItemIndex = GetGroupInfo(startCreatedGroup.groupIndex - 1).endIndex;
                            }
                            else
                            {
                                m_EndVisibleItemIndex--;
                            }
                        }

                        if (m_StartVisibleItemIndex == m_EndCreatedItemIndex) m_StartVisibleItemIndex = i;

                        m_StartCreatedItemIndex = i;
                        m_EndCreatedItemIndex = i - 1;

                        int visibility = GroupVisibleStatus(0, item.groupIndex);

                        if (visibility == 0)
                        {
                            ShowItem(i);
                            ShowGroup(item.groupIndex);
                        }
                        else
                        {
                            HideItem(i);
                            HideGroup(item.groupIndex);
                        }
                    }
                }
                // 首尾相接时反向列表最后一个元素前的元素应该平移的距离，暂未用到
                float dirOffset = endCreatedGroup.position[dirAxisIndex] - endCreatedGroup.groupHeight - (startCreatedGroup.position[dirAxisIndex] - startCreatedGroup.groupHeight);
                if ((startCreatedGroup.endIndex + 1) % m_ItemList.Count == startCreatedGroup.startIndex)
                {
                    HideGroup(startCreatedGroup.groupIndex);
                    m_GroupList.RemoveAt(m_GroupList.Count - 1);
                }
                else
                {
                    AdjustGroupPosition(endCreatedGroup.groupIndex);
                }
            }
        }
        private float CalcScrollToOffset(int index)
        {
            if (index < 0 || index >= m_ItemList.Count)
            {
                PlatDependant.LogError("Wrong Index To Scroll");
                return 0;
            }
            CreateTo(index);

            var item = m_ItemList[index];
            float offset = -(item.position + content.anchoredPosition)[dirAxisIndex] + dirSign * cellSpace[dirAxisIndex] / 2;
            if (m_IsLoop && IsCreatedAll)
            {
                offset = offset % TotalLength;
                if (offset > 0)
                {
                    if (offset > TotalLength / 2) offset -= TotalLength;
                }
                else
                {
                    if (offset < -TotalLength / 2) offset += TotalLength;
                }
            }

            var groupInfo = GetGroupInfo(item.groupIndex);
            OnItemIndexChanged(groupInfo.startIndex);

            return offset;
        }
        /// <summary>
        /// 创建新的元素
        /// </summary>
        /// <param name="index">待创建元素的索引，只有这个位置是下一个创建位置，调用才合理</param>
        /// <param name="reverse">是在正向创建还是反向创建</param>
        /// <returns>创建的元素</returns>
        private ScrollItem CreateItem(int index)
        {
            if (index >= 0 && index < m_ItemList.Count)
            {
                if (!IsItemCreated(index))
                {
                    ScrollItem item = ShowItem(index);
                    m_ItemList[index] = item;
                    item.index = index;
                    return item;
                }
                else
                {
                    PlatDependant.LogError("Item Already Created!");
                    return null;
                }
            }
            else
            {
                PlatDependant.LogError("Wrong Index To Create Item!");
                return null;
            }
        }
        /// <summary>
        /// 创建一个Group
        /// </summary>
        /// <param name="groupIndex">待创建Group的索引，大于等于零代表正向，小于零代表反向</param>
        /// <param name="startItemIndex">Group中的起始元素索引</param>
        /// <param name="startPosition">分组的起始位置</param>
        /// <returns>创建出来的Group对象</returns>
        private ScrollGroup CreateGroup(int groupIndex, int startItemIndex, out Vector2 startPosition)
        {
            ScrollGroup groupInfo = null;
            var item = m_ItemList[startItemIndex];
            var itemHeight = GetCellSizeWithSpace(item.size)[dirAxisIndex];
            startPosition = CalcGroupStartPosition(groupIndex);

            if (groupIndex >= 0)
            {
                if (groupIndex == m_GroupList.Count)
                {
                    groupInfo = ShowGroup(groupIndex);
                    m_GroupList.Add(groupInfo);
                }
                else
                {
                    groupInfo = GetGroupInfo(groupIndex);
                    content.sizeDelta -= GetVector2WithDir(groupInfo.groupHeight, 0);
                }
                groupInfo.SetPosition(GetVector2WithDir(
                    startPosition[dirAxisIndex] + dirSign * (itemHeight - cellSpace[dirAxisIndex] / 2),
                    otherDirSign * cellSpace[otherDirAxisIndex] / 2
                ));
            }
            else if (groupIndex < 0)
            {
                if (-groupIndex - 1 == m_ReverseGroupList.Count)
                {
                    groupInfo = ShowGroup(groupIndex);
                    m_ReverseGroupList.Add(groupInfo);
                }
                else
                {
                    groupInfo = GetGroupInfo(groupIndex);
                    content.sizeDelta -= GetVector2WithDir(groupInfo.groupHeight, 0);
                }
                groupInfo.SetPosition(GetVector2WithDir(
                    startPosition[dirAxisIndex] + dirSign * cellSpace[dirAxisIndex] / 2,
                    otherDirSign * cellSpace[otherDirAxisIndex] / 2
                ));
                startPosition += new Vector2(-item.size.x, item.size.y);
            }
            groupInfo.startIndex = startItemIndex;
            groupInfo.endIndex = startItemIndex;
            groupInfo.groupHeight = itemHeight;
            content.sizeDelta += GetVector2WithDir(itemHeight, 0);
            return groupInfo;
        }
        /// <summary>
        /// 删除一个元素的信息，只能删除m_StartCreatedItemIndex和m_EndCreatedItemIndex处的元素，同时把相应的Group信息也删掉
        /// </summary>
        /// <param name="index">待删除元素的索引</param>
        private void DropItemAndGroup(int index)
        {
            if (index == m_StartCreatedItemIndex)
            {
                var item = m_ItemList[index];

                content.sizeDelta -= GetVector2WithDir(GetGroupInfo(item.groupIndex).groupHeight, 0);
                HideGroup(item.groupIndex);
                m_GroupList.RemoveAt(m_GroupList.Count - 1);
                
                HideItem(index);
                m_ItemList[index] = null;
                m_StartCreatedItemIndex--;
            }
            else if (index == m_EndCreatedItemIndex)
            {
                var item = m_ItemList[index];

                content.sizeDelta -= GetVector2WithDir(GetGroupInfo(item.groupIndex).groupHeight, 0);
                HideGroup(item.groupIndex);
                m_ReverseGroupList.RemoveAt(m_ReverseGroupList.Count - 1);

                HideItem(index);
                m_ItemList[index] = null;
                m_EndCreatedItemIndex++;
            }
            else
            {
                PlatDependant.LogError("Wrong Index To Drop Item!");
            }
        }
        /// <summary>
        /// 正向创建到index位置，但不显示
        /// </summary>
        /// <param name="index">元素索引</param>
        private void CreateTo(int index)
        {
            if (IsItemValid(index) && index != m_EndCreatedItemIndex) return;
            if (m_StartCreatedItemIndex + 1 == m_EndCreatedItemIndex) return;

            for (int i = m_StartCreatedItemIndex + 1; i <= index; i++)
            {
                ScrollItem item;
                if (IsItemCreated(i)) item = m_ItemList[i];
                else item = CreateItem(i);
                m_StartCreatedItemIndex = i;

                Vector2 position;
                int groupIndex;
                var preItem = m_ItemList[i - 1];

                position = GetNextItemPosition(preItem);
                groupIndex = preItem.groupIndex;

                if (NeedChangeLine(item.size, position))
                {
                    groupIndex++;
                    CreateGroup(groupIndex, i, out position);
                }
                else
                {
                    ScrollGroup groupInfo = GetGroupInfo(groupIndex);
                    groupInfo.endIndex = i;
                    var itemHeight = GetCellSizeWithSpace(item.size)[dirAxisIndex];
                    if (groupInfo.groupHeight < itemHeight)
                    {
                        var change = GetVector2WithDir(itemHeight - groupInfo.groupHeight, 0);
                        content.sizeDelta += change;
                        groupInfo.SetPosition(groupInfo.position + dirSign * change);
                        groupInfo.groupHeight = itemHeight;
                    }
                }

                item.groupIndex = groupIndex;
                item.SetPosition(position);

                HideItem(i);
                HideGroup(item.groupIndex);
                if (IsCreatedAll && m_EndCreatedItemIndex < m_ItemList.Count)
                {
                    AdjustGroupPosition(m_ItemList[m_EndCreatedItemIndex].groupIndex);
                    MergeTailToHeadGroups();
                    break;
                }
            }
        }
        /// <summary>
        /// 向左或向上滚动时，用来检测并更新之前可见元素后面的元素的可见性
        /// </summary>
        /// <param name="offset">偏移量</param>
        /// <returns>是否需要重新排布</returns>
        private bool CheckVisibilityAfterPreEnd(float offset)
        {
            bool beforeVisible = false;
            int visibility = 0;
            int? preGroupIndex = null;

            for (int i = m_PreEndVisibleItemIndex + 1; true; i++)
            {
                if (m_IsLoop && offset == 0 && IsCreatedAll && TotalLength <= viewRect.rect.size[dirAxisIndex] + 2 * showOffset + MaxGroupHeight)
                {
                    m_IsLoop = false;
                    m_MovementType = MovementType.Elastic;
                    return true;
                }
                // 考虑循环滚动的轮回和结束条件
                if (i == m_ItemList.Count)
                {
                    if (!m_IsLoop) break;
                    else i = 0;
                }

                // 这个元素曾经创建过
                if (IsItemValid(i))
                {
                    var item = m_ItemList[i];
                    Vector2 itemPositionOffset = Vector2.zero;
                    Vector2 groupPositionOffset = Vector2.zero;
                    ScrollGroup groupInfo = GetGroupInfo(item.groupIndex);

                    int preEndVisibleItemIndex = Mathf.Max(m_PreEndVisibleItemIndex, 0);
                    if (preEndVisibleItemIndex >= 0 && (i == preEndVisibleItemIndex || (item.position[dirAxisIndex] - m_ItemList[preEndVisibleItemIndex].position[dirAxisIndex]) * dirSign < 0))
                    {
                        itemPositionOffset = GetVector2WithDir(TotalLength * dirSign, 0);
                        item.SetPositionRecord(item.position + itemPositionOffset);
                        if ((groupInfo.position[dirAxisIndex] - item.position[dirAxisIndex]) * dirSign < 0)
                        {
                            groupPositionOffset = GetVector2WithDir(TotalLength * dirSign, 0);
                            groupInfo.SetPositionRecord(groupInfo.position + groupPositionOffset);
                        }
                    }
                    if (preGroupIndex == null || preGroupIndex != item.groupIndex)
                    {
                        preGroupIndex = item.groupIndex;
                        visibility = GroupVisibleStatus(offset, item.groupIndex);
                    }
                    if (visibility == 1)
                    {
                        if (itemPositionOffset != Vector2.zero) item.SetPositionRecord(item.position - itemPositionOffset);
                        if (groupPositionOffset != Vector2.zero) groupInfo.SetPositionRecord(groupInfo.position - groupPositionOffset);
                        break;
                    }
                    else if (visibility == -1)
                    {
                        HideItem(i);
                        HideGroup(item.groupIndex);
                        beforeVisible = true;
                    }
                    else
                    {
                        // 如果原来的元素不可见，则应该初始化一遍
                        ShowItem(i);
                        ShowGroup(item.groupIndex);

                        if (beforeVisible)
                        {
                            beforeVisible = false;
                            m_StartVisibleItemIndex = i;
                        }
                        m_EndVisibleItemIndex = i;
                    }
                }
                else
                {
                    ScrollItem item;
                    if (IsItemCreated(i)) item = m_ItemList[i];
                    else item = CreateItem(i);
                    m_StartCreatedItemIndex = i;

                    Vector2 position;
                    int groupIndex;
                    if (i == 0)
                    {
                        groupIndex = 0;
                        CreateGroup(groupIndex, i, out position);
                    }
                    else
                    {
                        var preItem = m_ItemList[i - 1];

                        position = GetNextItemPosition(preItem);
                        groupIndex = preItem.groupIndex;

                        if (NeedChangeLine(item.size, position))
                        {
                            groupIndex++;
                            CreateGroup(groupIndex, i, out position);
                        }
                        else
                        {
                            ScrollGroup groupInfo = GetGroupInfo(groupIndex);
                            groupInfo.endIndex = i;
                            var itemHeight = GetCellSizeWithSpace(item.size)[dirAxisIndex];
                            if (groupInfo.groupHeight < itemHeight)
                            {
                                var change = GetVector2WithDir(itemHeight - groupInfo.groupHeight, 0);
                                content.sizeDelta += change;
                                groupInfo.SetPosition(groupInfo.position + dirSign * change);
                                groupInfo.groupHeight = itemHeight;
                            }
                        }
                    }

                    item.groupIndex = groupIndex;
                    item.SetPosition(position);

                    if (preGroupIndex == null || preGroupIndex != item.groupIndex)
                    {
                        preGroupIndex = item.groupIndex;
                        visibility = GroupVisibleStatus(offset, item.groupIndex);
                    }
                    if (visibility == 1)
                    {
                        //DropItemAndGroup(i);
                        HideItem(i);
                        HideGroup(item.groupIndex);
                    }
                    else if (visibility == -1)
                    {
                        HideItem(i);
                        HideGroup(item.groupIndex);
                        beforeVisible = true;
                    }
                    else if (visibility == 0)
                    {
                        ShowItem(i);
                        ShowGroup(item.groupIndex);
                        if (beforeVisible)
                        {
                            beforeVisible = false;
                            m_StartVisibleItemIndex = i;
                        }
                        m_EndVisibleItemIndex = i;
                    }
                    if (IsCreatedAll && m_EndCreatedItemIndex < m_ItemList.Count)
                    {
                        AdjustGroupPosition(m_ItemList[m_EndCreatedItemIndex].groupIndex);
                        MergeTailToHeadGroups();
                    }
                    if (visibility == 1) break;
                }
            }
            return false;
        }
        /// <summary>
        /// 向右或向下滚动时，用来检测并更新之前可见元素前面的元素的可见性
        /// </summary>
        /// <param name="offset">偏移量</param>
        /// <returns>是否需要重新排布</returns>
        private bool CheckVisibilityBeforePreStart(float offset)
        {
            bool beforeVisible = false;
            bool adjustCreated = false;
            int visibility = 0;
            int? preGroupIndex = null;

            int preStartVisibleItemIndex = Mathf.Max(m_PreStartVisibleItemIndex, 0);
            for (int i = preStartVisibleItemIndex - 1; true; i--)
            {
                if (m_IsLoop && offset == 0 && IsCreatedAll && TotalLength <= viewRect.rect.size[dirAxisIndex] + 2 * showOffset + MaxGroupHeight)
                {
                    m_IsLoop = false;
                    m_MovementType = MovementType.Elastic;

                    return true;
                }
                // 考虑循环滚动的轮回和结束条件
                if (i < 0)
                {
                    if (!m_IsLoop) break;
                    else i = i + m_ItemList.Count;
                }

                // 这个元素曾经创建过
                if (IsItemValid(i))
                {
                    if (!adjustCreated)
                    {
                        // 调整之前创建的一行的元素的位置
                        var lastItem = m_ItemList[(i + 1) % m_ItemList.Count];
                        AdjustGroupPosition(lastItem.groupIndex);
                        adjustCreated = true;
                    }
                    var item = m_ItemList[i];

                    Vector2 itemPositionOffset = Vector2.zero;
                    Vector2 groupPositionOffset = Vector2.zero;
                    ScrollGroup groupInfo = GetGroupInfo(item.groupIndex);
                    if (i == preStartVisibleItemIndex || (item.position[dirAxisIndex] - m_ItemList[preStartVisibleItemIndex].position[dirAxisIndex]) * dirSign > 0)
                    {
                        if ((groupInfo.position[dirAxisIndex] - item.position[dirAxisIndex]) * dirSign > 0)
                        {
                            groupPositionOffset = GetVector2WithDir(TotalLength * dirSign, 0);
                            groupInfo.SetPositionRecord(groupInfo.position - groupPositionOffset);
                        }
                        itemPositionOffset = GetVector2WithDir(TotalLength * dirSign, 0);
                        item.SetPositionRecord(item.position - itemPositionOffset);
                    }

                    if (preGroupIndex == null || preGroupIndex != item.groupIndex)
                    {
                        preGroupIndex = item.groupIndex;
                        visibility = GroupVisibleStatus(offset, item.groupIndex);
                    }

                    if (visibility == 1)
                    {
                        HideItem(i);
                        HideGroup(item.groupIndex);
                        beforeVisible = true;
                    }
                    else if (visibility == -1)
                    {
                        if (itemPositionOffset != Vector2.zero) item.SetPositionRecord(item.position + itemPositionOffset);
                        if (groupPositionOffset != Vector2.zero) groupInfo.SetPositionRecord(groupInfo.position + groupPositionOffset);
                        break;
                    }
                    else if (visibility == 0)
                    {
                        // 如果原来的元素不可见，则应该初始化一遍
                        ShowItem(i);
                        ShowGroup(item.groupIndex);

                        if (beforeVisible)
                        {
                            beforeVisible = false;
                            m_EndVisibleItemIndex = i;
                        }
                        m_StartVisibleItemIndex = i;
                    }
                }
                else
                {
                    var lastItem = m_ItemList[(i + 1) % m_ItemList.Count];
                    var lastGroupIndex = lastItem.groupIndex;
                    var groupIndex = lastGroupIndex;

                    ScrollItem item;
                    if (IsItemCreated(i)) item = m_ItemList[i];
                    else item = CreateItem(i);
                    m_EndCreatedItemIndex = i;

                    // 反向排布在下一个位置上
                    var position = GetPreviousItemPosition(lastItem);
                    if (NeedChangeLine(item.size, position, true))
                    {
                        // 如果需要换行，调整之前一行中创建元素的位置，将元素摆在新的一行的起始位置，创建分隔线
                        if (!adjustCreated)
                        {
                            AdjustGroupPosition(lastGroupIndex);
                            adjustCreated = true;
                        }

                        groupIndex--;
                        CreateGroup(groupIndex, i, out position);
                    }
                    else
                    {
                        // 否则，调整行信息，调整行的位置
                        position += new Vector2(-item.size.x, item.size.y);

                        ScrollGroup groupInfo = GetGroupInfo(groupIndex);
                        groupInfo.startIndex = i;
                        var itemHeight = GetCellSizeWithSpace(item.size)[dirAxisIndex];
                        if (groupInfo.groupHeight < itemHeight)
                        {
                            content.sizeDelta += GetVector2WithDir(itemHeight - groupInfo.groupHeight, 0);
                            groupInfo.groupHeight = itemHeight;
                        }
                    }

                    adjustCreated = false;
                    item.SetPosition(position);
                    item.groupIndex = groupIndex;

                    if (preGroupIndex == null || preGroupIndex != item.groupIndex)
                    {
                        preGroupIndex = item.groupIndex;
                        visibility = GroupVisibleStatus(offset, item.groupIndex);
                    }

                    if (visibility == 1)
                    {
                        HideItem(i);
                        HideGroup(item.groupIndex);
                        beforeVisible = true;
                    }
                    else if (visibility == -1)
                    {
                        //DropItemAndGroup(i);
                        HideItem(i);
                        HideGroup(item.groupIndex);
                    }
                    else if (visibility == 0)
                    {
                        ShowItem(i);
                        ShowGroup(item.groupIndex);
                        if (beforeVisible)
                        {
                            beforeVisible = false;
                            m_EndVisibleItemIndex = i;
                        }
                        m_StartVisibleItemIndex = i;
                    }
                    if (IsCreatedAll && m_EndCreatedItemIndex < m_ItemList.Count) MergeHeadToTailGroups();
                    if (visibility == -1) break;
                }
            }
            return false;
        }
        /// <summary>
        /// 向左或向上滚动时，用来检测并更新之前可见元素的可见性
        /// </summary>
        /// <param name="offset">本次的偏移量</param>
        private void CheckPreVisibleItem(float offset)
        {
            if (m_PreStartVisibleItemIndex == -1 || m_PreEndVisibleItemIndex == -1) return;

            bool beforeVisible = false;
            int visibility = 0;
            int? preGroupIndex = null;

            for (int i = m_PreStartVisibleItemIndex; true; i++)
            {
                var item = m_ItemList[i];
                if (preGroupIndex == null || preGroupIndex != item.groupIndex)
                {
                    preGroupIndex = item.groupIndex;
                    visibility = GroupVisibleStatus(offset, item.groupIndex);
                }

                if (visibility == 0)
                {
                    ShowItem(i);
                    ShowGroup(item.groupIndex);

                    if (beforeVisible)
                    {
                        beforeVisible = false;
                        m_StartVisibleItemIndex = i;
                    }
                    if (!m_IsLoop) break;
                }
                else if (visibility == -1)
                {
                    HideItem(i);
                    HideGroup(item.groupIndex);
                    if (!beforeVisible) beforeVisible = true;
                }

                if (i == m_PreEndVisibleItemIndex)
                {
                    if (visibility == -1)
                    {
                        int nextItemIndex = (i + 1) % m_ItemList.Count;
                        var nextItem = m_ItemList[nextItemIndex];
                        if (GroupVisibleStatus(offset, nextItem.groupIndex) == 0) m_StartVisibleItemIndex = nextItemIndex;
                    }
                    break;
                }
                if (m_IsLoop && i == m_ItemList.Count - 1) i = -1;
            }
        }
        /// <summary>
        /// 向右或向下滚动时，用来检测并更新之前可见元素的可见性
        /// </summary>
        /// <param name="offset">本次的偏移量</param>
        private void CheckPreVisibleItemReverse(float offset)
        {
            if (m_PreStartVisibleItemIndex == -1 || m_PreEndVisibleItemIndex == -1) return;

            bool beforeVisible = false;
            int visibility = 0;
            int? preGroupIndex = null;

            for (int i = m_PreEndVisibleItemIndex; true; i--)
            {
                var item = m_ItemList[i];
                if (preGroupIndex == null || preGroupIndex != item.groupIndex)
                {
                    preGroupIndex = item.groupIndex;
                    visibility = GroupVisibleStatus(offset, item.groupIndex);
                }

                if (visibility == 0)
                {
                    ShowItem(i);
                    ShowGroup(item.groupIndex);

                    if (beforeVisible)
                    {
                        beforeVisible = false;
                        m_EndVisibleItemIndex = i;
                    }
                }
                else if (visibility == 1)
                {
                    HideItem(i);
                    HideGroup(item.groupIndex);
                    if (!beforeVisible) beforeVisible = true;
                }

                if (i == m_PreStartVisibleItemIndex)
                {
                    if (visibility == 1)
                    {
                        int preItemIndex = (i + m_ItemList.Count - 1) % m_ItemList.Count;
                        var preItem = m_ItemList[preItemIndex];
                        if (GroupVisibleStatus(offset, preItem.groupIndex) == 0) m_EndVisibleItemIndex = preItemIndex;
                    }
                    break;
                }
                if (i == 0) i = m_ItemList.Count;
            }
        }
        /// <summary>
        /// content的位置在现在的基础上再偏移offset的可见性判断
        /// </summary>
        /// <param name="offset">偏移量，为0的时候说明是初始化或添加删除</param>
        private void CheckVisibility(float offset)
        {
            if (m_ItemList.Count == 0)
            {
                // 归位
                m_Content.anchoredPosition = Vector2.zero;
                return;
            }
            // 向左或向上滑动
            if (offset * dirSign < 0)
            {
                if (!CheckVisibilityAfterPreEnd(offset))
                {
                    CheckPreVisibleItem(offset);
                }
                else
                {
                    InitRecord(m_ItemList.Count, false);
                    m_Content.anchoredPosition = Vector2.zero;
                    CheckVisibility(0);
                }
            }
            // 向右或向下滚动
            else if (offset * dirSign > 0)
            {
                if (!CheckVisibilityBeforePreStart(offset))
                {
                    CheckPreVisibleItemReverse(offset);
                }
                else
                {
                    InitRecord(m_ItemList.Count, false);
                    m_Content.anchoredPosition = Vector2.zero;
                    CheckVisibility(0);
                }
            }
            // 初始化或添加删除
            else
            {
                if (!CheckVisibilityAfterPreEnd(offset))
                {
                    m_StartVisibleItemIndex = Mathf.Max(m_StartVisibleItemIndex, 0);
                    
                    if (m_IsLoop)
                    {
                        if (!CheckVisibilityBeforePreStart(offset))
                        {
                            CheckPreVisibleItemReverse(offset);
                        }
                        else
                        {
                            InitRecord(m_ItemList.Count, false);
                            m_Content.anchoredPosition = Vector2.zero;
                            CheckVisibility(0);
                        }
                    }
                }
                else
                {
                    InitRecord(m_ItemList.Count, false);
                    m_Content.anchoredPosition = Vector2.zero;
                    CheckVisibility(0);
                }
                InitPool();
            }
            UpdatePreVisibleIndex();
        }
        #endregion

        #endregion

#if  UNITY_EDITOR
        protected override void Reset()
        {
            base.Reset();
            if (string.IsNullOrEmpty(GetComponent<LuaBehav>().InitLuaPath))
            {
                GetComponent<LuaBehav>().InitLuaPath = "ui.control.scroll.LuaScrollRectEx";
            }
        }
#endif

        protected override void OnDestroy()
        {
            //createItemLuaFunc = null;
            //createLineLuaFunc = null;
            //resetItemLuaFunc = null;
            //getItemTagLuaFunc = null;
            //onItemIndexChanged = null;
            //onScrollPositionChanged = null;
            base.OnDestroy();
        }
    }
}
