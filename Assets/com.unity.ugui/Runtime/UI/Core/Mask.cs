using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace UnityEngine.UI
{
    [AddComponentMenu("UI/Mask", 13)]
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    [DisallowMultipleComponent]
    /// <summary>
    /// A component for masking children elements.
    /// Mask组件
    /// 将模板缓存中的值置为0，来进行模板测试阶段的剔除
    /// 会生成新的材质
    /// </summary>
    /// <remarks>
    /// By using this element any children elements that have masking enabled will mask where a sibling Graphic would write 0 to the stencil buffer.
    /// </remarks>
    public class Mask : UIBehaviour, ICanvasRaycastFilter, IMaterialModifier
    {
        [NonSerialized]
        private RectTransform m_RectTransform;
        public RectTransform rectTransform
        {
            get { return m_RectTransform ?? (m_RectTransform = GetComponent<RectTransform>()); }
        }

        [SerializeField]
        private bool m_ShowMaskGraphic = true;

        /// <summary>
        /// Show the graphic that is associated with the Mask render area.
        /// 是否显示Mask使用的图形
        /// 比如如果Image组件挂载了Mask组件，那么仅当这个选项勾选的时候，才会把图片展示出来
        /// </summary>
        public bool showMaskGraphic
        {
            get { return m_ShowMaskGraphic; }
            set
            {
                if (m_ShowMaskGraphic == value)
                    return;

                m_ShowMaskGraphic = value;
                if (graphic != null)
                    graphic.SetMaterialDirty();
            }
        }

        [NonSerialized]
        private Graphic m_Graphic;

        /// <summary>
        /// The graphic associated with the Mask.
        /// Mask组件所在的GO的Graphic组件
        /// 如果没有Graphic组件，那么Mask是不生效的
        /// </summary>
        public Graphic graphic
        {
            get { return m_Graphic ?? (m_Graphic = GetComponent<Graphic>()); }
        }

        //遮罩材质，Mask组件自身使用，用来标记遮罩区域，在模板缓存StencilBuffer中写入模板值（写入操作）
        [NonSerialized]
        private Material m_MaskMaterial;

        //反遮罩材质，Mask组件自身使用，用来恢复模板缓冲区（清理操作）
        [NonSerialized]
        private Material m_UnmaskMaterial;

        protected Mask()
        {}

        /// <summary>
        /// Mask组件是否生效
        /// 自己有效、并且Graphic不为空
        /// </summary>
        /// <returns></returns>
        public virtual bool MaskEnabled() { return IsActive() && graphic != null; }

        [Obsolete("Not used anymore.")]
        public virtual void OnSiblingGraphicEnabledDisabled() {}

        /// <summary>
        /// Enable时做的事：
        /// 1.设置Graphic材质Dirty
        /// 2.设置Graphic为Mask类型的Graphic
        /// 3.通知所有可Mask的子节点进行重新计算
        /// </summary>
        protected override void OnEnable()
        {
            base.OnEnable();
            if (graphic != null)
            {
                graphic.canvasRenderer.hasPopInstruction = true;
                graphic.SetMaterialDirty();

                // Default the graphic to being the maskable graphic if its found.
                if (graphic is MaskableGraphic)
                    (graphic as MaskableGraphic).isMaskingGraphic = true;
            }

            MaskUtilities.NotifyStencilStateChanged(this);
        }

        /// <summary>
        /// Disable时做的事：
        /// 1.设置Grahpic的材质Dirty
        /// 2.设置Graphic为非裁剪的Graphic
        /// 3.模板材质中删除自己的2个材质
        /// 4.通知所有可Mask的子节点进行重新计算
        /// </summary>
        protected override void OnDisable()
        {
            // we call base OnDisable first here
            // as we need to have the IsActive return the
            // correct value when we notify the children
            // that the mask state has changed.
            base.OnDisable();
            if (graphic != null)
            {
                graphic.SetMaterialDirty();
                graphic.canvasRenderer.hasPopInstruction = false;
                graphic.canvasRenderer.popMaterialCount = 0;

                if (graphic is MaskableGraphic)
                    (graphic as MaskableGraphic).isMaskingGraphic = false;
            }

            StencilMaterial.Remove(m_MaskMaterial);
            m_MaskMaterial = null;
            StencilMaterial.Remove(m_UnmaskMaterial);
            m_UnmaskMaterial = null;

            MaskUtilities.NotifyStencilStateChanged(this);
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();

            if (!IsActive())
                return;

            if (graphic != null)
            {
                // Default the graphic to being the maskable graphic if its found.
                if (graphic is MaskableGraphic)
                    (graphic as MaskableGraphic).isMaskingGraphic = true;

                graphic.SetMaterialDirty();
            }

            MaskUtilities.NotifyStencilStateChanged(this);
        }

#endif

        /// <summary>
        /// 与RectMask2D实现一致，只不过少了RectMask2D的Padding信息
        /// 也就是说，射线检测的时候，还是检测Rect区域，不管Mask
        /// </summary>
        /// <param name="sp"></param>
        /// <param name="eventCamera"></param>
        /// <returns></returns>
        public virtual bool IsRaycastLocationValid(Vector2 sp, Camera eventCamera)
        {
            if (!isActiveAndEnabled)
                return true;

            return RectTransformUtility.RectangleContainsScreenPoint(rectTransform, sp, eventCamera);
        }

        /// Stencil calculation time!
        /// 模板测试计算，生成新的Mask材质
        /// 
        /// 原理是：模板Buffer是每个像素有一个8位的模板Buff，然后由于Mask是可以嵌套的，所以Unity利用模板Buffer中的这些位，来存储最多8深度的模板。
        ///
        /// 
        /// StencilID解释：某个深度的遮罩的、拿来对比的模板值。
        /// Read Mask解释：这里的Mask不是这个Mask组件的意思，而是一个掩码。使用的公式是：(当前模板值 & readMask) CompFunc (stencilID & readMask)
        /// Write Mask解释：这里的Mask不是这个Mask组件的意思，而是一个掩码。使用的公式是：新的模板值 = (stencilID & writeMask)
        /// 
        /// 遮罩举例：对于深度0的Mask，它的stencilID是0B00000001，由于它是最外层，所以它所在的片元对应的模板值都写入0B00000001（十进制是1）
        /// 遮罩举例：对于深度1的Mask，它的stencilID是0B00000011，它的readMask掩码是0B00000001，然后根据公式去判断的时候，判断某个片元的模板当前值是0B00000001的，都能显示该片元。
        ///         然后如果能显示片元、那么就开始重新写入模板值，writeMask是0B00000011，根据写入公式它能够显示的片元位置的模板值都改为0B00000011（十进制是3）
        /// 遮罩举例：对于深度2的Mask，它的stencilID是0B00000111，它的readMask掩码是0B00000011，然后根据公式去判断的时候，判断某个片元的模板当前值是0B00000011的，都能显示该片元。
        ///         然后如果能显示片元、那么就开始重新写入模板值，writeMask是0B00000111，根据写入公式它能够显示的片元位置的模板值都改为0B00000111（十进制是7）
        ///
        /// 反遮罩举例：对于深度2的Mask，它的stencilID是0B00000011，它的readMask掩码是0B00000011，然后根据公式去判断的时候，判断某个片元的模板当前值是0B00000011的，都表示该片元是展示的。
        ///         然后如果片元是显示的、那么就开始重新写入模板值，writeMask是0B00000111，根据写入公式它能够显示的片元位置的模板值都改为0B00000011（十进制是3）
        /// 反遮罩举例：对于深度1的Mask，它的stencilID是0B00000001，它的readMask掩码是0B00000001，然后根据公式去判断的时候，判断某个片元的模板当前值是0B00000001的，都表示该片元是展示的。
        ///         然后如果片元是显示的、那么就开始重新写入模板值，writeMask是0B00000011，根据写入公式它能够显示的片元位置的模板值都改为0B00000001（十进制是1）
        /// 反遮罩举例：对于深度0的Mask，它的stencilID是0B00000000，由于它是最外层的，所以它直接把它能显示的片元所在的模板测试值都置为0
        ///
        /// 由此可见，m_MaskMaterial是按照深度从0~7一步一步增加深度值中的数值大小，而m_UnmaskMaterial则是根据深度从7~0一步一步减少深度值中的深度大小。一正一反。
        public virtual Material GetModifiedMaterial(Material baseMaterial)
        {
            if (!MaskEnabled())
                return baseMaterial;

            var rootSortCanvas = MaskUtilities.FindRootSortOverrideCanvas(transform);
            var stencilDepth = MaskUtilities.GetStencilDepth(transform, rootSortCanvas);
            //Mask深度值需要小于8，因为每个像素只有8位模板值存储，然后Unity为每个深度使用一个位
            if (stencilDepth >= 8)
            {
                Debug.LogWarning("Attempting to use a stencil mask with depth > 8", gameObject);
                return baseMaterial;
            }

            //根据深度值，左移1这个数字，这样就能得到每个深度的标志位
            int desiredStencilBit = 1 << stencilDepth;

            // if we are at the first level...
            // we want to destroy what is there
            //如果是1~8深度的第1个深度，那么进行比较特殊的处理
            if (desiredStencilBit == 1)
            {
                //生成Mask材质，模板值是Replace模式（成功则直接写入），使用方式是Always（无论模板值如何总是显示片元），根据是否要显示Mask图形、决定写入的颜色值（如果显示图形，那么图形区域都显示白色）
                var maskMaterial = StencilMaterial.Add(baseMaterial, 1, StencilOp.Replace, CompareFunction.Always, m_ShowMaskGraphic ? ColorWriteMask.All : 0);
                //删除老的已经存在的Mask材质
                StencilMaterial.Remove(m_MaskMaterial);
                m_MaskMaterial = maskMaterial;

                //生成非Mask材质，模板值是Zero模式（置为0），使用方式是Always（无论模板值如何总是显示片元），颜色值是0（不显示颜色）
                var unmaskMaterial = StencilMaterial.Add(baseMaterial, 1, StencilOp.Zero, CompareFunction.Always, 0);
                //删除老的已经存在的非Mask材质
                StencilMaterial.Remove(m_UnmaskMaterial);
                m_UnmaskMaterial = unmaskMaterial;
                graphic.canvasRenderer.popMaterialCount = 1;
                graphic.canvasRenderer.SetPopMaterial(m_UnmaskMaterial, 0);

                return m_MaskMaterial;
            }

            //如果不是最外层的Mask，那么进行如下处理
            //生成Mask材质
            //1.模板ID是本标识位以及前面所有标识位都为1的数字
            //2.模板值是Replace模式（成功则直接写入），使用方式是Equal（模板值相等才会显示片元）根据是否要显示Mask图形、决定写入的颜色值（如果显示图形，那么图形区域都显示白色），读取的Mask是上一级Mask，写入的Mask是本Mask
            //otherwise we need to be a bit smarter and set some read / write masks
            var maskMaterial2 = StencilMaterial.Add(baseMaterial, desiredStencilBit | (desiredStencilBit - 1), StencilOp.Replace, CompareFunction.Equal, m_ShowMaskGraphic ? ColorWriteMask.All : 0, desiredStencilBit - 1, desiredStencilBit | (desiredStencilBit - 1));
            //删除老的已经存在的Mask材质
            StencilMaterial.Remove(m_MaskMaterial);
            m_MaskMaterial = maskMaterial2;

            graphic.canvasRenderer.hasPopInstruction = true;
            //生成非Mask材质
            //1.模板ID是上一个标识位
            //2.其他的与生成Mask材质的数据一致
            var unmaskMaterial2 = StencilMaterial.Add(baseMaterial, desiredStencilBit - 1, StencilOp.Replace, CompareFunction.Equal, 0, desiredStencilBit - 1, desiredStencilBit | (desiredStencilBit - 1));
            StencilMaterial.Remove(m_UnmaskMaterial);
            m_UnmaskMaterial = unmaskMaterial2;
            graphic.canvasRenderer.popMaterialCount = 1;
            graphic.canvasRenderer.SetPopMaterial(m_UnmaskMaterial, 0);

            return m_MaskMaterial;
        }
    }
}
