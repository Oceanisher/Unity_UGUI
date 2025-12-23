using System;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace UnityEngine.UI
{
    [AddComponentMenu("UI/Scroll Rect", 37)]
    [SelectionBase]
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    /// <summary>
    /// A component for making a child RectTransform scroll.
    /// 滚动区域组件
    ///
    /// 1.组件本身不会主动Mask，需要自行提供Mask组件进行Mask
    /// 2.层级结构是 Scroll View -> Viewport -> Content
    /// 3.ViewPort是滑动展示区域，身上挂载了Mask组件
    /// 4.Content是内容区域，根据包含的内容进行扩展
    /// 5.它本身继承了ILayoutElement、ILayoutGroup，所以它本身即是个布局元素、也是个布局控制器
    /// </summary>
    /// <remarks>
    /// ScrollRect will not do any clipping on its own. Combined with a Mask component, it can be turned into a scroll view.
    /// </remarks>
    public class ScrollRect : UIBehaviour, IInitializePotentialDragHandler, IBeginDragHandler, IEndDragHandler, IDragHandler, IScrollHandler, ICanvasElement, ILayoutElement, ILayoutGroup
    {
        /// <summary>
        /// A setting for which behavior to use when content moves beyond the confines of its container.
        /// 滚动区域的移动类型
        /// </summary>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// using UnityEngine;
        /// using System.Collections;
        /// using UnityEngine.UI;  // Required when Using UI elements.
        ///
        /// public class ExampleClass : MonoBehaviour
        /// {
        ///     public ScrollRect myScrollRect;
        ///     public Scrollbar newScrollBar;
        ///
        ///     //Called when a button is pressed
        ///     public void Example(int option)
        ///     {
        ///         if (option == 0)
        ///         {
        ///             myScrollRect.movementType = ScrollRect.MovementType.Clamped;
        ///         }
        ///         else if (option == 1)
        ///         {
        ///             myScrollRect.movementType = ScrollRect.MovementType.Elastic;
        ///         }
        ///         else if (option == 2)
        ///         {
        ///             myScrollRect.movementType = ScrollRect.MovementType.Unrestricted;
        ///         }
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
        public enum MovementType
        {
            /// <summary>
            /// Unrestricted movement. The content can move forever.
            /// 不受限制，可以一直拖拽
            /// </summary>
            Unrestricted,

            /// <summary>
            /// Elastic movement. The content is allowed to temporarily move beyond the container, but is pulled back elastically.
            /// 弹性的，能够拖拽超过view区域，送开后自动弹回。
            /// </summary>
            Elastic,

            /// <summary>
            /// Clamped movement. The content can not be moved beyond its container.
            /// 夹紧的，没有弹性，拖拽时不能超过view区域
            /// </summary>
            Clamped,
        }

        /// <summary>
        /// Enum for which behavior to use for scrollbar visibility.
        /// ScrollBar的可见性类型
        /// </summary>
        public enum ScrollbarVisibility
        {
            /// <summary>
            /// Always show the scrollbar.
            /// 总是展示
            /// </summary>
            Permanent,

            /// <summary>
            /// Automatically hide the scrollbar when no scrolling is needed on this axis. The viewport rect will not be changed.
            /// 当content的内容不超过viewport的区域范围时，自动隐藏。ScrollBar的显隐不影响Viewport的Rect大小。
            /// </summary>
            AutoHide,

            /// <summary>
            /// Automatically hide the scrollbar when no scrolling is needed on this axis, and expand the viewport rect accordingly.
            /// 类似AutoHide，但是当它显隐时会改变Viewport的Rect大小
            /// </summary>
            /// <remarks>
            /// When this setting is used, the scrollbar and the viewport rect become driven, meaning that values in the RectTransform are calculated automatically and can't be manually edited.
            /// </remarks>
            AutoHideAndExpandViewport,
        }

        [Serializable]
        /// <summary>
        /// Event type used by the ScrollRect.
        /// </summary>
        public class ScrollRectEvent : UnityEvent<Vector2> {}

        //滑动区域内容。所有滑动区域的内部元素都放在这里面。
        //Content的质心pivot在左上角
        [SerializeField]
        private RectTransform m_Content;

        /// <summary>
        /// The content that can be scrolled. It should be a child of the GameObject with ScrollRect on it.
        /// 滑动区域内容。所有滑动区域的内部元素都放在这里面。
        /// Content的质心pivot在左上角
        /// </summary>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// using UnityEngine;
        /// using System.Collections;
        /// using UnityEngine.UI; // Required when Using UI elements.
        ///
        /// public class ExampleClass : MonoBehaviour
        /// {
        ///     public ScrollRect myScrollRect;
        ///     public RectTransform scrollableContent;
        ///
        ///     //Do this when the Save button is selected.
        ///     public void Start()
        ///     {
        ///         // assigns the contect that can be scrolled using the ScrollRect.
        ///         myScrollRect.content = scrollableContent;
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
        public RectTransform content { get { return m_Content; } set { m_Content = value; } }

        //是否开启水平滑动
        [SerializeField]
        private bool m_Horizontal = true;

        /// <summary>
        /// Should horizontal scrolling be enabled?
        /// 是否开启水平滑动
        /// </summary>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// using UnityEngine;
        /// using System.Collections;
        /// using UnityEngine.UI; // Required when Using UI elements.
        ///
        /// public class ExampleClass : MonoBehaviour
        /// {
        ///     public ScrollRect myScrollRect;
        ///
        ///     public void Start()
        ///     {
        ///         // Is horizontal scrolling enabled?
        ///         if (myScrollRect.horizontal == true)
        ///         {
        ///             Debug.Log("Horizontal Scrolling is Enabled!");
        ///         }
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
        public bool horizontal { get { return m_Horizontal; } set { m_Horizontal = value; } }

        //是否开启垂直滑动
        [SerializeField]
        private bool m_Vertical = true;

        /// <summary>
        /// Should vertical scrolling be enabled?
        /// 是否开启垂直滑动
        /// </summary>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// using UnityEngine;
        /// using System.Collections;
        /// using UnityEngine.UI;  // Required when Using UI elements.
        ///
        /// public class ExampleClass : MonoBehaviour
        /// {
        ///     public ScrollRect myScrollRect;
        ///
        ///     public void Start()
        ///     {
        ///         // Is Vertical scrolling enabled?
        ///         if (myScrollRect.vertical == true)
        ///         {
        ///             Debug.Log("Vertical Scrolling is Enabled!");
        ///         }
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
        public bool vertical { get { return m_Vertical; } set { m_Vertical = value; } }

        //滑动区域移动类型
        [SerializeField]
        private MovementType m_MovementType = MovementType.Elastic;

        /// <summary>
        /// The behavior to use when the content moves beyond the scroll rect.
        /// 滑动区域移动类型
        /// </summary>
        public MovementType movementType { get { return m_MovementType; } set { m_MovementType = value; } }

        //弹性值
        [SerializeField]
        private float m_Elasticity = 0.1f;

        /// <summary>
        /// The amount of elasticity to use when the content moves beyond the scroll rect.
        /// 弹性值
        /// </summary>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// using UnityEngine;
        /// using System.Collections;
        /// using UnityEngine.UI;
        ///
        /// public class ExampleClass : MonoBehaviour
        /// {
        ///     public ScrollRect myScrollRect;
        ///
        ///     public void Start()
        ///     {
        ///         // assigns a new value to the elasticity of the scroll rect.
        ///         // The higher the number the longer it takes to snap back.
        ///         myScrollRect.elasticity = 3.0f;
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
        public float elasticity { get { return m_Elasticity; } set { m_Elasticity = value; } }

        //是否有惯性
        [SerializeField]
        private bool m_Inertia = true;

        /// <summary>
        /// Should movement inertia be enabled?
        /// 是否有惯性
        /// </summary>
        /// <remarks>
        /// Inertia means that the scrollrect content will keep scrolling for a while after being dragged. It gradually slows down according to the decelerationRate.
        /// </remarks>
        public bool inertia { get { return m_Inertia; } set { m_Inertia = value; } }

        //减速率，仅当开启惯性时有效
        [SerializeField]
        private float m_DecelerationRate = 0.135f; // Only used when inertia is enabled

        /// <summary>
        /// The rate at which movement slows down.
        /// 减速率，仅当开启惯性时有效
        /// </summary>
        /// <remarks>
        /// The deceleration rate is the speed reduction per second. A value of 0.5 halves the speed each second. The default is 0.135. The deceleration rate is only used when inertia is enabled.
        /// </remarks>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// using UnityEngine;
        /// using System.Collections;
        /// using UnityEngine.UI; // Required when Using UI elements.
        ///
        /// public class ExampleClass : MonoBehaviour
        /// {
        ///     public ScrollRect myScrollRect;
        ///
        ///     public void Start()
        ///     {
        ///         // assigns a new value to the decelerationRate of the scroll rect.
        ///         // The higher the number the longer it takes to decelerate.
        ///         myScrollRect.decelerationRate = 5.0f;
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
        public float decelerationRate { get { return m_DecelerationRate; } set { m_DecelerationRate = value; } }

        //对鼠标滚轮、触屏滑动的敏感度
        [SerializeField]
        private float m_ScrollSensitivity = 1.0f;

        /// <summary>
        /// The sensitivity to scroll wheel and track pad scroll events.
        /// 对鼠标滚轮、触屏滑动的敏感度
        /// </summary>
        /// <remarks>
        /// Higher values indicate higher sensitivity.
        /// </remarks>
        public float scrollSensitivity { get { return m_ScrollSensitivity; } set { m_ScrollSensitivity = value; } }

        //可视区域
        //viewport的质心pivot在左上角
        [SerializeField]
        private RectTransform m_Viewport;

        /// <summary>
        /// Reference to the viewport RectTransform that is the parent of the content RectTransform.
        /// 可视区域
        /// 的质心pivot在左上角
        /// </summary>
        public RectTransform viewport { get { return m_Viewport; } set { m_Viewport = value; SetDirtyCaching(); } }

        //水平滑动条
        [SerializeField]
        private Scrollbar m_HorizontalScrollbar;

        /// <summary>
        /// Optional Scrollbar object linked to the horizontal scrolling of the ScrollRect.
        /// 水平滑动条
        /// </summary>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// using UnityEngine;
        /// using System.Collections;
        /// using UnityEngine.UI;  // Required when Using UI elements.
        ///
        /// public class ExampleClass : MonoBehaviour
        /// {
        ///     public ScrollRect myScrollRect;
        ///     public Scrollbar newScrollBar;
        ///
        ///     public void Start()
        ///     {
        ///         // Assigns a scroll bar element to the ScrollRect, allowing you to scroll in the horizontal axis.
        ///         myScrollRect.horizontalScrollbar = newScrollBar;
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
        public Scrollbar horizontalScrollbar
        {
            get
            {
                return m_HorizontalScrollbar;
            }
            set
            {
                //设置新水平滑动条时，重新设置下滑动条滑动的事件监听
                if (m_HorizontalScrollbar)
                    m_HorizontalScrollbar.onValueChanged.RemoveListener(SetHorizontalNormalizedPosition);
                m_HorizontalScrollbar = value;
                if (m_Horizontal && m_HorizontalScrollbar)
                    m_HorizontalScrollbar.onValueChanged.AddListener(SetHorizontalNormalizedPosition);
                SetDirtyCaching();
            }
        }

        //垂直滑动条
        [SerializeField]
        private Scrollbar m_VerticalScrollbar;

        /// <summary>
        /// Optional Scrollbar object linked to the vertical scrolling of the ScrollRect.
        /// 垂直滑动条
        /// </summary>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// using UnityEngine;
        /// using System.Collections;
        /// using UnityEngine.UI;  // Required when Using UI elements.
        ///
        /// public class ExampleClass : MonoBehaviour
        /// {
        ///     public ScrollRect myScrollRect;
        ///     public Scrollbar newScrollBar;
        ///
        ///     public void Start()
        ///     {
        ///         // Assigns a scroll bar element to the ScrollRect, allowing you to scroll in the vertical axis.
        ///         myScrollRect.verticalScrollbar = newScrollBar;
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
        public Scrollbar verticalScrollbar
        {
            get
            {
                return m_VerticalScrollbar;
            }
            set
            {
                //设置新垂直滑动条时，重新设置下滑动条滑动的事件监听
                if (m_VerticalScrollbar)
                    m_VerticalScrollbar.onValueChanged.RemoveListener(SetVerticalNormalizedPosition);
                m_VerticalScrollbar = value;
                if (m_Vertical && m_VerticalScrollbar)
                    m_VerticalScrollbar.onValueChanged.AddListener(SetVerticalNormalizedPosition);
                SetDirtyCaching();
            }
        }

        //水平滑动条的可见性
        [SerializeField]
        private ScrollbarVisibility m_HorizontalScrollbarVisibility;

        /// <summary>
        /// The mode of visibility for the horizontal scrollbar.
        /// 水平滑动条的可见性
        /// </summary>
        public ScrollbarVisibility horizontalScrollbarVisibility { get { return m_HorizontalScrollbarVisibility; } set { m_HorizontalScrollbarVisibility = value; SetDirtyCaching(); } }

        //垂直滑动条的可见性
        [SerializeField]
        private ScrollbarVisibility m_VerticalScrollbarVisibility;

        /// <summary>
        /// The mode of visibility for the vertical scrollbar.
        /// 垂直滑动条的可见性
        /// </summary>
        public ScrollbarVisibility verticalScrollbarVisibility { get { return m_VerticalScrollbarVisibility; } set { m_VerticalScrollbarVisibility = value; SetDirtyCaching(); } }

        //水平滑动条的与ViewPort的间隔，默认是-3。修改这个值，将会变更Viewport的Rect大小
        [SerializeField]
        private float m_HorizontalScrollbarSpacing;

        /// <summary>
        /// The space between the scrollbar and the viewport.
        /// 水平滑动条的与ViewPort的间隔，默认是-3。修改这个值，将会变更Viewport的Rect大小
        /// </summary>
        public float horizontalScrollbarSpacing { get { return m_HorizontalScrollbarSpacing; } set { m_HorizontalScrollbarSpacing = value; SetDirty(); } }

        //垂直滑动条的与ViewPort的间隔，默认是-3。修改这个值，将会变更Viewport的Rect大小
        [SerializeField]
        private float m_VerticalScrollbarSpacing;

        /// <summary>
        /// The space between the scrollbar and the viewport.
        /// 垂直滑动条的与ViewPort的间隔，默认是-3。修改这个值，将会变更Viewport的Rect大小
        /// </summary>
        public float verticalScrollbarSpacing { get { return m_VerticalScrollbarSpacing; } set { m_VerticalScrollbarSpacing = value; SetDirty(); } }

        //滚动区域值变更事件
        //Vector2的X、Y都是归一化的值，代表当前滑动到对应轴的多少百分比了
        [SerializeField]
        private ScrollRectEvent m_OnValueChanged = new ScrollRectEvent();

        /// <summary>
        /// Callback executed when the position of the child changes.
        /// 滚动区域值变更事件
        /// Vector2的X、Y都是归一化的值，代表当前滑动到对应轴的多少百分比了
        /// </summary>
        /// <remarks>
        /// onValueChanged is used to watch for changes in the ScrollRect object.
        /// The onValueChanged call will use the UnityEvent.AddListener API to watch for
        /// changes.  When changes happen script code provided by the user will be called.
        /// The UnityEvent.AddListener API for UI.ScrollRect._onValueChanged takes a Vector2.
        ///
        /// Note: The editor allows the onValueChanged value to be set up manually.For example the
        /// value can be set to run only a runtime.  The object and script function to call are also
        /// provided here.
        ///
        /// The onValueChanged variable can be alternatively set-up at runtime.The script example below
        /// shows how this can be done.The script is attached to the ScrollRect object.
        /// </remarks>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// using UnityEngine;
        /// using UnityEngine.UI;
        ///
        /// public class ExampleScript : MonoBehaviour
        /// {
        ///     static ScrollRect scrollRect;
        ///
        ///     void Start()
        ///     {
        ///         scrollRect = GetComponent<ScrollRect>();
        ///         scrollRect.onValueChanged.AddListener(ListenerMethod);
        ///     }
        ///
        ///     public void ListenerMethod(Vector2 value)
        ///     {
        ///         Debug.Log("ListenerMethod: " + value);
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
        public ScrollRectEvent onValueChanged { get { return m_OnValueChanged; } set { m_OnValueChanged = value; } }

        //鼠标在Viewport空间中的位置，开始拖拽事件时赋值
        // The offset from handle position to mouse down position
        private Vector2 m_PointerStartLocalCursor = Vector2.zero;
        //Content在拖拽开始时的anchoredPosition，开始拖拽事件时赋值
        protected Vector2 m_ContentStartPosition = Vector2.zero;

        //Viewport的Trs
        private RectTransform m_ViewRect;

        //Viewport的Trs
        //如果没有设置Viewport，那么Viewport就是ScrollRect自身
        protected RectTransform viewRect
        {
            get
            {
                if (m_ViewRect == null)
                    m_ViewRect = m_Viewport;
                if (m_ViewRect == null)
                    m_ViewRect = (RectTransform)transform;
                return m_ViewRect;
            }
        }

        //Content的Bound数据，是相对于Viewport的Bound，每次移动都会重新更新，因为要重新计算可移动的范围
        //所以两个Bound都是在Viewport空间下的Bound
        protected Bounds m_ContentBounds;
        //Viewport的Bound数据，是相对自身空间的Bound，每次移动都会重新更新，因为要重新计算可移动的范围
        //所以两个Bound都是在Viewport空间下的Bound
        private Bounds m_ViewBounds;

        //当前的滑动速度
        private Vector2 m_Velocity;

        /// <summary>
        /// The current velocity of the content.
        /// 当前的滑动速度
        /// </summary>
        /// <remarks>
        /// The velocity is defined in units per second.
        /// </remarks>
        public Vector2 velocity { get { return m_Velocity; } set { m_Velocity = value; } }

        //是否在拖拽中，指的是鼠标点击拖拽
        private bool m_Dragging;
        //是否在滚动中，指的是鼠标滑轮、或者触屏互动等触发的
        private bool m_Scrolling;

        //上一次滑动后的值，content的anchoredPosition值
        private Vector2 m_PrevPosition = Vector2.zero;
        //上一次滑动后的值，content的Bound值
        private Bounds m_PrevContentBounds;
        //上一次滑动后的值，viewport的Bound值
        private Bounds m_PrevViewBounds;
        //是否已经重建了网格了，每次Canvas重建网格后，这里都会被置为true；初始为false，Disable后置为False；可以认为是一次性的变量
        [NonSerialized]
        private bool m_HasRebuiltLayout = false;

        //水平滚动条隐藏时是否要扩展Viewport的区域大小，当水平滚动条显示类型是AutoHideAndExpandViewport、并且滚动条是ScrollRect的最近子节点时会置为true
        private bool m_HSliderExpand;
        //垂直滚动条隐藏时是否要扩展Viewport的区域大小，当水平滚动条显示类型是AutoHideAndExpandViewport、并且滚动条是ScrollRect的最近子节点时会置为true
        private bool m_VSliderExpand;
        //水平滚动条高度，水平滚动条不为空时会赋值
        private float m_HSliderHeight;
        //垂直滚动条宽度，垂直滚动条不为空时会赋值
        private float m_VSliderWidth;

        //ScrollRect自身的Trs
        [System.NonSerialized] private RectTransform m_Rect;
        private RectTransform rectTransform
        {
            get
            {
                if (m_Rect == null)
                    m_Rect = GetComponent<RectTransform>();
                return m_Rect;
            }
        }

        //水平滚动条的Trs
        private RectTransform m_HorizontalScrollbarRect;
        //垂直滚动条的Trs
        private RectTransform m_VerticalScrollbarRect;

        // field is never assigned warning
        #pragma warning disable 649
        private DrivenRectTransformTracker m_Tracker;
        #pragma warning restore 649

        protected ScrollRect()
        {}

        /// <summary>
        /// Rebuilds the scroll rect data after initialization.
        /// 初始化之后，重建ScrollRect的网格
        /// </summary>
        /// <param name="executing">The current step in the rendering CanvasUpdate cycle.</param>
        public virtual void Rebuild(CanvasUpdate executing)
        {
            //网格重建之前，先更新内部数据
            if (executing == CanvasUpdate.Prelayout)
            {
                UpdateCachedData();
            }

            //每次重建后，更新下Bound数据
            if (executing == CanvasUpdate.PostLayout)
            {
                UpdateBounds();
                UpdateScrollbars(Vector2.zero);
                UpdatePrevData();

                m_HasRebuiltLayout = true;
            }
        }

        public virtual void LayoutComplete()
        {}

        public virtual void GraphicUpdateComplete()
        {}

        /// <summary>
        /// 更新内部数据
        /// 主要是一些运行时的变量，比如水平垂直滚动条的Trs等
        /// </summary>
        void UpdateCachedData()
        {
            Transform transform = this.transform;
            m_HorizontalScrollbarRect = m_HorizontalScrollbar == null ? null : m_HorizontalScrollbar.transform as RectTransform;
            m_VerticalScrollbarRect = m_VerticalScrollbar == null ? null : m_VerticalScrollbar.transform as RectTransform;

            // These are true if either the elements are children, or they don't exist at all.
            //Viewport是否是ScrollRect的最近子节点
            bool viewIsChild = (viewRect.parent == transform);
            //水平滚动条是否是ScrollRect的最近子节点
            bool hScrollbarIsChild = (!m_HorizontalScrollbarRect || m_HorizontalScrollbarRect.parent == transform);
            //垂直滚动条是否是ScrollRect的最近子节点
            bool vScrollbarIsChild = (!m_VerticalScrollbarRect || m_VerticalScrollbarRect.parent == transform);
            //以上3个是否都是ScrollRect的最近子节点
            bool allAreChildren = (viewIsChild && hScrollbarIsChild && vScrollbarIsChild);

            m_HSliderExpand = allAreChildren && m_HorizontalScrollbarRect && horizontalScrollbarVisibility == ScrollbarVisibility.AutoHideAndExpandViewport;
            m_VSliderExpand = allAreChildren && m_VerticalScrollbarRect && verticalScrollbarVisibility == ScrollbarVisibility.AutoHideAndExpandViewport;
            m_HSliderHeight = (m_HorizontalScrollbarRect == null ? 0 : m_HorizontalScrollbarRect.rect.height);
            m_VSliderWidth = (m_VerticalScrollbarRect == null ? 0 : m_VerticalScrollbarRect.rect.width);
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            //如果启用了滚动条，那么会监听滚动条的滚动，从而变更滚动区域
            if (m_Horizontal && m_HorizontalScrollbar)
                m_HorizontalScrollbar.onValueChanged.AddListener(SetHorizontalNormalizedPosition);
            if (m_Vertical && m_VerticalScrollbar)
                m_VerticalScrollbar.onValueChanged.AddListener(SetVerticalNormalizedPosition);

            //每次Enable，都重建下布局
            CanvasUpdateRegistry.RegisterCanvasElementForLayoutRebuild(this);
            SetDirty();
        }

        protected override void OnDisable()
        {
            CanvasUpdateRegistry.UnRegisterCanvasElementForRebuild(this);

            if (m_HorizontalScrollbar)
                m_HorizontalScrollbar.onValueChanged.RemoveListener(SetHorizontalNormalizedPosition);
            if (m_VerticalScrollbar)
                m_VerticalScrollbar.onValueChanged.RemoveListener(SetVerticalNormalizedPosition);

            m_Dragging = false;
            m_Scrolling = false;
            m_HasRebuiltLayout = false;
            m_Tracker.Clear();
            m_Velocity = Vector2.zero;
            LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
            base.OnDisable();
        }

        /// <summary>
        /// See member in base class.
        /// 滚动区域的有效性，除了基础的Active，还必须有Content对象才行
        /// </summary>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// using UnityEngine;
        /// using System.Collections;
        /// using UnityEngine.UI;  // Required when Using UI elements.
        ///
        /// public class ExampleClass : MonoBehaviour
        /// {
        ///     public ScrollRect myScrollRect;
        ///
        ///     public void Start()
        ///     {
        ///         //Checks if the ScrollRect called "myScrollRect" is active.
        ///         if (myScrollRect.IsActive())
        ///         {
        ///             Debug.Log("The Scroll Rect is active!");
        ///         }
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
        public override bool IsActive()
        {
            return base.IsActive() && m_Content != null;
        }

        /// <summary>
        /// 确保网格已经重建了，如果没有重建，那么就强制重建
        /// </summary>
        private void EnsureLayoutHasRebuilt()
        {
            if (!m_HasRebuiltLayout && !CanvasUpdateRegistry.IsRebuildingLayout())
                Canvas.ForceUpdateCanvases();
        }

        /// <summary>
        /// Sets the velocity to zero on both axes so the content stops moving.
        /// </summary>
        public virtual void StopMovement()
        {
            m_Velocity = Vector2.zero;
        }

        /// <summary>
        /// 鼠标滚轮滚动事件处理
        /// </summary>
        /// <param name="data"></param>
        public virtual void OnScroll(PointerEventData data)
        {
            if (!IsActive())
                return;

            //确保Layout已经重建了，否则会强制重建
            EnsureLayoutHasRebuilt();
            //更新Bound大小
            UpdateBounds();

            Vector2 delta = data.scrollDelta;
            // Down is positive for scroll events, while in UI system up is positive.
            //在scroll事件中，Y轴为正数代表向下滚动，但是在UGUI里相反，所以这里的delta的Y值要翻转一下
            delta.y *= -1;
            //如果只是垂直滚动
            //这里之所以当X轴移动距离大于Y轴，要把X轴赋值给Y轴，是为了处理用户使用横向滚动设备的情况；
            //这样之后，即使 ScrollRect 只支持一个方向，也能响应另一个方向的滚轮输入，提升触控板等输入设备的体验
            if (vertical && !horizontal)
            {
                if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                    delta.y = delta.x;
                delta.x = 0;
            }
            //如果只是水平滚动
            if (horizontal && !vertical)
            {
                if (Mathf.Abs(delta.y) > Mathf.Abs(delta.x))
                    delta.x = delta.y;
                delta.y = 0;
            }

            //如果滚动事件正在滚动，那么将组件也标识为正在滚动
            if (data.IsScrolling())
                m_Scrolling = true;

            //滚动其实是修改Content的anchoredPosition
            //Content的对齐方式是上对齐、左右拉伸，然后它的质心和viewport一样、都是在左上角
            //初始情况下，anchorPosition为0，Content质心配置是左上角(0,1)
            //所以无论是上下滑动还是左右滑动，就直接修改anchoredPosition即可
            Vector2 position = m_Content.anchoredPosition;
            position += delta * m_ScrollSensitivity;
            //计算出Position之后，如果移动类型是Clamped，那么移动就不能超过0~1
            //CalculateOffset会根据当前Content位置+delta值，计算出一个将Content限制在Viewport内的偏移值
            if (m_MovementType == MovementType.Clamped)
                position += CalculateOffset(position - m_Content.anchoredPosition);

            SetContentAnchoredPosition(position);
            //TODO 多次更新bound，好像没必要，SetContentAnchoredPosition中已经有更新Bounds了
            UpdateBounds();
        }

        /// <summary>
        /// 鼠标按下还没开始拖拽的事件，也就是可能会拖拽，此时停止原有的滑动
        /// 只关心左键
        /// </summary>
        /// <param name="eventData"></param>
        public virtual void OnInitializePotentialDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            m_Velocity = Vector2.zero;
        }

        /// <summary>
        /// Handling for when the content is beging being dragged.
        /// 鼠标开始拖拽事件
        /// </summary>
        ///<example>
        /// <code>
        /// <![CDATA[
        /// using UnityEngine;
        /// using System.Collections;
        /// using UnityEngine.EventSystems; // Required when using event data
        ///
        /// public class ExampleClass : MonoBehaviour, IBeginDragHandler // required interface when using the OnBeginDrag method.
        /// {
        ///     //Do this when the user starts dragging the element this script is attached to..
        ///     public void OnBeginDrag(PointerEventData data)
        ///     {
        ///         Debug.Log("They started dragging " + this.name);
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
        public virtual void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            if (!IsActive())
                return;

            UpdateBounds();

            //此时记录鼠标此时在Viewport中的位置
            m_PointerStartLocalCursor = Vector2.zero;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(viewRect, eventData.position, eventData.pressEventCamera, out m_PointerStartLocalCursor);
            //记录此时Content的anchoredPosition
            m_ContentStartPosition = m_Content.anchoredPosition;
            //拖拽开始置为true
            m_Dragging = true;
        }

        /// <summary>
        /// Handling for when the content has finished being dragged.
        /// 拖拽结束事件
        /// 只修改拖拽标记
        /// </summary>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// using UnityEngine;
        /// using System.Collections;
        /// using UnityEngine.EventSystems; // Required when using event data
        ///
        /// public class ExampleClass : MonoBehaviour, IEndDragHandler // required interface when using the OnEndDrag method.
        /// {
        ///     //Do this when the user stops dragging this UI Element.
        ///     public void OnEndDrag(PointerEventData data)
        ///     {
        ///         Debug.Log("Stopped dragging " + this.name + "!");
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
        public virtual void OnEndDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            m_Dragging = false;
        }

        /// <summary>
        /// Handling for when the content is dragged.
        /// 拖拽事件，拖拽中
        /// </summary>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// using UnityEngine;
        /// using System.Collections;
        /// using UnityEngine.EventSystems; // Required when using event data
        ///
        /// public class ExampleClass : MonoBehaviour, IDragHandler // required interface when using the OnDrag method.
        /// {
        ///     //Do this while the user is dragging this UI Element.
        ///     public void OnDrag(PointerEventData data)
        ///     {
        ///         Debug.Log("Currently dragging " + this.name);
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
        public virtual void OnDrag(PointerEventData eventData)
        {
            if (!m_Dragging)
                return;

            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            if (!IsActive())
                return;

            //每次拖拽事件都计算下鼠标当前在viewport中的位置
            Vector2 localCursor;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(viewRect, eventData.position, eventData.pressEventCamera, out localCursor))
                return;

            UpdateBounds();

            //根据鼠标当前位置-鼠标开始拖拽的位置，计算出位置的delta值
            var pointerDelta = localCursor - m_PointerStartLocalCursor;
            Vector2 position = m_ContentStartPosition + pointerDelta;

            // Offset to get content into place in the view.
            //先根据Clapmed模式，把移动后的Content限制在Viewport内部
            Vector2 offset = CalculateOffset(position - m_Content.anchoredPosition);
            position += offset;
            //如果此时移动方式是弹性的，那么看是否可以移动的超出0~1
            if (m_MovementType == MovementType.Elastic)
            {
                //如果此时拉回的delta不为0，也就是当前的拖拽已经超过了0~1，那么计算一下当前应该超出的值，把它加到最终的position上
                if (offset.x != 0)
                    position.x = position.x - RubberDelta(offset.x, m_ViewBounds.size.x);
                if (offset.y != 0)
                    position.y = position.y - RubberDelta(offset.y, m_ViewBounds.size.y);
            }

            SetContentAnchoredPosition(position);
        }

        /// <summary>
        /// Sets the anchored position of the content.
        /// 根据最终计算的position值，将Content的位置更新
        /// 只更新需要滚动的方向
        /// </summary>
        protected virtual void SetContentAnchoredPosition(Vector2 position)
        {
            if (!m_Horizontal)
                position.x = m_Content.anchoredPosition.x;
            if (!m_Vertical)
                position.y = m_Content.anchoredPosition.y;

            if (position != m_Content.anchoredPosition)
            {
                m_Content.anchoredPosition = position;
                //更新位置之后，重新计算下Bound
                UpdateBounds();
            }
        }

        /// <summary>
        /// LateUpdate中来实际处理滑动
        /// </summary>
        protected virtual void LateUpdate()
        {
            if (!m_Content)
                return;

            EnsureLayoutHasRebuilt();
            UpdateBounds();
            float deltaTime = Time.unscaledDeltaTime;
            //计算一下需要拉回的delta，看是否需要回弹
            Vector2 offset = CalculateOffset(Vector2.zero);

            // Skip processing if deltaTime is invalid (0 or less) as it will cause inaccurate velocity calculations and a divide by zero error.
            if (deltaTime > 0.0f)
            {
                //如果不在拖拽中，并且是需要回弹、或者速度不为0（代表还在自行滑动）
                if (!m_Dragging && (offset != Vector2.zero || m_Velocity != Vector2.zero))
                {
                    Vector2 position = m_Content.anchoredPosition;
                    for (int axis = 0; axis < 2; axis++)
                    {
                        //如果当前是弹性移动，并且在回弹中，那么使用平滑回弹
                        // Apply spring physics if movement is elastic and content has an offset from the view.
                        if (m_MovementType == MovementType.Elastic && offset[axis] != 0)
                        {
                            float speed = m_Velocity[axis];
                            float smoothTime = m_Elasticity;
                            //如果当前在使用滑轮滚动中，那么滑动速度加快3倍
                            if (m_Scrolling)
                                smoothTime *= 3.0f;
                            //平滑移动position
                            position[axis] = Mathf.SmoothDamp(m_Content.anchoredPosition[axis], m_Content.anchoredPosition[axis] + offset[axis], ref speed, smoothTime, Mathf.Infinity, deltaTime);
                            //如果当前速度小于1，那么速度归0
                            if (Mathf.Abs(speed) < 1)
                                speed = 0;
                            m_Velocity[axis] = speed;
                        }
                        // Else move content according to velocity with deceleration applied.
                        //如果没有回弹，那么看是否启用了惯性，有惯性的话，结束滚动后仍旧有一定的滑动
                        else if (m_Inertia)
                        {
                            //计算减速
                            m_Velocity[axis] *= Mathf.Pow(m_DecelerationRate, deltaTime);
                            //减速到1之后，速度认为是0
                            if (Mathf.Abs(m_Velocity[axis]) < 1)
                                m_Velocity[axis] = 0;
                            //直接把当前位置加上该帧速度的移动距离
                            position[axis] += m_Velocity[axis] * deltaTime;
                        }
                        // If we have neither elaticity or friction, there shouldn't be any velocity.
                        //如果即没有回弹、也没有惯性，那么就停止滑动
                        else
                        {
                            m_Velocity[axis] = 0;
                        }
                    }

                    //如果是Clamp移动方式，那么将位置拉回一下
                    if (m_MovementType == MovementType.Clamped)
                    {
                        offset = CalculateOffset(position - m_Content.anchoredPosition);
                        position += offset;
                    }

                    //设置最终的position
                    SetContentAnchoredPosition(position);
                }

                //如果当前在拖拽中，并且有惯性，那么需要计算下速度值
                //速度的大小是该帧移动的距离除以时间，并且lerp一下，从而让当前速度平滑过渡到计算后的速度
                //这个速度值是拖拽结束后还能移动一会的关键
                if (m_Dragging && m_Inertia)
                {
                    Vector3 newVelocity = (m_Content.anchoredPosition - m_PrevPosition) / deltaTime;
                    m_Velocity = Vector3.Lerp(m_Velocity, newVelocity, deltaTime * 10);
                }
            }

            //如果经过上面的处理之后，数据有变化，也就是说有移动，那么需要更新下各类值
            if (m_ViewBounds != m_PrevViewBounds || m_ContentBounds != m_PrevContentBounds || m_Content.anchoredPosition != m_PrevPosition)
            {
                //更新ScrollBar的滑块大小
                UpdateScrollbars(offset);
                UISystemProfilerApi.AddMarker("ScrollRect.value", this);
                m_OnValueChanged.Invoke(normalizedPosition);
                //更新Pre值，用于后续对比计算
                UpdatePrevData();
            }
            //更新ScrollBar的滑动条可见性
            UpdateScrollbarVisibility();
            //因为滑轮滚动是每帧可能都有，那么每次LateUpdate之后就标记为滑动结束；每次滑动开启，都是由滑动事件启动的
            m_Scrolling = false;
        }

        /// <summary>
        /// Helper function to update the previous data fields on a ScrollRect. Call this before you change data in the ScrollRect.
        /// 记录上一次更新Content时的值，用于下一次移动的比较
        /// </summary>
        protected void UpdatePrevData()
        {
            if (m_Content == null)
                m_PrevPosition = Vector2.zero;
            else
                m_PrevPosition = m_Content.anchoredPosition;
            m_PrevViewBounds = m_ViewBounds;
            m_PrevContentBounds = m_ContentBounds;
        }

        /// <summary>
        /// 更新ScrollBar的滑块大小
        /// </summary>
        /// <param name="offset"></param>
        private void UpdateScrollbars(Vector2 offset)
        {
            //如果有offset，说明已经超界了，那么相应的ScrollBar也要缩小一些，以应对视觉上Content变大了的情况
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

        /// <summary>
        /// The scroll position as a Vector2 between (0,0) and (1,1) with (0,0) being the lower left corner.
        /// 归一化的当前滑动位置
        /// </summary>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// using UnityEngine;
        /// using System.Collections;
        /// using UnityEngine.UI;  // Required when Using UI elements.
        ///
        /// public class ExampleClass : MonoBehaviour
        /// {
        ///     public ScrollRect myScrollRect;
        ///     public Vector2 myPosition = new Vector2(0.5f, 0.5f);
        ///
        ///     public void Start()
        ///     {
        ///         //Change the current scroll position.
        ///         myScrollRect.normalizedPosition = myPosition;
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
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

        /// <summary>
        /// The horizontal scroll position as a value between 0 and 1, with 0 being at the left.
        /// 归一化的当前滑动位置，水平位置，初始是0
        /// </summary>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// using UnityEngine;
        /// using System.Collections;
        /// using UnityEngine.UI;  // Required when Using UI elements.
        ///
        /// public class ExampleClass : MonoBehaviour
        /// {
        ///     public ScrollRect myScrollRect;
        ///     public Scrollbar newScrollBar;
        ///
        ///     public void Start()
        ///     {
        ///         //Change the current horizontal scroll position.
        ///         myScrollRect.horizontalNormalizedPosition = 0.5f;
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
        public float horizontalNormalizedPosition
        {
            get
            {
                UpdateBounds();
                //如果content与viewport大小差不多、或者content小于viewport，那么要么返回0、要么返回1
                if ((m_ContentBounds.size.x <= m_ViewBounds.size.x) || Mathf.Approximately(m_ContentBounds.size.x, m_ViewBounds.size.x))
                    return (m_ViewBounds.min.x > m_ContentBounds.min.x) ? 1 : 0;
                //如果content比view大，那么要看移动的距离
                return (m_ViewBounds.min.x - m_ContentBounds.min.x) / (m_ContentBounds.size.x - m_ViewBounds.size.x);
            }
            set
            {
                SetNormalizedPosition(value, 0);
            }
        }

        /// <summary>
        /// The vertical scroll position as a value between 0 and 1, with 0 being at the bottom.
        /// 归一化的当前滑动位置，垂直位置，初始是1
        /// </summary>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// using UnityEngine;
        /// using System.Collections;
        /// using UnityEngine.UI;  // Required when Using UI elements.
        ///
        /// public class ExampleClass : MonoBehaviour
        /// {
        ///     public ScrollRect myScrollRect;
        ///     public Scrollbar newScrollBar;
        ///
        ///     public void Start()
        ///     {
        ///         //Change the current vertical scroll position.
        ///         myScrollRect.verticalNormalizedPosition = 0.5f;
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>

        public float verticalNormalizedPosition
        {
            get
            {
                UpdateBounds();
                if ((m_ContentBounds.size.y <= m_ViewBounds.size.y) || Mathf.Approximately(m_ContentBounds.size.y, m_ViewBounds.size.y))
                    return (m_ViewBounds.min.y > m_ContentBounds.min.y) ? 1 : 0;

                return (m_ViewBounds.min.y - m_ContentBounds.min.y) / (m_ContentBounds.size.y - m_ViewBounds.size.y);
            }
            set
            {
                SetNormalizedPosition(value, 1);
            }
        }

        private void SetHorizontalNormalizedPosition(float value) { SetNormalizedPosition(value, 0); }
        private void SetVerticalNormalizedPosition(float value) { SetNormalizedPosition(value, 1); }

        /// <summary>
        /// >Set the horizontal or vertical scroll position as a value between 0 and 1, with 0 being at the left or at the bottom.
        /// 将归一化的Position位置，设置给Content
        /// 也就是说直接跳转到某个位置上，会同时将速度设置为0
        /// </summary>
        /// <param name="value">The position to set, between 0 and 1.</param>
        /// <param name="axis">The axis to set: 0 for horizontal, 1 for vertical.</param>
        protected virtual void SetNormalizedPosition(float value, int axis)
        {
            EnsureLayoutHasRebuilt();
            UpdateBounds();
            // How much the content is larger than the view.
            //Content可以移动的总距离
            float hiddenLength = m_ContentBounds.size[axis] - m_ViewBounds.size[axis];
            // Where the position of the lower left corner of the content bounds should be, in the space of the view.
            //content按照value移动的后的Bound的min的位置
            float contentBoundsMinPosition = m_ViewBounds.min[axis] - value * hiddenLength;
            // The new content localPosition, in the space of the view.
            //根据这个min的位置，计算出新的anchoredPosition的位置；
            //contentBoundsMinPosition - m_ContentBounds.min[axis]，是计算出当前的Bound需要移动的距离，然后加上content现在的anchoredPosition，就等于最终的anchoredPosition位置
            float newAnchoredPosition = m_Content.anchoredPosition[axis] + contentBoundsMinPosition - m_ContentBounds.min[axis];

            Vector3 anchoredPosition = m_Content.anchoredPosition;
            //直接设置位置，并且把速度置为0
            if (Mathf.Abs(anchoredPosition[axis] - newAnchoredPosition) > 0.01f)
            {
                anchoredPosition[axis] = newAnchoredPosition;
                m_Content.anchoredPosition = anchoredPosition;
                m_Velocity[axis] = 0;
                UpdateBounds();
            }
        }

        /// <summary>
        /// 根据Viewport的在某个轴上的大小，以及当前超出viewport的位置大小，计算出在移动方式是弹性的条件下，最终可以超框移动的距离
        /// 基于反比例函数的弹性阻力算法
        /// </summary>
        /// <param name="overStretching"></param>
        /// <param name="viewSize"></param>
        /// <returns></returns>
        private static float RubberDelta(float overStretching, float viewSize)
        {
            return (1 - (1 / ((Mathf.Abs(overStretching) * 0.55f / viewSize) + 1))) * viewSize * Mathf.Sign(overStretching);
        }

        protected override void OnRectTransformDimensionsChange()
        {
            SetDirty();
        }

        /// <summary>
        /// 水平ScrollBar是否需要滑动
        /// 当Content大于View的时候才需要滑动
        /// </summary>
        private bool hScrollingNeeded
        {
            get
            {
                if (Application.isPlaying)
                    return m_ContentBounds.size.x > m_ViewBounds.size.x + 0.01f;
                return true;
            }
        }
        /// <summary>
        /// 垂直ScrollBar是否需要滑动
        /// 当Content大于View的时候才需要滑动
        /// </summary>
        private bool vScrollingNeeded
        {
            get
            {
                if (Application.isPlaying)
                    return m_ContentBounds.size.y > m_ViewBounds.size.y + 0.01f;
                return true;
            }
        }

        /// <summary>
        /// Called by the layout system.
        /// </summary>
        public virtual void CalculateLayoutInputHorizontal() {}

        /// <summary>
        /// Called by the layout system.
        /// </summary>
        public virtual void CalculateLayoutInputVertical() {}

        /// <summary>
        /// Called by the layout system.
        /// </summary>
        public virtual float minWidth { get { return -1; } }
        /// <summary>
        /// Called by the layout system.
        /// </summary>
        public virtual float preferredWidth { get { return -1; } }
        /// <summary>
        /// Called by the layout system.
        /// </summary>
        public virtual float flexibleWidth { get { return -1; } }

        /// <summary>
        /// Called by the layout system.
        /// </summary>
        public virtual float minHeight { get { return -1; } }
        /// <summary>
        /// Called by the layout system.
        /// </summary>
        public virtual float preferredHeight { get { return -1; } }
        /// <summary>
        /// Called by the layout system.
        /// </summary>
        public virtual float flexibleHeight { get { return -1; } }

        /// <summary>
        /// Called by the layout system.
        /// </summary>
        public virtual int layoutPriority { get { return -1; } }

        /// <summary>
        /// Called by the layout system.
        /// 变更水平布局
        /// </summary>
        public virtual void SetLayoutHorizontal()
        {
            m_Tracker.Clear();
            UpdateCachedData();

            //如果要扩展Viewport的大小，那么先把viewport充满它的父节点，并重新计算Bound
            if (m_HSliderExpand || m_VSliderExpand)
            {
                m_Tracker.Add(this, viewRect,
                    DrivenTransformProperties.Anchors |
                    DrivenTransformProperties.SizeDelta |
                    DrivenTransformProperties.AnchoredPosition);

                // Make view full size to see if content fits.
                viewRect.anchorMin = Vector2.zero;
                viewRect.anchorMax = Vector2.one;
                viewRect.sizeDelta = Vector2.zero;
                viewRect.anchoredPosition = Vector2.zero;

                // Recalculate content layout with this size to see if it fits when there are no scrollbars.
                LayoutRebuilder.ForceRebuildLayoutImmediate(content);
                m_ViewBounds = new Bounds(viewRect.rect.center, viewRect.rect.size);
                m_ContentBounds = GetBounds();
            }
            
            //如果垂直滑动条需要展示，那么会缩小一下viewport的大小、减去滑动条的大小即可，直接变更它的sizeDelta即可，因为它的pivot是左上角(0,1)
            // If it doesn't fit vertically, enable vertical scrollbar and shrink view horizontally to make room for it.
            if (m_VSliderExpand && vScrollingNeeded)
            {
                viewRect.sizeDelta = new Vector2(-(m_VSliderWidth + m_VerticalScrollbarSpacing), viewRect.sizeDelta.y);

                // Recalculate content layout with this size to see if it fits vertically
                // when there is a vertical scrollbar (which may reflowed the content to make it taller).
                LayoutRebuilder.ForceRebuildLayoutImmediate(content);
                m_ViewBounds = new Bounds(viewRect.rect.center, viewRect.rect.size);
                m_ContentBounds = GetBounds();
            }

            //如果水平滑动条需要展示，那么会缩小一下viewport的大小、减去滑动条的大小即可，直接变更它的sizeDelta即可，因为它的pivot是左上角(0,1)
            // If it doesn't fit horizontally, enable horizontal scrollbar and shrink view vertically to make room for it.
            if (m_HSliderExpand && hScrollingNeeded)
            {
                viewRect.sizeDelta = new Vector2(viewRect.sizeDelta.x, -(m_HSliderHeight + m_HorizontalScrollbarSpacing));
                m_ViewBounds = new Bounds(viewRect.rect.center, viewRect.rect.size);
                m_ContentBounds = GetBounds();
            }

            //如果垂直滑动条第一时间没有刷新出来，但是水平滑动条有了，那么会重复操作一次
            // If the vertical slider didn't kick in the first time, and the horizontal one did,
            // we need to check again if the vertical slider now needs to kick in.
            // If it doesn't fit vertically, enable vertical scrollbar and shrink view horizontally to make room for it.
            if (m_VSliderExpand && vScrollingNeeded && viewRect.sizeDelta.x == 0 && viewRect.sizeDelta.y < 0)
            {
                viewRect.sizeDelta = new Vector2(-(m_VSliderWidth + m_VerticalScrollbarSpacing), viewRect.sizeDelta.y);
            }
        }

        /// <summary>
        /// Called by the layout system.
        /// 垂直布局设置
        /// </summary>
        public virtual void SetLayoutVertical()
        {
            //更新ScrollBar
            UpdateScrollbarLayout();
            m_ViewBounds = new Bounds(viewRect.rect.center, viewRect.rect.size);
            m_ContentBounds = GetBounds();
        }

        /// <summary>
        /// 更新ScrollBar的滑动条可见性
        /// </summary>
        void UpdateScrollbarVisibility()
        {
            UpdateOneScrollbarVisibility(vScrollingNeeded, m_Vertical, m_VerticalScrollbarVisibility, m_VerticalScrollbar);
            UpdateOneScrollbarVisibility(hScrollingNeeded, m_Horizontal, m_HorizontalScrollbarVisibility, m_HorizontalScrollbar);
        }

        /// <summary>
        /// 更新滑动条可见性
        /// </summary>
        /// <param name="xScrollingNeeded"></param>
        /// <param name="xAxisEnabled"></param>
        /// <param name="scrollbarVisibility"></param>
        /// <param name="scrollbar"></param>
        private static void UpdateOneScrollbarVisibility(bool xScrollingNeeded, bool xAxisEnabled, ScrollbarVisibility scrollbarVisibility, Scrollbar scrollbar)
        {
            if (scrollbar)
            {
                if (scrollbarVisibility == ScrollbarVisibility.Permanent)
                {
                    if (scrollbar.gameObject.activeSelf != xAxisEnabled)
                        scrollbar.gameObject.SetActive(xAxisEnabled);
                }
                else
                {
                    //只要不是永久显示，那么只要没有滑动，就会隐藏
                    if (scrollbar.gameObject.activeSelf != xScrollingNeeded)
                        scrollbar.gameObject.SetActive(xScrollingNeeded);
                }
            }
        }

        /// <summary>
        /// 更新进度条布局，在垂直布局重建的时候设置
        /// </summary>
        void UpdateScrollbarLayout()
        {
            if (m_VSliderExpand && m_HorizontalScrollbar)
            {
                m_Tracker.Add(this, m_HorizontalScrollbarRect,
                    DrivenTransformProperties.AnchorMinX |
                    DrivenTransformProperties.AnchorMaxX |
                    DrivenTransformProperties.SizeDeltaX |
                    DrivenTransformProperties.AnchoredPositionX);
                m_HorizontalScrollbarRect.anchorMin = new Vector2(0, m_HorizontalScrollbarRect.anchorMin.y);
                m_HorizontalScrollbarRect.anchorMax = new Vector2(1, m_HorizontalScrollbarRect.anchorMax.y);
                m_HorizontalScrollbarRect.anchoredPosition = new Vector2(0, m_HorizontalScrollbarRect.anchoredPosition.y);
                //如果有垂直滑动条，那么水平滑动条的右侧大小要小一点
                if (vScrollingNeeded)
                    m_HorizontalScrollbarRect.sizeDelta = new Vector2(-(m_VSliderWidth + m_VerticalScrollbarSpacing), m_HorizontalScrollbarRect.sizeDelta.y);
                else
                    m_HorizontalScrollbarRect.sizeDelta = new Vector2(0, m_HorizontalScrollbarRect.sizeDelta.y);
            }

            if (m_HSliderExpand && m_VerticalScrollbar)
            {
                m_Tracker.Add(this, m_VerticalScrollbarRect,
                    DrivenTransformProperties.AnchorMinY |
                    DrivenTransformProperties.AnchorMaxY |
                    DrivenTransformProperties.SizeDeltaY |
                    DrivenTransformProperties.AnchoredPositionY);
                m_VerticalScrollbarRect.anchorMin = new Vector2(m_VerticalScrollbarRect.anchorMin.x, 0);
                m_VerticalScrollbarRect.anchorMax = new Vector2(m_VerticalScrollbarRect.anchorMax.x, 1);
                m_VerticalScrollbarRect.anchoredPosition = new Vector2(m_VerticalScrollbarRect.anchoredPosition.x, 0);
                //如果有水平滑动条，那么垂直滑动条的下方大小要小一点
                if (hScrollingNeeded)
                    m_VerticalScrollbarRect.sizeDelta = new Vector2(m_VerticalScrollbarRect.sizeDelta.x, -(m_HSliderHeight + m_HorizontalScrollbarSpacing));
                else
                    m_VerticalScrollbarRect.sizeDelta = new Vector2(m_VerticalScrollbarRect.sizeDelta.x, 0);
            }
        }

        /// <summary>
        /// Calculate the bounds the ScrollRect should be using.
        /// 重新计算各类Bound大小，以便于ScrollRect滚动计算中使用
        /// </summary>
        protected void UpdateBounds()
        {
            //viewport的bound就是自己坐标下的大小和位置
            m_ViewBounds = new Bounds(viewRect.rect.center, viewRect.rect.size);
            //content的Bound要转换为它在viewport下的bound
            m_ContentBounds = GetBounds();

            if (m_Content == null)
                return;

            Vector3 contentSize = m_ContentBounds.size;
            Vector3 contentPos = m_ContentBounds.center;
            var contentPivot = m_Content.pivot;
            //如果Content小于viewport，那么它的bound扩展到跟viewport一样大小，以保证滑动功能的正常
            AdjustBounds(ref m_ViewBounds, ref contentPivot, ref contentSize, ref contentPos);
            m_ContentBounds.size = contentSize;
            m_ContentBounds.center = contentPos;

            //如果移动类型是Clamped，需要将Bounds限制在viewport内
            if (movementType == MovementType.Clamped)
            {
                // Adjust content so that content bounds bottom (right side) is never higher (to the left) than the view bounds bottom (right side).
                // top (left side) is never lower (to the right) than the view bounds top (left side).
                // All this can happen if content has shrunk.
                // This works because content size is at least as big as view size (because of the call to InternalUpdateBounds above).
                Vector2 delta = Vector2.zero;
                if (m_ViewBounds.max.x > m_ContentBounds.max.x)
                {
                    delta.x = Math.Min(m_ViewBounds.min.x - m_ContentBounds.min.x, m_ViewBounds.max.x - m_ContentBounds.max.x);
                }
                else if (m_ViewBounds.min.x < m_ContentBounds.min.x)
                {
                    delta.x = Math.Max(m_ViewBounds.min.x - m_ContentBounds.min.x, m_ViewBounds.max.x - m_ContentBounds.max.x);
                }

                if (m_ViewBounds.min.y < m_ContentBounds.min.y)
                {
                    delta.y = Math.Max(m_ViewBounds.min.y - m_ContentBounds.min.y, m_ViewBounds.max.y - m_ContentBounds.max.y);
                }
                else if (m_ViewBounds.max.y > m_ContentBounds.max.y)
                {
                    delta.y = Math.Min(m_ViewBounds.min.y - m_ContentBounds.min.y, m_ViewBounds.max.y - m_ContentBounds.max.y);
                }
                if (delta.sqrMagnitude > float.Epsilon)
                {
                    contentPos = m_Content.anchoredPosition + delta;
                    if (!m_Horizontal)
                        contentPos.x = m_Content.anchoredPosition.x;
                    if (!m_Vertical)
                        contentPos.y = m_Content.anchoredPosition.y;
                    //TODO 这里再执行应该没什么用了
                    AdjustBounds(ref m_ViewBounds, ref contentPivot, ref contentSize, ref contentPos);
                }
            }
        }

        /// <summary>
        ///当content的bound小于viewport的大小时，将其扩展到跟viewport一样大小、并将它的位置逻辑位置移动到中心
        /// </summary>
        /// <param name="viewBounds"></param>
        /// <param name="contentPivot"></param>
        /// <param name="contentSize"></param>
        /// <param name="contentPos"></param>
        internal static void AdjustBounds(ref Bounds viewBounds, ref Vector2 contentPivot, ref Vector3 contentSize, ref Vector3 contentPos)
        {
            // Make sure content bounds are at least as large as view by adding padding if not.
            // One might think at first that if the content is smaller than the view, scrolling should be allowed.
            // However, that's not how scroll views normally work.
            // Scrolling is *only* possible when content is *larger* than view.
            // We use the pivot of the content rect to decide in which directions the content bounds should be expanded.
            // E.g. if pivot is at top, bounds are expanded downwards.
            // This also works nicely when ContentSizeFitter is used on the content.
            Vector3 excess = viewBounds.size - contentSize;
            if (excess.x > 0)
            {
                contentPos.x -= excess.x * (contentPivot.x - 0.5f);
                contentSize.x = viewBounds.size.x;
            }
            if (excess.y > 0)
            {
                contentPos.y -= excess.y * (contentPivot.y - 0.5f);
                contentSize.y = viewBounds.size.y;
            }
        }

        //世界坐标下，Content的4个角的世界坐标位置；用来进行缓存并进行后续计算
        private readonly Vector3[] m_Corners = new Vector3[4];
        
        /// <summary>
        /// 计算Content区域的Bound大小
        /// </summary>
        /// <returns></returns>
        private Bounds GetBounds()
        {
            //没有Content，那么Bound大小是0
            if (m_Content == null)
                return new Bounds();
            //获取世界坐标下，Content的4个角的位置
            m_Content.GetWorldCorners(m_Corners);
            //获取ViewRect的世界坐标转本地坐标的矩阵
            var viewWorldToLocalMatrix = viewRect.worldToLocalMatrix;
            //计算最终的Content的Bound
            return InternalGetBounds(m_Corners, ref viewWorldToLocalMatrix);
        }

        /// <summary>
        /// 传入的是Content的4个角的世界坐标，和Viewport的世界坐标转本地坐标的矩阵
        /// 计算最终的Content在Viewport下的Bound
        /// </summary>
        /// <param name="corners"></param>
        /// <param name="viewWorldToLocalMatrix"></param>
        /// <returns></returns>
        internal static Bounds InternalGetBounds(Vector3[] corners, ref Matrix4x4 viewWorldToLocalMatrix)
        {
            var vMin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            var vMax = new Vector3(float.MinValue, float.MinValue, float.MinValue);

            for (int j = 0; j < 4; j++)
            {
                Vector3 v = viewWorldToLocalMatrix.MultiplyPoint3x4(corners[j]);
                //Vector3.Min意思是混合2个点，取2个点在xyz每个位置上最小的，组成一个新的Vector3；Vector3.Max同理
                vMin = Vector3.Min(v, vMin);
                vMax = Vector3.Max(v, vMax);
            }

            //这里是使用vMin先生成一个退化的大小为0的Bound，然后使用Encapsulate方法，它会将Bound扩展到vMax区域，然后重新计算center和size。所以这里是没有错误的。
            var bounds = new Bounds(vMin, Vector3.zero);
            bounds.Encapsulate(vMax);
            return bounds;
        }

        /// <summary>
        /// 传入一个变更的Delta的Position，然后根据各类Bound大小、滚动方式等，修正最终的delta值
        /// 返回的结果是为了将Content限制在Viewport内，需要拉回的Delta值
        /// </summary>
        /// <param name="delta"></param>
        /// <returns></returns>
        private Vector2 CalculateOffset(Vector2 delta)
        {
            return InternalCalculateOffset(ref m_ViewBounds, ref m_ContentBounds, m_Horizontal, m_Vertical, m_MovementType, ref delta);
        }

        /// <summary>
        /// 根据Viewport、Content的Bound大小，滚动方式，移动方式，对最终的delta值进行修正
        /// 得到的结果是将Content限制在Viewport内，需要拉回的Delta值。
        /// </summary>
        /// <param name="viewBounds"></param>
        /// <param name="contentBounds"></param>
        /// <param name="horizontal"></param>
        /// <param name="vertical"></param>
        /// <param name="movementType"></param>
        /// <param name="delta"></param>
        /// <returns></returns>
        internal static Vector2 InternalCalculateOffset(ref Bounds viewBounds, ref Bounds contentBounds, bool horizontal, bool vertical, MovementType movementType, ref Vector2 delta)
        {
            Vector2 offset = Vector2.zero;
            //如果是不限制的移动方式，那么不修正，因为可以无限移动
            if (movementType == MovementType.Unrestricted)
                return offset;

            Vector2 min = contentBounds.min;
            Vector2 max = contentBounds.max;

            // min/max offset extracted to check if approximately 0 and avoid recalculating layout every frame (case 1010178)

            if (horizontal)
            {
                min.x += delta.x;
                max.x += delta.x;

                //用Viewport的Bound去减去经过delta移动后Content的bound，就能得出在Viewport空间下的相对移动值
                float maxOffset = viewBounds.max.x - max.x;
                float minOffset = viewBounds.min.x - min.x;

                //过小的移动忽略，防止每帧更新
                //这个计算是计算出将Content限制在Viewport内，需要偏移的x值
                if (minOffset < -0.001f)
                    offset.x = minOffset;
                else if (maxOffset > 0.001f)
                    offset.x = maxOffset;
            }

            if (vertical)
            {
                min.y += delta.y;
                max.y += delta.y;

                float maxOffset = viewBounds.max.y - max.y;
                float minOffset = viewBounds.min.y - min.y;

                if (maxOffset > 0.001f)
                    offset.y = maxOffset;
                else if (minOffset < -0.001f)
                    offset.y = minOffset;
            }

            return offset;
        }

        /// <summary>
        /// Override to alter or add to the code that keeps the appearance of the scroll rect synced with its data.
        /// 设置布局重建脏标记
        /// </summary>
        protected void SetDirty()
        {
            if (!IsActive())
                return;

            LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
        }

        /// <summary>
        /// Override to alter or add to the code that caches data to avoid repeated heavy operations.
        /// 重刷滚动区域自身
        /// </summary>
        protected void SetDirtyCaching()
        {
            if (!IsActive())
                return;

            CanvasUpdateRegistry.RegisterCanvasElementForLayoutRebuild(this);
            LayoutRebuilder.MarkLayoutForRebuild(rectTransform);

            m_ViewRect = null;
        }

        #if UNITY_EDITOR
        protected override void OnValidate()
        {
            SetDirtyCaching();
        }

        #endif
    }
}
