using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngineEx;

namespace UnityEngineEx.UI
{
    public class ScorllItemData
    {
        public int index = 0; // 内部索引
        public GameObject gameObject;
    }

    [AddComponentMenu("UI/Scroll Rect Ex Same Size", 34)]
    [SelectionBase]
    [RequireComponent(typeof(ScrollRect))]
    [RequireComponent(typeof(LuaBehav))]
    [DisallowMultipleComponent]
    public class ScrollRectExSameSize : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler, ILuaBehavEx
    {
        public enum Direction
        {
            Horizontal,
            Vertical
        }

        #region Public Interface
        public void RefreshWithItemCount(int itemCount)    // 外部调用的主要接口，传入数据个数，然后整个ScrollRect就会刷新
        {
            totalCount = itemCount;
            InitConfig();
            UpdateTotalWidthAndHeight();
            InitObject();
            OnValueChange(Vector2.zero);
            OnItemIndexChanged(currentPageIndex);
        }

        public void RefreshWithItemCountByScrollPos(int itemCount, float normalizedPos)    // 防止手动移动位置时设置上一次RestItem数据(手动控制滑动位置时不会刷新旧数据)
        {
            totalCount = itemCount;
            InitConfig();
            UpdateTotalWidthAndHeight();
            ScrollToPosImmediate(normalizedPos);
        }
        public void RecalcCellCountWithViewSize(float width, float height)
        {
            var viewSize = GetVector2WithDirection(width, height);
            maxPerLine = Mathf.Max(1, Mathf.FloorToInt((viewSize.y + cellSpace[otherDirectionAxisIndex]) / cellSizeWithSpace[otherDirectionAxisIndex]));
            if (usePool)
            {
                viewCount = Mathf.FloorToInt(viewSize.x / cellSizeWithSpace[directionAxisIndex]) + 2;
            }
        }
        public void RemoveAllItem()
        {
            for (int i = contentTransform.childCount - 1; i >= 0; i--)
            {
                var obj = contentTransform.GetChild(i).gameObject;
                Object.Destroy(obj);
            }
            ResetData();
            unUsedItemQueue.Clear();
            unUsedLineQueue.Clear();
        }
        public void ClearData()
        {
            for (int i = 0; i < itemList.Count; i++)
            {
                var itemData = itemList[i];
                itemData.index = i;
                MoveItemToPool(itemData);
            }
            for (int i = 0; i < lineList.Count; i++)
            {
                var lineData = lineList[i];
                lineData.index = i;
                MoveLineToPool(lineData);
            }
            ResetData();
        }
        public void ScrollToCellImmediate(int index) // 没有滚动效果瞬间设置,外部索引
        {
            int pageIndex = Mathf.RoundToInt(index / maxPerLine);
            ScrollToPageImmediate(pageIndex);
        }
        public void ScrollToPageImmediate(int index) // 没有滚动效果瞬间设置,外部索引
        {
            if (totalCount <= 0)
            {
                PlatDependant.LogInfo("error element Count =" + totalCount);
                return;
            }
            currentPageIndex = index;
            currentInternalIndex = index;
            IsTurnPage();
            contentTransform.anchoredPosition = GetVector2WithDirection(
                FixContentPos(-cellSizeWithSpace[directionAxisIndex] * currentInternalIndex * directionSign[directionAxisIndex]),
                contentTransform.anchoredPosition[otherDirectionAxisIndex]
            );

            itemIndex = int.MaxValue;

            OnItemIndexChanged(currentPageIndex);
            SetCurrentScrollPosition(contentTransform.anchoredPosition[directionAxisIndex]);
            OnValueChange(Vector2.zero);
        }
        public void ScrollToCell(int index) // 外部固定索引
        {
            int pageIndex = Mathf.RoundToInt(index / maxPerLine);
            ScrollToPage(pageIndex);
        }

        public void ScrollToCellEx(int index) // 外部固定索引
        {
            int pageIndex = Mathf.RoundToInt(index / maxPerLine);
            ScrollToPageEx(pageIndex);
        }

        public void ScrollToPage(int index) // 外部固定索引
        {
            if (totalCount <= 0)
            {
                PlatDependant.LogInfo("error element Count =" + totalCount);
                return;
            }
            if (index >= 0 && index < totalCount)
            {
                var itemCount = itemList.Count;
                var pageIndex = index;
                for (int i = 0; i < itemCount; i++)
                {
                    var item = itemList[i];
                    var adjustIndex = item.index;
                    adjustIndex = adjustIndex % totalCount;
                    if (adjustIndex < 0)
                    {
                        adjustIndex += totalCount;
                    }
                    if (adjustIndex == index)
                    {
                        pageIndex = item.index;
                        break;
                    }
                }
                ScrollToInternalPage(pageIndex);
            }
            else
            {
                PlatDependant.LogInfo("input page error index =" + index + ",current pageTotalCount = " + totalCount);
            }
        }

        // 不改动原有版本 去掉元素滚动时边界判断
        public void ScrollToPageEx(int index) // 外部固定索引
        {
            if (totalCount <= 0)
            {
                PlatDependant.LogInfo("error element Count =" + totalCount);
                return;
            }
            if (index >= 0 && index < totalCount)
            {
                var itemCount = itemList.Count;
                var pageIndex = index;
                for (int i = 0; i < itemCount; i++)
                {
                    var item = itemList[i];
                    var adjustIndex = item.index;
                    adjustIndex = adjustIndex % totalCount;
                    if (adjustIndex < 0)
                    {
                        adjustIndex += totalCount;
                    }
                    if (adjustIndex == index)
                    {
                        pageIndex = item.index;
                        break;
                    }
                }
                ScrollToInternalPageEx(pageIndex);
            }
            else
            {
                PlatDependant.LogInfo("input page error index =" + index + ",current pageTotalCount = " + totalCount);
            }
        }
        public void ScrollToNextGroup()
        { // 不能用itemIndex 会有小数点偏差，改用Round精确判断
            if (totalCount <= 0)
            {
                PlatDependant.LogInfo("error element Count =" + totalCount);
                return;
            }
            int pos = 0;
            pos = GetPageIndex();
            pos = pos + 1;

            if (isLoop || Mathf.Abs(contentTransform.anchoredPosition[directionAxisIndex]) + Mathf.Abs(selfTransform.rect.size[directionAxisIndex]) < contentTransform.rect.size[directionAxisIndex])
            {
                MoveOffset(pos);
            }
            else
            {
                var maxNextRoll = totalLines;
                if (currentPageIndex < (maxNextRoll - 1))
                {
                    currentPageIndex = currentPageIndex + 1;
                }
                OnItemIndexChanged(currentPageIndex);
            }
        }
        public void ScrollToPreviousGroup()
        {
            if (totalCount <= 0)
            {
                PlatDependant.LogInfo("error element Count =" + totalCount);
                return;
            }
            int pos = 0;
            pos = GetPageIndex();
            pos = pos - 1;

            if (isLoop || Mathf.Abs((int)contentTransform.anchoredPosition[directionAxisIndex]) > 0)
            {
                MoveOffset(pos);
            }
        }
        public void AddItem(int index)
        {
            if (isLoop) // 循环滚动增加容易造成数据混乱
            {
                return;
            }
            if (index > totalCount)
            {
                PlatDependant.LogError("add error:" + index);
                return;
            }
            totalCount += 1;
            AddItemIntoPanel(index);
            if (isShowLine) AddLine();
            UpdateTotalWidthAndHeight();
        }
        public void RemoveItem(int index) // 内部索引 会跟着item刷数据走，不是真实索引
        {
            // 循环滚动删除容易造成数据混乱
            if (isLoop) return;

            if (index < 0 || index > totalCount - 1)
            {
                PlatDependant.LogError("删除错误:" + index);
                return;
            }
            totalCount = totalCount - 1;
            DelItemFromPanel(index);
            UpdateTotalWidthAndHeight();
            if (isShowLine) DeleteLine();
        }
        public void AddItemLast()
        {
            AddItem(totalCount);
        }
        public void OnValueChange(Vector2 pos)  // 当ScrollRect发生滑动时会被持续调用
        {
            SetCurrentScrollPosition(contentTransform.anchoredPosition[directionAxisIndex]);
            // 采用默认方式循环时候 必须使用reset，反之return
            if (!usePool && !isLoop) { return; }

            var index = GetPosIndex();
            // 满足这个条件说明已经进入下一行或下一列了
            if (itemIndex != index)
            {
                itemIndex = index;
                // 处理元素
                for (int i = itemList.Count; i > 0; i--)
                {
                    var item = itemList[i - 1];
                    if (item.index < itemIndex * maxPerLine || (item.index >= (itemIndex + viewCount) * maxPerLine))
                    {
                        MoveItemToPool(item);
                        itemList.Remove(item);
                    }
                }
                for (int i = itemIndex * maxPerLine; i < (itemIndex + viewCount) * maxPerLine; i++)
                {
                    if (!isLoop)
                    {
                        if (i < 0) continue;
                        if (i > totalCount - 1) continue;
                    }
                    bool isExist = false;
                    foreach (ScorllItemData item in itemList)
                    {
                        if (item.index == i)
                        {
                            isExist = true;
                            break;
                        }
                    }
                    if (isExist)
                    {
                        continue;
                    }
                    CreateItem(i);
                }

                // 处理线
                if (isShowLine)
                {
                    var maxRoll = totalLines;
                    for (int i = lineList.Count; i > 0; i--)
                    {
                        var line = lineList[i - 1];
                        if (line.index < itemIndex || (line.index >= (itemIndex + viewCount)))
                        {
                            MoveLineToPool(line);
                            lineList.Remove(line);
                        }
                    }

                    for (int i = itemIndex; i < (itemIndex + viewCount); i++)
                    {
                        if (!isLoop)
                        {
                            if (i < 0) continue;
                            if (i > maxRoll - 1) continue;
                        }
                        bool isExist = false;
                        foreach (ScorllItemData line in lineList)
                        {
                            if (line.index == i)
                            {
                                isExist = true;
                                break;
                            }
                        }
                        if (isExist)
                        {
                            continue;
                        }
                        CreateLine(i);
                    }
                }
            }
        }
        public int GetMaxPerLine()
        {
            return maxPerLine;
        }
        public Direction GetDirection()
        {
            if (scrollRect.horizontal)
            {
                return Direction.Horizontal;
            }
            else
            {
                return Direction.Vertical;
            }
        }
        public float GetLineSpace()
        {
            return lineSpace;
        }
        public void ResetWithCellSize(float width, float height)
        {
            RemoveAllItem();
            SetCellSize(width, height);
        }
        public void ResetWithViewSize(float width, float height)
        {
            selfTransform.sizeDelta = new Vector2(width, height);
            CalcCellCount();
        }
        public void ResetWithCellSpace(float spaceWidth, float spaceHeight)
        {
            cellSpace = new Vector2(spaceWidth, spaceHeight);
            CalcCellCount();
        }
        public void ScrollToPosImmediate(float normalizedPos)
        {
            if (totalCount <= 0)
            {
                PlatDependant.LogInfo("error element Count =" + totalCount);
                return;
            }
            float dst = (normalizedPos * otherDirectionAxisIndex + (1 - normalizedPos) * directionAxisIndex) * contentTransform.sizeDelta[directionAxisIndex];
            currentPageIndex = Mathf.FloorToInt(dst / cellSizeWithSpace[directionAxisIndex]);
            currentInternalIndex = currentPageIndex;
            IsTurnPage();
            contentTransform.anchoredPosition = GetVector2WithDirection(
                FixContentPos(-dst * directionSign[directionAxisIndex]),
                contentTransform.anchoredPosition[otherDirectionAxisIndex]
            );

            itemIndex = int.MaxValue;
            OnItemIndexChanged(currentPageIndex);
            SetCurrentScrollPosition(contentTransform.anchoredPosition[directionAxisIndex]);
            OnValueChange(Vector2.zero);
        }

        public float GetScrollToPosNormalizedPos()
        {
            return -contentTransform.anchoredPosition[directionAxisIndex] / contentTransform.sizeDelta[directionAxisIndex] + 1 * directionAxisIndex;
        }
        public void CalcCellCount()
        {
            var contentWidth = selfTransform.rect.size[otherDirectionAxisIndex];
            var contentHeight = selfTransform.rect.size[directionAxisIndex];
            maxPerLine = Mathf.Max(1, Mathf.FloorToInt((contentWidth + cellSpace[otherDirectionAxisIndex]) / cellSizeWithSpace[otherDirectionAxisIndex]));
            if (usePool)
            {
                viewCount = Mathf.FloorToInt(contentHeight / cellSizeWithSpace[directionAxisIndex]) + 2;
            }
        }
        #endregion

        #region Editor Fields
        [SerializeField]
        private ScrollRect scrollRect;
        private RectTransform contentTransform
        {
            get
            {
                return scrollRect.content;
            }
        }

        private RectTransform selfTransform
        {
            get
            {
                return (RectTransform)this.gameObject.transform;
            }
        }

        private ScrollRect.MovementType movementType
        {
            get
            {
                return scrollRect.movementType;
            }
        }
        [SerializeField]
        private Vector2 cellSpace = Vector2.zero;
        [SerializeField]
        private Vector2 cellSize = Vector2.zero;
        [SerializeField]
        private float lineSpace;
        [SerializeField]
        private float dragParameter = 100f; // 值越大，拖拽结束时归位越快
        [SerializeField]
        private bool isShowLine = true;
        [SerializeField]
        private bool usePool = true; // 使用对象池
        [SerializeField]
        private bool isLoop = false; // 循环滚动
        [SerializeField]
        private bool snapToGrid = false; // 按元素滚动
        [SerializeField]
        private float intervalRoll = 30f; // 按元素滚动 超过当前数 进行换页
        [SerializeField]
        private bool b_CheckPosition;

        private float elasticity
        {
            get
            {
                return scrollRect.elasticity;
            }
        }

        private float decelerationRate
        {
            get
            {
                return scrollRect.decelerationRate;
            }
        }
        private bool inertia
        {
            get
            {
                return scrollRect.inertia;
            }
        }
        [SerializeField]
        private float dragRat = 0.5f; // 滑动系数
        #endregion

        #region Internal Fields
        private int viewCount;    // 实例化的行数或列数
        private int maxPerLine; //单行或单列的Item数量
        private int currentPageIndex = 0; // 如果初始状态为0，代表已经滚动了的行数或列数
        private int currentInternalIndex = 0;  // currentPageIndex的内部索引
        private int nextIndex = 0;    // 当前超出可视区左侧或上侧的元素的个数
        private int prePageIndex = 0;
        private int totalCount;
        private int itemIndex = int.MaxValue;
        private List<ScorllItemData> itemList = new List<ScorllItemData>();   // 实例化的item
        private List<ScorllItemData> lineList = new List<ScorllItemData>();   // 实例化的line
        private Dictionary<int, GameObject> itemViewDict = new Dictionary<int, GameObject>(); //显示可视区域中元素
        private bool isAutoScrolling; // 当前是否正在进行自动滑动，可能是由于惯性，可能是在归位，也可能是正在自动翻页
        private Vector2 preContentPosition = Vector2.zero;
        private float offset = 0; // 距离目的地的距离
        private float totalTime = 0; // 拖拽结束归位用时
        private float leftTime = 0; // 拖拽结束剩余时间
        private float changePosition = 0; // 转动系数（cube使用）
        private Queue<ScorllItemData> unUsedItemQueue = new Queue<ScorllItemData>(); // 缓存上一列移出item 或者 移除的item
        private Queue<ScorllItemData> unUsedLineQueue = new Queue<ScorllItemData>(); // 缓存上一列移出line 或者 移除的line
        private ScrollRectExSameSize parentScrollRect; //距离最近的祖先CScrollRect，在交叉滚动的判定时会用到
        private bool isDrag;  // 当前是否正在拖动
        private float destination = 0;
        private Vector2 disableDistance = new Vector2(100000, 100000); // 回收的对象采用移动出屏幕外 提高效率
        private int[] directionSign = { 1, -1 };  // x方向为1，y方向为-1
        private int totalLines { get { return Mathf.CeilToInt((float)totalCount / maxPerLine); } }
        private Vector2 cellSizeWithSpace { get { return GetVector2WithDirection(cellSize[directionAxisIndex] + cellSpace[directionAxisIndex] + lineSpace, cellSize[otherDirectionAxisIndex] + cellSpace[otherDirectionAxisIndex]); } }
        private int directionAxisIndex { get { return scrollRect.horizontal ? 0 : 1; } }   // 横向滚动时返回0，纵向滚动时返回1
        private int otherDirectionAxisIndex { get { return 1 - directionAxisIndex; } }  // 横向滚动时返回1，纵向滚动时返回0
        private Vector2 GetVector2WithDirection(float x, float y)   // 根据当前的滚动方向决定返回的Vector2中x和y的先后顺序
        {
            if (scrollRect.horizontal)
            {
                return new Vector2(x, y);
            }
            else
            {
                return new Vector2(y, x);
            }
        }
        private float FixContentPos(float dest)   // 防止可视区域超出边界反弹
        {
            if (isLoop) { return dest; }
            var selfValue = selfTransform.rect.size[directionAxisIndex];
            var contentValue = contentTransform.rect.size[directionAxisIndex];
            var difference = contentValue - selfValue > 0 ? contentValue - selfValue : 0;
            if (directionSign[otherDirectionAxisIndex] * dest > difference)
            {
                dest = directionSign[otherDirectionAxisIndex] * difference;
            }
            return dest;
        }
        #endregion

        #region LuaCallback

        private GameObject CreateItemLuaFunc(int index)
        {
            GameObject obj;
            this.CallLuaFunc("createItem", out obj, index + 1);
            itemViewDict[index + 1] = obj;
            return obj;
        }
        private GameObject CreateLineLuaFunc(int index)
        {
            GameObject obj;
            this.CallLuaFunc("createLine", out obj, index +1);
            return obj;
        }
        private void ResetItemLuaFunc(GameObject obj, int internalIndex, int index)
        {
            // 带循环滚动，所以用internalIndex作为传进来参数(索引跟着循环走)，index用作使用数据的索引（数据中的真实索引）
            itemViewDict[index + 1] = obj;
            var itemLuaBehaviour = obj.GetComponent<LuaBehav>();
            //resetItemLuaFunc(luaBehaviour.lua, itemLuaBehaviour ? itemLuaBehaviour.lua : null, index + 1);
            this.CallLuaFunc("resetItem", itemLuaBehaviour, index + 1);
        }
        private void UpdateItemIndexLuaFunc(GameObject obj, int index)
        {
            itemViewDict[index + 1] = obj;
            var itemLuaBehaviour = obj.GetComponent<LuaBehav>();
            //updateItemIndexLuaFunc(luaBehaviour.lua, itemLuaBehaviour ? itemLuaBehaviour.lua : null, index + 1);
            this.CallLuaFunc("updateItemIndex", itemLuaBehaviour, index + 1);
        }
        private void DestroyItemLuaFunc(int index)
        {
            //destroyItemLuaFunc(luaBehaviour.lua, index + 1);
            this.CallLuaFunc("destroyItem", index + 1);
        }
        private void OnItemIndexChanged(int index)
        {
            //onItemIndexChanged(luaBehaviour.lua, index + 1);
            this.CallLuaFunc("onItemIndexChanged", index + 1);
        }
        private void OnScrollPositionChanged(float position)
        {
            //onScrollPositionChanged(luaBehaviour.lua, position);
            this.CallLuaFunc("onScrollPositionChanged", position);
        }
        #endregion

        LuaBehav ILuaBehavEx.Major { get; set; }
        bool ILuaBehavEx.RouteToParents { get { return false; } }

        #region Unity Method

        public Dictionary<int, GameObject> GetItemViewDict()
        {
            return itemViewDict;
        }

        public GameObject GetSelectItem(int index)
        {
            if (itemViewDict.ContainsKey(index))
            {
                return itemViewDict[index];
            }
            return null;
        }

        //public delegate void ScrollRectExDelegate(LuaTable self, int param1);

        //public delegate void ScrollRectExDelegate1(LuaTable self, LuaTable param1, int param2);

        //public delegate void ScrollRectExDelegate2(LuaTable self, float param1);

        //public delegate GameObject ScrollRectExDelegate3(LuaTable self, int param1);

        //private ScrollRectExDelegate3 createItemLuaFunc;
        //private ScrollRectExDelegate3 createLineLuaFunc;
        //private ScrollRectExDelegate1 resetItemLuaFunc;
        //private ScrollRectExDelegate1 updateItemIndexLuaFunc;
        //private ScrollRectExDelegate destroyItemLuaFunc;
        //private ScrollRectExDelegate onItemIndexChanged;
        //private ScrollRectExDelegate2 onScrollPositionChanged;

        public void Awake()
        {
            //if (createItemLuaFunc == null)
            //{
            //    luaBehaviour.lua.Get("createItem", out createItemLuaFunc);
            //}
            //if (createLineLuaFunc == null)
            //{
            //    luaBehaviour.lua.Get("createLine", out createLineLuaFunc);
            //}
            //if (resetItemLuaFunc == null)
            //{
            //    luaBehaviour.lua.Get("resetItem", out resetItemLuaFunc);
            //}
            //if (updateItemIndexLuaFunc == null)
            //{
            //    luaBehaviour.lua.Get("updateItemIndex", out updateItemIndexLuaFunc);
            //}
            //if (destroyItemLuaFunc == null)
            //{
            //    luaBehaviour.lua.Get("destroyItem", out destroyItemLuaFunc);
            //}
            //if (onItemIndexChanged == null)
            //{
            //    luaBehaviour.lua.Get("onItemIndexChanged", out onItemIndexChanged);
            //}
            //if (onScrollPositionChanged == null)
            //{
            //    luaBehaviour.lua.Get("onScrollPositionChanged", out onScrollPositionChanged);
            //}

            scrollRect = GetComponent<ScrollRect>();
            if (!isShowLine)
            {
                lineSpace = 0;
            }
            CalcCellCount();
        }

        public void Start()
        {
            // 获取到最近祖先节点中的CScrollRect，留待多个CScrollRect交错滚动时使用
            if (transform.parent != null) { parentScrollRect = transform.parent.GetComponentInParent<ScrollRectExSameSize>(); }
        }
        public void Update()
        {
            if (isAutoScrolling)
            {
                if (leftTime <= 0) { isAutoScrolling = false; }
                else
                {
                    float updatePos;
                    if (leftTime <= Time.unscaledDeltaTime)
                    {
                        updatePos = destination;
                    }
                    else
                    {
                        updatePos = contentTransform.anchoredPosition[directionAxisIndex] + offset / totalTime * (2 * leftTime - Time.unscaledDeltaTime) / totalTime * Time.unscaledDeltaTime;
                    }
                    contentTransform.anchoredPosition = GetVector2WithDirection(updatePos, 0);
                    SetCurrentScrollPosition(updatePos);
                }
                leftTime -= Time.unscaledDeltaTime;
            }
        }
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (parentScrollRect != null && Mathf.Abs(eventData.delta[otherDirectionAxisIndex]) > Mathf.Abs(eventData.delta[directionAxisIndex]))
            {
                isDrag = true;
                parentScrollRect.OnBeginDrag(eventData);
                parentScrollRect.GetComponent<ScrollRect>().OnBeginDrag(eventData);
                GetComponent<ScrollRect>().enabled = false;
            }

            if (!isDrag && snapToGrid)
            {
                preContentPosition = eventData.position;
                prePageIndex = GetPageIndex();
                isAutoScrolling = false;
            }
        }
        public void OnDrag(PointerEventData eventData)
        {
            if (parentScrollRect != null && isDrag)
            {
                parentScrollRect.OnDrag(eventData);
                parentScrollRect.transform.GetComponent<ScrollRect>().OnDrag(eventData);
            }
        }
        public void OnEndDrag(PointerEventData eventData)
        {
            if (isDrag)
            {
                if (parentScrollRect != null)
                {
                    parentScrollRect.OnEndDrag(eventData);
                    parentScrollRect.transform.GetComponent<ScrollRect>().OnEndDrag(eventData);
                }
                isDrag = false;
                GetComponent<ScrollRect>().enabled = true;
            }
            else if (snapToGrid)
            {
                var sub = (eventData.position[directionAxisIndex] - preContentPosition[directionAxisIndex]) * directionSign[directionAxisIndex];
                nextIndex = GetPageIndex();
                if (nextIndex == prePageIndex && Mathf.Abs(sub) > intervalRoll)
                {
                    if (sub < 0) nextIndex++;
                    else nextIndex--;
                }
                MoveOffset(nextIndex);
            }
        }
        #endregion

        #region Private Method
        private void SetCellSize(float width, float height)
        {
            cellSize = new Vector2(width, height);
            CalcCellCount();
        }
        private void InitConfig()
        {
            scrollRect.vertical = !scrollRect.horizontal;

            // 根据CScrollRect的设置同步ScrollRect的设置
            scrollRect.inertia = inertia;

            if (inertia) { scrollRect.decelerationRate = decelerationRate; }
            if (snapToGrid) { scrollRect.inertia = false; }
            if (isLoop) { scrollRect.movementType = ScrollRect.MovementType.Unrestricted; }

            // 如果不使用对象池，则使得m_ViewCount的值足够让所有元素都能显示出来
            if (!usePool) { viewCount = totalLines; }
        }
        private void ResetData()
        {
            itemViewDict.Clear();
            totalCount = 0;
            itemList.Clear();
            lineList.Clear();
            currentPageIndex = 0;
            currentInternalIndex = 0;
            nextIndex = 0;
            prePageIndex = 0;
            itemIndex = int.MaxValue;
            preContentPosition = Vector2.zero;
            offset = 0;
            totalTime = 0;
            leftTime = 0;
            changePosition = 0;
            destination = 0;
            contentTransform.anchoredPosition = Vector2.zero;
        }
        private void InitObject()
        {
            if (!usePool)
            {
                for (int i = 0; i < totalCount; i++) { CreateItem(i); }
                if (isShowLine)
                {
                    for (int i = 0; i < totalLines; i++) { CreateLine(i); }
                }
            }
            else
            {
                for (int i = 0; i < Mathf.Min(totalCount, viewCount * maxPerLine); i++) { CreateItem(i); }
                if (isShowLine)
                {
                    for (int i = 0; i < Mathf.Min(totalLines, viewCount); i++) { CreateLine(i); }
                }
            }
        }
        private bool IsTurnPage()
        {
            if (totalCount <= 0)
            {
                PlatDependant.LogInfo("error element Count =" + totalCount);
                return false;
            }
            var maxRoll = totalLines;
            if (!isLoop)
            {
                if (currentPageIndex < 0)
                {
                    currentPageIndex = 0;
                    return true;
                }
                else if (currentPageIndex > (maxRoll - 1))
                {
                    currentPageIndex = maxRoll - 1;
                    return true;
                }
            }
            else
            {
                currentPageIndex = currentPageIndex % maxRoll;
                if (currentPageIndex < 0)
                {
                    currentPageIndex += maxRoll;
                }
            }
            return false;
        }
        private void MoveOffset(int posIndex)
        {
            currentPageIndex = posIndex;
            currentInternalIndex = posIndex;
            isAutoScrolling = true;
            if (IsTurnPage())
            {
                OnItemIndexChanged(currentPageIndex);
                totalTime = 0;
                leftTime = 0;
                isAutoScrolling = false;
                return;
            }
            destination = FixContentPos(-posIndex * cellSizeWithSpace[directionAxisIndex] * directionSign[directionAxisIndex]);
            offset = destination - contentTransform.anchoredPosition[directionAxisIndex];
            totalTime = Mathf.Pow(Mathf.Abs(offset), dragRat) / dragParameter;
            leftTime = totalTime;
            OnItemIndexChanged(currentPageIndex);
        }
        private void ScrollToInternalPage(int internalIndex)
        {
            if (totalCount <= 0)
            {
                PlatDependant.LogInfo("error element Count =" + totalCount);
                return;
            }
            internalIndex = Mathf.FloorToInt((float)internalIndex / maxPerLine);
            internalIndex = internalIndex - Mathf.RoundToInt((float)(internalIndex - currentInternalIndex) / totalCount) * totalCount;

            if (isLoop || Mathf.Abs(contentTransform.anchoredPosition[directionAxisIndex]) + Mathf.Abs(selfTransform.rect.size[directionAxisIndex]) < contentTransform.rect.size[directionAxisIndex])
            {
                MoveOffset(internalIndex);
            }
            else { currentPageIndex = internalIndex; }
        }

        // 不改动原有版本 去掉元素滚动时边界判断
        private void ScrollToInternalPageEx(int internalIndex)
        {
            if (totalCount <= 0)
            {
                PlatDependant.LogInfo("error element Count =" + totalCount);
                return;
            }
            internalIndex = Mathf.FloorToInt((float)internalIndex / maxPerLine);
            internalIndex = internalIndex - Mathf.RoundToInt((float)(internalIndex - currentInternalIndex) / totalCount) * totalCount;

            MoveOffset(internalIndex);
        }
        private void AddItemIntoPanel(int index)
        {
            var maxItemIndex = int.MinValue;
            ScorllItemData maxItem = null;
            for (int i = 0; i < itemList.Count; i++)
            {
                ScorllItemData item = itemList[i];
                if (item.index >= index)
                {
                    item.index += 1;
                    var rectTransf = item.gameObject.GetComponent<RectTransform>();
                    rectTransf.anchoredPosition = GetPosition(item.index);
                    UpdateItemIndexLuaFunc(item.gameObject, item.index);
                }
                if (item.index > maxItemIndex)
                {
                    maxItemIndex = item.index;
                    maxItem = item;
                }
            }

            if (usePool)
            {
                if (maxItem != null)
                {
                    var offsetIndex = GetPosIndex();
                    var lastIndex = (viewCount + offsetIndex) * maxPerLine;
                    if (maxItemIndex >= lastIndex)
                    {
                        MoveItemToPool(maxItem);
                        itemList.Remove(maxItem);
                    }
                }
            }
            CreateItem(index);
        }
        private void DelItemFromPanel(int index)
        {
            var maxIndex = -1;
            ScorllItemData deleteItem = null;
            for (int i = itemList.Count; i > 0; i--)
            {
                ScorllItemData item = itemList[i - 1];
                if (item.index == index)
                {
                    deleteItem = item;
                    var dataIndex = index;
                    dataIndex = dataIndex % (totalCount + 1); // 之前DelItem 中totalCount 已经减1，但是数据还没移除
                    if (dataIndex < 0)
                    {
                        dataIndex += (totalCount + 1);
                    }
                    DestroyItemLuaFunc(dataIndex);
                }
                if (item.index > maxIndex)
                {
                    maxIndex = item.index;
                }
                if (item.index > index)
                {
                    item.index -= 1;
                    UpdateItemIndexLuaFunc(item.gameObject, item.index);
                    var rectTransf = item.gameObject.GetComponent<RectTransform>();
                    rectTransf.anchoredPosition = GetPosition(item.index);
                }
            }
            if (deleteItem != null)
            {
                MoveItemToPool(deleteItem);
                itemList.Remove(deleteItem);
                if (maxIndex < totalCount)
                {
                    CreateItem(maxIndex);
                }
            }
            else
            {
                DestroyItemLuaFunc(index);
            }
        }
        private void CreateItem(int index)
        {
            ScorllItemData itemData;
            if (unUsedItemQueue.Count > 0)
            {
                itemData = unUsedItemQueue.Dequeue();
                ResetBaseItemData(index, itemData);
            }
            else
            {
                int adjustIndex = index;
                if (isLoop)
                {
                    adjustIndex = adjustIndex % totalCount;
                    if (adjustIndex < 0)
                    {
                        adjustIndex += totalCount;
                    }
                }
                var obj = CreateItemLuaFunc(adjustIndex);
                if (obj == null)
                {
                    PlatDependant.LogError("Expected to find obj of type gameObject,but found none" + obj);
                }
                itemData = new ScorllItemData();
                itemData.index = index;
                itemData.gameObject = obj;
                var rectTransf = itemData.gameObject.GetComponent<RectTransform>();
                rectTransf.SetParent(contentTransform, false);
                rectTransf.anchorMin = new Vector2(0, 1);
                rectTransf.anchorMax = new Vector2(0, 1);
                rectTransf.pivot = new Vector2(0, 1);
                rectTransf.sizeDelta = cellSize;
                rectTransf.anchoredPosition = GetPosition(index);
            }
            itemList.Add(itemData);
        }
        public void ResetItem(int index)
        {
            for (int i = 0; i < itemList.Count; i++)
            {
                ScorllItemData item = itemList[i];
                if (item.index == index)
                {
                    ResetBaseItemData(index, item);
                    break;
                }
            }
        }
        private void ResetBaseItemData(int index, ScorllItemData itemBase)
        {
            int adjustIndex = index;
            if (isLoop)
            {
                adjustIndex = adjustIndex % totalCount;
                if (adjustIndex < 0)
                {
                    adjustIndex += totalCount;
                }
            }
            itemBase.index = index;
            var rectTransf = itemBase.gameObject.GetComponent<RectTransform>();
            rectTransf.anchoredPosition = GetPosition(index);
            if (adjustIndex < totalCount)
            {
                ResetItemLuaFunc(itemBase.gameObject, index, adjustIndex);
            }
            else
            {
                PlatDependant.LogInfo("index is more than totalCount : " + adjustIndex);
            }
        }
        private void CreateLine(int index)
        {
            ScorllItemData lineData;
            if (unUsedLineQueue.Count > 0)
            {
                lineData = unUsedLineQueue.Dequeue();
                ResetBaseLineData(index, lineData);
            }
            else
            {
                var lineObject = CreateLineLuaFunc(index);
                if (lineObject == null)
                {
                    PlatDependant.LogError("Expected to find obj of type gameObject,but found none" + lineObject);
                }
                var rectTransf = lineObject.GetComponent<RectTransform>();
                rectTransf.SetParent(contentTransform, false);
                rectTransf.anchorMin = new Vector2(0, 1);
                rectTransf.anchorMax = new Vector2(0, 1);
                rectTransf.pivot = GetVector2WithDirection(0.5f, 1.0f - directionAxisIndex);
                rectTransf.anchoredPosition = GetVector2WithDirection(
                    directionSign[directionAxisIndex] * (cellSizeWithSpace[directionAxisIndex] * (index + 1) - (cellSpace[directionAxisIndex] + lineSpace) / 2),
                    0
                );
                rectTransf.sizeDelta = GetVector2WithDirection(
                    lineSpace,
                    GetComponent<RectTransform>().rect.size[otherDirectionAxisIndex]
                );
                lineData = new ScorllItemData();
                lineData.index = index;
                lineData.gameObject = lineObject;
            }
            lineList.Add(lineData);
        }
        private void ResetLine(int index)
        {
            ScorllItemData lineBase;
            if (unUsedLineQueue.Count > 0)
            {
                lineBase = unUsedLineQueue.Dequeue();
                ResetBaseLineData(index, lineBase);
            }
            else
            {
                PlatDependant.LogInfo("ResetLine error ,m_UnUsedLineQueue less then 1 !, index = " + index);
            }
        }
        private void ResetBaseLineData(int index, ScorllItemData lineBase)
        {
            var maxRoll = totalLines;
            int adjustIndex = index;
            if (isLoop)
            {
                adjustIndex = adjustIndex % maxRoll;
                if (adjustIndex < 0)
                {
                    if (adjustIndex != 0)
                    {
                        adjustIndex += maxRoll;
                    }
                }
            }
            lineBase.index = index;
            ResetLinePosition(lineBase, index);
        }
        private void ResetLinePosition(ScorllItemData line, int index)
        {
            var rectTransf = line.gameObject.GetComponent<RectTransform>();
            rectTransf.anchoredPosition = GetVector2WithDirection(
               directionSign[directionAxisIndex] * (cellSizeWithSpace[directionAxisIndex] * (index + 1) - (cellSpace[directionAxisIndex] + lineSpace) / 2),
               0
            );
        }
        private void DeleteLine()
        {
            var _index = totalLines;
            for (int i = lineList.Count - 1; i >= 0; i--)
            {
                var line = lineList[i];
                if (line.index >= _index)
                {
                    MoveLineToPool(line);
                    lineList.Remove(line);
                }
            }
        }

        private void AddLine()
        {
            var maxLineIndex = int.MinValue;
            var maxRoll = totalLines;
            var lineCount = lineList.Count;
            for (int i = lineCount - 1; i >= 0; i--)
            {
                var line = lineList[i];
                if (line.index > maxLineIndex)
                {
                    maxLineIndex = line.index;
                }
            }
            if (!usePool)
            {
                if (maxLineIndex < maxRoll - 1)
                {
                    CreateLine(maxRoll - 1);
                }
            }
            else
            {
                if (maxLineIndex < viewCount - 1 && maxLineIndex < maxRoll - 1)
                {
                    CreateLine(maxRoll - 1);
                }
            }
        }
        private int GetPageIndex() // TODO:考虑四舍五入造成的连续滚动超过一页的情况
        {
            return Mathf.RoundToInt(directionSign[directionAxisIndex] * contentTransform.anchoredPosition[directionAxisIndex] / -cellSizeWithSpace[directionAxisIndex]);
        }
        private int GetPosIndex()   // 计算出超出可视区左侧或上侧元素的列数或排数
        {
            return Mathf.FloorToInt(directionSign[directionAxisIndex] * contentTransform.anchoredPosition[directionAxisIndex] / -cellSizeWithSpace[directionAxisIndex]);
        }
        private Vector3 GetPosition(int i)  // 这个方法好像是在不考虑对象池，所有数据都显示出来，计算出第i个元素相对于左上角的坐标
        {
            var pos = i % maxPerLine;
            pos = pos < 0 ? pos + maxPerLine : pos;

            return GetVector2WithDirection(
                cellSizeWithSpace[directionAxisIndex] * Mathf.FloorToInt((float)i / maxPerLine) * directionSign[directionAxisIndex],
                -(cellSize[otherDirectionAxisIndex] + cellSpace[otherDirectionAxisIndex]) * pos * directionSign[directionAxisIndex]
            );
        }

        private void MoveItemToPool(ScorllItemData item)
        {
            var rectTransf = item.gameObject.GetComponent<RectTransform>();
            rectTransf.anchoredPosition += disableDistance;
            itemViewDict.Remove(item.index + 1);
            unUsedItemQueue.Enqueue(item);
        }

        private void MoveLineToPool(ScorllItemData line)
        {
            var rectTransf = line.gameObject.GetComponent<RectTransform>();
            rectTransf.anchoredPosition += disableDistance;
            unUsedLineQueue.Enqueue(line);
        }

        private void UpdateTotalWidthAndHeight()
        {
            contentTransform.sizeDelta = GetVector2WithDirection(
                cellSizeWithSpace[directionAxisIndex] * totalLines - cellSpace[directionAxisIndex],
                contentTransform.rect.size[otherDirectionAxisIndex]
            );
        }
        private void SetCurrentScrollPosition(float change) // 更新当前滚动到的位置
        {
            if (b_CheckPosition)
            {
                // 第二次取模是为了防止返回负数
                changePosition = (totalCount + (change / cellSizeWithSpace[directionAxisIndex]) % totalCount) % totalCount;
                OnScrollPositionChanged(changePosition);
            }
        }
        #endregion

#if UNITY_EDITOR
        void Reset()
        {
            scrollRect = GetComponent<ScrollRect>();

            UnityEditor.Events.UnityEventTools.RemovePersistentListener<Vector2>(scrollRect.onValueChanged, this.OnValueChange);
            UnityEditor.Events.UnityEventTools.AddPersistentListener(scrollRect.onValueChanged, this.OnValueChange);

            InitConfig();
            if (string.IsNullOrEmpty(GetComponent<LuaBehav>().InitLuaPath))
            {
                GetComponent<LuaBehav>().InitLuaPath = "ui.control.scroll.LuaScrollRectExSameSize";
            }
            isShowLine = false;
            cellSize = new Vector2(200, 100);
        }
#endif

        protected void OnDestroy()
        {
            //createItemLuaFunc = null;
            //createLineLuaFunc = null;
            //resetItemLuaFunc = null;
            //updateItemIndexLuaFunc = null;
            //destroyItemLuaFunc = null;
            //onItemIndexChanged = null;
            //onScrollPositionChanged = null;
        }
    }
}