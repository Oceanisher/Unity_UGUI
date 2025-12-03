using System.Linq;

namespace UnityEngine.UI
{
    [AddComponentMenu("UI/Effects/Position As UV1", 82)]
    /// <summary>
    /// An IVertexModifier which sets the raw vertex position into UV1 of the generated verts.
    /// 把顶点的位置信息，写入到顶点的UV1通道中
    /// UI元素默认使用UV0进行纹理映射，这里启用UV1是为了后面shader做一些特殊效果而使用的
    /// </summary>
    public class PositionAsUV1 : BaseMeshEffect
    {
        protected PositionAsUV1()
        {}

        public override void ModifyMesh(VertexHelper vh)
        {
            UIVertex vert = new UIVertex();
            for (int i = 0; i < vh.currentVertCount; i++)
            {
                vh.PopulateUIVertex(ref vert, i);
                vert.uv1 =  new Vector2(vert.position.x, vert.position.y);
                vh.SetUIVertex(vert, i);
            }
        }
    }
}
