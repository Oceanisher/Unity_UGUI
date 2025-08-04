using System;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.Pool;

namespace UnityEngine.UI
{
    [AddComponentMenu("UI/Rect Mask 2D", 14)]
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    /// <summary>
    /// A 2D rectangular mask that allows for clipping / masking of areas outside the mask.
    /// 矩形区域裁剪接口
    /// 不是通过模板测试，而是通过设置矩形区域，然后超出区域外的元素透明度设置为0.
    /// 然后shader会丢弃透明度小于0.001的片元，从而实现遮罩效果
    /// </summary>
    /// <remarks>
    /// The RectMask2D behaves in a similar way to a standard Mask component. It differs though in some of the restrictions that it has.
    /// A RectMask2D:
    /// *Only works in the 2D plane
    /// *Requires elements on the mask to be coplanar.
    /// *Does not require stencil buffer / extra draw calls
    /// *Requires fewer draw calls
    /// *Culls elements that are outside the mask area.
    /// </remarks>
    public class RectMask2D : UIBehaviour, IClipper, ICanvasRaycastFilter
    {
        [NonSerialized]
        private readonly RectangularVertexClipper m_VertexClipper = new RectangularVertexClipper();

        [NonSerialized]
        private RectTransform m_RectTransform;

        [NonSerialized]
        //存储所有子节点的带有MaskableGraphic的可裁剪子节点，与m_ClipTargets互斥
        //如果子节点和该RectMask2D之间还有其他RectMask2D,那么子节点将存储到其他RectMask2D中，也就是说该列表只存储中间没有其他RectMask2D的所有子节点
        private HashSet<MaskableGraphic> m_MaskableTargets = new HashSet<MaskableGraphic>();

        [NonSerialized]
        //存储所有子节点的带有IClippable、且不是MaskableGraphic的可裁剪子节点，与m_MaskableTargets互斥
        //如果子节点和该RectMask2D之间还有其他RectMask2D,那么子节点将存储到其他RectMask2D中，也就是说该列表只存储中间没有其他RectMask2D的所有子节点
        private HashSet<IClippable> m_ClipTargets = new HashSet<IClippable>();

        [NonSerialized]
        //是否需要重新计算裁切区域
        private bool m_ShouldRecalculateClipRects;

        [NonSerialized]
        //用来存储该元素所有的RectMask2D上级、包含自己，每次需要重新计算裁切区域时，该List会重新被赋值
        private List<RectMask2D> m_Clippers = new List<RectMask2D>();

        [NonSerialized]
        //上次裁切区域的缓存，用来减少一些计算
        private Rect m_LastClipRectCanvasSpace;
        [NonSerialized]
        private bool m_ForceClip;

        [SerializeField]
        private Vector4 m_Padding = new Vector4();

        /// <summary>
        /// Padding to be applied to the masking
        /// Rect内部边框，相当于在不改变RectMask2D的Trs大小的情况下，动态缩放Mask区域。
        /// 比如如果 X = 100，那么原本子元素在到达父节点左边界时才会Mask，现在距离左边界100距离就会被Mask
        /// 
        /// X = Left
        /// Y = Bottom
        /// Z = Right
        /// W = Top
        /// </summary>
        public Vector4 padding
        {
            get { return m_Padding; }
            set
            {
                m_Padding = value;
                MaskUtilities.Notify2DMaskStateChanged(this);
            }
        }

        [SerializeField]
        //边界的柔和程度
        //边界外还是直接Mask的，只是边界内部进行柔和
        //通过softness，设置的时候会强制限制为非负值
        private Vector2Int m_Softness;

        /// <summary>
        /// The softness to apply to the horizontal and vertical axis.
        /// 边界的柔和程度
        /// 设置的时候会强制限制为非负值
        /// </summary>
        public Vector2Int softness
        {
            get { return m_Softness;  }
            set
            {
                m_Softness.x = Mathf.Max(0, value.x);
                m_Softness.y = Mathf.Max(0, value.y);
                MaskUtilities.Notify2DMaskStateChanged(this);
            }
        }

        /// <remarks>
        /// Returns a non-destroyed instance or a null reference.
        /// 最外层的RootCanvas节点，必须是Active的
        /// </remarks>
        [NonSerialized] private Canvas m_Canvas;
        internal Canvas Canvas
        {
            get
            {
                if (m_Canvas == null)
                {
                    var list = ListPool<Canvas>.Get();
                    gameObject.GetComponentsInParent(false, list);
                    if (list.Count > 0)
                        m_Canvas = list[list.Count - 1];
                    else
                        m_Canvas = null;
                    ListPool<Canvas>.Release(list);
                }

                return m_Canvas;
            }
        }

        /// <summary>
        /// Get the Rect for the mask in canvas space.
        /// 获取本Trs在Canvas中的相对矩形区域
        /// </summary>
        public Rect canvasRect
        {
            get
            {
                return m_VertexClipper.GetCanvasRect(rectTransform, Canvas);
            }
        }

        /// <summary>
        /// Helper function to get the RectTransform for the mask.
        /// </summary>
        public RectTransform rectTransform
        {
            get { return m_RectTransform ?? (m_RectTransform = GetComponent<RectTransform>()); }
        }

        protected RectMask2D()
        {}

        protected override void OnEnable()
        {
            base.OnEnable();
            m_ShouldRecalculateClipRects = true;
            //注册到裁剪中心
            ClipperRegistry.Register(this);
            //发送裁剪状态变化通知
            MaskUtilities.Notify2DMaskStateChanged(this);
        }

        protected override void OnDisable()
        {
            // we call base OnDisable first here
            // as we need to have the IsActive return the
            // correct value when we notify the children
            // that the mask state has changed.
            base.OnDisable();
            m_ClipTargets.Clear();
            m_MaskableTargets.Clear();
            m_Clippers.Clear();
            ClipperRegistry.Disable(this);
            MaskUtilities.Notify2DMaskStateChanged(this);
        }

        protected override void OnDestroy()
        {
            ClipperRegistry.Unregister(this);
            base.OnDestroy();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            m_ShouldRecalculateClipRects = true;

            // Dont allow negative softness.
            m_Softness.x = Mathf.Max(0, m_Softness.x);
            m_Softness.y = Mathf.Max(0, m_Softness.y);

            if (!IsActive())
                return;

            MaskUtilities.Notify2DMaskStateChanged(this);
        }

#endif

        /// <summary>
        /// 射线是否能打到该元素上
        /// </summary>
        /// <param name="sp"></param>
        /// <param name="eventCamera"></param>
        /// <returns></returns>
        public virtual bool IsRaycastLocationValid(Vector2 sp, Camera eventCamera)
        {
            if (!isActiveAndEnabled)
                return true;

            //判断射线是否能够打到元素上，把padding信息穿进去当做offset，也就是说padding之外的不但会被裁剪、也会被射线无视
            return RectTransformUtility.RectangleContainsScreenPoint(rectTransform, sp, eventCamera, m_Padding);
        }

        //用来存储该元素在根Canvas中的4个角的本地坐标
        private Vector3[] m_Corners = new Vector3[4];

        //该元素在根Canvas中的Rect区域，是个本地坐标的Rect
        private Rect rootCanvasRect
        {
            get
            {
                rectTransform.GetWorldCorners(m_Corners);

                if (!ReferenceEquals(Canvas, null))
                {
                    Canvas rootCanvas = Canvas.rootCanvas;
                    for (int i = 0; i < 4; ++i)
                        m_Corners[i] = rootCanvas.transform.InverseTransformPoint(m_Corners[i]);
                }

                return new Rect(m_Corners[0].x, m_Corners[0].y, m_Corners[2].x - m_Corners[0].x, m_Corners[2].y - m_Corners[0].y);
            }
        }

        /// <summary>
        /// 裁切，实际执行裁切的方法
        /// </summary>
        public virtual void PerformClipping()
        {
            //没有Canvas的元素不裁切
            if (ReferenceEquals(Canvas, null))
            {
                return;
            }

            //TODO See if an IsActive() test would work well here or whether it might cause unexpected side effects (re case 776771)

            // if the parents are changed
            // or something similar we
            // do a recalculate here
            //重新计算裁切区域
            if (m_ShouldRecalculateClipRects)
            {
                //找出所有本RectMask2D、以及它所有的上级RectMask2D，除去Canvas是OverrideSorting的
                MaskUtilities.GetRectMasksForClip(this, m_Clippers);
                m_ShouldRecalculateClipRects = false;
            }

            // get the compound rects from
            // the clippers that are valid
            //计算所有RectMask2D都重叠的区域
            bool validRect = true;
            Rect clipRect = Clipping.FindCullAndClipWorldRect(m_Clippers, out validRect);

            // If the mask is in ScreenSpaceOverlay/Camera render mode, its content is only rendered when its rect
            // overlaps that of the root canvas.
            //是否要进行剔除，也就是完全不渲染了
            //只有当Canvas的渲染模式是Camera/Overlay、并且裁切区域和根Canvas没有交集的时候才会被剔除掉
            //如果Canvas是WorldSpace，那么不会剔除掉
            RenderMode renderMode = Canvas.rootCanvas.renderMode;
            bool maskIsCulled =
                (renderMode == RenderMode.ScreenSpaceCamera || renderMode == RenderMode.ScreenSpaceOverlay) &&
                !clipRect.Overlaps(rootCanvasRect, true);

            //如果该元素需要被剔除掉，那么clipRect区域大小置为0、validRect置为false
            if (maskIsCulled)
            {
                // Children are only displayed when inside the mask. If the mask is culled, then the children
                // inside the mask are also culled. In that situation, we pass an invalid rect to allow callees
                // to avoid some processing.
                clipRect = Rect.zero;
                validRect = false;
            }

            //如果裁切区域有变更
            if (clipRect != m_LastClipRectCanvasSpace)
            {
                foreach (IClippable clipTarget in m_ClipTargets)
                {
                    clipTarget.SetClipRect(clipRect, validRect);
                }
                //MaskableGraphic 还要进行剔除
                foreach (MaskableGraphic maskableTarget in m_MaskableTargets)
                {
                    maskableTarget.SetClipRect(clipRect, validRect);
                    maskableTarget.Cull(clipRect, validRect);
                }
            }
            //或者强制裁切
            else if (m_ForceClip)
            {
                foreach (IClippable clipTarget in m_ClipTargets)
                {
                    clipTarget.SetClipRect(clipRect, validRect);
                }

                //MaskableGraphic 还要进行剔除
                foreach (MaskableGraphic maskableTarget in m_MaskableTargets)
                {
                    maskableTarget.SetClipRect(clipRect, validRect);

                    //只有元素发生了变更导致几何图形位置发生变更，才会执行剔除操作
                    if (maskableTarget.canvasRenderer.hasMoved)
                        maskableTarget.Cull(clipRect, validRect);
                }
            }
            //否则，只进行剔除设置
            else
            {
                foreach (MaskableGraphic maskableTarget in m_MaskableTargets)
                {
                    //Case 1170399 - hasMoved is not a valid check when animating on pivot of the object
                    maskableTarget.Cull(clipRect, validRect);
                }
            }

            m_LastClipRectCanvasSpace = clipRect;
            m_ForceClip = false;

            UpdateClipSoftness();
        }

        /// <summary>
        /// 更新裁切边缘柔和度
        /// 在处理裁切后被调用
        /// </summary>
        public virtual void UpdateClipSoftness()
        {
            if (ReferenceEquals(Canvas, null))
            {
                return;
            }

            foreach (IClippable clipTarget in m_ClipTargets)
            {
                clipTarget.SetClipSoftness(m_Softness);
            }

            foreach (MaskableGraphic maskableTarget in m_MaskableTargets)
            {
                maskableTarget.SetClipSoftness(m_Softness);
            }
        }

        /// <summary>
        /// Add a IClippable to be tracked by the mask.
        /// 子节点主动调用
        /// 在子节点找到父节点RectMask2D之后，将自己加入到父节点的裁剪管理中
        /// </summary>
        /// <param name="clippable">Add the clippable object for this mask</param>
        public void AddClippable(IClippable clippable)
        {
            if (clippable == null)
                return;
            //每有一个子节点加入，都需要重新计算裁切
            m_ShouldRecalculateClipRects = true;
            MaskableGraphic maskable = clippable as MaskableGraphic;

            //根据子节点是不是MaskableGraphic，决定加到哪个队列中
            if (maskable == null)
                m_ClipTargets.Add(clippable);
            else
                m_MaskableTargets.Add(maskable);

            m_ForceClip = true;
        }

        /// <summary>
        /// Remove an IClippable from being tracked by the mask.
        /// 子节点主动调用
        /// 当子节点切换父节点、或者父节点失效等，子节点把自己从父节点的裁切中拿出来
        /// </summary>
        /// <param name="clippable">Remove the clippable object from this mask</param>
        public void RemoveClippable(IClippable clippable)
        {
            if (clippable == null)
                return;

            m_ShouldRecalculateClipRects = true;
            clippable.SetClipRect(new Rect(), false);

            MaskableGraphic maskable = clippable as MaskableGraphic;

            if (maskable == null)
                m_ClipTargets.Remove(clippable);
            else
                m_MaskableTargets.Remove(maskable);

            m_ForceClip = true;
        }

        protected override void OnTransformParentChanged()
        {
            m_Canvas = null;
            base.OnTransformParentChanged();
            m_ShouldRecalculateClipRects = true;
        }

        protected override void OnCanvasHierarchyChanged()
        {
            m_Canvas = null;
            base.OnCanvasHierarchyChanged();
            m_ShouldRecalculateClipRects = true;
        }
    }
}
