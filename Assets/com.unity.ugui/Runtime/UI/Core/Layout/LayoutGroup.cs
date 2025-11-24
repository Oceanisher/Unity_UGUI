using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.Pool;

namespace UnityEngine.UI
{
    [DisallowMultipleComponent]
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    /// <summary>
    /// Abstract base class to use for layout groups.
    /// 布局控制器-控制子节点，抽象类
    /// 水平布局控制器、垂直布局控制器、网格布局控制器都是继承自该接口
    ///
    /// 布局控制器控制子元素尺寸的基础逻辑：（以水平布局控制器为例）
    /// 1.如果 控制器宽度<=子元素总的Min宽度，那么每个子元素都保持Min大小
    /// 2.如果 子元素总的Min宽度<控制器宽度<=子元素总的最佳宽度，那么每个子元素都保持按照最佳宽度的比例扩展
    /// 3.如果 子元素总的最佳宽度<控制器宽度，那么没有配置灵活宽度的子元素会保持最佳宽度、配置了灵活宽度的子元素会按照比例分配额外的宽度
    /// </summary>
    public abstract class LayoutGroup : UIBehaviour, ILayoutElement, ILayoutGroup
    {
        //本布局内部Padding，不是每个子元素的Paddings
        [SerializeField] protected RectOffset m_Padding = new RectOffset();

        /// <summary>
        /// The padding to add around the child layout elements.
        /// 本布局内部Padding，不是每个子元素的Paddings
        /// </summary>
        public RectOffset padding { get { return m_Padding; } set { SetProperty(ref m_Padding, value); } }

        //子节点的对齐方式
        [SerializeField] protected TextAnchor m_ChildAlignment = TextAnchor.UpperLeft;

        /// <summary>
        /// The alignment to use for the child layout elements in the layout group.
        /// 子元素的对齐方式
        /// </summary>
        /// <remarks>
        /// If a layout element does not specify a flexible width or height, its child elements many not use the available space within the layout group. In this case, use the alignment settings to specify how to align child elements within their layout group.
        /// </remarks>
        public TextAnchor childAlignment { get { return m_ChildAlignment; } set { SetProperty(ref m_ChildAlignment, value); } }

        //缓存元素的RectTransform
        [System.NonSerialized] private RectTransform m_Rect;
        protected RectTransform rectTransform
        {
            get
            {
                if (m_Rect == null)
                    m_Rect = GetComponent<RectTransform>();
                return m_Rect;
            }
        }

        protected DrivenRectTransformTracker m_Tracker;
        //子元素总的Min尺寸
        private Vector2 m_TotalMinSize = Vector2.zero;
        //子元素总的最佳尺寸
        private Vector2 m_TotalPreferredSize = Vector2.zero;
        //子元素总的灵活尺寸
        private Vector2 m_TotalFlexibleSize = Vector2.zero;

        //缓存的直接子节点，每次重新计算都会清空并重新赋值
        [System.NonSerialized] private List<RectTransform> m_RectChildren = new List<RectTransform>();
        protected List<RectTransform> rectChildren { get { return m_RectChildren; } }

        /// <summary>
        /// 计算水平布局
        /// 这里由于是基类，所以其实只是把需要进行布局计算的子元素收集到列表 m_RectChildren 中，并没有直接计算子节点的宽度
        /// </summary>
        public virtual void CalculateLayoutInputHorizontal()
        {
            m_RectChildren.Clear();
            var toIgnoreList = ListPool<Component>.Get();
            //只计算直接子节点
            for (int i = 0; i < rectTransform.childCount; i++)
            {
                var rect = rectTransform.GetChild(i) as RectTransform;
                //跳过Inactive的节点
                if (rect == null || !rect.gameObject.activeInHierarchy)
                    continue;
                
                //跳过设置ILayoutIgnorer为true的节点
                rect.GetComponents(typeof(ILayoutIgnorer), toIgnoreList);

                if (toIgnoreList.Count == 0)
                {
                    m_RectChildren.Add(rect);
                    continue;
                }

                for (int j = 0; j < toIgnoreList.Count; j++)
                {
                    var ignorer = (ILayoutIgnorer)toIgnoreList[j];
                    if (!ignorer.ignoreLayout)
                    {
                        m_RectChildren.Add(rect);
                        break;
                    }
                }
            }
            ListPool<Component>.Release(toIgnoreList);
            m_Tracker.Clear();
        }

        /// <summary>
        /// 计算垂直布局，直接交给子类去实现
        /// </summary>
        public abstract void CalculateLayoutInputVertical();

        /// <summary>
        /// See LayoutElement.minWidth
        /// </summary>
        public virtual float minWidth { get { return GetTotalMinSize(0); } }

        /// <summary>
        /// See LayoutElement.preferredWidth
        /// </summary>
        public virtual float preferredWidth { get { return GetTotalPreferredSize(0); } }

        /// <summary>
        /// See LayoutElement.flexibleWidth
        /// </summary>
        public virtual float flexibleWidth { get { return GetTotalFlexibleSize(0); } }

        /// <summary>
        /// See LayoutElement.minHeight
        /// </summary>
        public virtual float minHeight { get { return GetTotalMinSize(1); } }

        /// <summary>
        /// See LayoutElement.preferredHeight
        /// </summary>
        public virtual float preferredHeight { get { return GetTotalPreferredSize(1); } }

        /// <summary>
        /// See LayoutElement.flexibleHeight
        /// </summary>
        public virtual float flexibleHeight { get { return GetTotalFlexibleSize(1); } }

        /// <summary>
        /// See LayoutElement.layoutPriority
        /// </summary>
        public virtual int layoutPriority { get { return 0; } }

        // ILayoutController Interface

        /// <summary>
        /// 水平布局设置外部接口，被LayoutRebuilder调用
        /// </summary>
        public abstract void SetLayoutHorizontal();
        /// <summary>
        /// 垂直布局设置外部接口，被LayoutRebuilder调用
        /// </summary>
        public abstract void SetLayoutVertical();

        // Implementation

        protected LayoutGroup()
        {
            if (m_Padding == null)
                m_Padding = new RectOffset();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            SetDirty();
        }

        protected override void OnDisable()
        {
            m_Tracker.Clear();
            LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
            base.OnDisable();
        }

        /// <summary>
        /// Callback for when properties have been changed by animation.
        /// </summary>
        protected override void OnDidApplyAnimationProperties()
        {
            SetDirty();
        }

        /// <summary>
        /// The min size for the layout group on the given axis.
        /// 获取子元素总的Min尺寸——宽度或者高度
        /// </summary>
        /// <param name="axis">The axis index. 0 is horizontal and 1 is vertical.0代表水平，1代表竖直</param>
        /// <returns>The min size</returns>
        protected float GetTotalMinSize(int axis)
        {
            return m_TotalMinSize[axis];
        }

        /// <summary>
        /// The preferred size for the layout group on the given axis.
        /// 获取子元素总的最佳尺寸——宽度或者高度
        /// </summary>
        /// <param name="axis">The axis index. 0 is horizontal and 1 is vertical.0代表水平，1代表竖直</param>
        /// <returns>The preferred size.</returns>
        protected float GetTotalPreferredSize(int axis)
        {
            return m_TotalPreferredSize[axis];
        }

        /// <summary>
        /// The flexible size for the layout group on the given axis.
        /// 获取子元素总的灵活尺寸——宽度或者高度
        /// </summary>
        /// <param name="axis">The axis index. 0 is horizontal and 1 is vertical.0代表水平，1代表竖直</param>
        /// <returns>The flexible size</returns>
        protected float GetTotalFlexibleSize(int axis)
        {
            return m_TotalFlexibleSize[axis];
        }

        /// <summary>
        /// Returns the calculated position of the first child layout element along the given axis.
        /// 获取第一个子元素的起始位置，也就是一个Offset
        /// </summary>
        /// <param name="axis">The axis index. 0 is horizontal and 1 is vertical. 0代表水平，1代表竖直</param>
        /// <param name="requiredSpaceWithoutPadding">The total space required on the given axis for all the layout elements including spacing and excluding padding.额外需求的空间</param>
        /// <returns>The position of the first child along the given axis.</returns>
        protected float GetStartOffset(int axis, float requiredSpaceWithoutPadding)
        {
            //需求空间=传入的空间+padding值（如果是水平，那么padding是left+right，如果是竖直，那么padding是top+bottom）
            float requiredSpace = requiredSpaceWithoutPadding + (axis == 0 ? padding.horizontal : padding.vertical);
            //可用空间，本RectTransform的当前尺寸
            float availableSpace = rectTransform.rect.size[axis];
            //盈余空间，可用空间-需求空间
            float surplusSpace = availableSpace - requiredSpace;
            //对齐方式对应的值
            float alignmentOnAxis = GetAlignmentOnAxis(axis);
            //offset空间=Padding空间+（盈余空间*对齐方式对应的值）
            //对于（盈余空间*对齐方式对应的值）的解释（水平方向）：
            //比如如果是左对齐，那么盈余空间都应该放在右边，所以左边就没有盈余空间了；
            //如果是居中，那么盈余空间应该左右各一半，所以左边是盈余空间*0.5
            return (axis == 0 ? padding.left : padding.top) + surplusSpace * alignmentOnAxis;
        }

        /// <summary>
        /// Returns the alignment on the specified axis as a fraction where 0 is left/top, 0.5 is middle, and 1 is right/bottom.
        /// 对齐方式对应的值，也就是个百分比的值
        ///
        /// 枚举根据轴向，按照从左到右、从上到下的顺序；
        /// 也就是说，如果是X轴向，那么左->中->右的值分别是0、0.5、1；如果是Y轴向，那么上->中->下的值分别是0、0.5、1
        /// </summary>
        /// <param name="axis">The axis to get alignment along. 0 is horizontal and 1 is vertical.</param>
        /// <returns>The alignment as a fraction where 0 is left/top, 0.5 is middle, and 1 is right/bottom.</returns>
        protected float GetAlignmentOnAxis(int axis)
        {
            if (axis == 0)
                return ((int)childAlignment % 3) * 0.5f;
            else
                return ((int)childAlignment / 3) * 0.5f;
        }

        /// <summary>
        /// Used to set the calculated layout properties for the given axis.
        /// 设置3个总值
        /// </summary>
        /// <param name="totalMin">The min size for the layout group.</param>
        /// <param name="totalPreferred">The preferred size for the layout group.</param>
        /// <param name="totalFlexible">The flexible size for the layout group.</param>
        /// <param name="axis">The axis to set sizes for. 0 is horizontal and 1 is vertical.</param>
        protected void SetLayoutInputForAxis(float totalMin, float totalPreferred, float totalFlexible, int axis)
        {
            m_TotalMinSize[axis] = totalMin;
            m_TotalPreferredSize[axis] = totalPreferred;
            m_TotalFlexibleSize[axis] = totalFlexible;
        }

        /// <summary>
        /// Set the position and size of a child layout element along the given axis.
        /// </summary>
        /// <param name="rect">The RectTransform of the child layout element.</param>
        /// <param name="axis">The axis to set the position and size along. 0 is horizontal and 1 is vertical.</param>
        /// <param name="pos">The position from the left side or top.</param>
        protected void SetChildAlongAxis(RectTransform rect, int axis, float pos)
        {
            if (rect == null)
                return;

            SetChildAlongAxisWithScale(rect, axis, pos, 1.0f);
        }

        /// <summary>
        /// Set the position and size of a child layout element along the given axis.
        /// 设置子元素在某个轴向上的anchoredPosition位置
        /// </summary>
        /// <param name="rect">The RectTransform of the child layout element.</param>
        /// <param name="axis">The axis to set the position and size along. 0 is horizontal and 1 is vertical.</param>
        /// <param name="pos">The position from the left side or top.</param>
        protected void SetChildAlongAxisWithScale(RectTransform rect, int axis, float pos, float scaleFactor)
        {
            if (rect == null)
                return;

            m_Tracker.Add(this, rect,
                DrivenTransformProperties.Anchors |
                (axis == 0 ? DrivenTransformProperties.AnchoredPositionX : DrivenTransformProperties.AnchoredPositionY));

            // Inlined rect.SetInsetAndSizeFromParentEdge(...) and refactored code in order to multiply desired size by scaleFactor.
            // sizeDelta must stay the same but the size used in the calculation of the position must be scaled by the scaleFactor.

            //把锚点定在父节点左上角，也就是说，无论Layout中设置的子元素对齐方式是什么，子元素锚点的方式始终是左上角
            //因为不同锚点的方式会影响anchoredPosition，所以为了统一计算，都以左上角为原点进行计算
            rect.anchorMin = Vector2.up;
            rect.anchorMax = Vector2.up;

            //设置子元素在某个轴上的位置
            //由于锚点是左上角，所以sizeDelta其实就是子元素本身的大小
            Vector2 anchoredPosition = rect.anchoredPosition;
            //计算方式（水平方向）：传入的位置 + 元素自身宽度 * 元素质心百分比 * 缩放
            //之所以这样计算是因为要把子元素放在父节点内部，而由于子元素对齐方式是左上角、质心是元素中心。所以为了能把子节点放进来，需要在传入的位置上加上元素自身质心的大小
            anchoredPosition[axis] = (axis == 0) ? (pos + rect.sizeDelta[axis] * rect.pivot[axis] * scaleFactor) : (-pos - rect.sizeDelta[axis] * (1f - rect.pivot[axis]) * scaleFactor);
            rect.anchoredPosition = anchoredPosition;
        }

        /// <summary>
        /// Set the position and size of a child layout element along the given axis.
        /// 设置子元素在某个轴向上的位置和尺寸
        /// </summary>
        /// <param name="rect">The RectTransform of the child layout element.</param>
        /// <param name="axis">The axis to set the position and size along. 0 is horizontal and 1 is vertical.</param>
        /// <param name="pos">The position from the left side or top.</param>
        /// <param name="size">The size.</param>
        protected void SetChildAlongAxis(RectTransform rect, int axis, float pos, float size)
        {
            if (rect == null)
                return;

            SetChildAlongAxisWithScale(rect, axis, pos, size, 1.0f);
        }

        /// <summary>
        /// Set the position and size of a child layout element along the given axis.
        /// 设置子元素在某个轴向上的位置和尺寸
        /// </summary>
        /// <param name="rect">The RectTransform of the child layout element.</param>
        /// <param name="axis">The axis to set the position and size along. 0 is horizontal and 1 is vertical.</param>
        /// <param name="pos">The position from the left side or top.</param>
        /// <param name="size">The size.</param>
        protected void SetChildAlongAxisWithScale(RectTransform rect, int axis, float pos, float size, float scaleFactor)
        {
            if (rect == null)
                return;

            m_Tracker.Add(this, rect,
                DrivenTransformProperties.Anchors |
                (axis == 0 ?
                    (DrivenTransformProperties.AnchoredPositionX | DrivenTransformProperties.SizeDeltaX) :
                    (DrivenTransformProperties.AnchoredPositionY | DrivenTransformProperties.SizeDeltaY)
                )
            );

            // Inlined rect.SetInsetAndSizeFromParentEdge(...) and refactored code in order to multiply desired size by scaleFactor.
            // sizeDelta must stay the same but the size used in the calculation of the position must be scaled by the scaleFactor.

            //把锚点定在父节点左上角，也就是说，无论Layout中设置的子元素对齐方式是什么，子元素锚点的方式始终是左上角
            //因为不同锚点的方式会影响anchoredPosition，所以为了统一计算，都以左上角为原点进行计算
            rect.anchorMin = Vector2.up;
            rect.anchorMax = Vector2.up;

            //由于锚点是左上角，所以sizeDelta其实就是子元素本身的大小
            Vector2 sizeDelta = rect.sizeDelta;
            sizeDelta[axis] = size;
            rect.sizeDelta = sizeDelta;

            //计算方式（水平方向）：传入的位置 + 元素自身宽度 * 元素质心百分比 * 缩放
            //之所以这样计算是因为要把子元素放在父节点内部，而由于子元素对齐方式是左上角、质心是元素中心。所以为了能把子节点放进来，需要在传入的位置上加上元素自身质心的大小
            Vector2 anchoredPosition = rect.anchoredPosition;
            anchoredPosition[axis] = (axis == 0) ? (pos + size * rect.pivot[axis] * scaleFactor) : (-pos - size * (1f - rect.pivot[axis]) * scaleFactor);
            rect.anchoredPosition = anchoredPosition;
        }

        //它的父节点有没有布局管理器
        //如果父节点不是一个布局管理器，那么该元素自己就是Root的布局管理器
        private bool isRootLayoutGroup
        {
            get
            {
                Transform parent = transform.parent;
                if (parent == null)
                    return true;
                return transform.parent.GetComponent(typeof(ILayoutGroup)) == null;
            }
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            if (isRootLayoutGroup)
                SetDirty();
        }

        protected virtual void OnTransformChildrenChanged()
        {
            SetDirty();
        }

        /// <summary>
        /// Helper method used to set a given property if it has changed.
        /// 设置变量
        /// 一个是在值变化的时候才去修改，另一个就是能自动调用SetDirty()
        /// </summary>
        /// <param name="currentValue">A reference to the member value.</param>
        /// <param name="newValue">The new value.</param>
        protected void SetProperty<T>(ref T currentValue, T newValue)
        {
            if ((currentValue == null && newValue == null) || (currentValue != null && currentValue.Equals(newValue)))
                return;
            currentValue = newValue;
            SetDirty();
        }

        /// <summary>
        /// Mark the LayoutGroup as dirty.
        /// 设置该Layout为Dirty
        /// </summary>
        protected void SetDirty()
        {
            if (!IsActive())
                return;

            //如果当前正在进行布局重建，那么延迟一帧去标记自身重建
            if (!CanvasUpdateRegistry.IsRebuildingLayout())
                LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
            else
                StartCoroutine(DelayedSetDirty(rectTransform));
        }

        /// <summary>
        /// 延迟一帧去标记自身需要布局重建
        /// </summary>
        /// <param name="rectTransform"></param>
        /// <returns></returns>
        IEnumerator DelayedSetDirty(RectTransform rectTransform)
        {
            yield return null;
            LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
        }

    #if UNITY_EDITOR
        protected override void OnValidate()
        {
            SetDirty();
        }

    #endif
    }
}
