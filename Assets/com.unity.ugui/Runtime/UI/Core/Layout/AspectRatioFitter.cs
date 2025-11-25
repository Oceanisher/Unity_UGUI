using UnityEngine.EventSystems;

namespace UnityEngine.UI
{
    [AddComponentMenu("Layout/Aspect Ratio Fitter", 142)]
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    [DisallowMultipleComponent]
    /// <summary>
    /// Resizes a RectTransform to fit a specified aspect ratio.
    /// 宽高比适配组件
    /// 用于保持固定宽高比，防止拉伸；比如一些Logo、视频等，保持原有宽高比
    /// </summary>
    public class AspectRatioFitter : UIBehaviour, ILayoutSelfController
    {
        /// <summary>
        /// Specifies a mode to use to enforce an aspect ratio.
        /// </summary>
        public enum AspectMode
        {
            /// <summary>
            /// The aspect ratio is not enforced
            /// 不强制保持宽高比
            /// </summary>
            None,
            /// <summary>
            /// Changes the height of the rectangle to match the aspect ratio.
            /// 宽度控制高度
            /// </summary>
            WidthControlsHeight,
            /// <summary>
            /// Changes the width of the rectangle to match the aspect ratio.
            /// 高度控制宽度
            /// </summary>
            HeightControlsWidth,
            /// <summary>
            /// Sizes the rectangle such that it's fully contained within the parent rectangle.
            /// 适配父节点，能够完全在父节点内部，可能有留白
            /// </summary>
            FitInParent,
            /// <summary>
            /// Sizes the rectangle such that the parent rectangle is fully contained within.
            /// 铺满父节点，没有留白、可能会超出父节点
            /// </summary>
            EnvelopeParent
        }

        //宽高比的方式
        [SerializeField] private AspectMode m_AspectMode = AspectMode.None;

        /// <summary>
        /// The mode to use to enforce the aspect ratio.
        /// 宽高比的方式
        /// </summary>
        public AspectMode aspectMode { get { return m_AspectMode; } set { if (SetPropertyUtility.SetStruct(ref m_AspectMode, value)) SetDirty(); } }

        //宽高比
        [SerializeField] private float m_AspectRatio = 1;

        /// <summary>
        /// The aspect ratio to enforce. This means width divided by height.
        /// 宽高比数据，是宽除以高
        /// </summary>
        public float aspectRatio { get { return m_AspectRatio; } set { if (SetPropertyUtility.SetStruct(ref m_AspectRatio, value)) SetDirty(); } }

        [System.NonSerialized]
        private RectTransform m_Rect;

        // This "delayed" mechanism is required for case 1014834.
        //延迟设置脏标记，用于Update中去设置Dirty，目前用于编辑器模式下
        private bool m_DelayedSetDirty = false;

        //Does the gameobject has a parent for reference to enable FitToParent/EnvelopeParent modes.
        //是否有父节点
        private bool m_DoesParentExist = false;

        private RectTransform rectTransform
        {
            get
            {
                if (m_Rect == null)
                    m_Rect = GetComponent<RectTransform>();
                return m_Rect;
            }
        }

        // field is never assigned warning
        #pragma warning disable 649
        private DrivenRectTransformTracker m_Tracker;
        #pragma warning restore 649

        protected AspectRatioFitter() {}

        protected override void OnEnable()
        {
            base.OnEnable();
            m_DoesParentExist = rectTransform.parent ? true : false;
            SetDirty();
        }

        protected override void Start()
        {
            base.Start();
            //Disable the component if the aspect mode is not valid or the object state/setup is not supported with AspectRatio setup.
            if (!IsComponentValidOnObject() || !IsAspectModeValid())
                this.enabled = false;
        }

        protected override void OnDisable()
        {
            m_Tracker.Clear();
            LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
            base.OnDisable();
        }

        protected override void OnTransformParentChanged()
        {
            base.OnTransformParentChanged();

            m_DoesParentExist = rectTransform.parent ? true : false;
            SetDirty();
        }

        /// <summary>
        /// Update the rect based on the delayed dirty.
        /// Got around issue of calling onValidate from OnEnable function.
        /// </summary>
        protected virtual void Update()
        {
            if (m_DelayedSetDirty)
            {
                m_DelayedSetDirty = false;
                SetDirty();
            }
        }

        /// <summary>
        /// Function called when this RectTransform or parent RectTransform has changed dimensions.
        /// </summary>
        protected override void OnRectTransformDimensionsChange()
        {
            UpdateRect();
        }

        /// <summary>
        /// 重新设置Rect
        /// </summary>
        private void UpdateRect()
        {
            if (!IsActive() || !IsComponentValidOnObject())
                return;

            m_Tracker.Clear();

            switch (m_AspectMode)
            {
#if UNITY_EDITOR
                case AspectMode.None:
                {
                    if (!Application.isPlaying)
                        m_AspectRatio = Mathf.Clamp(rectTransform.rect.width / rectTransform.rect.height, 0.001f, 1000f);

                    break;
                }
#endif
                //高度控制宽度
                case AspectMode.HeightControlsWidth:
                {
                    m_Tracker.Add(this, rectTransform, DrivenTransformProperties.SizeDeltaX);
                    //只设置宽度，高度可以随着父节点变化；并且只改大小、不修改位置等其他数据
                    rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, rectTransform.rect.height * m_AspectRatio);
                    break;
                }
                //宽度控制高度
                case AspectMode.WidthControlsHeight:
                {
                    m_Tracker.Add(this, rectTransform, DrivenTransformProperties.SizeDeltaY);
                    //只设置高度，宽度可以随着父节点变化；并且只改大小、不修改位置等其他数据
                    rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, rectTransform.rect.width / m_AspectRatio);
                    break;
                }
                //适应父容器
                case AspectMode.FitInParent:
                case AspectMode.EnvelopeParent:
                {
                    if (!DoesParentExists())
                        break;

                    m_Tracker.Add(this, rectTransform,
                        DrivenTransformProperties.Anchors |
                        DrivenTransformProperties.AnchoredPosition |
                        DrivenTransformProperties.SizeDeltaX |
                        DrivenTransformProperties.SizeDeltaY);

                    //适应父容器，会把锚点对齐方式设置为全扩展、并且anchoredPosition归零
                    rectTransform.anchorMin = Vector2.zero;
                    rectTransform.anchorMax = Vector2.one;
                    rectTransform.anchoredPosition = Vector2.zero;

                    Vector2 sizeDelta = Vector2.zero;
                    //获取父容器大小
                    Vector2 parentSize = GetParentSize();
                    //根据父容器大小，设置本元素的大小
                    //对于子元素的宽高比，父容器宽度更大、并且是铺满父容器模式，或者父容器宽度更小、并且是适配父容器模式，那么就是宽度铺满、高度用宽度算出来
                    if ((parentSize.y * aspectRatio < parentSize.x) ^ (m_AspectMode == AspectMode.FitInParent))
                    {
                        sizeDelta.y = GetSizeDeltaToProduceSize(parentSize.x / aspectRatio, 1);
                    }
                    //对于子元素的宽高比，如果父容器宽度更大、并且是适配父容器模式，或者容器宽度更小、并且是铺满父容器模式，那么子元素的高度跟父容器一样、宽度用高度的算出来
                    else
                    {
                        sizeDelta.x = GetSizeDeltaToProduceSize(parentSize.y * aspectRatio, 0);
                    }
                    //只需要设置一个轴大小的原因是，这里的对齐方式是水平竖直都是stretch，所以sizeDelta是本元素大小与4锚点矩形大小的差值，由于有一个轴需要铺满，所以这个轴是的sizeDelta就是0
                    rectTransform.sizeDelta = sizeDelta;

                    break;
                }
            }
        }

        /// <summary>
        /// 获取这个轴向上的sizeDelta值
        /// 由于调用的时候，元素的对齐方式都是stretch，所以sizeDelta是本元素大小与4锚点矩形大小的差值
        /// </summary>
        /// <param name="size"></param>
        /// <param name="axis"></param>
        /// <returns></returns>
        private float GetSizeDeltaToProduceSize(float size, int axis)
        {
            //这里的乘法没必要，因为调用的地方已经提前把anchorMax设置为了1，anchorMin设置为了0
            return size - GetParentSize()[axis] * (rectTransform.anchorMax[axis] - rectTransform.anchorMin[axis]);
        }

        /// <summary>
        /// 获取父容器大小
        /// </summary>
        /// <returns></returns>
        private Vector2 GetParentSize()
        {
            RectTransform parent = rectTransform.parent as RectTransform;
            return !parent ? Vector2.zero : parent.rect.size;
        }

        /// <summary>
        /// Method called by the layout system. Has no effect
        /// </summary>
        public virtual void SetLayoutHorizontal() {}

        /// <summary>
        /// Method called by the layout system. Has no effect
        /// </summary>
        public virtual void SetLayoutVertical() {}

        /// <summary>
        /// Mark the AspectRatioFitter as dirty.
        /// 设置脏标记
        /// </summary>
        protected void SetDirty()
        {
            UpdateRect();
        }

        public bool IsComponentValidOnObject()
        {
            Canvas canvas = gameObject.GetComponent<Canvas>();
            if (canvas && canvas.isRootCanvas && canvas.renderMode != RenderMode.WorldSpace)
            {
                return false;
            }
            return true;
        }

        public bool IsAspectModeValid()
        {
            if (!DoesParentExists() && (aspectMode == AspectMode.EnvelopeParent || aspectMode == AspectMode.FitInParent))
                return false;

            return true;
        }

        private bool DoesParentExists()
        {
            return m_DoesParentExist;
        }

    #if UNITY_EDITOR
        protected override void OnValidate()
        {
            m_AspectRatio = Mathf.Clamp(m_AspectRatio, 0.001f, 1000f);
            m_DelayedSetDirty = true;
        }

    #endif
    }
}
