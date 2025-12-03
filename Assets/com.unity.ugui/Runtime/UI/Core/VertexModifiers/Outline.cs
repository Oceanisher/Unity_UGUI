using UnityEngine.Pool;

namespace UnityEngine.UI
{
    [AddComponentMenu("UI/Effects/Outline", 81)]
    /// <summary>
    /// Adds an outline to a graphic using IVertexModifier.
    /// 描边组件
    ///
    /// 重写了阴影组件，因为跟阴影组件基本相同，原理如下：
    /// 1.把原始顶点按照偏移距离，上下左右各绘制一次
    /// 2.再把原始顶点绘制一次，造成描边效果
    /// 3.缺点：距离过大的话，阴影可能有断线；消耗过大
    /// 4.TextMeshPro的Outline不再使用基于顶点绘制的描边，而是使用材质绘制，单次绘制、不扩展顶点数据，性能会好很多
    /// </summary>
    public class Outline : Shadow
    {
        protected Outline()
        {}

        /// <summary>
        /// 修改顶点信息，加入描边效果
        /// </summary>
        /// <param name="vh"></param>
        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive())
                return;

            var verts = ListPool<UIVertex>.Get();
            vh.GetUIVertexStream(verts);

            //描边效果需要把顶点容量扩展到原来的5倍
            var neededCpacity = verts.Count * 5;
            if (verts.Capacity < neededCpacity)
                verts.Capacity = neededCpacity;

            //原始是把原始顶点按照阴影偏移距离，往上、下、左、右各绘制一次，然后再把原始的绘制一次，就变成了描边了
            //所以比阴影更耗费性能，因为绘制次数是5倍、顶点数据也是5倍
            var start = 0;
            var end = verts.Count;
            ApplyShadowZeroAlloc(verts, effectColor, start, verts.Count, effectDistance.x, effectDistance.y);

            start = end;
            end = verts.Count;
            ApplyShadowZeroAlloc(verts, effectColor, start, verts.Count, effectDistance.x, -effectDistance.y);

            start = end;
            end = verts.Count;
            ApplyShadowZeroAlloc(verts, effectColor, start, verts.Count, -effectDistance.x, effectDistance.y);

            start = end;
            end = verts.Count;
            ApplyShadowZeroAlloc(verts, effectColor, start, verts.Count, -effectDistance.x, -effectDistance.y);

            vh.Clear();
            vh.AddUIVertexTriangleStream(verts);
            ListPool<UIVertex>.Release(verts);
        }
    }
}
