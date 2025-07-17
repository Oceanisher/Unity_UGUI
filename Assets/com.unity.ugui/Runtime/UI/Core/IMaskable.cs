using System;

namespace UnityEngine.UI
{
    /// <summary>
    /// This element is capable of being masked out.
    /// 遮罩接口
    /// 通过模板测试，重新生成材质，实现对元素的遮罩效果
    /// </summary>
    public interface IMaskable
    {
        /// <summary>
        /// Recalculate masking for this element and all children elements.
        /// 对元素以及其子元素重新计算遮罩区域
        /// </summary>
        /// <remarks>
        /// Use this to update the internal state (recreate materials etc).
        /// </remarks>
        void RecalculateMasking();
    }
}
