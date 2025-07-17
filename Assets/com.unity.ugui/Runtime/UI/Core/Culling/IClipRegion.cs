namespace UnityEngine.UI
{
    /// <summary>
    /// Interface that can be used to recieve clipping callbacks as part of the canvas update loop.
    /// 裁剪接口
    /// RectMask2D实现了该接口，从而实现矩形区域的裁剪
    /// </summary>
    public interface IClipper
    {
        /// <summary>
        /// Function to to cull / clip children elements.
        /// 裁剪执行接口
        /// 在布局重建后、图形重建前执行
        /// </summary>
        /// <remarks>
        /// Called after layout and before Graphic update of the Canvas update loop.
        /// </remarks>

        void PerformClipping();
    }

    /// <summary>
    /// Interface for elements that can be clipped if they are under an IClipper
    /// 被裁剪接口
    /// MaskableGraphic类实现该接口，也就代表着它所有的子类都可以被遮罩处理
    /// </summary>
    public interface IClippable
    {
        /// <summary>
        /// GameObject of the IClippable object
        /// </summary>
        GameObject gameObject { get; }

        /// <summary>
        /// Will be called when the state of a parent IClippable changed.
        /// 父节点裁剪变更的时候，会调用这个接口，从而重新计算所有元素的裁切
        /// </summary>
        void RecalculateClipping();

        /// <summary>
        /// The RectTransform of the clippable.
        /// </summary>
        RectTransform rectTransform { get; }

        /// <summary>
        /// Clip and cull the IClippable given a specific clipping rect
        /// 渲染剔除
        /// </summary>
        /// <param name="clipRect">The Rectangle in which to clip against.</param>
        /// <param name="validRect">Is the Rect valid. If not then the rect has 0 size.</param>
        void Cull(Rect clipRect, bool validRect);

        /// <summary>
        /// Set the clip rect for the IClippable.
        /// 设置裁剪区域
        /// MaskableGraphic类实现，把该区域写进CanvasRenderer中，然后shader来通过透明度实现片元丢弃
        /// </summary>
        /// <param name="value">The Rectangle for the clipping</param>
        /// <param name="validRect">Is the rect valid.</param>
        void SetClipRect(Rect value, bool validRect);

        /// <summary>
        /// Set the clip softness for the IClippable.
        ///
        /// The softness is a linear alpha falloff over clipSoftness pixels.
        ///
        /// 柔和裁切效果，而不是直接所有裁切区域的透明度都为0.
        /// 用来设置裁切边缘的平滑过渡
        /// </summary>
        /// <param name="clipSoftness">The number of pixels to apply the softness to </param>
        void SetClipSoftness(Vector2 clipSoftness);
    }
}
