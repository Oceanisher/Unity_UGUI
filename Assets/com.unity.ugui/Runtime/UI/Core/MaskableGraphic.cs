using System;
using UnityEngine.Events;
using UnityEngine.Rendering;

namespace UnityEngine.UI
{
    /// <summary>
    /// A Graphic that is capable of being masked out.
    /// 可以被Mask的图形组件
    /// IClippable接口的实现，使其可以被矩形裁切，从而实现RectMask2D的功能
    /// IMaskable接口的视线，使其可以进行模板测试，从而实现更复杂的遮罩Mask效果
    /// </summary>
    public abstract class MaskableGraphic : Graphic, IClippable, IMaskable, IMaterialModifier
    {
        [NonSerialized]
        //是否需要进行模板测试的重新计算，与图形、布局重建类似，也是在下一帧执行
        protected bool m_ShouldRecalculateStencil = true;

        [NonSerialized]
        //经过模板遮罩处理的后的材质-Mask组件使用
        protected Material m_MaskMaterial;

        [NonSerialized]
        //带有RectMask2D的父节点
        private RectMask2D m_ParentMask;

        // m_Maskable is whether this graphic is allowed to be masked or not. It has the matching public property maskable.
        // The default for m_Maskable is true, so graphics under a mask are masked out of the box.
        // The maskable property can be turned off from script by the user if masking is not desired.
        // m_IncludeForMasking is whether we actually consider this graphic for masking or not - this is an implementation detail.
        // m_IncludeForMasking should only be true if m_Maskable is true AND a parent of the graphic has an IMask component.
        // Things would still work correctly if m_IncludeForMasking was always true when m_Maskable is, but performance would suffer.
        [SerializeField]
        private bool m_Maskable = true;

        private bool m_IsMaskingGraphic = false;

        [NonSerialized]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Not used anymore.", true)]
        protected bool m_IncludeForMasking = false;

        [Serializable]
        public class CullStateChangedEvent : UnityEvent<bool> {}

        // Event delegates triggered on click.
        [SerializeField]
        //剔除回调
        private CullStateChangedEvent m_OnCullStateChanged = new CullStateChangedEvent();

        /// <summary>
        /// Callback issued when culling changes.
        /// 剔除回调
        /// 元素被剔除渲染时调用
        /// </summary>
        /// <remarks>
        /// Called whene the culling state of this MaskableGraphic either becomes culled or visible. You can use this to control other elements of your UI as culling happens.
        /// </remarks>
        public CullStateChangedEvent onCullStateChanged
        {
            get { return m_OnCullStateChanged; }
            set { m_OnCullStateChanged = value; }
        }

        /// <summary>
        /// Does this graphic allow masking.
        /// 是否允许图形被遮罩
        /// </summary>
        public bool maskable
        {
            get { return m_Maskable; }
            set
            {
                if (value == m_Maskable)
                    return;
                m_Maskable = value;
                m_ShouldRecalculateStencil = true;
                SetMaterialDirty();
            }
        }


        /// <summary>
        /// Is this graphic the graphic on the same object as a Mask that is enabled.
        /// 该图形是否是用作Mask的遮罩发起者
        /// Mask组件在Enable时会设置该变量为true，也就是可以认为是否有Mask组件
        /// </summary>
        /// <remarks>
        /// If toggled ensure to call MaskUtilities.NotifyStencilStateChanged(this); manually as it changes how stenciles are calculated for this image.
        /// </remarks>
        public bool isMaskingGraphic
        {
            get { return m_IsMaskingGraphic; }
            set
            {
                if (value == m_IsMaskingGraphic)
                    return;

                m_IsMaskingGraphic = value;
            }
        }

        [NonSerialized]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Not used anymore", true)]
        protected bool m_ShouldRecalculate = true;

        [NonSerialized]
        //模板深度值，从上往下，每个带有Mask的深度+1
        //同层Mask一致
        //但是如果有重写排序的Mask，那么它的深度值的计算是从该Mask开始计算
        protected int m_StencilValue;

        /// <summary>
        /// See IMaterialModifier.GetModifiedMaterial
        /// 材质修改
        /// 获取经过模板测试处理后的材质
        /// </summary>
        public virtual Material GetModifiedMaterial(Material baseMaterial)
        {
            var toUse = baseMaterial;

            //如果需要重新进行模板测试
            if (m_ShouldRecalculateStencil)
            {
                if (maskable)
                {
                    //往上找，找到最根节点的Canvas、或者第一个重写了排序的Canvas
                    var rootCanvas = MaskUtilities.FindRootSortOverrideCanvas(transform);
                    //计算模板深度值
                    m_StencilValue = MaskUtilities.GetStencilDepth(transform, rootCanvas);
                }
                else
                    m_StencilValue = 0;

                m_ShouldRecalculateStencil = false;
            }

            // if we have a enabled Mask component then it will
            // generate the mask material. This is an optimization
            // it adds some coupling between components though :(
            //如果有模板深度值、并且元素本身是个裁剪者（有Mask组件、并Active）
            if (m_StencilValue > 0 && !isMaskingGraphic)
            {
                //材质和模板值进行处理，生成新的材质
                var maskMat = StencilMaterial.Add(toUse, (1 << m_StencilValue) - 1, StencilOp.Keep, CompareFunction.Equal, ColorWriteMask.All, (1 << m_StencilValue) - 1, 0);
                StencilMaterial.Remove(m_MaskMaterial);
                m_MaskMaterial = maskMat;
                toUse = m_MaskMaterial;
            }
            return toUse;
        }

        /// <summary>
        /// See IClippable.Cull
        /// 剔除，决定是否把整个元素剔除、不渲染
        /// </summary>
        public virtual void Cull(Rect clipRect, bool validRect)
        {
            //裁切区域与RootCanvas区域没有重叠才行，没有重叠代表该元素不在Canvas的区域范围内，是可以进行完全剔除的
            var cull = !validRect || !clipRect.Overlaps(rootCanvasRect, true);
            UpdateCull(cull);
        }

        /// <summary>
        /// 剔除接口，决定是否需要完全把该元素从渲染队列剔除出去
        /// 决定是否把整个元素裁剪掉、不渲染，使用canvasRenderer.cull
        /// </summary>
        /// <param name="cull"></param>
        private void UpdateCull(bool cull)
        {
            if (canvasRenderer.cull != cull)
            {
                //设置 canvasRenderer 是否进行剔除
                canvasRenderer.cull = cull;
                UISystemProfilerApi.AddMarker("MaskableGraphic.cullingChanged", this);
                m_OnCullStateChanged.Invoke(cull);
                OnCullingChanged();
            }
        }

        /// <summary>
        /// See IClippable.SetClipRect
        /// 设置裁切区域，写进 canvasRenderer 中
        /// </summary>
        public virtual void SetClipRect(Rect clipRect, bool validRect)
        {
            if (validRect)
                canvasRenderer.EnableRectClipping(clipRect);
            else
                canvasRenderer.DisableRectClipping();
        }

        /// <summary>
        /// 设置裁切边缘平滑
        /// </summary>
        /// <param name="clipSoftness"></param>
        public virtual void SetClipSoftness(Vector2 clipSoftness)
        {
            canvasRenderer.clippingSoftness = clipSoftness;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            //enable时，需要重新计算模板测试
            m_ShouldRecalculateStencil = true;
            UpdateClipParent();
            SetMaterialDirty();

            //如果拥有Mask组件，那么要通知模板测试状态发生变更
            if (isMaskingGraphic)
            {
                MaskUtilities.NotifyStencilStateChanged(this);
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            m_ShouldRecalculateStencil = true;
            SetMaterialDirty();
            UpdateClipParent();
            StencilMaterial.Remove(m_MaskMaterial);
            m_MaskMaterial = null;

            if (isMaskingGraphic)
            {
                MaskUtilities.NotifyStencilStateChanged(this);
            }
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            m_ShouldRecalculateStencil = true;
            UpdateClipParent();
            SetMaterialDirty();
        }

#endif

        /// <summary>
        /// 元素的父节点切换，也就是从一个Trs变更到另一个Trs上
        /// 重写了Graphic接口，先执行了Graphic的原有的重建，然后再执行新的内容
        /// OnTransformParentChanged、OnCanvasHierarchyChanged 执行的新内容相同
        /// </summary>
        protected override void OnTransformParentChanged()
        {
            base.OnTransformParentChanged();

            if (!isActiveAndEnabled)
                return;

            m_ShouldRecalculateStencil = true;
            UpdateClipParent();
            SetMaterialDirty();
        }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Not used anymore.", true)]
        public virtual void ParentMaskStateChanged() {}

        /// <summary>
        /// 元素的归属Canvas切换，也就是从一个Canvas变更到另一个Canvas上
        /// 重写了Graphic接口，先执行了Graphic的原有的重建，然后再执行新的内容
        ///
        /// OnTransformParentChanged、OnCanvasHierarchyChanged 执行的新内容相同
        /// </summary>
        /// <returns></returns>
        protected override void OnCanvasHierarchyChanged()
        {
            base.OnCanvasHierarchyChanged();

            if (!isActiveAndEnabled)
                return;

            m_ShouldRecalculateStencil = true;
            UpdateClipParent();
            SetMaterialDirty();
        }

        //用来存储本RectTransform在世界空间中的4个角的坐标
        readonly Vector3[] m_Corners = new Vector3[4];
        //在RootCanvas（最外层的Canvas，如果没有Canvas，那么这里就是世界空间下）空间下的相对坐标的Rect
        private Rect rootCanvasRect
        {
            get
            {
                //获取本RectTransform在世界空间中的4个角的坐标
                rectTransform.GetWorldCorners(m_Corners);

                //如果父Canvas不为空，那么找到Canvas的最外层的RootCanvas，使用它的矩阵计算出4个角在最外层RootCanvas中的本地坐标
                if (canvas)
                {
                    Matrix4x4 mat = canvas.rootCanvas.transform.worldToLocalMatrix;
                    for (int i = 0; i < 4; ++i)
                        m_Corners[i] = mat.MultiplyPoint(m_Corners[i]);
                }

                // bounding box is now based on the min and max of all corners (case 1013182)

                Vector2 min = m_Corners[0];
                Vector2 max = m_Corners[0];
                for (int i = 1; i < 4; i++)
                {
                    min.x = Mathf.Min(m_Corners[i].x, min.x);
                    min.y = Mathf.Min(m_Corners[i].y, min.y);
                    max.x = Mathf.Max(m_Corners[i].x, max.x);
                    max.y = Mathf.Max(m_Corners[i].y, max.y);
                }

                return new Rect(min, max - min);
            }
        }

        /// <summary>
        /// 更新父节点的裁剪，也就是重新计算新的带有RectMask2D的父节点
        /// 然后把自己注册到父节点的裁剪队列中，等待父节点发起裁剪
        /// </summary>
        private void UpdateClipParent()
        {
            //重新往上找父节点的RectMask2D
            var newParent = (maskable && IsActive()) ? MaskUtilities.GetRectMaskForClippable(this) : null;

            // if the new parent is different OR is now inactive
            //如果老父节点不为空、并且新父节点失效或与老父节点不一致，那么取消剔除
            if (m_ParentMask != null && (newParent != m_ParentMask || !newParent.IsActive()))
            {
                m_ParentMask.RemoveClippable(this);
                UpdateCull(false);
            }

            // don't re-add it if the newparent is inactive
            //如果新父节点生效，那么重新加入裁剪
            if (newParent != null && newParent.IsActive())
                newParent.AddClippable(this);

            m_ParentMask = newParent;
        }

        /// <summary>
        /// See IClippable.RecalculateClipping
        /// 重新计算裁剪
        /// 被RectMask2D通知后，调用该接口。目前的实现是重新计算父节点
        /// </summary>
        public virtual void RecalculateClipping()
        {
            UpdateClipParent();
        }

        /// <summary>
        /// See IMaskable.RecalculateMasking
        /// 重新计算模板测试值
        /// 被Mask组件通知后，调用该接口
        /// </summary>
        public virtual void RecalculateMasking()
        {
            // Remove the material reference as either the graphic of the mask has been enable/ disabled.
            // This will cause the material to be repopulated from the original if need be. (case 994413)
            StencilMaterial.Remove(m_MaskMaterial);
            m_MaskMaterial = null;
            m_ShouldRecalculateStencil = true;
            SetMaterialDirty();
        }
    }
}
