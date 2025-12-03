using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace UnityEngine.UI
{
    [AddComponentMenu("UI/Effects/Shadow", 80)]
    /// <summary>
    /// Adds an outline to a graphic using IVertexModifier.
    /// Graphic的阴影效果
    /// </summary>
    public class Shadow : BaseMeshEffect
    {
        //阴影颜色
        [SerializeField]
        private Color m_EffectColor = new Color(0f, 0f, 0f, 0.5f);

        //阴影距离
        [SerializeField]
        private Vector2 m_EffectDistance = new Vector2(1f, -1f);

        //是否使用Graphic自身的透明度
        [SerializeField]
        private bool m_UseGraphicAlpha = true;

        //最大阴影距离
        private const float kMaxEffectDistance = 600f;

        protected Shadow()
        {}

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            effectDistance = m_EffectDistance;
            base.OnValidate();
        }

#endif
        /// <summary>
        /// Color for the effect
        /// 阴影颜色
        /// 每次修改都会引起Graphic图形重建
        /// </summary>
        public Color effectColor
        {
            get { return m_EffectColor; }
            set
            {
                m_EffectColor = value;
                if (graphic != null)
                    graphic.SetVerticesDirty();
            }
        }

        /// <summary>
        /// How far is the shadow from the graphic.
        /// 阴影距离
        /// 每次修改都会引起Graphic图形重建
        /// </summary>
        public Vector2 effectDistance
        {
            get { return m_EffectDistance; }
            set
            {
                //根据最大阴影距离，修正一下XY的数据
                if (value.x > kMaxEffectDistance)
                    value.x = kMaxEffectDistance;
                if (value.x < -kMaxEffectDistance)
                    value.x = -kMaxEffectDistance;

                if (value.y > kMaxEffectDistance)
                    value.y = kMaxEffectDistance;
                if (value.y < -kMaxEffectDistance)
                    value.y = -kMaxEffectDistance;

                if (m_EffectDistance == value)
                    return;

                m_EffectDistance = value;

                if (graphic != null)
                    graphic.SetVerticesDirty();
            }
        }

        /// <summary>
        /// Should the shadow inherit the alpha from the graphic?
        /// 是否使用Graphic透明度
        /// 每次修改都会引起Graphic图形重建
        /// </summary>
        public bool useGraphicAlpha
        {
            get { return m_UseGraphicAlpha; }
            set
            {
                m_UseGraphicAlpha = value;
                if (graphic != null)
                    graphic.SetVerticesDirty();
            }
        }

        /// <summary>
        /// 执行为verts顶点数据添加阴影效果
        ///
        /// 原理是：
        /// 1.将所有原始顶点数据的位置，按照阴影距离做一个偏移，并修改顶点颜色为新颜色
        /// 2.并且把阴影顶点数据放在前面，保证阴影顶点先被绘制
        /// 3.顶点绘制按照三角形绘制，这样的话，所有阴影顶点先被绘制，会绘制跟原先一样的图形、但是位置和颜色是新的
        /// 4.然后原始顶点再被绘制，会部分覆盖阴影的图形，这样就造成了阴影的效果
        /// </summary>
        /// <param name="verts">原始顶点数据</param>
        /// <param name="color">阴影颜色</param>
        /// <param name="start">起始顶点</param>
        /// <param name="end">结束顶点</param>
        /// <param name="x">阴影X轴偏移</param>
        /// <param name="y">阴影Y轴偏移</param>
        protected void ApplyShadowZeroAlloc(List<UIVertex> verts, Color32 color, int start, int end, float x, float y)
        {
            UIVertex vt;
            //为顶点数据扩容，计算扩容后的顶点列表大小
            var neededCapacity = verts.Count + end - start;
            if (verts.Capacity < neededCapacity)
                verts.Capacity = neededCapacity;

            for (int i = start; i < end; ++i)
            {
                //原始顶点数据复制一份，放到顶点数据列表的最后
                vt = verts[i];
                verts.Add(vt);

                //将原始顶点的位置、颜色信息，根据传入的阴影数据做一个修正，然后重新放回它在列表中的原始位置上
                //也就是说原来的顶点将作为阴影顶点，新的顶点是原来的顶点数据
                //这样的话，阴影顶点数据先被绘制，然后上面再覆盖绘制原始顶点数据，这样就能实现阴影效果
                Vector3 v = vt.position;
                v.x += x;
                v.y += y;
                vt.position = v;
                var newColor = color;
                if (m_UseGraphicAlpha)
                    newColor.a = (byte)((newColor.a * verts[i].color.a) / 255);
                vt.color = newColor;
                verts[i] = vt;
            }
        }

        /// <summary>
        /// Duplicate vertices from start to end and turn them into shadows with the given offset.
        /// 为顶点数据添加阴影效果
        /// </summary>
        /// <param name="verts">Vert list to copy</param>
        /// <param name="color">Shadow color</param>
        /// <param name="start">The start index in the verts list</param>
        /// <param name="end">The end index in the vers list</param>
        /// <param name="x">The shadows x offset</param>
        /// <param name="y">The shadows y offset</param>
        protected void ApplyShadow(List<UIVertex> verts, Color32 color, int start, int end, float x, float y)
        {
            ApplyShadowZeroAlloc(verts, color, start, end, x, y);
        }

        /// <summary>
        /// 实现IMeshModifier接口，实现对Mesh的修改
        /// </summary>
        /// <param name="vh"></param>
        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive())
                return;

            //创建流式顶点数据，把顶点数据都放到output中
            var output = ListPool<UIVertex>.Get();
            vh.GetUIVertexStream(output);

            //写入阴影数据
            ApplyShadow(output, effectColor, 0, output.Count, effectDistance.x, effectDistance.y);
            //清理vh、并把修改后的顶点数据写入vh
            vh.Clear();
            vh.AddUIVertexTriangleStream(output);
            ListPool<UIVertex>.Release(output);
        }
    }
}
