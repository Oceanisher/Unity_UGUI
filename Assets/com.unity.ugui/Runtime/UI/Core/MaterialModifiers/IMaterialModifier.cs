namespace UnityEngine.UI
{
    /// <summary>
    /// Use this interface to modify a Material that renders a Graphic. The Material is modified before the it is passed to the CanvasRenderer.
    /// 材质修改器
    /// 会对传入的材质进行修改，返回一个修改后的材质。
    /// 在材质被传递到CanvasRenderer之前修改
    /// </summary>
    /// <remarks>
    /// When a Graphic sets a material that is passed (in order) to any components on the GameObject that implement IMaterialModifier. This component can modify the material to be used for rendering.
    /// </remarks>
    public interface IMaterialModifier
    {
        /// <summary>
        /// Perform material modification in this function.
        /// 修改传入的材质、返回新材质
        /// </summary>
        /// <param name="baseMaterial">The material that is to be modified</param>
        /// <returns>The modified material.</returns>
        Material GetModifiedMaterial(Material baseMaterial);
    }
}
