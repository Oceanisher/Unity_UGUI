using System;
using UnityEngine.Serialization;

namespace UnityEngine.UI
{
    [Serializable]
    /// <summary>
    /// Struct for storing Text generation settings.
    /// 字体配置数据
    /// </summary>
    public class FontData : ISerializationCallbackReceiver
    {
        //字体对象
        [SerializeField]
        [FormerlySerializedAs("font")]
        private Font m_Font;

        //字体大小，0~300
        [SerializeField]
        [FormerlySerializedAs("fontSize")]
        private int m_FontSize;

        //字体风格，通用、加粗、斜体、加粗+斜体
        [SerializeField]
        [FormerlySerializedAs("fontStyle")]
        private FontStyle m_FontStyle;

        //是否开启最佳适配，开启后会优先根据控件区域大小，动态缩放字体
        //缩小到最小值后，才会换行
        //勾选后FontSize无效
        //unity官方都不推荐使用
        [SerializeField]
        private bool m_BestFit;

        //最小尺寸，下限到0
        [SerializeField]
        private int m_MinSize;

        //最大尺寸，上限到300
        [SerializeField]
        private int m_MaxSize;

        //对齐方式
        [SerializeField]
        [FormerlySerializedAs("alignment")]
        private TextAnchor m_Alignment;

        //是否根据字体的几何图形进行对齐
        //不勾选时，是根据字体设定的Glyph Metrics决定对齐时的上下左右Margin
        //勾选时，会根据字母的真实几何图形，决定对齐时的上下左右Margin
        //TTF的Glyph Metrics中，有一个Baseline基线，然后有往上的Ascent高度、往下的Descent高度
        //字体会在Ascent与Descent之间，但是不一定会到达边界。不同字体的设计是自定义的。
        [SerializeField]
        private bool m_AlignByGeometry;

        //是否支持富文本
        [SerializeField]
        [FormerlySerializedAs("richText")]
        private bool m_RichText;

        //水平换行模式
        //wrap:超出边界后换行
        //overflow:溢出
        [SerializeField]
        private HorizontalWrapMode m_HorizontalOverflow;

        //垂直换行模式
        //truncate:超出边界后截断、不显示
        //overflow:溢出
        [SerializeField]
        private VerticalWrapMode m_VerticalOverflow;

        //行间距
        //不影响第一行与上边界的距离
        [SerializeField]
        private float m_LineSpacing;

        /// <summary>
        /// Get a font data with sensible defaults.
        /// 默认的字体配置数据
        /// </summary>
        public static FontData defaultFontData
        {
            get
            {
                var fontData = new FontData
                {
                    m_FontSize  = 14,
                    m_LineSpacing = 1f,
                    m_FontStyle = FontStyle.Normal,
                    m_BestFit = false,
                    m_MinSize = 10,
                    m_MaxSize = 40,
                    m_Alignment = TextAnchor.UpperLeft,
                    m_HorizontalOverflow = HorizontalWrapMode.Wrap,
                    m_VerticalOverflow = VerticalWrapMode.Truncate,
                    m_RichText  = true,
                    m_AlignByGeometry = false
                };
                return fontData;
            }
        }

        /// <summary>
        /// The Font to use for this generated Text object.
        /// </summary>
        public Font font
        {
            get { return m_Font; }
            set { m_Font = value; }
        }

        /// <summary>
        /// The Font size to use for this generated Text object.
        /// </summary>
        public int fontSize
        {
            get { return m_FontSize; }
            set { m_FontSize = value; }
        }

        /// <summary>
        /// The font style to use for this generated Text object.
        /// </summary>
        public FontStyle fontStyle
        {
            get { return m_FontStyle; }
            set { m_FontStyle = value; }
        }

        /// <summary>
        /// Is best fit used for this generated Text object.
        /// </summary>
        public bool bestFit
        {
            get { return m_BestFit; }
            set { m_BestFit = value; }
        }

        /// <summary>
        /// The min size for this generated Text object.
        /// </summary>
        public int minSize
        {
            get { return m_MinSize; }
            set { m_MinSize = value; }
        }

        /// <summary>
        /// The max size for this generated Text object.
        /// </summary>
        public int maxSize
        {
            get { return m_MaxSize; }
            set { m_MaxSize = value; }
        }

        /// <summary>
        /// How is the text aligned for this generated Text object.
        /// </summary>
        public TextAnchor alignment
        {
            get { return m_Alignment; }
            set { m_Alignment = value; }
        }

        /// <summary>
        /// Use the extents of glyph geometry to perform horizontal alignment rather than glyph metrics.
        /// </summary>
        /// <remarks>
        /// This can result in better fitting left and right alignment, but may result in incorrect positioning when attempting to overlay multiple fonts (such as a specialized outline font) on top of each other.
        /// </remarks>
        public bool alignByGeometry
        {
            get { return m_AlignByGeometry; }
            set { m_AlignByGeometry = value;  }
        }

        /// <summary>
        /// Should rich text be used for this generated Text object.
        /// </summary>
        public bool richText
        {
            get { return m_RichText; }
            set { m_RichText = value; }
        }

        /// <summary>
        /// The horizontal overflow policy for this generated Text object.
        /// </summary>
        public HorizontalWrapMode horizontalOverflow
        {
            get { return m_HorizontalOverflow; }
            set { m_HorizontalOverflow = value; }
        }

        /// <summary>
        /// The vertical overflow policy for this generated Text object.
        /// </summary>
        public VerticalWrapMode verticalOverflow
        {
            get { return m_VerticalOverflow; }
            set { m_VerticalOverflow = value; }
        }

        /// <summary>
        /// The line spaceing for this generated Text object.
        /// </summary>
        public float lineSpacing
        {
            get { return m_LineSpacing; }
            set { m_LineSpacing = value; }
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {}

        /// <summary>
        /// 反序列化之后，重新限制一下字体的大小
        /// </summary>
        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            //字体最大是300，最小是0
            m_FontSize = Mathf.Clamp(m_FontSize, 0, 300);
            m_MinSize = Mathf.Clamp(m_MinSize, 0, m_FontSize);
            m_MaxSize = Mathf.Clamp(m_MaxSize, m_FontSize, 300);
        }
    }
}
