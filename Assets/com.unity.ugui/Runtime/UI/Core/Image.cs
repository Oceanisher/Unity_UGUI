using System;
using System.Collections.Generic;
using System.Text;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine.Experimental.Rendering;
using UnityEngine.Serialization;
using UnityEngine.U2D;

namespace UnityEngine.UI
{
    /// <summary>
    /// Image is a textured element in the UI hierarchy.
    /// </summary>

    [RequireComponent(typeof(CanvasRenderer))]
    [AddComponentMenu("UI/Image", 11)]
    /// <summary>
    ///   Displays a Sprite inside the UI System.
    /// 图片组件，用于展示精灵Sprite
    /// </summary>
    public class Image : MaskableGraphic, ISerializationCallbackReceiver, ILayoutElement, ICanvasRaycastFilter
    {
        /// <summary>
        /// Image fill type controls how to display the image.
        /// 图片组件填充类型
        /// </summary>
        public enum Type
        {
            /// <summary>
            /// Displays the full Image
            /// 简单模式，填充图片到整个RectTransform
            /// </summary>
            /// <remarks>
            /// This setting shows the entire image stretched across the Image's RectTransform
            /// </remarks>
            Simple,

            /// <summary>
            /// Displays the Image as a 9-sliced graphic.
            /// 九宫模式，展示九宫图片
            /// Sprite必须有Border配置，否则不生效
            /// </summary>
            /// <remarks>
            /// A 9-sliced image displays a central area stretched across the image surrounded by a border comprising of 4 corners and 4 stretched edges.
            ///
            /// This has the effect of creating a resizable skinned rectangular element suitable for dialog boxes, windows, and general UI elements.
            ///
            /// Note: For this method to work properly the Sprite assigned to Image.sprite needs to have Sprite.border defined.
            /// </remarks>
            Sliced,

            /// <summary>
            /// Displays a sliced Sprite with its resizable sections tiled instead of stretched.
            /// 平铺模式
            /// 也是用Sprite的Border属性，来决定边界和中心如何进行平铺
            /// 如果使用带Border或者压缩的sprite，会生成新的Mesh
            /// 所以基于性能的考虑，最好使用没有Border、不压缩的Sprite，并且把Sprite.texture的wrap mode设置为TextureWrapMode.Repeat，这样能避免生成新的Mesh。
            /// 如果实在没办法，那么要注意生成的Tile的数量。
            /// </summary>
            /// <remarks>
            /// A Tiled image behaves similarly to a UI.Image.Type.Sliced|Sliced image, except that the resizable sections of the image are repeated instead of being stretched. This can be useful for detailed UI graphics that do not look good when stretched.
            ///
            /// It uses the Sprite.border value to determine how each part (border and center) should be tiled.
            ///
            /// The Image sections will repeat the corresponding section in the Sprite until the whole section is filled. The corner sections will be unaffected and will draw in the same way as a Sliced Image. The edges will repeat along their lengths. The center section will repeat across the whole central part of the Image.
            ///
            /// The Image section will repeat the corresponding section in the Sprite until the whole section is filled.
            ///
            /// Be aware that if you are tiling a Sprite with borders or a packed sprite, a mesh will be generated to create the tiles. The size of the mesh will be limited to 16250 quads; if your tiling would require more tiles, the size of the tiles will be enlarged to ensure that the number of generated quads stays below this limit.
            ///
            /// For optimum efficiency, use a Sprite with no borders and with no packing, and make sure the Sprite.texture wrap mode is set to TextureWrapMode.Repeat.These settings will prevent the generation of additional geometry.If this is not possible, limit the number of tiles in your Image.
            /// </remarks>
            Tiled,

            /// <summary>
            /// Displays only a portion of the Image.
            /// 部分模式
            /// 用于控制展示图片的一部分，可以当做圆形进度条使用。
            /// 用FillMethod、FillAmount去控制展示的方式和进度
            /// </summary>
            /// <remarks>
            /// A Filled Image will display a section of the Sprite, with the rest of the RectTransform left transparent. The Image.fillAmount determines how much of the Image to show, and Image.fillMethod controls the shape in which the Image will be cut.
            ///
            /// This can be used for example to display circular or linear status information such as timers, health bars, and loading bars.
            /// </remarks>
            Filled
        }

        /// <summary>
        /// The possible fill method types for a Filled Image.
        /// 当Image的填充模式设置为Filled时，可以选择的填充方式
        /// </summary>
        public enum FillMethod
        {
            /// <summary>
            /// The Image will be filled Horizontally.
            /// 水平填充
            /// 像普通水平进度条一样进行填充
            /// </summary>
            /// <remarks>
            /// The Image will be Cropped at either left or right size depending on Image.fillOriging at the Image.fillAmount
            /// </remarks>
            Horizontal,

            /// <summary>
            /// The Image will be filled Vertically.
            /// 竖直填充
            /// </summary>
            /// <remarks>
            /// The Image will be Cropped at either top or Bottom size depending on Image.fillOrigin at the Image.fillAmount
            /// </remarks>
            Vertical,

            /// <summary>
            /// The Image will be filled Radially with the radial center in one of the corners.
            /// 90度扇形填充
            /// </summary>
            /// <remarks>
            /// For this method the Image.fillAmount represents an angle between 0 and 90 degrees. The Image will be cut by a line passing at the Image.fillOrigin at the specified angle.
            /// </remarks>
            Radial90,

            /// <summary>
            /// The Image will be filled Radially with the radial center in one of the edges.
            /// 180度扇形填充
            /// </summary>
            /// <remarks>
            /// For this method the Image.fillAmount represents an angle between 0 and 180 degrees. The Image will be cut by a line passing at the Image.fillOrigin at the specified angle.
            /// </remarks>
            Radial180,

            /// <summary>
            /// The Image will be filled Radially with the radial center at the center.
            /// 360度环形填充
            /// </summary>
            /// <remarks>
            /// or this method the Image.fillAmount represents an angle between 0 and 360 degrees. The Arc defined by the center of the Image, the Image.fillOrigin and the angle will be cut from the Image.
            /// </remarks>
            Radial360,
        }

        /// <summary>
        /// Origin for the Image.FillMethod.Horizontal.
        /// 水平填充时的起始位置
        /// </summary>
        public enum OriginHorizontal
        {
            /// <summary>
            /// >Origin at the Left side.
            /// </summary>
            Left,

            /// <summary>
            /// >Origin at the Right side.
            /// </summary>
            Right,
        }


        /// <summary>
        /// Origin for the Image.FillMethod.Vertical.
        /// 竖直填充时的起始位置
        /// </summary>
        public enum OriginVertical
        {
            /// <summary>
            /// >Origin at the Bottom Edge.
            /// </summary>
            Bottom,

            /// <summary>
            /// >Origin at the Top Edge.
            /// </summary>
            Top,
        }

        /// <summary>
        /// Origin for the Image.FillMethod.Radial90.
        /// 90度扇形填充时的起始位置
        /// </summary>
        public enum Origin90
        {
            /// <summary>
            /// Radial starting at the Bottom Left corner.
            /// </summary>
            BottomLeft,

            /// <summary>
            /// Radial starting at the Top Left corner.
            /// </summary>
            TopLeft,

            /// <summary>
            /// Radial starting at the Top Right corner.
            /// </summary>
            TopRight,

            /// <summary>
            /// Radial starting at the Bottom Right corner.
            /// </summary>
            BottomRight,
        }

        /// <summary>
        /// Origin for the Image.FillMethod.Radial180.
        /// 180度扇形填充时的起始位置
        /// </summary>
        public enum Origin180
        {
            /// <summary>
            /// Center of the radial at the center of the Bottom edge.
            /// </summary>
            Bottom,

            /// <summary>
            /// Center of the radial at the center of the Left edge.
            /// </summary>
            Left,

            /// <summary>
            /// Center of the radial at the center of the Top edge.
            /// </summary>
            Top,

            /// <summary>
            /// Center of the radial at the center of the Right edge.
            /// </summary>
            Right,
        }

        /// <summary>
        /// One of the points of the Arc for the Image.FillMethod.Radial360.
        /// 360度环形填充时的起始位置
        /// </summary>
        public enum Origin360
        {
            /// <summary>
            /// Arc starting at the center of the Bottom edge.
            /// </summary>
            Bottom,

            /// <summary>
            /// Arc starting at the center of the Right edge.
            /// </summary>
            Right,

            /// <summary>
            /// Arc starting at the center of the Top edge.
            /// </summary>
            Top,

            /// <summary>
            /// Arc starting at the center of the Left edge.
            /// </summary>
            Left,
        }

        static protected Material s_ETC1DefaultUI = null;

        //使用的精灵，用于渲染
        [FormerlySerializedAs("m_Frame")]
        [SerializeField]
        private Sprite m_Sprite;

        /// <summary>
        /// The sprite that is used to render this image.
        /// </summary>
        /// <remarks>
        /// This returns the source Sprite of an Image. This Sprite can also be viewed and changed in the Inspector as part of an Image component. This can also be used to change the Sprite using a script.
        /// </remarks>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// //Attach this script to an Image GameObject and set its Source Image to the Sprite you would like.
        /// //Press the space key to change the Sprite. Remember to assign a second Sprite in this script's section of the Inspector.
        ///
        /// using UnityEngine;
        /// using UnityEngine.UI;
        ///
        /// public class Example : MonoBehaviour
        /// {
        ///     Image m_Image;
        ///     //Set this in the Inspector
        ///     public Sprite m_Sprite;
        ///
        ///     void Start()
        ///     {
        ///         //Fetch the Image from the GameObject
        ///         m_Image = GetComponent<Image>();
        ///     }
        ///
        ///     void Update()
        ///     {
        ///         //Press space to change the Sprite of the Image
        ///         if (Input.GetKey(KeyCode.Space))
        ///         {
        ///             m_Image.sprite = m_Sprite;
        ///         }
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>

        //精灵
        public Sprite sprite
        {
            get { return m_Sprite; }
            set
            {
                //如果当前有精灵了
                if (m_Sprite != null)
                {
                    if (m_Sprite != value)
                    {
                        //当新旧精灵尺寸不一样的时候，才会触发布局重建
                        m_SkipLayoutUpdate = m_Sprite.rect.size.Equals(value ? value.rect.size : Vector2.zero);
                        //当新旧精灵纹理不一样的时候，才会触发图形重建
                        m_SkipMaterialUpdate = m_Sprite.texture == (value ? value.texture : null);
                        m_Sprite = value;

                        //重置Alpha检测阈值
                        ResetAlphaHitThresholdIfNeeded();
                        //设置全部Dirty
                        SetAllDirty();
                        TrackSprite();
                    }
                }
                //如果当前没有精灵、且新的精灵不为空，才会进行设置
                else if (value != null)
                {
                    //如果新的精灵尺寸为0，那么跳过布局重建
                    m_SkipLayoutUpdate = value.rect.size == Vector2.zero;
                    //如果精灵的纹理为空，那么跳过图形重建
                    m_SkipMaterialUpdate = value.texture == null;
                    m_Sprite = value;

                    //重置Alpha检测阈值
                    ResetAlphaHitThresholdIfNeeded();
                    //设置全部Dirty
                    SetAllDirty();
                    TrackSprite();
                }

                //重置Alpha点击测试阈值
                void ResetAlphaHitThresholdIfNeeded()
                {
                    //如果Sprite不支持Alpha点击检测，并且当前阈值大于0，那么重置阈值为0
                    //也就是说所有透明度大于0的地方都能被点击检测
                    if (!SpriteSupportsAlphaHitTest() && m_AlphaHitTestMinimumThreshold > 0)
                    {
                        Debug.LogWarning("Sprite was changed for one not readable or with Crunch Compression. Resetting the AlphaHitThreshold to 0.", this);
                        m_AlphaHitTestMinimumThreshold = 0;
                    }
                }

                //精灵是否支持Alpha点击检测
                //如果想要实现非矩形的、不规则的点击区域，那么精灵需要能够支持Alpha检测
                //条件：精灵开启Read/Write选项，并且Format压缩格式不是Crunch
                bool SpriteSupportsAlphaHitTest()
                {
                    return m_Sprite != null && m_Sprite.texture != null && !GraphicsFormatUtility.IsCrunchFormat(m_Sprite.texture.format) && m_Sprite.texture.isReadable;
                }
            }
        }


        /// <summary>
        /// Disable all automatic sprite optimizations.
        /// 禁用精灵的自动优化
        /// 其实就是跳过布局、图形重建
        /// </summary>
        /// <remarks>
        /// When a new Sprite is assigned update optimizations are automatically applied.
        /// </remarks>

        public void DisableSpriteOptimizations()
        {
            m_SkipLayoutUpdate = false;
            m_SkipMaterialUpdate = false;
        }

        //覆盖的Sprite
        [NonSerialized]
        private Sprite m_OverrideSprite;

        /// <summary>
        /// Set an override sprite to be used for rendering.
        /// 覆盖的Sprite，当不为空时，优先渲染覆盖的精灵；否则渲染原始精灵
        /// 用于临时性展示其他图片精灵
        /// </summary>
        /// <remarks>
        /// The UI.Image-overrideSprite|overrideSprite variable allows a sprite to have the
        /// sprite changed.This change happens immediately.When the changed
        /// sprite is no longer needed the sprite can be reverted back to the
        /// original version.This happens when the overrideSprite
        /// is set to /null/.
        /// </remarks>
        /// <example>
        /// Note: The script example below has two buttons.  The button textures are loaded from the
        /// /Resources/ folder.  (They are not used in the shown example).  Two sprites are added to
        /// the example code.  /Example1/ and /Example2/ are functions called by the button OnClick
        /// functions.  Example1 calls overrideSprite and Example2 sets overrideSprite to null.
        /// <code>
        /// <![CDATA[
        /// using System.Collections;
        /// using System.Collections.Generic;
        /// using UnityEngine;
        /// using UnityEngine.UI;
        ///
        /// public class ExampleClass : MonoBehaviour
        /// {
        ///     private Sprite sprite1;
        ///     private Sprite sprite2;
        ///     private Image i;
        ///
        ///     public void Start()
        ///     {
        ///         i = GetComponent<Image>();
        ///         sprite1 = Resources.Load<Sprite>("texture1");
        ///         sprite2 = Resources.Load<Sprite>("texture2");
        ///
        ///         i.sprite = sprite1;
        ///     }
        ///
        ///     // Called by a Button OnClick() with ExampleClass.Example1
        ///     // Uses overrideSprite to make this change temporary
        ///     public void Example1()
        ///     {
        ///         i.overrideSprite = sprite2;
        ///     }
        ///
        ///     // Called by a Button OnClick() with ExampleClass.Example2
        ///     // Removes the overrideSprite which causes the original sprite to be used again.
        ///     public void Example2()
        ///     {
        ///         i.overrideSprite = null;
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
        public Sprite overrideSprite
        {
            get { return activeSprite; }
            set
            {
                //设置覆盖的精灵，也会导致布局和图形重建
                if (SetPropertyUtility.SetClass(ref m_OverrideSprite, value))
                {
                    SetAllDirty();
                    TrackSprite();
                }
            }
        }

        //当前生效的精灵，优先选择覆盖的精灵
        private Sprite activeSprite { get { return m_OverrideSprite != null ? m_OverrideSprite : sprite; } }

        /// How the Image is drawn.
        /// 图片展示的方式
        [SerializeField] private Type m_Type = Type.Simple;

        /// <summary>
        /// How to display the image.
        /// 图片展示的方式
        /// </summary>
        /// <remarks>
        /// Unity can interpret an Image in various different ways depending on the intended purpose. This can be used to display:
        /// - Whole images stretched to fit the RectTransform of the Image.
        /// - A 9-sliced image useful for various decorated UI boxes and other rectangular elements.
        /// - A tiled image with sections of the sprite repeated.
        /// - As a partial image, useful for wipes, fades, timers, status bars etc.
        /// </remarks>
        public Type type { get { return m_Type; } set { if (SetPropertyUtility.SetStruct(ref m_Type, value)) SetVerticesDirty(); } }

        //是否要保持精灵的纵横比
        [SerializeField] private bool m_PreserveAspect = false;

        /// <summary>
        /// Whether this image should preserve its Sprite aspect ratio.
        /// 是否要保持精灵的纵横比
        /// </summary>
        public bool preserveAspect { get { return m_PreserveAspect; } set { if (SetPropertyUtility.SetStruct(ref m_PreserveAspect, value)) SetVerticesDirty(); } }

        //九宫或者平铺模式下，是否要绘制精灵的中心部分
        //Sprite本身必须有Borders才能生效
        [SerializeField] private bool m_FillCenter = true;

        /// <summary>
        /// Whether or not to render the center of a Tiled or Sliced image.
        /// 九宫或者平铺模式下，是否要绘制精灵的中心部分
        /// Sprite本身必须有Borders才能生效
        /// </summary>
        /// <remarks>
        /// This will only have any effect if the Image.sprite has borders.
        /// </remarks>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// using UnityEngine;
        /// using System.Collections;
        /// using UnityEngine.UI;
        ///
        /// public class FillCenterScript : MonoBehaviour
        /// {
        ///     public Image xmasCalenderDoor;
        ///
        ///     // removes the center of the image to reveal the image behind it
        ///     void OpenCalendarDoor()
        ///     {
        ///         xmasCalenderDoor.fillCenter = false;
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
        public bool fillCenter { get { return m_FillCenter; } set { if (SetPropertyUtility.SetStruct(ref m_FillCenter, value)) SetVerticesDirty(); } }

        /// Filling method for filled sprites.
        /// Filled模式下，具体的Fill方式
        [SerializeField] private FillMethod m_FillMethod = FillMethod.Radial360;
        public FillMethod fillMethod { get { return m_FillMethod; } set { if (SetPropertyUtility.SetStruct(ref m_FillMethod, value)) { SetVerticesDirty(); m_FillOrigin = 0; } } }

        /// Amount of the Image shown. 0-1 range with 0 being nothing shown, and 1 being the full Image.
        //Filled模式下，填充度0~1
        [Range(0, 1)]
        [SerializeField]
        private float m_FillAmount = 1.0f;

        /// <summary>
        /// Amount of the Image shown when the Image.type is set to Image.Type.Filled.
        /// Filled模式下，填充度0~1
        /// </summary>
        /// <remarks>
        /// 0-1 range with 0 being nothing shown, and 1 being the full Image.
        /// </remarks>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// using UnityEngine;
        /// using System.Collections;
        /// using UnityEngine.UI; // Required when Using UI elements.
        ///
        /// public class Cooldown : MonoBehaviour
        /// {
        ///     public Image cooldown;
        ///     public bool coolingDown;
        ///     public float waitTime = 30.0f;
        ///
        ///     // Update is called once per frame
        ///     void Update()
        ///     {
        ///         if (coolingDown == true)
        ///         {
        ///             //Reduce fill amount over 30 seconds
        ///             cooldown.fillAmount -= 1.0f / waitTime * Time.deltaTime;
        ///         }
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
        public float fillAmount { get { return m_FillAmount; } set { if (SetPropertyUtility.SetStruct(ref m_FillAmount, Mathf.Clamp01(value))) SetVerticesDirty(); } }

        /// Whether the Image should be filled clockwise (true) or counter-clockwise (false).
        /// Filled模式下，从0~1的增长方向，顺时针还是逆时针
        [SerializeField] private bool m_FillClockwise = true;

        /// <summary>
        /// Whether the Image should be filled clockwise (true) or counter-clockwise (false).
        /// Filled模式下，从0~1的增长方向，顺时针还是逆时针
        /// </summary>
        /// <remarks>
        /// This will only have any effect if the Image.type is set to Image.Type.Filled and Image.fillMethod is set to any of the Radial methods.
        /// </remarks>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// using UnityEngine;
        /// using System.Collections;
        /// using UnityEngine.UI; // Required when Using UI elements.
        ///
        /// public class FillClockwiseScript : MonoBehaviour
        /// {
        ///     public Image healthCircle;
        ///
        ///     // This method sets the direction of the health circle.
        ///     // Clockwise for the Player, Counter Clockwise for the opponent.
        ///     void SetHealthDirection(GameObject target)
        ///     {
        ///         if (target.tag == "Player")
        ///         {
        ///             healthCircle.fillClockwise = true;
        ///         }
        ///         else if (target.tag == "Opponent")
        ///         {
        ///             healthCircle.fillClockwise = false;
        ///         }
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
        public bool fillClockwise { get { return m_FillClockwise; } set { if (SetPropertyUtility.SetStruct(ref m_FillClockwise, value)) SetVerticesDirty(); } }

        /// Controls the origin point of the Fill process. Value means different things with each fill method.
        /// Filled模式下，初始方向。根据不同的FilledMethod，这个值不同
        /// 可以根据不同的方式，映射到不同的枚举 OriginHorizontal、OriginVertical、Origin90、Origin180、Origin360
        [SerializeField] private int m_FillOrigin;

        /// <summary>
        /// Controls the origin point of the Fill process. Value means different things with each fill method.
        /// Filled模式下，初始方向。根据不同的FilledMethod，这个值不同
        /// 可以根据不同的方式，映射到不同的枚举 OriginHorizontal、OriginVertical、Origin90、Origin180、Origin360
        /// </summary>
        /// <remarks>
        /// You should cast to the appropriate origin type: Image.OriginHorizontal, Image.OriginVertical, Image.Origin90, Image.Origin180 or Image.Origin360 depending on the Image.Fillmethod.
        /// Note: This will only have any effect if the Image.type is set to Image.Type.Filled.
        /// </remarks>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// using UnityEngine;
        /// using UnityEngine.UI;
        /// using System.Collections;
        ///
        /// [RequireComponent(typeof(Image))]
        /// public class ImageOriginCycle : MonoBehaviour
        /// {
        ///     void OnEnable()
        ///     {
        ///         Image image = GetComponent<Image>();
        ///         string fillOriginName = "";
        ///
        ///         switch ((Image.FillMethod)image.fillMethod)
        ///         {
        ///             case Image.FillMethod.Horizontal:
        ///                 fillOriginName = ((Image.OriginHorizontal)image.fillOrigin).ToString();
        ///                 break;
        ///             case Image.FillMethod.Vertical:
        ///                 fillOriginName = ((Image.OriginVertical)image.fillOrigin).ToString();
        ///                 break;
        ///             case Image.FillMethod.Radial90:
        ///
        ///                 fillOriginName = ((Image.Origin90)image.fillOrigin).ToString();
        ///                 break;
        ///             case Image.FillMethod.Radial180:
        ///
        ///                 fillOriginName = ((Image.Origin180)image.fillOrigin).ToString();
        ///                 break;
        ///             case Image.FillMethod.Radial360:
        ///                 fillOriginName = ((Image.Origin360)image.fillOrigin).ToString();
        ///                 break;
        ///         }
        ///         Debug.Log(string.Format("{0} is using {1} fill method with the origin on {2}", name, image.fillMethod, fillOriginName));
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
        public int fillOrigin { get { return m_FillOrigin; } set { if (SetPropertyUtility.SetStruct(ref m_FillOrigin, value)) SetVerticesDirty(); } }

        // Not serialized until we support read-enabled sprites better.
        //Alpha点击测试的最小阈值
        private float m_AlphaHitTestMinimumThreshold = 0;

        // Whether this is being tracked for Atlas Binding.
        //是否在图集绑定中追踪
        private bool m_Tracked = false;

        [Obsolete("eventAlphaThreshold has been deprecated. Use eventMinimumAlphaThreshold instead (UnityUpgradable) -> alphaHitTestMinimumThreshold")]

        /// <summary>
        /// Obsolete. You should use UI.Image.alphaHitTestMinimumThreshold instead.
        /// The alpha threshold specifies the minimum alpha a pixel must have for the event to considered a "hit" on the Image.
        /// </summary>
        public float eventAlphaThreshold { get { return 1 - alphaHitTestMinimumThreshold; } set { alphaHitTestMinimumThreshold = 1 - value; } }

        /// <summary>
        /// The alpha threshold specifies the minimum alpha a pixel must have for the event to considered a "hit" on the Image.
        /// alpha点击时像素可响应的最小Alpha阈值
        /// 如果像素Alpha小于这个值，那么射线检测将穿透该像素
        /// 是检测Sprite像素的Alpha，而不是Image组件的Alpha值（UI.Graphic.color）
        /// Sprite必须开启Read/write，并且不能使用Crunch压缩
        /// </summary>
        /// <remarks>
        /// Alpha values less than the threshold will cause raycast events to pass through the Image. An value of 1 would cause only fully opaque pixels to register raycast events on the Image. The alpha tested is retrieved from the image sprite only, while the alpha of the Image [[UI.Graphic.color]] is disregarded.
        ///
        /// alphaHitTestMinimumThreshold defaults to 0; all raycast events inside the Image rectangle are considered a hit. In order for greater than 0 to values to work, the sprite used by the Image must have readable pixels. This can be achieved by enabling Read/Write enabled in the advanced Texture Import Settings for the sprite and disabling atlassing for the sprite.
        /// </remarks>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// using UnityEngine;
        /// using System.Collections;
        /// using UnityEngine.UI; // Required when Using UI elements.
        ///
        /// public class ExampleClass : MonoBehaviour
        /// {
        ///     public Image theButton;
        ///
        ///     // Use this for initialization
        ///     void Start()
        ///     {
        ///         theButton.alphaHitTestMinimumThreshold = 0.5f;
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
        public float alphaHitTestMinimumThreshold { get { return m_AlphaHitTestMinimumThreshold; }
            set
            {
                if (sprite != null && (GraphicsFormatUtility.IsCrunchFormat(sprite.texture.format) || !sprite.texture.isReadable))
                    throw new InvalidOperationException("alphaHitTestMinimumThreshold should not be modified on a texture not readeable or not using Crunch Compression.");

                m_AlphaHitTestMinimumThreshold = value;
            }
        }

        /// Controls whether or not to use the generated mesh from the sprite importer.
        /// 是否使用精灵的TextureImporter生成的Mesh，或者仅使用简单QuadMesh
        /// 如果Sprite导入的方式是Tight、而不是FullRect，那么必须启用该选项，否则可能会渲染到其他Sprite的一部分
        /// 如果Sprite自身不是Tight的，那么TextureImporter也只会生成一个简单QuadMesh，此时即使启用了该选项，也会被忽略。
        [SerializeField] private bool m_UseSpriteMesh;

        /// <summary>
        /// Allows you to specify whether the UI Image should be displayed using the mesh generated by the TextureImporter, or by a simple quad mesh.
        /// 是否使用精灵的TextureImporter生成的Mesh，或者仅使用简单Quad Mesh
        /// 如果Sprite导入的方式是Tight、而不是FullRect，那么必须启用该选项，否则可能会渲染到其他Sprite的一部分
        /// 如果Sprite自身不是Tight的，那么TextureImporter也只会生成一个简单QuadMesh，此时即使启用了该选项，也会被忽略。
        /// </summary>
        /// <remarks>
        /// When this property is set to false, the UI Image uses a simple quad. When set to true, the UI Image uses the sprite mesh generated by the [[TextureImporter]]. You should set this to true if you want to use a tightly fitted sprite mesh based on the alpha values in your image.
        /// Note: If the texture importer's SpriteMeshType property is set to SpriteMeshType.FullRect, it will only generate a quad, and not a tightly fitted sprite mesh, which means this UI image will be drawn using a quad regardless of the value of this property. Therefore, when enabling this property to use a tightly fitted sprite mesh, you must also ensure the texture importer's SpriteMeshType property is set to Tight.
        /// </remarks>
        public bool useSpriteMesh { get { return m_UseSpriteMesh; } set { if (SetPropertyUtility.SetStruct(ref m_UseSpriteMesh, value)) SetVerticesDirty(); } }


        protected Image()
        {
            useLegacyMeshGeneration = false;
        }

        /// <summary>
        /// Cache of the default Canvas Ericsson Texture Compression 1 (ETC1) and alpha Material.
        /// 缓存ETC1压缩方式的材质
        ///
        /// 确保 UI/DefaultETC1 shader包含在始终包含的Shader列表中，才能确保这里能获取到
        /// 可以从 Project Settings -> Graphics -> Allways Included Shaders中进行设置
        /// </summary>
        /// <remarks>
        /// Stores the ETC1 supported Canvas Material that is returned from GetETC1SupportedCanvasMaterial().
        /// Note: Always specify the UI/DefaultETC1 Shader in the Always Included Shader list, to use the ETC1 and alpha Material.
        /// </remarks>
        static public Material defaultETC1GraphicMaterial
        {
            get
            {
                if (s_ETC1DefaultUI == null)
                    s_ETC1DefaultUI = Canvas.GetETC1SupportedCanvasMaterial();
                return s_ETC1DefaultUI;
            }
        }

        /// <summary>
        /// Image's texture comes from the UnityEngine.Image.
        /// 主纹理对象
        ///
        /// 优先从activeSprite的精灵中获取，再从material中获取主纹理，如果都没有，那么返回UI默认的纯白纹理。
        /// </summary>
        public override Texture mainTexture
        {
            get
            {
                if (activeSprite == null)
                {
                    if (material != null && material.mainTexture != null)
                    {
                        return material.mainTexture;
                    }
                    return s_WhiteTexture;
                }

                return activeSprite.texture;
            }
        }

        /// <summary>
        /// Whether the Sprite of the image has a border to work with.
        /// 当前activeSprite是否有Border、并且Border的四个边框不能都为0
        /// </summary>

        public bool hasBorder
        {
            get
            {
                if (activeSprite != null)
                {
                    Vector4 v = activeSprite.border;
                    return v.sqrMagnitude > 0f;
                }
                return false;
            }
        }


        //展示模式为Sliced平铺模式时，这个值能够决定平铺时的每个图像的缩放，不会为负值
        [SerializeField]
        private float m_PixelsPerUnitMultiplier = 1.0f;

        /// <summary>
        /// Pixel per unit modifier to change how sliced sprites are generated.
        /// 展示模式为Sliced平铺模式时，这个值能够决定平铺时的每个图像的缩放，不会为负值
        /// </summary>
        public float pixelsPerUnitMultiplier
        {
            get { return m_PixelsPerUnitMultiplier; }
            set
            {
                m_PixelsPerUnitMultiplier = Mathf.Max(0.01f, value);
                SetVerticesDirty();
            }
        }

        // case 1066689 cache referencePixelsPerUnit when canvas parent is disabled;
        //缓存的Canvas的每单位像素数，默认是100
        //PPU:多少个像素对应世界中的一个单位长度
        private float m_CachedReferencePixelsPerUnit = 100;

        //计算精灵中的PPU，与Canvas的PPU的比例
        //默认sprite中的PPU=100，默认Canvas的Scaler中的PPU=100，所以默认情况下这个值=1
        //越大，可以代表图像越小，因为每个世界单位长度需要更多的像素
        //越小，可以代表图像越大，因为每个世界单位长度需要更少的像素
        public float pixelsPerUnit
        {
            get
            {
                float spritePixelsPerUnit = 100;
                if (activeSprite)
                    spritePixelsPerUnit = activeSprite.pixelsPerUnit;

                if (canvas)
                    m_CachedReferencePixelsPerUnit = canvas.referencePixelsPerUnit;

                return spritePixelsPerUnit / m_CachedReferencePixelsPerUnit;
            }
        }

        //九宫/平铺模式下的每个图像大小的缩放，百分比
        protected float multipliedPixelsPerUnit
        {
            get { return pixelsPerUnit * m_PixelsPerUnitMultiplier; }
        }

        /// <summary>
        /// The specified Material used by this Image. The default Material is used instead if one wasn't specified.
        /// Image使用的材质
        /// 一般不设置，会直接使用UI的默认材质。如果设置了，那么就使用这个设置的材质。
        /// 在UI需要置灰或者特殊处理的时候有用，就像overrideSprite一样。
        /// </summary>
        public override Material material
        {
            get
            {
                if (m_Material != null)
                    return m_Material;

                //Edit and Runtime should use Split Alpha Shader if EditorSettings.spritePackerMode = Sprite Atlas V2
                //编辑器模式下，如果运行或者图集中、并且有Alpha通道纹理，那么默认材质是ETC1的材质。
                //如果使用ETC1等压格式，UNITY会自动生成包含透明通道的内部纹理
#if UNITY_EDITOR
                if ((Application.isPlaying || EditorSettings.spritePackerMode == SpritePackerMode.SpriteAtlasV2) &&
                    activeSprite && activeSprite.associatedAlphaSplitTexture != null)
                {
                    return defaultETC1GraphicMaterial;
                }
#else

                if (activeSprite && activeSprite.associatedAlphaSplitTexture != null)
                    return defaultETC1GraphicMaterial;
#endif

                return defaultMaterial;
            }

            set
            {
                base.material = value;
            }
        }

        /// <summary>
        /// See ISerializationCallbackReceiver.
        /// </summary>
        public virtual void OnBeforeSerialize() {}

        /// <summary>
        /// See ISerializationCallbackReceiver.
        /// 反序列化之后，防御性的将一些变量的值修正一下
        /// </summary>
        public virtual void OnAfterDeserialize()
        {
            if (m_FillOrigin < 0)
                m_FillOrigin = 0;
            else if (m_FillMethod == FillMethod.Horizontal && m_FillOrigin > 1)
                m_FillOrigin = 0;
            else if (m_FillMethod == FillMethod.Vertical && m_FillOrigin > 1)
                m_FillOrigin = 0;
            else if (m_FillOrigin > 3)
                m_FillOrigin = 0;

            m_FillAmount = Mathf.Clamp(m_FillAmount, 0f, 1f);
        }

        /// <summary>
        /// 根据精灵的原始尺寸，重新计算Rect的大小与位置，让Rect保持跟精灵同样的纵横比
        /// </summary>
        /// <param name="rect"></param>
        /// <param name="spriteSize"></param>
        private void PreserveSpriteAspectRatio(ref Rect rect, Vector2 spriteSize)
        {
            //计算精灵x与y的长度比例
            var spriteRatio = spriteSize.x / spriteSize.y;
            //计算当前Rect的x与y的长度比例
            var rectRatio = rect.width / rect.height;

            //如果精灵的比值大于Rect，也就是说精灵比Rect的比例更宽
            //那么保持Rect的宽度不变、改变Rect的高度值，让Rect保持跟精灵一样的比例
            //由于Rect的x、y代表该Rect的左上角，x向右是增加、y向下是增加
            //所以Rect高度改变后，它的y值要移动一下，因为要保持它的质心位置不变
            //Rect的y值变更的值，跟增加或者减少的高度有关
            if (spriteRatio > rectRatio)
            {
                var oldHeight = rect.height;
                rect.height = rect.width * (1.0f / spriteRatio);
                //Rect的y轴向下是增加
                //比如Rect的原始y是1.2，高度增加了1，质心是在中心（0.5,0.5），算出来Rect的y需要-0.5、变成0.7
                rect.y += (oldHeight - rect.height) * rectTransform.pivot.y;
            }
            //否则，保持Rect的高度不变、改变Rect的宽度
            //计算方式跟上面一致
            else
            {
                var oldWidth = rect.width;
                rect.width = rect.height * spriteRatio;
                rect.x += (oldWidth - rect.width) * rectTransform.pivot.x;
            }
        }

        /// Image's dimensions used for drawing. X = left, Y = bottom, Z = right, W = top.
        /// 获取需要绘制的区域的尺寸，世界空间尺寸，非像素
        private Vector4 GetDrawingDimensions(bool shouldPreserveAspect)
        {
            //精灵上下左右裁掉的空白像素大小
            var padding = activeSprite == null ? Vector4.zero : Sprites.DataUtility.GetPadding(activeSprite);
            //精灵的像素尺寸
            var size = activeSprite == null ? Vector2.zero : new Vector2(activeSprite.rect.width, activeSprite.rect.height);

            //获取该图形元素的绘制尺寸
            Rect r = GetPixelAdjustedRect();
            // Debug.Log(string.Format("r:{2}, size:{0}, padding:{1}", size, padding, r));

            //对精灵尺寸进行取整
            int spriteW = Mathf.RoundToInt(size.x);
            int spriteH = Mathf.RoundToInt(size.y);

            //精灵padding各部分的比例值
            //x：精灵padding的左边，占精灵尺寸宽度的百分比
            //y：精灵padding的下边，占精灵尺寸高度的百分比
            //z：精灵尺寸减去padding的右边，占精灵尺寸宽度的百分比
            //w：精灵尺寸减去padding的上边，占精灵尺寸高度的百分比
            var v = new Vector4(
                padding.x / spriteW,
                padding.y / spriteH,
                (spriteW - padding.z) / spriteW,
                (spriteH - padding.w) / spriteH);

            //如果是需要保持精灵的纵横比、并且精灵尺寸不为0，那么重新计算下该元素的绘制尺寸
            if (shouldPreserveAspect && size.sqrMagnitude > 0.0f)
            {
                PreserveSpriteAspectRatio(ref r, size);
            }

            //最终的绘制区域，需要在原有的绘制区域上、裁去padding部分
            v = new Vector4(
                r.x + r.width * v.x,
                r.y + r.height * v.y,
                r.x + r.width * v.z,
                r.y + r.height * v.w
            );

            return v;
        }

        /// <summary>
        /// Adjusts the image size to make it pixel-perfect.
        /// 调整组件大小，让其能够像素完美匹配精灵的尺寸
        /// 实际是调整RectTransform.sizeDelta为精灵的尺寸
        ///
        /// 目前是编辑器下调用
        /// </summary>
        /// <remarks>
        /// This means setting the Images RectTransform.sizeDelta to be equal to the Sprite dimensions.
        /// </remarks>
        public override void SetNativeSize()
        {
            if (activeSprite != null)
            {
                float w = activeSprite.rect.width / pixelsPerUnit;
                float h = activeSprite.rect.height / pixelsPerUnit;
                rectTransform.anchorMax = rectTransform.anchorMin;
                rectTransform.sizeDelta = new Vector2(w, h);
                SetAllDirty();
            }
        }

        /// <summary>
        /// Update the UI renderer mesh.
        /// 真正构建网格的地方
        /// 根据不同的Image类型、填充方式等进行不同的网格构建
        /// </summary>
        protected override void OnPopulateMesh(VertexHelper toFill)
        {
            //如果没有精灵，那么走父类Graphic的Mesh构建
            if (activeSprite == null)
            {
                base.OnPopulateMesh(toFill);
                return;
            }

            switch (type)
            {
                case Type.Simple:
                    if (!useSpriteMesh)
                        GenerateSimpleSprite(toFill, m_PreserveAspect);
                    else
                        GenerateSprite(toFill, m_PreserveAspect);
                    break;
                case Type.Sliced:
                    GenerateSlicedSprite(toFill);
                    break;
                case Type.Tiled:
                    GenerateTiledSprite(toFill);
                    break;
                case Type.Filled:
                    GenerateFilledSprite(toFill, m_PreserveAspect);
                    break;
            }
        }

        private void TrackSprite()
        {
            if (activeSprite != null && activeSprite.texture == null)
            {
                TrackImage(this);
                m_Tracked = true;
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            TrackSprite();
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            if (m_Tracked)
                UnTrackImage(this);
        }

        /// <summary>
        /// Update the renderer's material.
        /// </summary>

        protected override void UpdateMaterial()
        {
            base.UpdateMaterial();

            // check if this sprite has an associated alpha texture (generated when splitting RGBA = RGB + A as two textures without alpha)

            if (activeSprite == null)
            {
                canvasRenderer.SetAlphaTexture(null);
                return;
            }

            Texture2D alphaTex = activeSprite.associatedAlphaSplitTexture;

            if (alphaTex != null)
            {
                canvasRenderer.SetAlphaTexture(alphaTex);
            }
        }

        protected override void OnCanvasHierarchyChanged()
        {
            base.OnCanvasHierarchyChanged();
            if (canvas == null)
            {
                m_CachedReferencePixelsPerUnit = 100;
            }
            else if (canvas.referencePixelsPerUnit != m_CachedReferencePixelsPerUnit)
            {
                m_CachedReferencePixelsPerUnit = canvas.referencePixelsPerUnit;
                if (type == Type.Sliced || type == Type.Tiled)
                {
                    SetVerticesDirty();
                    SetLayoutDirty();
                }
            }
        }

        /// <summary>
        /// Generate vertices for a simple Image.
        /// Simple模式下，不使用精灵自己的Mesh，生成Quad网格（2个三角形）
        ///
        /// 处理方式与Graphic的OnPopulateMesh基本一致
        /// </summary>
        void GenerateSimpleSprite(VertexHelper vh, bool lPreserveAspect)
        {
            Vector4 v = GetDrawingDimensions(lPreserveAspect);
            //获取精灵的UV
            //innerUV与outerUV是与Sprite的九宫切图相关的
            //九宫切图中，outerUV代表了整个完整矩形区域，innerUV代表中心区域的UV
            //如果sprite本身不是九宫，那么innerUV和outerUV就都是(0,0,1,1)，也就是整个精灵区域
            var uv = (activeSprite != null) ? Sprites.DataUtility.GetOuterUV(activeSprite) : Vector4.zero;

            var color32 = color;
            //VertexHelper使用前要进行清理
            vh.Clear();
            //4个顶点的UV跟整个精灵对应，从而能渲染整个精灵
            vh.AddVert(new Vector3(v.x, v.y), color32, new Vector2(uv.x, uv.y));
            vh.AddVert(new Vector3(v.x, v.w), color32, new Vector2(uv.x, uv.w));
            vh.AddVert(new Vector3(v.z, v.w), color32, new Vector2(uv.z, uv.w));
            vh.AddVert(new Vector3(v.z, v.y), color32, new Vector2(uv.z, uv.y));

            vh.AddTriangle(0, 1, 2);
            vh.AddTriangle(2, 3, 0);
        }

        /// <summary>
        /// 使用精灵自身的Mesh来生成网格
        /// </summary>
        /// <param name="vh"></param>
        /// <param name="lPreserveAspect"></param>
        private void GenerateSprite(VertexHelper vh, bool lPreserveAspect)
        {
            //精灵自身的尺寸
            var spriteSize = new Vector2(activeSprite.rect.width, activeSprite.rect.height);

            // Covert sprite pivot into normalized space.
            //sprite.pivot是个像素值，而不是个百分比，所以需要除以精灵本身的尺寸，换算成归一化的百分比
            var spritePivot = activeSprite.pivot / spriteSize;
            var rectPivot = rectTransform.pivot;
            Rect r = GetPixelAdjustedRect();

            //获得最终要绘制的区域
            if (lPreserveAspect & spriteSize.sqrMagnitude > 0.0f)
            {
                PreserveSpriteAspectRatio(ref r, spriteSize);
            }

            var drawingSize = new Vector2(r.width, r.height);
            //精灵的Bounds尺寸，使用的是世界空间单位，不是像素值
            var spriteBoundSize = activeSprite.bounds.size;

            // Calculate the drawing offset based on the difference between the two pivots.
            //计算绘制精灵时的偏移值，因为RectTransform的质心、精灵的质心可能不一致，所以通过计算两者的质心差值*绘制尺寸，就能计算出偏移值
            var drawOffset = (rectPivot - spritePivot) * drawingSize;

            var color32 = color;
            //VertexHelper使用前要进行清理
            vh.Clear();

            //使用精灵自身的顶点和UV
            Vector2[] vertices = activeSprite.vertices;
            Vector2[] uvs = activeSprite.uv;
            for (int i = 0; i < vertices.Length; ++i)
            {
                //真正的顶点位置是：精灵的顶点位置 / 精灵世界空间尺寸 * 实际绘制尺寸 - 偏移值，其实就是根据实际绘制的大小把顶点进行缩放，然后再根据偏移值进行位移
                vh.AddVert(new Vector3((vertices[i].x / spriteBoundSize.x) * drawingSize.x - drawOffset.x, (vertices[i].y / spriteBoundSize.y) * drawingSize.y - drawOffset.y), color32, new Vector2(uvs[i].x, uvs[i].y));
            }

            //使用精灵自身的顶点环绕
            UInt16[] triangles = activeSprite.triangles;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                vh.AddTriangle(triangles[i + 0], triangles[i + 1], triangles[i + 2]);
            }
        }

        //Sliced模式下的顶点处理流程的临时数组，减少GC
        static readonly Vector2[] s_VertScratch = new Vector2[4];
        //Sliced模式下的UV处理流程的临时数组，减少GC
        static readonly Vector2[] s_UVScratch = new Vector2[4];

        /// <summary>
        /// Generate vertices for a 9-sliced Image.
        /// 九宫精灵绘制
        /// 大致过程其实就是九宫的每个宫都各自生成一个Quad
        /// </summary>
        private void GenerateSlicedSprite(VertexHelper toFill)
        {
            //如果没有切九宫，那么就使用简单绘制
            if (!hasBorder)
            {
                GenerateSimpleSprite(toFill, false);
                return;
            }

            //获取外层UV、中心区域UV、padding、border
            //outer:外层UV，通常是（0,0,1,1）
            //inner:九宫的中心区域UV
            //padding:九宫下padding没有设置的地方，目前是（0,0,0,0）
            //border:左下右上像素值（85,90,82,212），对应sprite Editor窗口下的Border值
            Vector4 outer, inner, padding, border;

            if (activeSprite != null)
            {
                outer = Sprites.DataUtility.GetOuterUV(activeSprite);
                inner = Sprites.DataUtility.GetInnerUV(activeSprite);
                padding = Sprites.DataUtility.GetPadding(activeSprite);
                border = activeSprite.border;
            }
            else
            {
                outer = Vector4.zero;
                inner = Vector4.zero;
                padding = Vector4.zero;
                border = Vector4.zero;
            }

            //由于Rect可能会进行像素对齐，所以要根据像素对齐后的Rect，调整Border大小
            Rect rect = GetPixelAdjustedRect();

            Vector4 adjustedBorders = GetAdjustedBorders(border / multipliedPixelsPerUnit, rect);
            padding = padding / multipliedPixelsPerUnit;

            //工具顶点计算，其实是计算内外边框的顶点，0/3是外边框，1/2是内边框，这样正好是按照从左下到右上的4个顶点顺序0/1/2/3
            //0/3：存储去除padding后的左下、右上顶点位置（相对Rect的位置）
            s_VertScratch[0] = new Vector2(padding.x, padding.y);
            s_VertScratch[3] = new Vector2(rect.width - padding.z, rect.height - padding.w);

            //1/2：去除Border后的顶点左下、右上位置（相对Rect的位置）
            s_VertScratch[1].x = adjustedBorders.x;
            s_VertScratch[1].y = adjustedBorders.y;

            s_VertScratch[2].x = rect.width - adjustedBorders.z;
            s_VertScratch[2].y = rect.height - adjustedBorders.w;

            //上面计算的4个坐标都是相对Rect的坐标，每个坐标加上Rect自身的坐标，就是相对于Rect的父节点的相对坐标
            for (int i = 0; i < 4; ++i)
            {
                s_VertScratch[i].x += rect.x;
                s_VertScratch[i].y += rect.y;
            }

            //工具UV计算
            //0/3，最外层左下、右上的UV
            //1/2，中心区域的左下、右上UV
            s_UVScratch[0] = new Vector2(outer.x, outer.y);
            s_UVScratch[1] = new Vector2(inner.x, inner.y);
            s_UVScratch[2] = new Vector2(inner.z, inner.w);
            s_UVScratch[3] = new Vector2(outer.z, outer.w);

            toFill.Clear();

            //绘制9个Quad，就是九宫的每个方块都各自绘制一个Quad
            for (int x = 0; x < 3; ++x)
            {
                int x2 = x + 1;

                for (int y = 0; y < 3; ++y)
                {
                    //如果不绘制中心，那么会跳过中心Quad绘制
                    if (!m_FillCenter && x == 1 && y == 1)
                        continue;

                    int y2 = y + 1;

                    // Check for zero or negative dimensions to prevent invalid quads (UUM-71372)
                    //如果该Quad面积是0，或者是反面的，那么不进行绘制
                    if ((s_VertScratch[x2].x - s_VertScratch[x].x <= 0) || (s_VertScratch[y2].y - s_VertScratch[y].y <= 0))
                        continue;

                    //把Quad的左下、右上两个顶点，还有对应的两个UV传进去，生成Quad
                    AddQuad(toFill,
                        new Vector2(s_VertScratch[x].x, s_VertScratch[y].y),
                        new Vector2(s_VertScratch[x2].x, s_VertScratch[y2].y),
                        color,
                        new Vector2(s_UVScratch[x].x, s_UVScratch[y].y),
                        new Vector2(s_UVScratch[x2].x, s_UVScratch[y2].y));
                }
            }
        }

        /// <summary>
        /// Generate vertices for a tiled Image.
        /// 平铺精灵绘制
        ///
        /// 平铺模式下，只有精灵的中心区域、四个边的中心会进行平铺，Border的4个角只绘制一次、不平铺。这是这个模式下特殊的地方。
        /// </summary>
        void GenerateTiledSprite(VertexHelper toFill)
        {
            Vector4 outer, inner, border;
            //精灵尺寸，像素值
            Vector2 spriteSize;

            //获取精灵内、外边框位置，见九宫精灵绘制
            if (activeSprite != null)
            {
                outer = Sprites.DataUtility.GetOuterUV(activeSprite);
                inner = Sprites.DataUtility.GetInnerUV(activeSprite);
                border = activeSprite.border;
                //精灵自身rect的值也都是像素值
                spriteSize = activeSprite.rect.size;
            }
            else
            {
                outer = Vector4.zero;
                inner = Vector4.zero;
                border = Vector4.zero;
                //如果没有精灵，那么默认精灵的像素尺寸是100*100
                spriteSize = Vector2.one * 100;
            }

            Rect rect = GetPixelAdjustedRect();
            //计算平铺精灵的中心绘制大小：精灵尺寸减去Border尺寸，然后再乘以平铺缩放
            float tileWidth = (spriteSize.x - border.x - border.z) / multipliedPixelsPerUnit;
            float tileHeight = (spriteSize.y - border.y - border.w) / multipliedPixelsPerUnit;

            //Border也要根据像素对齐后的Rect精修一下
            border = GetAdjustedBorders(border / multipliedPixelsPerUnit, rect);

            //UV只要内边框的UV
            var uvMin = new Vector2(inner.x, inner.y);
            var uvMax = new Vector2(inner.z, inner.w);

            // Min to max max range for tiled region in coordinates relative to lower left corner.
            //Image本身的Rect，计算去掉精修的Border之后的中心平铺绘制区域，都是自身的相对位置
            //通过这几个数据，就能进行中心区域、4边区域的平铺绘制了
            float xMin = border.x;
            float xMax = rect.width - border.z;
            float yMin = border.y;
            float yMax = rect.height - border.w;

            toFill.Clear();
            //临时的用于计算裁切时右上顶点UV的值
            var clipped = uvMax;

            // if either width is zero we cant tile so just assume it was the full width.
            //如果精灵的平铺自身尺寸是0，那么就假设它的尺寸是整个Rect的中心平铺绘制区域
            //这样的效果是，比如tileWidth是0，那么x轴方向上就不再进行平铺了，而是中心像素点进行拉伸。
            //因为tilewidth=0，说明中间没有像素了，但是这里又把它的width尺寸设置为整个区域的宽度，那么就相当于中间的像素要填充整个宽度，相当于拉伸了
            if (tileWidth <= 0)
                tileWidth = xMax - xMin;

            if (tileHeight <= 0)
                tileHeight = yMax - yMin;

            //精灵不为空、且精灵有边界或者是在图集中的、且纹理导入的环绕方式不是Repeat
            //这样的精灵本身无法进行平铺，所以需要重新生成顶点，进行平铺
            //但是要限制生成的顶点数量，不超过65000（Unity单个网格不能超过65000）
            if (activeSprite != null && (hasBorder || activeSprite.packed || activeSprite.texture != null && activeSprite.texture.wrapMode != TextureWrapMode.Repeat))
            {
                // Sprite has border, or is not in repeat mode, or cannot be repeated because of packing.
                // We cannot use texture tiling so we will generate a mesh of quads to tile the texture.

                // Evaluate how many vertices we will generate. Limit this number to something sane,
                // especially since meshes can not have more than 65000 vertices.

                //X轴平铺数量
                long nTilesW = 0;
                //Y轴平铺数量
                long nTilesH = 0;
                
                //计算每个Tile的宽高
                //如果要绘制中心区域的话
                if (m_FillCenter)
                {
                    //XY轴的绘制数量分别是总长度/单个Tile的长度
                    nTilesW = (long)Math.Ceiling((xMax - xMin) / tileWidth);
                    nTilesH = (long)Math.Ceiling((yMax - yMin) / tileHeight);

                    //总的顶点数量
                    double nVertices = 0;
                    //如果有Border，那么XY轴绘制数量要各自多2个；然后每个Tile要用4个顶点进行绘制，所以能计算出总的顶点数量
                    if (hasBorder)
                    {
                        nVertices = (nTilesW + 2.0) * (nTilesH + 2.0) * 4.0; // 4 vertices per tile
                    }
                    else
                    {
                        nVertices = nTilesW * nTilesH * 4.0; // 4 vertices per tile
                    }

                    //如果总的顶点数超过65000，那么将总顶点数量将固定在65000
                    if (nVertices > 65000.0)
                    {
                        Debug.LogError("Too many sprite tiles on Image \"" + name + "\". The tile size will be increased. To remove the limit on the number of tiles, set the Wrap mode to Repeat in the Image Import Settings", this);

                        //由于将顶点限制在65000个，那么这里就计算出总的quad数量
                        double maxTiles = 65000.0 / 4.0; // Max number of vertices is 65000; 4 vertices per tile.
                        //未经过限制前，X轴绘制的Quad数量与Y轴绘制Quad数量的比值，后面重新计算Tile数量时要保持这个比值
                        double imageRatio;
                        if (hasBorder)
                        {
                            imageRatio = (nTilesW + 2.0) / (nTilesH + 2.0);
                        }
                        else
                        {
                            imageRatio = (double)nTilesW / nTilesH;
                        }

                        //由于限制了总的Tile数量，这里就根据总的Tile数量，反向计算出新的Tile的XY轴各自的数量
                        double targetTilesW = Math.Sqrt(maxTiles / imageRatio);
                        double targetTilesH = targetTilesW * imageRatio;
                        //如果有边界，那么XY轴还要各自减去2
                        if (hasBorder)
                        {
                            targetTilesW -= 2;
                            targetTilesH -= 2;
                        }

                        //计算出来的数量向下取整，然后就能计算出每个Tile的宽高
                        nTilesW = (long)Math.Floor(targetTilesW);
                        nTilesH = (long)Math.Floor(targetTilesH);
                        tileWidth = (xMax - xMin) / nTilesW;
                        tileHeight = (yMax - yMin) / nTilesH;
                    }
                }
                //如果不绘制中心区域，意思就是中心区域不平铺、不绘制
                else
                {
                    //如果有Border，那么即使不绘制中心区域，还是要绘制Border、并且Border4个边的中心还是要平铺的
                    if (hasBorder)
                    {
                        // Texture on the border is repeated only in one direction.
                        //平铺数量的计算方式，跟上面计算方式一致
                        nTilesW = (long)Math.Ceiling((xMax - xMin) / tileWidth);
                        nTilesH = (long)Math.Ceiling((yMax - yMin) / tileHeight);
                        //总顶点数量=去除中心Tile的总Tile数量*4
                        double nVertices = (nTilesH + nTilesW + 2.0 /*corners*/) * 2.0 /*sides*/ * 4.0 /*vertices per tile*/;
                        //如果总顶点数>65000，那么还是按照上面一样处理
                        if (nVertices > 65000.0)
                        {
                            Debug.LogError("Too many sprite tiles on Image \"" + name + "\". The tile size will be increased. To remove the limit on the number of tiles, set the Wrap mode to Repeat in the Image Import Settings", this);

                            double maxTiles = 65000.0 / 4.0; // Max number of vertices is 65000; 4 vertices per tile.
                            double imageRatio = (double)nTilesW / nTilesH;
                            double targetTilesW = (maxTiles - 4 /*corners*/) / (2 * (1.0 + imageRatio));
                            double targetTilesH = targetTilesW * imageRatio;

                            nTilesW = (long)Math.Floor(targetTilesW);
                            nTilesH = (long)Math.Floor(targetTilesH);
                            tileWidth = (xMax - xMin) / nTilesW;
                            tileHeight = (yMax - yMin) / nTilesH;
                        }
                    }
                    //如果没有Border、又不绘制中心区域，那么就没有任何东西需要绘制了
                    else
                    {
                        nTilesH = nTilesW = 0;
                    }
                }

                //根据得出的宽高，给每个Tile生成Quad
                //先生成中心区域的Tile
                if (m_FillCenter)
                {
                    // TODO: we could share vertices between quads. If vertex sharing is implemented. update the computation for the number of vertices accordingly.
                    //目前没有使用相邻Quad的顶点共享，其实可以共享，但是前面的计算总的顶点数的方式要改一下
                    
                    //这里2次for循环，对每个Tile进行生成
                    for (long j = 0; j < nTilesH; j++)
                    {
                        //左下角顶点的Y
                        float y1 = yMin + j * tileHeight;
                        //右上角顶点的Y
                        float y2 = yMin + (j + 1) * tileHeight;
                        //如果此时右上角的Y值超过了最大Y值，那么需要裁切一下，把右上角限制到最大值上
                        //同时计算出裁切之后的该点的新的UV值
                        if (y2 > yMax)
                        {
                            clipped.y = uvMin.y + (uvMax.y - uvMin.y) * (yMax - y1) / (y2 - y1);
                            y2 = yMax;
                        }
                        clipped.x = uvMax.x;
                        for (long i = 0; i < nTilesW; i++)
                        {
                            //左下角顶点的X
                            float x1 = xMin + i * tileWidth;
                            //右上角顶点的X
                            float x2 = xMin + (i + 1) * tileWidth;
                            //如果此时右上角的X值超过了最大X值，那么需要裁切一下，把右上角限制到最大值上
                            //同时计算出裁切之后的该点的新的UV值
                            if (x2 > xMax)
                            {
                                clipped.x = uvMin.x + (uvMax.x - uvMin.x) * (xMax - x1) / (x2 - x1);
                                x2 = xMax;
                            }
                            //把该Tile生成Quad
                            AddQuad(toFill, new Vector2(x1, y1) + rect.position, new Vector2(x2, y2) + rect.position, color, uvMin, clipped);
                        }
                    }
                }
                //然后再生成边界
                if (hasBorder)
                {
                    clipped = uvMax;
                    //左右两条Border边的中间部分的Tile
                    for (long j = 0; j < nTilesH; j++)
                    {
                        //左下角顶点的Y
                        float y1 = yMin + j * tileHeight;
                        //右上角顶点的Y
                        float y2 = yMin + (j + 1) * tileHeight;
                        //裁切方式与上面一致
                        if (y2 > yMax)
                        {
                            clipped.y = uvMin.y + (uvMax.y - uvMin.y) * (yMax - y1) / (y2 - y1);
                            y2 = yMax;
                        }
                        //左边BorderTile的左下角相对X一定是0，右上角的相对X是xMin
                        AddQuad(toFill,
                            new Vector2(0, y1) + rect.position,
                            new Vector2(xMin, y2) + rect.position,
                            color,
                            new Vector2(outer.x, uvMin.y),
                            new Vector2(uvMin.x, clipped.y));
                        //右边BorderTile的左下角相对X一定是xMax，右上角的相对X是Rect的宽度最大值
                        AddQuad(toFill,
                            new Vector2(xMax, y1) + rect.position,
                            new Vector2(rect.width, y2) + rect.position,
                            color,
                            new Vector2(uvMax.x, uvMin.y),
                            new Vector2(outer.z, clipped.y));
                    }

                    // Bottom and top tiled border
                    //上下两条Border边的中间部分的Tile
                    //计算方式一致
                    clipped = uvMax;
                    for (long i = 0; i < nTilesW; i++)
                    {
                        float x1 = xMin + i * tileWidth;
                        float x2 = xMin + (i + 1) * tileWidth;
                        if (x2 > xMax)
                        {
                            clipped.x = uvMin.x + (uvMax.x - uvMin.x) * (xMax - x1) / (x2 - x1);
                            x2 = xMax;
                        }
                        AddQuad(toFill,
                            new Vector2(x1, 0) + rect.position,
                            new Vector2(x2, yMin) + rect.position,
                            color,
                            new Vector2(uvMin.x, outer.y),
                            new Vector2(clipped.x, uvMin.y));
                        AddQuad(toFill,
                            new Vector2(x1, yMax) + rect.position,
                            new Vector2(x2, rect.height) + rect.position,
                            color,
                            new Vector2(uvMin.x, uvMax.y),
                            new Vector2(clipped.x, outer.w));
                    }

                    // Corners
                    //4个角不会平铺，所以各自绘制一个即可
                    //左下
                    AddQuad(toFill,
                        new Vector2(0, 0) + rect.position,
                        new Vector2(xMin, yMin) + rect.position,
                        color,
                        new Vector2(outer.x, outer.y),
                        new Vector2(uvMin.x, uvMin.y));
                    //右下
                    AddQuad(toFill,
                        new Vector2(xMax, 0) + rect.position,
                        new Vector2(rect.width, yMin) + rect.position,
                        color,
                        new Vector2(uvMax.x, outer.y),
                        new Vector2(outer.z, uvMin.y));
                    //左上
                    AddQuad(toFill,
                        new Vector2(0, yMax) + rect.position,
                        new Vector2(xMin, rect.height) + rect.position,
                        color,
                        new Vector2(outer.x, uvMax.y),
                        new Vector2(uvMin.x, outer.w));
                    //右上
                    AddQuad(toFill,
                        new Vector2(xMax, yMax) + rect.position,
                        new Vector2(rect.width, rect.height) + rect.position,
                        color,
                        new Vector2(uvMax.x, uvMax.y),
                        new Vector2(outer.z, outer.w));
                }
            }
            //如果精灵没有Border、并且纹理导入的环绕模式是Repeat、并且没有在图集中，那么使用纹理自己的Tile
            else
            {
                // Texture has no border, is in repeat mode and not packed. Use texture tiling.
                //计算一下UV的缩放，因为平铺模式下其实是使用UV来进行平铺的，比如UV=（1.5,1.5），那么超过1的部分会进行重新定位、实现平铺功能
                Vector2 uvScale = new Vector2((xMax - xMin) / tileWidth, (yMax - yMin) / tileHeight);

                //因为没有Border，所以只有勾上了中心区域绘制，才能够绘制
                if (m_FillCenter)
                {
                    AddQuad(toFill, new Vector2(xMin, yMin) + rect.position, new Vector2(xMax, yMax) + rect.position, color, Vector2.Scale(uvMin, uvScale), Vector2.Scale(uvMax, uvScale));
                }
            }
        }

        /// <summary>
        /// 向VertexHelper中添加Quad四边形
        ///
        /// 使用传入的4个顶点和对应的UV生成，可以生成不规则四边形、非矩形
        /// </summary>
        /// <param name="vertexHelper"></param>
        /// <param name="quadPositions"></param>
        /// <param name="color"></param>
        /// <param name="quadUVs"></param>
        static void AddQuad(VertexHelper vertexHelper, Vector3[] quadPositions, Color32 color, Vector3[] quadUVs)
        {
            int startIndex = vertexHelper.currentVertCount;

            for (int i = 0; i < 4; ++i)
                vertexHelper.AddVert(quadPositions[i], color, quadUVs[i]);

            vertexHelper.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
            vertexHelper.AddTriangle(startIndex + 2, startIndex + 3, startIndex);
        }

        /// <summary>
        /// 向VertexHelper中添加Quad四边形
        /// 2个三角形，顺时针顺序，只会生成矩形
        /// </summary>
        /// <param name="vertexHelper"></param>
        /// <param name="posMin"></param>
        /// <param name="posMax"></param>
        /// <param name="color"></param>
        /// <param name="uvMin"></param>
        /// <param name="uvMax"></param>
        static void AddQuad(VertexHelper vertexHelper, Vector2 posMin, Vector2 posMax, Color32 color, Vector2 uvMin, Vector2 uvMax)
        {
            int startIndex = vertexHelper.currentVertCount;

            vertexHelper.AddVert(new Vector3(posMin.x, posMin.y, 0), color, new Vector2(uvMin.x, uvMin.y));
            vertexHelper.AddVert(new Vector3(posMin.x, posMax.y, 0), color, new Vector2(uvMin.x, uvMax.y));
            vertexHelper.AddVert(new Vector3(posMax.x, posMax.y, 0), color, new Vector2(uvMax.x, uvMax.y));
            vertexHelper.AddVert(new Vector3(posMax.x, posMin.y, 0), color, new Vector2(uvMax.x, uvMin.y));

            vertexHelper.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
            vertexHelper.AddTriangle(startIndex + 2, startIndex + 3, startIndex);
        }
        
        /// <summary>
        /// 获取调整之后的精灵Border值
        /// 入参、出参都是世界单位
        /// </summary>
        /// <param name="border">传入的是Border世界单位</param>
        /// <param name="adjustedRect">像素对齐后的Rect</param>
        /// <returns></returns>
        private Vector4 GetAdjustedBorders(Vector4 border, Rect adjustedRect)
        {
            Rect originalRect = rectTransform.rect;

            for (int axis = 0; axis <= 1; axis++)
            {
                float borderScaleRatio;

                // The adjusted rect (adjusted for pixel correctness)
                // may be slightly larger than the original rect.
                // Adjust the border to match the adjustedRect to avoid
                // small gaps between borders (case 833201).
                //由于RectTransform为了像素对齐，可能会略微调整Rect大小，
                //所以为了适配调整后的Rect，Border也会对应的进行调整，防止Border之间出现缝隙。
                //实际上就是算出像素对齐前后的Rect的缩放大小，然后相应的缩放对应的Border
                if (originalRect.size[axis] != 0)
                {
                    borderScaleRatio = adjustedRect.size[axis] / originalRect.size[axis];
                    border[axis] *= borderScaleRatio;
                    border[axis + 2] *= borderScaleRatio;
                }

                // If the rect is smaller than the combined borders, then there's not room for the borders at their normal size.
                // In order to avoid artefacts with overlapping borders, we scale the borders down to fit.
                //九宫的Border，理论上是不会随着Image大小而变化的，所以它才能有九宫缩放的效果。
                //但是如果Image的Rect本身的大小要比Border还要小，无法放下Border，那么也会对Border进行缩放
                float combinedBorders = border[axis] + border[axis + 2];
                if (adjustedRect.size[axis] < combinedBorders && combinedBorders != 0)
                {
                    borderScaleRatio = adjustedRect.size[axis] / combinedBorders;
                    border[axis] *= borderScaleRatio;
                    border[axis + 2] *= borderScaleRatio;
                }
            }
            return border;
        }

        //临时的、用于填充绘制的左下、左上、右上、右下顶点缓存
        static readonly Vector3[] s_Xy = new Vector3[4];
        //临时的、用于填充绘制的左下、左上、右上、右下UV缓存
        static readonly Vector3[] s_Uv = new Vector3[4];

        /// <summary>
        /// Generate vertices for a filled Image.
        /// 填充精灵绘制
        ///
        /// 主要是在于实现扇形、进度条类型的裁切。
        /// </summary>
        void GenerateFilledSprite(VertexHelper toFill, bool preserveAspect)
        {
            toFill.Clear();

            if (m_FillAmount < 0.001f)
                return;

            Vector4 v = GetDrawingDimensions(preserveAspect);
            Vector4 outer = activeSprite != null ? Sprites.DataUtility.GetOuterUV(activeSprite) : Vector4.zero;
            UIVertex uiv = UIVertex.simpleVert;
            uiv.color = color;
            
            //左下与右上UV，外边框UV
            float tx0 = outer.x;
            float ty0 = outer.y;
            float tx1 = outer.z;
            float ty1 = outer.w;

            // Horizontal and vertical filled sprites are simple -- just end the Image prematurely
            //水平与垂直模式处理
            if (m_FillMethod == FillMethod.Horizontal || m_FillMethod == FillMethod.Vertical)
            {
                //水平方式
                if (fillMethod == FillMethod.Horizontal)
                {
                    //变更的UV部分，比如左下UV=（0.1,0.1）、右上=（1,1），那么可变更的总的UV可变范围是x是0.9、y是0.9
                    //那么如果当前填充度是0.5，那么UV变化就变成了0.45
                    float fill = (tx1 - tx0) * m_FillAmount;

                    //右->左，同时变更绘制区域和UV
                    if (m_FillOrigin == 1)
                    {
                        //绘制区域左边界变化
                        v.x = v.z - (v.z - v.x) * m_FillAmount;
                        //UV左下角变化
                        tx0 = tx1 - fill;
                    }
                    //左->右，同时变更绘制区域和UV
                    else
                    {
                        //绘制区域右边界变化
                        v.z = v.x + (v.z - v.x) * m_FillAmount;
                        //UV右上角变化
                        tx1 = tx0 + fill;
                    }
                }
                //竖直方式，同水平方式
                else if (fillMethod == FillMethod.Vertical)
                {
                    float fill = (ty1 - ty0) * m_FillAmount;

                    //上->下
                    if (m_FillOrigin == 1)
                    {
                        v.y = v.w - (v.w - v.y) * m_FillAmount;
                        ty0 = ty1 - fill;
                    }
                    //下->上
                    else
                    {
                        v.w = v.y + (v.w - v.y) * m_FillAmount;
                        ty1 = ty0 + fill;
                    }
                }
            }

            //顶点与UV都缓存一下
            s_Xy[0] = new Vector2(v.x, v.y);
            s_Xy[1] = new Vector2(v.x, v.w);
            s_Xy[2] = new Vector2(v.z, v.w);
            s_Xy[3] = new Vector2(v.z, v.y);

            s_Uv[0] = new Vector2(tx0, ty0);
            s_Uv[1] = new Vector2(tx0, ty1);
            s_Uv[2] = new Vector2(tx1, ty1);
            s_Uv[3] = new Vector2(tx1, ty0);

            {
                if (m_FillAmount < 1f && m_FillMethod != FillMethod.Horizontal && m_FillMethod != FillMethod.Vertical)
                {
                    //如果是90度扇形
                    if (fillMethod == FillMethod.Radial90)
                    {
                        //扇形裁剪成功，那么直接添加Quad，此时的Quad可能是个不规则的Quad，因为要减去不渲染的部分
                        if (RadialCut(s_Xy, s_Uv, m_FillAmount, m_FillClockwise, m_FillOrigin))
                            AddQuad(toFill, s_Xy, color, s_Uv);
                    }
                    //如果是180度扇形
                    //核心算法是把矩形分成2个部分，左边一个矩形、右边一个矩形，这样就能当成2个90°扇形处理。
                    //原本180扇形可能出现5个顶点，这样就保持每个都是4个顶点，就能用Quad了
                    else if (fillMethod == FillMethod.Radial180)
                    {
                        for (int side = 0; side < 2; ++side)
                        {
                            float fx0, fx1, fy0, fy1;
                            int even = m_FillOrigin > 1 ? 1 : 0;

                            if (m_FillOrigin == 0 || m_FillOrigin == 2)
                            {
                                fy0 = 0f;
                                fy1 = 1f;
                                if (side == even)
                                {
                                    fx0 = 0f;
                                    fx1 = 0.5f;
                                }
                                else
                                {
                                    fx0 = 0.5f;
                                    fx1 = 1f;
                                }
                            }
                            else
                            {
                                fx0 = 0f;
                                fx1 = 1f;
                                if (side == even)
                                {
                                    fy0 = 0.5f;
                                    fy1 = 1f;
                                }
                                else
                                {
                                    fy0 = 0f;
                                    fy1 = 0.5f;
                                }
                            }

                            s_Xy[0].x = Mathf.Lerp(v.x, v.z, fx0);
                            s_Xy[1].x = s_Xy[0].x;
                            s_Xy[2].x = Mathf.Lerp(v.x, v.z, fx1);
                            s_Xy[3].x = s_Xy[2].x;

                            s_Xy[0].y = Mathf.Lerp(v.y, v.w, fy0);
                            s_Xy[1].y = Mathf.Lerp(v.y, v.w, fy1);
                            s_Xy[2].y = s_Xy[1].y;
                            s_Xy[3].y = s_Xy[0].y;

                            s_Uv[0].x = Mathf.Lerp(tx0, tx1, fx0);
                            s_Uv[1].x = s_Uv[0].x;
                            s_Uv[2].x = Mathf.Lerp(tx0, tx1, fx1);
                            s_Uv[3].x = s_Uv[2].x;

                            s_Uv[0].y = Mathf.Lerp(ty0, ty1, fy0);
                            s_Uv[1].y = Mathf.Lerp(ty0, ty1, fy1);
                            s_Uv[2].y = s_Uv[1].y;
                            s_Uv[3].y = s_Uv[0].y;

                            float val = m_FillClockwise ? fillAmount * 2f - side : m_FillAmount * 2f - (1 - side);

                            if (RadialCut(s_Xy, s_Uv, Mathf.Clamp01(val), m_FillClockwise, ((side + m_FillOrigin + 3) % 4)))
                            {
                                AddQuad(toFill, s_Xy, color, s_Uv);
                            }
                        }
                    }
                    //如果是360度扇形
                    //核心算法是把矩形分成4个部分，左上、左下、右上、右下4个部分，这样就能当成4个90°扇形处理。
                    //也就是说用4个Quad就能表示360度的情况了
                    else if (fillMethod == FillMethod.Radial360)
                    {
                        for (int corner = 0; corner < 4; ++corner)
                        {
                            float fx0, fx1, fy0, fy1;

                            if (corner < 2)
                            {
                                fx0 = 0f;
                                fx1 = 0.5f;
                            }
                            else
                            {
                                fx0 = 0.5f;
                                fx1 = 1f;
                            }

                            if (corner == 0 || corner == 3)
                            {
                                fy0 = 0f;
                                fy1 = 0.5f;
                            }
                            else
                            {
                                fy0 = 0.5f;
                                fy1 = 1f;
                            }

                            s_Xy[0].x = Mathf.Lerp(v.x, v.z, fx0);
                            s_Xy[1].x = s_Xy[0].x;
                            s_Xy[2].x = Mathf.Lerp(v.x, v.z, fx1);
                            s_Xy[3].x = s_Xy[2].x;

                            s_Xy[0].y = Mathf.Lerp(v.y, v.w, fy0);
                            s_Xy[1].y = Mathf.Lerp(v.y, v.w, fy1);
                            s_Xy[2].y = s_Xy[1].y;
                            s_Xy[3].y = s_Xy[0].y;

                            s_Uv[0].x = Mathf.Lerp(tx0, tx1, fx0);
                            s_Uv[1].x = s_Uv[0].x;
                            s_Uv[2].x = Mathf.Lerp(tx0, tx1, fx1);
                            s_Uv[3].x = s_Uv[2].x;

                            s_Uv[0].y = Mathf.Lerp(ty0, ty1, fy0);
                            s_Uv[1].y = Mathf.Lerp(ty0, ty1, fy1);
                            s_Uv[2].y = s_Uv[1].y;
                            s_Uv[3].y = s_Uv[0].y;

                            float val = m_FillClockwise ?
                                m_FillAmount * 4f - ((corner + m_FillOrigin) % 4) :
                                m_FillAmount * 4f - (3 - ((corner + m_FillOrigin) % 4));

                            if (RadialCut(s_Xy, s_Uv, Mathf.Clamp01(val), m_FillClockwise, ((corner + 2) % 4)))
                                AddQuad(toFill, s_Xy, color, s_Uv);
                        }
                    }
                }
                //如果是全部填充，那么直接生成一个简单Quad即可
                else
                {
                    AddQuad(toFill, s_Xy, color, s_Uv);
                }
            }
        }

        /// <summary>
        /// Adjust the specified quad, making it be radially filled instead.
        /// 将Quad进行扇形裁剪
        /// <param name="fill">填充度，0~1</param>
        /// <param name="invert">是否是顺时针</param>
        /// <param name="corner">Fill方式，90/180/360，传进来的是数字枚举值</param>
        /// <returns>true:按照Quad填充； false：无效、不处理</returns>
        /// </summary>

        static bool RadialCut(Vector3[] xy, Vector3[] uv, float fill, bool invert, int corner)
        {
            // Nothing to fill
            //如果没有任何填充度，那么不填充
            if (fill < 0.001f) return false;

            // Even corners invert the fill direction
            //根据Fill方式，调整顺时针类型
            if ((corner & 1) == 1) invert = !invert;

            // Nothing to adjust
            //如果顺时针、并且是满填充，那么直接画个Quad即可
            if (!invert && fill > 0.999f) return true;

            // Convert 0-1 value into 0 to 90 degrees angle in radians
            //填充度转angle百分比，如果顺时针、填充度0.2，那么最终转换的角度是90 * 0.8 = 72，再转为弧度值
            //angle是个弧度值
            //angle是不填充的部分的弧度值
            //从计算的方式能看出来，角度计算与RectTransform的形状没有关系，无论是长方形、还是正方形，只要填充度是0.5，那么计算出来的角度就是45度
            float angle = Mathf.Clamp01(fill);
            if (invert) angle = 1f - angle;
            angle *= 90f * Mathf.Deg2Rad;

            // Calculate the effective X and Y factors
            //计算angle的sin、cos值
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);

            RadialCut(xy, cos, sin, invert, corner);
            RadialCut(uv, cos, sin, invert, corner);
            return true;
        }

        /// <summary>
        /// Adjust the specified quad, making it be radially filled instead.
        /// 将Quad进行扇形裁剪，调整顶点
        /// 因为扇形要么是3个顶点、要么是4个，不会超过这个顶点数，所以无论怎么填充，都用传入的4个顶点即可
        /// </summary>

        static void RadialCut(Vector3[] xy, float cos, float sin, bool invert, int corner)
        {
            //下面的注释都把Corner当做是Origin90
            //假如Corner是Origin90的BottomLeft=0
            int i0 = corner;
            //此时i1是TopLeft=1
            int i1 = ((corner + 1) % 4);
            //此时i2是TopRight=2
            int i2 = ((corner + 2) % 4);
            //此时i3是BottomRight=3
            int i3 = ((corner + 3) % 4);
            
            //如果Corner是TopLeft
            if ((corner & 1) == 1)
            {
                if (sin > cos)
                {
                    cos /= sin;
                    sin = 1f;

                    if (invert)
                    {
                        xy[i1].x = Mathf.Lerp(xy[i0].x, xy[i2].x, cos);
                        xy[i2].x = xy[i1].x;
                    }
                }
                else if (cos > sin)
                {
                    sin /= cos;
                    cos = 1f;

                    if (!invert)
                    {
                        xy[i2].y = Mathf.Lerp(xy[i0].y, xy[i2].y, sin);
                        xy[i3].y = xy[i2].y;
                    }
                }
                else
                {
                    cos = 1f;
                    sin = 1f;
                }

                if (!invert) xy[i3].x = Mathf.Lerp(xy[i0].x, xy[i2].x, cos);
                else xy[i1].y = Mathf.Lerp(xy[i0].y, xy[i2].y, sin);
            }
            //如果Corner不是TopLeft，假定此时是BottomLeft
            else
            {
                //如果Cos大于Sin，说明不填充部分的角度小于45度，也就是说右上角的顶点是包含在渲染部分中，那么渲染部分是个四边形
                if (cos > sin)
                {
                    sin /= cos;
                    cos = 1f;

                    if (!invert)
                    {
                        xy[i1].y = Mathf.Lerp(xy[i0].y, xy[i2].y, sin);
                        xy[i2].y = xy[i1].y;
                    }
                }
                //如果Sin大于Cos，说明不填充部分的角度大于45度，也就是说右上角的顶点是不包含在渲染部分中，那么渲染部分是三角形
                else if (sin > cos)
                {
                    //这里是求出不填充的部分的下边与右边的比值，当成正方形来计算的，因为外部使用的是填充度
                    //无论是矩形、还是正方形，当填充度时0.5的时候，随着形状变化、填充的角度也会变化，但是始终填充一半。
                    //所以 cos/=sin 计算出来的就是正方形扇形中下边与右边的比值。
                    cos /= sin;
                    //因为当成正方形，所以Sin变成1
                    sin = 1f;

                    if (invert)
                    {
                        //使用这个比值，来对右上角的X点进行重新计算
                        xy[i2].x = Mathf.Lerp(xy[i0].x, xy[i2].x, cos);
                        //右下角的X要跟右上角一致
                        xy[i3].x = xy[i2].x;
                    }
                }
                else
                {
                    cos = 1f;
                    sin = 1f;
                }

                if (invert) xy[i3].y = Mathf.Lerp(xy[i0].y, xy[i2].y, sin);
                else xy[i1].x = Mathf.Lerp(xy[i0].x, xy[i2].x, cos);
            }
        }

        /// <summary>
        /// See ILayoutElement.CalculateLayoutInputHorizontal.
        /// </summary>
        public virtual void CalculateLayoutInputHorizontal() {}

        /// <summary>
        /// See ILayoutElement.CalculateLayoutInputVertical.
        /// </summary>
        public virtual void CalculateLayoutInputVertical() {}

        /// <summary>
        /// See ILayoutElement.minWidth.
        /// </summary>
        public virtual float minWidth { get { return 0; } }

        /// <summary>
        /// If there is a sprite being rendered returns the size of that sprite.
        /// In the case of a slided or tiled sprite will return the calculated minimum size possible
        /// 如果有精灵，那么完美宽度返回精灵的宽度
        /// 如果有精灵、且是裁切或者是平铺的，那么完美宽度返回精灵的最小尺寸
        /// </summary>
        public virtual float preferredWidth
        {
            get
            {
                if (activeSprite == null)
                    return 0;
                if (type == Type.Sliced || type == Type.Tiled)
                    return Sprites.DataUtility.GetMinSize(activeSprite).x / pixelsPerUnit;
                return activeSprite.rect.size.x / pixelsPerUnit;
            }
        }

        /// <summary>
        /// See ILayoutElement.flexibleWidth.
        /// </summary>
        public virtual float flexibleWidth { get { return -1; } }

        /// <summary>
        /// See ILayoutElement.minHeight.
        /// </summary>
        public virtual float minHeight { get { return 0; } }

        /// <summary>
        /// If there is a sprite being rendered returns the size of that sprite.
        /// In the case of a slided or tiled sprite will return the calculated minimum size possible
        /// 如果有精灵，那么完美高度返回精灵的高度
        /// 如果有精灵、且是裁切或者是平铺的，那么完美高度返回精灵的最小尺寸
        /// </summary>
        public virtual float preferredHeight
        {
            get
            {
                if (activeSprite == null)
                    return 0;
                if (type == Type.Sliced || type == Type.Tiled)
                    return Sprites.DataUtility.GetMinSize(activeSprite).y / pixelsPerUnit;
                return activeSprite.rect.size.y / pixelsPerUnit;
            }
        }

        /// <summary>
        /// See ILayoutElement.flexibleHeight.
        /// </summary>
        public virtual float flexibleHeight { get { return -1; } }

        /// <summary>
        /// See ILayoutElement.layoutPriority.
        /// </summary>
        public virtual int layoutPriority { get { return 0; } }

        /// <summary>
        /// Calculate if the ray location for this image is a valid hit location. Takes into account a Alpha test threshold.
        /// 判断射线是否有效命中，这里需要考虑Alpha值
        /// </summary>
        /// <param name="screenPoint">The screen point to check against</param>
        /// <param name="eventCamera">The camera in which to use to calculate the coordinating position</param>
        /// <returns>If the location is a valid hit or not.</returns>
        /// <remarks> Also see See:ICanvasRaycastFilter.</remarks>
        public virtual bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
        {
            //如果Alpha值小于等于0，也就是说完全不看Alpha值，那么一定是命中的
            if (alphaHitTestMinimumThreshold <= 0)
                return true;

            //如果Alpha大于1，那么没有任何精灵的像素透明度会超过1，那么此时完全不命中
            if (alphaHitTestMinimumThreshold > 1)
                return false;

            //没有精灵，那么就不需要看透明度了，此时就是命中
            if (activeSprite == null)
                return true;

            Vector2 local;
            //如果射线在RectTransform之外，那么不算命中
            //计算出射线点位置，是在RectTransform中的相对坐标，相对于Rect的逻辑中心点
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, eventCamera, out local))
                return false;

            //获取像素对齐后的Rect范围
            Rect rect = GetPixelAdjustedRect();

            //根据宽高比调整Rect范围
            if (m_PreserveAspect)
                PreserveSpriteAspectRatio(ref rect, new Vector2(activeSprite.texture.width, activeSprite.texture.height));

            //射线交汇点转换为相对于Rect左下角的相对位置
            // Convert to have lower left corner as reference point.
            local.x += rectTransform.pivot.x * rect.width;
            local.y += rectTransform.pivot.y * rect.height;

            //交汇点转换为纹理中的映射像素位置
            local = MapCoordinate(local, rect);

            //把纹理的映射位置转换为百分比，也就是归一化的坐标
            // Convert local coordinates to texture space.
            float x = local.x / activeSprite.texture.width;
            float y = local.y / activeSprite.texture.height;

            try
            {
                //根据这个百分比，经过二次线性过滤找到纹理中对应的像素点，然后看透明度是否是大于阈值
                return activeSprite.texture.GetPixelBilinear(x, y).a >= alphaHitTestMinimumThreshold;
            }
            catch (UnityException e)
            {
                Debug.LogError("Using alphaHitTestMinimumThreshold greater than 0 on Image whose sprite texture cannot be read. " + e.Message + " Also make sure to disable sprite packing for this sprite.", this);
                return true;
            }
        }

        /// <summary>
        /// 获取local点在纹理上的像素点位置
        /// </summary>
        /// <param name="local">相对于Transform左下角的相对位置，射线命中位置</param>
        /// <param name="rect">像素对齐并调整后的Transform的Rect区域</param>
        /// <returns></returns>
        private Vector2 MapCoordinate(Vector2 local, Rect rect)
        {
            //精灵的像素Rect，在纹理中的大小和位置，都是像素的
            Rect spriteRect = activeSprite.rect;
            //如果是简单模式、填充模式，那么直接就是精灵偏移+精灵根据local比例的像素点，就是最终计算出来的在纹理上的位置
            if (type == Type.Simple || type == Type.Filled)
                return new Vector2(spriteRect.position.x + local.x * spriteRect.width / rect.width, spriteRect.position.y + local.y * spriteRect.height / rect.height);

            //如果是平铺、九宫模式，那么根据具体的模式，返回命中的纹理位置
            //像素单位Border
            Vector4 border = activeSprite.border;
            //适配后的世界单位Border
            Vector4 adjustedBorder = GetAdjustedBorders(border / pixelsPerUnit, rect);

            //在下面的步骤中，世界单位的local最终转换为在纹理中的像素坐标
            for (int i = 0; i < 2; i++)
            {
                //如果射线位置在Border左边界、下边界之外，那么不处理
                if (local[i] <= adjustedBorder[i])
                    continue;

                //如果射线位置在中心区域的上边界、右边界之外，也就是落在Border中、或者Border外
                //TODO 这里的计算是错误的，因为rect是世界单位，spriteRect是像素单位
                //正确的计算应该是 percent = (rect.size[i] - local[i]) / adjustedBorder[i + 2]，先计算射线点在在边界内的比例
                //然后使用像素的Border计算射线点距离边界的像素距离 percent * border[i + 2]
                if (rect.size[i] - local[i] <= adjustedBorder[i + 2])
                {
                    local[i] -= (rect.size[i] - spriteRect.size[i]);
                    continue;
                }

                //如果是九宫模式
                if (type == Type.Sliced)
                {
                    //计算命中位置在中心区域的百分比
                    float lerp = Mathf.InverseLerp(adjustedBorder[i], rect.size[i] - adjustedBorder[i + 2], local[i]);
                    //根据这个百分比，计算出命中位置在精灵Rect上的像素位置
                    local[i] = Mathf.Lerp(border[i], spriteRect.size[i] - border[i + 2], lerp);
                }
                //如果是平铺的，那么调整命中local位置到纹理中
                else
                {
                    //先把命中位置减去边界
                    local[i] -= adjustedBorder[i];
                    //然后把 local[i] 不断的对中心区域平铺求余，从而得到平铺后的相对位置
                    //TODO 这里感觉有BUG，local是世界单位、spriteRect是像素单位，有问题
                    local[i] = Mathf.Repeat(local[i], spriteRect.size[i] - border[i] - border[i + 2]);
                    //然后加上边界，就是精灵上的纹理位置
                    local[i] += border[i];
                }
            }

            //最后，修正后的local位置加上精灵自身的偏移，就是最终映射到纹理的位置
            return local + spriteRect.position;
        }

        // To track textureless images, which will be rebuild if sprite atlas manager registered a Sprite Atlas that will give this image new texture
        static List<Image> m_TrackedTexturelessImages = new List<Image>();
        //是否进行了初始化，在OnEnable中进行初始化
        static bool s_Initialized;

        static void RebuildImage(SpriteAtlas spriteAtlas)
        {
            for (var i = m_TrackedTexturelessImages.Count - 1; i >= 0; i--)
            {
                var g = m_TrackedTexturelessImages[i];
                //如果Atlas中包含当前sprite，那么重建，并且从追踪列表中踢出去
                if (null != g.activeSprite && spriteAtlas.CanBindTo(g.activeSprite))
                {
                    g.SetAllDirty();
                    m_TrackedTexturelessImages.RemoveAt(i);
                }
            }
        }

        private static void TrackImage(Image g)
        {
            if (!s_Initialized)
            {
                //监听atlas注册信息
                SpriteAtlasManager.atlasRegistered += RebuildImage;
                s_Initialized = true;
            }

            m_TrackedTexturelessImages.Add(g);
        }

        private static void UnTrackImage(Image g)
        {
            m_TrackedTexturelessImages.Remove(g);
        }

        protected override void OnDidApplyAnimationProperties()
        {
            SetMaterialDirty();
            SetVerticesDirty();
            SetRaycastDirty();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            m_PixelsPerUnitMultiplier = Mathf.Max(0.01f, m_PixelsPerUnitMultiplier);
        }

#endif
    }
}
