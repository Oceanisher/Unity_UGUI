using System.Collections.Generic;

namespace UnityEngine.UI
{
    /// <summary>
    /// Abstract base class for HorizontalLayoutGroup and VerticalLayoutGroup to generalize common functionality.
    /// </summary>
    ///
    [ExecuteAlways]
    public abstract class HorizontalOrVerticalLayoutGroup : LayoutGroup
    {
        //子元素之间的空间尺寸
        [SerializeField] protected float m_Spacing = 0;

        /// <summary>
        /// The spacing to use between layout elements in the layout group.
        /// 子元素之间的空间尺寸
        /// </summary>
        public float spacing { get { return m_Spacing; } set { SetProperty(ref m_Spacing, value); } }

        //是否强制扩展子节点宽度
        //当控制器有剩余宽度时，是否强制扩展子元素的宽度，让其填充剩余空间
        [SerializeField] protected bool m_ChildForceExpandWidth = true;

        /// <summary>
        /// Whether to force the children to expand to fill additional available horizontal space.
        /// 是否强制扩展子节点宽度
        /// 当控制器有剩余宽度时，是否强制扩展子元素的宽度，让其填充剩余空间
        /// </summary>
        public bool childForceExpandWidth { get { return m_ChildForceExpandWidth; } set { SetProperty(ref m_ChildForceExpandWidth, value); } }

        //见 m_ChildForceExpandWidth
        [SerializeField] protected bool m_ChildForceExpandHeight = true;

        /// <summary>
        /// Whether to force the children to expand to fill additional available vertical space.
        /// 见 m_ChildForceExpandWidth
        /// </summary>
        public bool childForceExpandHeight { get { return m_ChildForceExpandHeight; } set { SetProperty(ref m_ChildForceExpandHeight, value); } }

        //是否控制子节点的宽度、
        //如果是false，那么将会使用子节点原有的宽度、只修改子节点的位置；并且能够手动修改子节点的宽度
        //如果是true，那么控制器将会根据子节点的宽度（最小宽度、理想宽度、灵活宽度）、以及控制器本身的宽度大小，来决定子节点的大小；此时不能手动修子节点的宽度，但是可以通过在子节点上添加LayoutElement组件，来控制子节点的大小
        [SerializeField] protected bool m_ChildControlWidth = true;

        /// <summary>
        /// Returns true if the Layout Group controls the widths of its children. Returns false if children control their own widths.
        /// 是否控制子节点的宽度
        /// 如果是false，那么将会使用子节点原有的宽度，并且能够手动修改子节点的宽度
        /// 如果是true，那么控制器将会根据子节点的宽度（最小宽度、理想宽度、灵活宽度）、以及控制器本身的宽度大小，来决定子节点的大小；此时不能手动修子节点的宽度，但是可以通过在子节点上添加LayoutElement组件，来控制子节点的大小
        /// </summary>
        /// <remarks>
        /// If set to false, the layout group will only affect the positions of the children while leaving the widths untouched. The widths of the children can be set via the respective RectTransforms in this case.
        ///
        /// If set to true, the widths of the children are automatically driven by the layout group according to their respective minimum, preferred, and flexible widths. This is useful if the widths of the children should change depending on how much space is available.In this case the width of each child cannot be set manually in the RectTransform, but the minimum, preferred and flexible width for each child can be controlled by adding a LayoutElement component to it.
        /// </remarks>
        public bool childControlWidth { get { return m_ChildControlWidth; } set { SetProperty(ref m_ChildControlWidth, value); } }

        //见 m_ChildControlWidth
        [SerializeField] protected bool m_ChildControlHeight = true;

        /// <summary>
        /// Returns true if the Layout Group controls the heights of its children. Returns false if children control their own heights.
        /// 见 m_ChildControlWidth
        /// </summary>
        /// <remarks>
        /// If set to false, the layout group will only affect the positions of the children while leaving the heights untouched. The heights of the children can be set via the respective RectTransforms in this case.
        ///
        /// If set to true, the heights of the children are automatically driven by the layout group according to their respective minimum, preferred, and flexible heights. This is useful if the heights of the children should change depending on how much space is available.In this case the height of each child cannot be set manually in the RectTransform, but the minimum, preferred and flexible height for each child can be controlled by adding a LayoutElement component to it.
        /// </remarks>
        public bool childControlHeight { get { return m_ChildControlHeight; } set { SetProperty(ref m_ChildControlHeight, value); } }

        //是否使用子节点的X轴缩放值
        [SerializeField] protected bool m_ChildScaleWidth = false;

        /// <summary>
        /// Whether to use the x scale of each child when calculating its width.
        /// 是否使用子节点的X轴缩放值
        /// </summary>
        public bool childScaleWidth { get { return m_ChildScaleWidth; } set { SetProperty(ref m_ChildScaleWidth, value); } }

        //见 m_ChildScaleWidth
        [SerializeField] protected bool m_ChildScaleHeight = false;

        /// <summary>
        /// Whether to use the y scale of each child when calculating its height.
        /// 见 m_ChildScaleWidth
        /// </summary>
        public bool childScaleHeight { get { return m_ChildScaleHeight; } set { SetProperty(ref m_ChildScaleHeight, value); } }

        /// <summary>
        /// Whether the order of children objects should be sorted in reverse.
        /// 是否倒序排列子元素
        /// </summary>
        /// <remarks>
        /// If False the first child object will be positioned first.
        /// If True the last child object will be positioned first.
        /// </remarks>
        public bool reverseArrangement { get { return m_ReverseArrangement; } set { SetProperty(ref m_ReverseArrangement, value); } }

        //是否倒序排列子元素
        [SerializeField] protected bool m_ReverseArrangement = false;

        /// <summary>
        /// Calculate the layout element properties for this layout element along the given axis.
        /// 计算组件本身某个轴向上的布局数据
        /// 只进行计算和结果存储，不实际执行
        /// </summary>
        /// <param name="axis">The axis to calculate for. 0 is horizontal and 1 is vertical. 0代表水平轴向，1代表垂直轴向</param>
        /// <param name="isVertical">Is this group a vertical group?</param>
        protected void CalcAlongAxis(int axis, bool isVertical)
        {
            //轴向上的Padding
            float combinedPadding = (axis == 0 ? padding.horizontal : padding.vertical);
            //是否控制轴向上子节点尺寸
            bool controlSize = (axis == 0 ? m_ChildControlWidth : m_ChildControlHeight);
            //是否使用轴向上的子节点缩放
            bool useScale = (axis == 0 ? m_ChildScaleWidth : m_ChildScaleHeight);
            //是否强制子节点填充轴向上的剩余空间
            bool childForceExpandSize = (axis == 0 ? m_ChildForceExpandWidth : m_ChildForceExpandHeight);

            //所有子节点的轴向上最小尺寸
            float totalMin = combinedPadding;
            //所有子节点的轴向上理想尺寸
            float totalPreferred = combinedPadding;
            //所有子节点的轴向上灵活尺寸
            float totalFlexible = 0;

            //是否是在某个轴向控制器下，处理其他轴向。比如说水平布局控制器、但是处理Y轴布局
            //因为无论是什么类型的控制器，都得处理所有轴向的布局
            bool alongOtherAxis = (isVertical ^ (axis == 1));
            var rectChildrenCount = rectChildren.Count;
            //遍历每个子元素，处理尺寸问题
            for (int i = 0; i < rectChildrenCount; i++)
            {
                RectTransform child = rectChildren[i];
                float min, preferred, flexible;
                //获取子元素的3个尺寸
                GetChildSizes(child, axis, controlSize, childForceExpandSize, out min, out preferred, out flexible);

                //如果使用子节点的缩放，那么子元素的3个尺寸都要缩放一下
                if (useScale)
                {
                    float scaleFactor = child.localScale[axis];
                    min *= scaleFactor;
                    preferred *= scaleFactor;
                    flexible *= scaleFactor;
                }

                //如果是处理其他轴向，那么其实就是最大的子元素尺寸+padding值
                //比如对于水平布局组件，它只有在水平轴向是跟随子元素一直扩展的，而在垂直轴向上，它的值是由最大的子元素尺寸决定的
                //灵活尺寸不加Padding
                if (alongOtherAxis)
                {
                    totalMin = Mathf.Max(min + combinedPadding, totalMin);
                    totalPreferred = Mathf.Max(preferred + combinedPadding, totalPreferred);
                    totalFlexible = Mathf.Max(flexible, totalFlexible);
                }
                //如果是处理本轴向，那么就是一直跟着子元素数量进行扩展，尺寸+元素间的间距
                //灵活尺寸不加Padding
                else
                {
                    totalMin += min + spacing;
                    totalPreferred += preferred + spacing;

                    // Increment flexible size with element's flexible size.
                    totalFlexible += flexible;
                }
            }

            //遍历完所有元素之后，处理最终结果
            //如果是处理本轴向、并且有子元素，那么总尺寸要减去一个间距，因为上面计算的时候，每个子元素都加了一个间距，而实际上是每两个元素之间才会有一个
            if (!alongOtherAxis && rectChildren.Count > 0)
            {
                totalMin -= spacing;
                totalPreferred -= spacing;
            }
            //最佳尺寸要取最小尺寸与最佳尺寸之间，较大的那个；防止配置的时候最小值比最佳值大
            totalPreferred = Mathf.Max(totalMin, totalPreferred);
            //写入最终尺寸
            SetLayoutInputForAxis(totalMin, totalPreferred, totalFlexible, axis);
        }

        /// <summary>
        /// Set the positions and sizes of the child layout elements for the given axis.
        /// 根据CalcAlongAxis()的计算结果，对布局进行处理，主要处理子元素的位置与大小、以及自身的位置与大小
        /// </summary>
        /// <param name="axis">The axis to handle. 0 is horizontal and 1 is vertical.0是X轴、1是Y轴</param>
        /// <param name="isVertical">Is this group a vertical group?</param>
        protected void SetChildrenAlongAxis(int axis, bool isVertical)
        {
            //自身轴向上的尺寸
            float size = rectTransform.rect.size[axis];
            //是否控制轴向上的子元素尺寸
            bool controlSize = (axis == 0 ? m_ChildControlWidth : m_ChildControlHeight);
            //是否使用轴向上的子元素缩放
            bool useScale = (axis == 0 ? m_ChildScaleWidth : m_ChildScaleHeight);
            //是否强制子节点填充轴向上的剩余空间
            bool childForceExpandSize = (axis == 0 ? m_ChildForceExpandWidth : m_ChildForceExpandHeight);
            //获取子元素对齐方式在轴向上的百分比（0/0.5/1）
            float alignmentOnAxis = GetAlignmentOnAxis(axis);

            bool alongOtherAxis = (isVertical ^ (axis == 1));
            //根据是否是倒排，计算起始序号、结算序号、步长
            int startIndex = m_ReverseArrangement ? rectChildren.Count - 1 : 0;
            int endIndex = m_ReverseArrangement ? 0 : rectChildren.Count;
            int increment = m_ReverseArrangement ? -1 : 1;
            
            //设置其他轴向，比如在VerticalLayoutGroup中去设置元素在X轴的位置
            //其他轴向无需排列子元素，因为在该轴向上所有子元素的位置是一样的
            if (alongOtherAxis)
            {
                //布局总尺寸
                float innerSize = size - (axis == 0 ? padding.horizontal : padding.vertical);

                for (int i = startIndex; m_ReverseArrangement ? i >= endIndex : i < endIndex; i += increment)
                {
                    RectTransform child = rectChildren[i];
                    float min, preferred, flexible;
                    GetChildSizes(child, axis, controlSize, childForceExpandSize, out min, out preferred, out flexible);
                    float scaleFactor = useScale ? child.localScale[axis] : 1f;

                    //需求空间（不考虑padding的情况下），最小是总尺寸大小，最大要看有没有灵活尺寸，有的话那么需求空间跟总尺寸一样大小；没有灵活尺寸，那么最大就是总理想尺寸
                    float requiredSpace = Mathf.Clamp(innerSize, min, flexible > 0 ? size : preferred);
                    //根据需求空间，获得位置偏移
                    float startOffset = GetStartOffset(axis, requiredSpace * scaleFactor);
                    //如果控制子元素尺寸，那么子元素的偏移是计算出来的位置
                    if (controlSize)
                    {
                        SetChildAlongAxisWithScale(child, axis, startOffset, requiredSpace, scaleFactor);
                    }
                    //如果不控制子元素的尺寸，那么计算方式与下面设置本轴向的方法一致
                    else
                    {
                        float offsetInCell = (requiredSpace - child.sizeDelta[axis]) * alignmentOnAxis;
                        SetChildAlongAxisWithScale(child, axis, startOffset + offsetInCell, scaleFactor);
                    }
                }
            }
            //设置本轴向
            else
            {
                //子元素的位置，在子元素进行遍历设置时，会动态修改位置
                float pos = (axis == 0 ? padding.left : padding.top);
                //灵活尺寸的扩展系数
                float itemFlexibleMultiplier = 0;
                //盈余空间，也就是总尺寸-理想尺寸总和
                float surplusSpace = size - GetTotalPreferredSize(axis);

                //当有盈余空间时，要对灵活尺寸的子元素进行处理
                //主要是计算出起始位置、灵活尺寸的扩展系数
                if (surplusSpace > 0)
                {
                    //如果所有子元素都没有灵活尺寸，那么根据对齐方式获取起始元素的位置
                    if (GetTotalFlexibleSize(axis) == 0)
                        pos = GetStartOffset(axis, GetTotalPreferredSize(axis) - (axis == 0 ? padding.horizontal : padding.vertical));
                    //如果有灵活尺寸，那么获取灵活空间的扩展系数
                    //扩展系数：剩余空间 除以 总灵活尺寸，得到单位灵活尺寸对应的剩余空间，也就是扩展系数
                    else if (GetTotalFlexibleSize(axis) > 0)
                        itemFlexibleMultiplier = surplusSpace / GetTotalFlexibleSize(axis);
                }

                //最小尺寸->理想尺寸之间的线性插值（0~1）
                //这个线性插值来源于总尺寸-总最小尺寸，与总理想尺寸-总最小尺寸的比例
                float minMaxLerp = 0;
                //如果总最小尺寸！=总理想尺寸，那么从最小到理想之间会有一个缩放
                if (GetTotalMinSize(axis) != GetTotalPreferredSize(axis))
                    minMaxLerp = Mathf.Clamp01((size - GetTotalMinSize(axis)) / (GetTotalPreferredSize(axis) - GetTotalMinSize(axis)));

                //每个子元素进行处理
                for (int i = startIndex; m_ReverseArrangement ? i >= endIndex : i < endIndex; i += increment)
                {
                    RectTransform child = rectChildren[i];
                    //先计算出子元素的3个尺寸
                    float min, preferred, flexible;
                    GetChildSizes(child, axis, controlSize, childForceExpandSize, out min, out preferred, out flexible);
                    //是否使用子元素轴向上的缩放
                    float scaleFactor = useScale ? child.localScale[axis] : 1f;

                    //使用线性插值，让子元素尺寸始终处于min与prefer之间
                    float childSize = Mathf.Lerp(min, preferred, minMaxLerp);
                    //计算出子元素尺寸之后，再加上灵活尺寸，就是它的实际尺寸
                    childSize += flexible * itemFlexibleMultiplier;
                    //如果控制子元素尺寸，那么直接使用上面计算出来的子元素尺寸来设置它的位置、大小
                    if (controlSize)
                    {
                        SetChildAlongAxisWithScale(child, axis, pos, childSize, scaleFactor);
                    }
                    //如果不控制子元素尺寸，那么不改变子元素大小，只改变它的位置
                    //首先，无论是否控制子元素的大小，子元素的位置都是上面计算出来的、不会变的
                    //但是由于不改变子元素的大小，所以为了子元素能够在给它计算出来的位置上摆出来，需要对子元素的位置进行略微的修饰
                    //修饰的方式就是用计算后的尺寸-子元素原始尺寸、然后再乘以一个对齐系数，就能够将子元素完美的摆在给它计算好的位置上，不会发生偏移的情况
                    else
                    {
                        float offsetInCell = (childSize - child.sizeDelta[axis]) * alignmentOnAxis;
                        SetChildAlongAxisWithScale(child, axis, pos + offsetInCell, scaleFactor);
                    }
                    //子元素位置定位到下一个
                    pos += childSize * scaleFactor + spacing;
                }
            }
        }

        /// <summary>
        /// 根据布局控制器的配置，计算子节点的尺寸
        /// 最终计算的时候，要取子节点身上的ILayoutElement组件的3个值，所以如果子节点没有该组件，那么3个值都将是0
        /// </summary>
        /// <param name="child"></param>
        /// <param name="axis"></param>
        /// <param name="controlSize"></param>
        /// <param name="childForceExpand"></param>
        /// <param name="min"></param>
        /// <param name="preferred"></param>
        /// <param name="flexible"></param>
        private void GetChildSizes(RectTransform child, int axis, bool controlSize, bool childForceExpand,
            out float min, out float preferred, out float flexible)
        {
            //如果不控制子元素的尺寸：最小尺寸、理想尺寸是子节点的SizeDelta，灵活尺寸是0
            if (!controlSize)
            {
                min = child.sizeDelta[axis];
                preferred = min;
                flexible = 0;
            }
            //如果控制子节点尺寸，那么会读取子节点上 ILayoutElement 对应的值；如果子节点没有实现 ILayoutElement 的组件，那么值都是0
            else
            {
                min = LayoutUtility.GetMinSize(child, axis);
                preferred = LayoutUtility.GetPreferredSize(child, axis);
                flexible = LayoutUtility.GetFlexibleSize(child, axis);
            }

            if (childForceExpand)
                flexible = Mathf.Max(flexible, 1);
        }

#if UNITY_EDITOR
        protected override void Reset()
        {
            base.Reset();

            // For new added components we want these to be set to false,
            // so that the user's sizes won't be overwritten before they
            // have a chance to turn these settings off.
            // However, for existing components that were added before this
            // feature was introduced, we want it to be on be default for
            // backwardds compatibility.
            // Hence their default value is on, but we set to off in reset.
            m_ChildControlWidth = false;
            m_ChildControlHeight = false;
        }

        private int m_Capacity = 10;
        private Vector2[] m_Sizes = new Vector2[10];

        protected virtual void Update()
        {
            if (Application.isPlaying)
                return;

            int count = transform.childCount;

            if (count > m_Capacity)
            {
                if (count > m_Capacity * 2)
                    m_Capacity = count;
                else
                    m_Capacity *= 2;

                m_Sizes = new Vector2[m_Capacity];
            }

            // If children size change in editor, update layout (case 945680 - Child GameObjects in a Horizontal/Vertical Layout Group don't display their correct position in the Editor)
            bool dirty = false;
            for (int i = 0; i < count; i++)
            {
                RectTransform t = transform.GetChild(i) as RectTransform;
                if (t != null && t.sizeDelta != m_Sizes[i])
                {
                    dirty = true;
                    m_Sizes[i] = t.sizeDelta;
                }
            }

            if (dirty)
                LayoutRebuilder.MarkLayoutForRebuild(transform as RectTransform);
        }

#endif
    }
}
