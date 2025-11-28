using System;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace UnityEngine.UI
{
    [RequireComponent(typeof(Canvas))]
    [ExecuteAlways]
    [AddComponentMenu("Layout/Canvas Scaler", 101)]
    [DisallowMultipleComponent]
    /// <summary>
    ///   The Canvas Scaler component is used for controlling the overall scale and pixel density of UI elements in the Canvas. This scaling affects everything under the Canvas, including font sizes and image borders.
    /// Canvas缩放组件，只会在根Canvas上生效
    /// 会对Canvas组件的scaleFactor属性进行设置，从而让Canvas下所有的UI都能变更
    /// 
    /// 能够设置在不同分辨率或者显示尺寸下，Canvas内部元素的缩放模式和最终大小
    /// Canvas的RectTransform能够自动铺满整个屏幕，因为它的锚点自动设置为stretch模式，那么为什么还需要该组件？
    /// 因为如果不使用该组件，那么Canvas下的UI元素会按照固定像素大小进行显示，如下例子所示：
    /*
     * 假设：
            设计分辨率：1920x1080
            实际屏幕：3840x2160（4K）
       没有 CanvasScaler：
            Canvas 大小 = 3840x2160
            UI 元素仍按 1920x1080 的像素大小显示
            结果：UI 看起来很小（只有屏幕的 1/4 大小）
       有 CanvasScaler（Scale With Screen Size）：
            Canvas 大小 = 3840x2160
            UI 元素自动放大 2 倍
            结果：UI 保持相对大小，正常显示
     */
    /// 所以大部门情况下，都需要使用CanvasScaler组件。
    /// 通常根Canvas上配置一个就可以了，子Canvas可以继承生效
    ///
    /// </summary>
    /// <remarks>
    /// For a Canvas set to 'Screen Space - Overlay' or 'Screen Space - Camera', the Canvas Scaler UI Scale Mode can be set to Constant Pixel Size, Scale With Screen Size, or Constant Physical Size.
    ///
    /// Using the Constant Pixel Size mode, positions and sizes of UI elements are specified in pixels on the screen. This is also the default functionality of the Canvas when no Canvas Scaler is attached. However, With the Scale Factor setting in the Canvas Scaler, a constant scaling can be applied to all UI elements in the Canvas.
    ///
    /// Using the Scale With Screen Size mode, positions and sizes can be specified according to the pixels of a specified reference resolution. If the current screen resolution is larger than the reference resolution, the Canvas will keep having only the resolution of the reference resolution, but will scale up in order to fit the screen. If the current screen resolution is smaller than the reference resolution, the Canvas will similarly be scaled down to fit. If the current screen resolution has a different aspect ratio than the reference resolution, scaling each axis individually to fit the screen would result in non-uniform scaling, which is generally undesirable. Instead of this, the ReferenceResolution component will make the Canvas resolution deviate from the reference resolution in order to respect the aspect ratio of the screen. It is possible to control how this deviation should behave using the ::ref::screenMatchMode setting.
    ///
    /// Using the Constant Physical Size mode, positions and sizes of UI elements are specified in physical units, such as millimeters, points, or picas. This mode relies on the device reporting its screen DPI correctly. You can specify a fallback DPI to use for devices that do not report a DPI.
    ///
    /// For a Canvas set to 'World Space' the Canvas Scaler can be used to control the pixel density of UI elements in the Canvas.
    /// </remarks>
    public class CanvasScaler : UIBehaviour
    {
        /// <summary>
        /// Determines how UI elements in the Canvas are scaled.
        /// 缩放模式
        /// </summary>
        public enum ScaleMode
        {
            /// <summary>
            /// Using the Constant Pixel Size mode, positions and sizes of UI elements are specified in pixels on the screen.
            /// 固定分辨率，不会跟随屏幕分辨率而变化
            /// </summary>
            ConstantPixelSize,
            /// <summary>
            /// Using the Scale With Screen Size mode, positions and sizes can be specified according to the pixels of a specified reference resolution.
            /// If the current screen resolution is larger than the reference resolution, the Canvas will keep having only the resolution of the reference resolution, but will scale up in order to fit the screen. If the current screen resolution is smaller than the reference resolution, the Canvas will similarly be scaled down to fit.
            /// 跟随屏幕尺寸变化，响应式、最常用
            /// </summary>
            ScaleWithScreenSize,
            /// <summary>
            /// Using the Constant Physical Size mode, positions and sizes of UI elements are specified in physical units, such as millimeters, points, or picas.
            /// 固定物理尺寸，基于屏幕DPI，也就是无论屏幕大小，肉眼看着Canvas是一样大小的
            /// </summary>
            ConstantPhysicalSize
        }

        //缩放模式
        [Tooltip("Determines how UI elements in the Canvas are scaled.")]
        [SerializeField] private ScaleMode m_UiScaleMode = ScaleMode.ConstantPixelSize;

        ///<summary>
        ///Determines how UI elements in the Canvas are scaled.
        /// 缩放模式
        ///</summary>
        public ScaleMode uiScaleMode { get { return m_UiScaleMode; } set { m_UiScaleMode = value; } }

        //参考PPU，如果Sprite自身有配置，那么会覆盖UI上配置的这个PPU
        [Tooltip("If a sprite has this 'Pixels Per Unit' setting, then one pixel in the sprite will cover one unit in the UI.")]
        [SerializeField] protected float m_ReferencePixelsPerUnit = 100;

        /// <summary>
        /// If a sprite has this 'Pixels Per Unit' setting, then one pixel in the sprite will cover one unit in the UI.
        /// 参考PPU，如果Sprite自身有配置，那么会覆盖UI上配置的这个PPU
        /// </summary>
        public float referencePixelsPerUnit { get { return m_ReferencePixelsPerUnit; } set { m_ReferencePixelsPerUnit = value; } }


        // Constant Pixel Size settings
        //固定分辨率使用，缩放因子，能够缩放所有的UI元素
        [Tooltip("Scales all UI elements in the Canvas by this factor.")]
        [SerializeField] protected float m_ScaleFactor = 1;

        /// <summary>
        /// Scales all UI elements in the Canvas by this factor.
        /// 能够缩放所有子元素
        /// </summary>
        /// <summary>
        /// Scales all UI elements in the Canvas by this factor.
        /// </summary>
        public float scaleFactor { get { return m_ScaleFactor; } set { m_ScaleFactor = Mathf.Max(0.01f, value); } }

        /// Scale the canvas area with the width as reference, the height as reference, or something in between.
        /// <summary>
        /// Scale the canvas area with the width as reference, the height as reference, or something in between.
        /// 屏幕适配模式
        /// </summary>
        public enum ScreenMatchMode
        {
            /// <summary>
            /// Scale the canvas area with the width as reference, the height as reference, or something in between.
            /// 根据宽度或者高度进行适配，具体要看参数配置，两者可以融合
            /// </summary>
            MatchWidthOrHeight = 0,
            /// <summary>
            /// Expand the canvas area either horizontally or vertically, so the size of the canvas will never be smaller than the reference.
            /// 扩展模式，屏幕分辨率的长宽、与参考分辨率的长宽的比值中，较小的那个值作为缩放因子。
            /// </summary>
            Expand = 1,
            /// <summary>
            /// Crop the canvas area either horizontally or vertically, so the size of the canvas will never be larger than the reference.
            /// 适配模式，屏幕分辨率的长宽、与参考分辨率的长宽的比值中，较大的那个值作为缩放因子。
            /// </summary>
            Shrink = 2
        }

        //参考分辨率，也就是设计分辨率
        [Tooltip("The resolution the UI layout is designed for. If the screen resolution is larger, the UI will be scaled up, and if it's smaller, the UI will be scaled down. This is done in accordance with the Screen Match Mode.")]
        [SerializeField] protected Vector2 m_ReferenceResolution = new Vector2(800, 600);

        /// <summary>
        /// The resolution the UI layout is designed for.
        /// 参考分辨率
        /// </summary>
        /// <remarks>
        /// If the screen resolution is larger, the UI will be scaled up, and if it's smaller, the UI will be scaled down. This is done in accordance with the Screen Match Mode.
        /// </remarks>
        public Vector2 referenceResolution
        {
            get
            {
                return m_ReferenceResolution;
            }
            set
            {
                m_ReferenceResolution = value;

                const float k_MinimumResolution = 0.00001f;

                if (m_ReferenceResolution.x > -k_MinimumResolution && m_ReferenceResolution.x < k_MinimumResolution) m_ReferenceResolution.x = k_MinimumResolution * Mathf.Sign(m_ReferenceResolution.x);
                if (m_ReferenceResolution.y > -k_MinimumResolution && m_ReferenceResolution.y < k_MinimumResolution) m_ReferenceResolution.y = k_MinimumResolution * Mathf.Sign(m_ReferenceResolution.y);
            }
        }

        //屏幕适配模式
        [Tooltip("A mode used to scale the canvas area if the aspect ratio of the current resolution doesn't fit the reference resolution.")]
        [SerializeField] protected ScreenMatchMode m_ScreenMatchMode = ScreenMatchMode.MatchWidthOrHeight;
        /// <summary>
        /// A mode used to scale the canvas area if the aspect ratio of the current resolution doesn't fit the reference resolution.
        /// </summary>
        public ScreenMatchMode screenMatchMode { get { return m_ScreenMatchMode; } set { m_ScreenMatchMode = value; } }

        //在屏幕适配模式为MatchWidthOrHeight时，这个参数用来调整根据宽还是高的比例，0是根据Width扩展Height、1是根据Height扩展Width，中间值是混合
        [Tooltip("Determines if the scaling is using the width or height as reference, or a mix in between.")]
        [Range(0, 1)]
        [SerializeField] protected float m_MatchWidthOrHeight = 0;

        /// <summary>
        /// Setting to scale the Canvas to match the width or height of the reference resolution, or a combination.
        /// 屏幕适配模式为MatchWidthOrHeight时，这个参数用来调整根据宽还是高的比例，0是根据Width扩展Height、1是根据Height扩展Width，中间值是混合
        ///
        /// 当设置为0时，参考分辨率是(600,800)，实际分辨率是(800,600)，那么此时完全依据Width。那么此时Canvas的实际Width设置为参考分辨率600，它的
        /// </summary>
        /// <remarks>
        /// If the setting is set to 0, the Canvas is scaled according to the difference between the current screen resolution width and the reference resolution width. If the setting is set to 1, the Canvas is scaled according to the difference between the current screen resolution height and the reference resolution height.
        ///
        /// For values in between 0 and 1, the scaling is based on a combination of the relative width and height.
        ///
        /// Consider an example where the reference resolution of 640x480, and the current screen resolution is a landscape mode of 480x640.
        ///
        /// If the scaleWidthOrHeight setting is set to 0, the Canvas is scaled by 0.75 because the current resolution width of 480 is 0.75 times the reference resolution width of 640. The Canvas resolution gets a resolution of 640x853.33. This resolution has the same width as the reference resolution width, but has the aspect ratio of the current screen resolution. Note that the Canvas resolution of 640x853.33 is the current screen resolution divided by the scale factor of 0.75.
        ///
        /// If the scaleWidthOrHeight setting is set to 1, the Canvas is scaled by 1.33 because the current resolution height of 640 is 1.33 times the reference resolution height of 480. The Canvas resolution gets a resolution of 360x480. This resolution has the same height as the reference resolution width, but has the aspect ratio of the current screen resolution. Note that the Canvas resolution of 360x480 is the current screen resolution divided by the scale factor of 1.33.
        ///
        /// If the scaleWidthOrHeight setting is set to 0.5, we find the horizontal scaling needed (0.75) and the vertical scaling needed (1.33) and find the average. However, we do the average in logarithmic space. A regular average of 0.75 and 1.33 would produce a result of 1.04. However, since multiplying by 1.33 is the same as diving by 0.75, the two scale factor really corresponds to multiplying by 0.75 versus dividing by 0.75, and the average of those two things should even out and produce a neutral result. The average in logarithmic space of 0.75 and 1.33 is exactly 1.0, which is what we want. The Canvas resolution hence ends up being 480x640 which is the current resolution divided by the scale factor of 1.0.
        ///
        /// The logic works the same for all values. The average between the horizontal and vertical scale factor is a weighted average based on the matchWidthOrHeight value.
        /// </remarks>
        public float matchWidthOrHeight { get { return m_MatchWidthOrHeight; } set { m_MatchWidthOrHeight = value; } }

        // The log base doesn't have any influence on the results whatsoever, as long as the same base is used everywhere.
        //对数计算的对数底
        private const float kLogBase = 2;

        /// <summary>
        /// The possible physical unit types
        /// </summary>
        public enum Unit
        {
            /// <summary>
            /// Use centimeters.
            /// A centimeter is 1/100 of a meter
            /// 厘米
            /// </summary>
            Centimeters,
            /// <summary>
            /// Use millimeters.
            /// A millimeter is 1/10 of a centimeter, and 1/1000 of a meter.
            /// 毫米
            /// </summary>
            Millimeters,
            /// <summary>
            /// Use inches.
            /// 英寸
            /// </summary>
            Inches,
            /// <summary>
            /// Use points.
            /// One point is 1/12 of a pica, and 1/72 of an inch.
            /// 派卡的1/12
            /// </summary>
            Points,
            /// <summary>
            /// Use picas.
            /// One pica is 1/6 of an inch.
            /// 派卡
            /// </summary>
            Picas
        }

        //当使用物理尺寸时，物理单位类型
        [Tooltip("The physical unit to specify positions and sizes in.")]
        [SerializeField] protected Unit m_PhysicalUnit = Unit.Points;

        /// <summary>
        /// The physical unit to specify positions and sizes in.
        /// 当使用物理尺寸时，物理单位类型
        /// </summary>
        public Unit physicalUnit { get { return m_PhysicalUnit; } set { m_PhysicalUnit = value; } }

        //当获取不到设备DPI时，默认使用的设备DPI值
        [Tooltip("The DPI to assume if the screen DPI is not known.")]
        [SerializeField] protected float m_FallbackScreenDPI = 96;

        /// <summary>
        /// The DPI to assume if the screen DPI is not known.
        /// 当获取不到设备DPI时，默认使用的设备DPI值
        /// </summary>
        public float fallbackScreenDPI { get { return m_FallbackScreenDPI; } set { m_FallbackScreenDPI = value; } }

        //默认的精灵DPI，当精灵设置为Reference Pixels Per Unit时使用
        [Tooltip("The pixels per inch to use for sprites that have a 'Pixels Per Unit' setting that matches the 'Reference Pixels Per Unit' setting.")]
        [SerializeField] protected float m_DefaultSpriteDPI = 96;

        /// <summary>
        /// The pixels per inch to use for sprites that have a 'Pixels Per Unit' setting that matches the 'Reference Pixels Per Unit' setting.
        /// </summary>
        public float defaultSpriteDPI { get { return m_DefaultSpriteDPI; } set { m_DefaultSpriteDPI = Mathf.Max(1, value); } }


        // World Canvas settings
        //世界空间下Canvas设置：动态PPU，主要是用于世界空间下动态位图（主要是Text），默认是1个像素代表一个Unity单位
        //如果要提高表现精度，可以设置为2等等，但是会消耗更多性能
        [Tooltip("The amount of pixels per unit to use for dynamically created bitmaps in the UI, such as Text.")]
        [SerializeField] protected float m_DynamicPixelsPerUnit = 1;

        /// <summary>
        /// The amount of pixels per unit to use for dynamically created bitmaps in the UI, such as Text.
        /// </summary>
        public float dynamicPixelsPerUnit { get { return m_DynamicPixelsPerUnit; } set { m_DynamicPixelsPerUnit = value; } }


        // General variables

        private Canvas m_Canvas;
        //上次设置的scaleFactor
        [System.NonSerialized]
        private float m_PrevScaleFactor = 1;
        //上次设置的PPU
        [System.NonSerialized]
        private float m_PrevReferencePixelsPerUnit = 100;

        [SerializeField] protected bool m_PresetInfoIsWorld = false;

        protected CanvasScaler() {}

        protected override void OnEnable()
        {
            base.OnEnable();
            m_Canvas = GetComponent<Canvas>();
            Handle();
            Canvas.preWillRenderCanvases += Canvas_preWillRenderCanvases;
        }

        private void Canvas_preWillRenderCanvases()
        {
            Handle();
        }

        protected override void OnDisable()
        {
            SetScaleFactor(1);
            SetReferencePixelsPerUnit(100);
            Canvas.preWillRenderCanvases -= Canvas_preWillRenderCanvases;
            base.OnDisable();
        }

        ///<summary>
        ///Method that handles calculations of canvas scaling.
        /// 在Canvas重绘之前会调用处理
        ///</summary>
        protected virtual void Handle()
        {
            //只能在根Canvas上调用，子Canvas的不生效
            if (m_Canvas == null || !m_Canvas.isRootCanvas)
                return;

            //如果是世界空间的Canvas
            if (m_Canvas.renderMode == RenderMode.WorldSpace)
            {
                HandleWorldCanvas();
                return;
            }

            //不是世界空间Canvas，而是屏幕空间的Canvas，那么走下面的逻辑
            switch (m_UiScaleMode)
            {
                case ScaleMode.ConstantPixelSize: HandleConstantPixelSize(); break;
                case ScaleMode.ScaleWithScreenSize: HandleScaleWithScreenSize(); break;
                case ScaleMode.ConstantPhysicalSize: HandleConstantPhysicalSize(); break;
            }
        }

        /// <summary>
        /// Handles canvas scaling for world canvas.
        /// 世界空间的Canvas进行处理
        /// </summary>
        protected virtual void HandleWorldCanvas()
        {
            //世界空间使用 m_DynamicPixelsPerUnit 作为缩放因子
            SetScaleFactor(m_DynamicPixelsPerUnit);
            SetReferencePixelsPerUnit(m_ReferencePixelsPerUnit);
        }

        /// <summary>
        /// Handles canvas scaling for a constant pixel size.
        /// constant pixel size固定像素渲染类型，直接使用原始缩放因子，不会根据分辨率等进行重新计算
        /// </summary>
        protected virtual void HandleConstantPixelSize()
        {
            SetScaleFactor(m_ScaleFactor);
            SetReferencePixelsPerUnit(m_ReferencePixelsPerUnit);
        }

        /// <summary>
        /// Handles canvas scaling that scales with the screen size.
        /// scales with the screen size跟随屏幕类型，进行缩放因子计算
        /// </summary>
        protected virtual void HandleScaleWithScreenSize()
        {
            //当前Canvas的渲染尺寸，像素值，不受缩放因子影响的
            //在Screen Space模式下的Canvas，这个尺寸等同于屏幕渲染尺寸
            Vector2 screenSize = m_Canvas.renderingDisplaySize;

            // Multiple display support only when not the main display. For display 0 the reported
            // resolution is always the desktops resolution since its part of the display API,
            // so we use the standard none multiple display method. (case 741751)
            //查看使用的屏幕，如果是屏幕不是屏幕0，那么screenSize将会修改为屏幕的渲染尺寸
            int displayIndex = m_Canvas.targetDisplay;
            if (displayIndex > 0 && displayIndex < Display.displays.Length)
            {
                Display disp = Display.displays[displayIndex];
                screenSize = new Vector2(disp.renderingWidth, disp.renderingHeight);
            }


            float scaleFactor = 0;
            switch (m_ScreenMatchMode)
            {
                //如果是跟随宽度或者高度
                //使用对数计算，能够计算出正确的缩放倍数。具体的数学逻辑看不懂了已经。
                //其实就是计算现在的分辨率、参考分辨率之间的比例，看多大的缩放才能适配这个当前的分辨率
                case ScreenMatchMode.MatchWidthOrHeight:
                {
                    // We take the log of the relative width and height before taking the average.
                    // Then we transform it back in the original space.
                    // the reason to transform in and out of logarithmic space is to have better behavior.
                    // If one axis has twice resolution and the other has half, it should even out if widthOrHeight value is at 0.5.
                    // In normal space the average would be (0.5 + 2) / 2 = 1.25
                    // In logarithmic space the average is (-1 + 1) / 2 = 0
                    float logWidth = Mathf.Log(screenSize.x / m_ReferenceResolution.x, kLogBase);
                    float logHeight = Mathf.Log(screenSize.y / m_ReferenceResolution.y, kLogBase);
                    float logWeightedAverage = Mathf.Lerp(logWidth, logHeight, m_MatchWidthOrHeight);
                    scaleFactor = Mathf.Pow(kLogBase, logWeightedAverage);
                    break;
                }
                //扩展模式，屏幕分辨率的长宽、与参考分辨率的长宽的比值中，较小的那个值作为缩放因子。
                case ScreenMatchMode.Expand:
                {
                    scaleFactor = Mathf.Min(screenSize.x / m_ReferenceResolution.x, screenSize.y / m_ReferenceResolution.y);
                    break;
                }
                //适配模式，屏幕分辨率的长宽、与参考分辨率的长宽的比值中，较大的那个值作为缩放因子。
                case ScreenMatchMode.Shrink:
                {
                    scaleFactor = Mathf.Max(screenSize.x / m_ReferenceResolution.x, screenSize.y / m_ReferenceResolution.y);
                    break;
                }
            }

            SetScaleFactor(scaleFactor);
            SetReferencePixelsPerUnit(m_ReferencePixelsPerUnit);
        }

        ///<summary>
        ///Handles canvas scaling for a constant physical size.
        /// constant physical size模式，需要用到DPI，这样才能计算出物理尺寸
        ///</summary>
        protected virtual void HandleConstantPhysicalSize()
        {
            float currentDpi = Screen.dpi;
            float dpi = (currentDpi == 0 ? m_FallbackScreenDPI : currentDpi);
            float targetDPI = 1;
            switch (m_PhysicalUnit)
            {
                case Unit.Centimeters: targetDPI = 2.54f; break;
                case Unit.Millimeters: targetDPI = 25.4f; break;
                case Unit.Inches:      targetDPI =     1; break;
                case Unit.Points:      targetDPI =    72; break;
                case Unit.Picas:       targetDPI =     6; break;
            }

            SetScaleFactor(dpi / targetDPI);
            SetReferencePixelsPerUnit(m_ReferencePixelsPerUnit * targetDPI / m_DefaultSpriteDPI);
        }

        /// <summary>
        /// Sets the scale factor on the canvas.
        /// 直接设置Canvas的scaleFactor
        /// </summary>
        /// <param name="scaleFactor">The scale factor to use.</param>
        protected void SetScaleFactor(float scaleFactor)
        {
            //如果缩放值没有变化，那么不设置
            if (scaleFactor == m_PrevScaleFactor)
                return;

            m_Canvas.scaleFactor = scaleFactor;
            m_PrevScaleFactor = scaleFactor;
        }

        /// <summary>
        /// Sets the referencePixelsPerUnit on the Canvas.
        /// 直接设置Canvas的PPU
        /// </summary>
        /// <param name="referencePixelsPerUnit">The new reference pixels per Unity value</param>
        protected void SetReferencePixelsPerUnit(float referencePixelsPerUnit)
        {
            //没有发生变化，不设置
            if (referencePixelsPerUnit == m_PrevReferencePixelsPerUnit)
                return;

            m_Canvas.referencePixelsPerUnit = referencePixelsPerUnit;
            m_PrevReferencePixelsPerUnit = referencePixelsPerUnit;
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            m_ScaleFactor = Mathf.Max(0.01f, m_ScaleFactor);
            m_DefaultSpriteDPI = Mathf.Max(1, m_DefaultSpriteDPI);
        }

#endif
    }
}
