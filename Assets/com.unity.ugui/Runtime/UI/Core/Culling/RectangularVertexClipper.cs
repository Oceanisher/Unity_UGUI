namespace UnityEngine.UI
{
    /// <summary>
    /// 矩形顶点裁切
    /// </summary>
    internal class RectangularVertexClipper
    {
        readonly Vector3[] m_WorldCorners = new Vector3[4];
        readonly Vector3[] m_CanvasCorners = new Vector3[4];

        /// <summary>
        /// 获取一个trs在Canvas中的相对区域Rect
        /// Rect是一个相对于Canvas的区域，本地坐标
        /// </summary>
        /// <param name="t"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        public Rect GetCanvasRect(RectTransform t, Canvas c)
        {
            if (c == null)
                return new Rect();

            //获取trs四个角的世界坐标
            t.GetWorldCorners(m_WorldCorners);
            //将trs四个角的世界坐标，转换为在canvas中的相对坐标
            var canvasTransform = c.GetComponent<Transform>();
            for (int i = 0; i < 4; ++i)
                m_CanvasCorners[i] = canvasTransform.InverseTransformPoint(m_WorldCorners[i]);

            return new Rect(m_CanvasCorners[0].x, m_CanvasCorners[0].y, m_CanvasCorners[2].x - m_CanvasCorners[0].x, m_CanvasCorners[2].y - m_CanvasCorners[0].y);
        }
    }
}
