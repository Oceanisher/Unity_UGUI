using System;
#if UNITY_EDITOR
using System.Reflection;
#endif
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI.CoroutineTween;
using UnityEngine.Pool;

namespace UnityEngine.UI
{
    /// <summary>
    /// Base class for all UI components that should be derived from when creating new Graphic types.
    /// UI图形基类
    /// 所有需要绘制的UI都继承自这个类: Text/Image/RawImage
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    [ExecuteAlways]
    /// <summary>
    ///   Base class for all visual UI Component.
    ///   When creating visual UI components you should inherit from this class.
    /// </summary>
    /// <example>
    /// Below is a simple example that draws a colored quad inside the Rect Transform area.
    /// <code>
    /// <![CDATA[
    /// using UnityEngine;
    /// using UnityEngine.UI;
    ///
    /// [ExecuteInEditMode]
    /// public class SimpleImage : Graphic
    /// {
    ///     protected override void OnPopulateMesh(VertexHelper vh)
    ///     {
    ///         Vector2 corner1 = Vector2.zero;
    ///         Vector2 corner2 = Vector2.zero;
    ///
    ///         corner1.x = 0f;
    ///         corner1.y = 0f;
    ///         corner2.x = 1f;
    ///         corner2.y = 1f;
    ///
    ///         corner1.x -= rectTransform.pivot.x;
    ///         corner1.y -= rectTransform.pivot.y;
    ///         corner2.x -= rectTransform.pivot.x;
    ///         corner2.y -= rectTransform.pivot.y;
    ///
    ///         corner1.x *= rectTransform.rect.width;
    ///         corner1.y *= rectTransform.rect.height;
    ///         corner2.x *= rectTransform.rect.width;
    ///         corner2.y *= rectTransform.rect.height;
    ///
    ///         vh.Clear();
    ///
    ///         UIVertex vert = UIVertex.simpleVert;
    ///
    ///         vert.position = new Vector2(corner1.x, corner1.y);
    ///         vert.color = color;
    ///         vh.AddVert(vert);
    ///
    ///         vert.position = new Vector2(corner1.x, corner2.y);
    ///         vert.color = color;
    ///         vh.AddVert(vert);
    ///
    ///         vert.position = new Vector2(corner2.x, corner2.y);
    ///         vert.color = color;
    ///         vh.AddVert(vert);
    ///
    ///         vert.position = new Vector2(corner2.x, corner1.y);
    ///         vert.color = color;
    ///         vh.AddVert(vert);
    ///
    ///         vh.AddTriangle(0, 1, 2);
    ///         vh.AddTriangle(2, 3, 0);
    ///     }
    /// }
    /// ]]>
    ///</code>
    /// </example>
    public abstract class Graphic
        : UIBehaviour,
          ICanvasElement
    {
        //UI默认材质
        static protected Material s_DefaultUI = null;
        //UI的默认纹理，OnEnable时会被赋值为白色
        static protected Texture2D s_WhiteTexture = null;

        /// <summary>
        /// Default material used to draw UI elements if no explicit material was specified.
        /// </summary>

        //UI默认材质
        static public Material defaultGraphicMaterial
        {
            get
            {
                if (s_DefaultUI == null)
                    s_DefaultUI = Canvas.GetDefaultCanvasMaterial();
                return s_DefaultUI;
            }
        }

        // Cached and saved values
        //自定义使用的材质，如果为空，那么会使用UI的默认材质
        [FormerlySerializedAs("m_Mat")]
        [SerializeField] protected Material m_Material;

        //图形顶点颜色，默认是白色。当通过color属性来修改颜色值的时候，会改变这个值
        [SerializeField] private Color m_Color = Color.white;

        //是否跳过布局更新阶段
        [NonSerialized] protected bool m_SkipLayoutUpdate;
        //是否跳过材质更新阶段
        [NonSerialized] protected bool m_SkipMaterialUpdate;

        /// <summary>
        /// Base color of the Graphic.
        /// </summary>
        /// <remarks>
        /// The builtin UI Components use this as their vertex color. Use this to fetch or change the Color of visual UI elements, such as an Image.
        /// </remarks>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// //Place this script on a GameObject with a Graphic component attached e.g. a visual UI element (Image).
        ///
        /// using UnityEngine;
        /// using UnityEngine.UI;
        ///
        /// public class Example : MonoBehaviour
        /// {
        ///     Graphic m_Graphic;
        ///     Color m_MyColor;
        ///
        ///     void Start()
        ///     {
        ///         //Fetch the Graphic from the GameObject
        ///         m_Graphic = GetComponent<Graphic>();
        ///         //Create a new Color that starts as red
        ///         m_MyColor = Color.red;
        ///         //Change the Graphic Color to the new Color
        ///         m_Graphic.color = m_MyColor;
        ///     }
        ///
        ///     // Update is called once per frame
        ///     void Update()
        ///     {
        ///         //When the mouse button is clicked, change the Graphic Color
        ///         if (Input.GetKey(KeyCode.Mouse0))
        ///         {
        ///             //Change the Color over time between blue and red while the mouse button is pressed
        ///             m_MyColor = Color.Lerp(Color.red, Color.blue, Mathf.PingPong(Time.time, 1));
        ///         }
        ///         //Change the Graphic Color to the new Color
        ///         m_Graphic.color = m_MyColor;
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
        //设置图形顶点颜色，有变更，就会设置顶点为Dirty
        public virtual Color color { 
            get { return m_Color; } 
            set { if (SetPropertyUtility.SetColor(ref m_Color, value)) SetVerticesDirty(); } }

        //是否接受射线检测
        [SerializeField] private bool m_RaycastTarget = true;
        //是否接受射线检测的缓存标记，在改变 m_RaycastTarget 后，先判断与缓存值是不是一致，不一致才会进行操作
        private bool m_RaycastTargetCache = true;

        /// <summary>
        /// Should this graphic be considered a target for raycasting?
        /// 是否能够接受射线，如果不接受，那么射线会穿过该元素、忽略该元素
        /// 会注册/反注册到 GraphicRegistry
        /// 不会触发重建逻辑
        /// </summary>
        public virtual bool raycastTarget
        {
            get
            {
                return m_RaycastTarget;
            }
            set
            {
                if (value != m_RaycastTarget)
                {
                    if (m_RaycastTarget)
                        GraphicRegistry.UnregisterRaycastGraphicForCanvas(canvas, this);

                    m_RaycastTarget = value;

                    if (m_RaycastTarget && isActiveAndEnabled)
                        GraphicRegistry.RegisterRaycastGraphicForCanvas(canvas, this);
                }
                m_RaycastTargetCache = value;
            }
        }

        [SerializeField]
        private Vector4 m_RaycastPadding = new Vector4();

        /// <summary>
        /// Padding to be applied to the masking
        /// 射线检测的Padding
        /// X = Left
        /// Y = Bottom
        /// Z = Right
        /// W = Top
        /// </summary>
        public Vector4 raycastPadding
        {
            get { return m_RaycastPadding; }
            set
            {
                m_RaycastPadding = value;
            }
        }

        //为了访问速度缓存的RectTransform
        [NonSerialized] private RectTransform m_RectTransform;
        [NonSerialized] private CanvasRenderer m_CanvasRenderer;
        [NonSerialized] private Canvas m_Canvas;

        //顶点信息是否Dirty
        [NonSerialized] private bool m_VertsDirty;
        //材质信息是否Dirty
        [NonSerialized] private bool m_MaterialDirty;

        //布局标记为Dirty的回调
        [NonSerialized] protected UnityAction m_OnDirtyLayoutCallback;
        //顶点标记为Dirty的回调
        [NonSerialized] protected UnityAction m_OnDirtyVertsCallback;
        //材质标记为Dirty的回调
        [NonSerialized] protected UnityAction m_OnDirtyMaterialCallback;
        //图形的Mesh信息，生成、修改过程都会在这个Mesh上操作
        [NonSerialized] protected static Mesh s_Mesh;
        [NonSerialized] private static readonly VertexHelper s_VertexHelper = new VertexHelper();

        //缓存的网格数据
        [NonSerialized] protected Mesh m_CachedMesh;
        [NonSerialized] protected Vector2[] m_CachedUvs;
        // Tween controls for the Graphic
        //颜色动画组件
        [NonSerialized]
        private readonly TweenRunner<ColorTween> m_ColorTweenRunner;

        //是否使用旧版Mesh生成方法
        //默认是使用legacy，但是所有继承Graphic的组件，都设置为false，也就是使用新版的Mesh生成
        protected bool useLegacyMeshGeneration { get; set; }

        // Called by Unity prior to deserialization,
        // should not be called by users
        protected Graphic()
        {
            if (m_ColorTweenRunner == null)
                m_ColorTweenRunner = new TweenRunner<ColorTween>();
            m_ColorTweenRunner.Init(this);
            useLegacyMeshGeneration = true;
        }

        /// <summary>
        /// Set all properties of the Graphic dirty and needing rebuilt.
        /// Dirties Layout, Vertices, and Materials.
        /// 布局、顶点、材质都设置为Dirty
        /// </summary>
        public virtual void SetAllDirty()
        {
            // Optimization: Graphic layout doesn't need recalculation if
            // the underlying Sprite is the same size with the same texture.
            // (e.g. Sprite sheet texture animation)

            if (m_SkipLayoutUpdate)
            {
                m_SkipLayoutUpdate = false;
            }
            else
            {
                SetLayoutDirty();
            }

            if (m_SkipMaterialUpdate)
            {
                m_SkipMaterialUpdate = false;
            }
            else
            {
                SetMaterialDirty();
            }

            SetVerticesDirty();
            SetRaycastDirty();
        }

        /// <summary>
        /// Mark the layout as dirty and needing rebuilt.
        /// 布局标记为Dirty，后面会触发布局更新
        /// 如果当前是隐藏的、无效的，那么会跳过。
        /// </summary>
        /// <remarks>
        /// Send a OnDirtyLayoutCallback notification if any elements are registered. See RegisterDirtyLayoutCallback
        /// </remarks>
        public virtual void SetLayoutDirty()
        {
            //如果当前是隐藏的、无效的，那么会跳过
            if (!IsActive())
                return;

            //将RectTransform注册到布局重建队列中
            LayoutRebuilder.MarkLayoutForRebuild(rectTransform);

            if (m_OnDirtyLayoutCallback != null)
                m_OnDirtyLayoutCallback();
        }

        /// <summary>
        /// Mark the vertices as dirty and needing rebuilt.
        /// 顶点数据标记为Dirty，后面会触发图形重建
        /// </summary>
        /// <remarks>
        /// Send a OnDirtyVertsCallback notification if any elements are registered. See RegisterDirtyVerticesCallback
        /// </remarks>
        public virtual void SetVerticesDirty()
        {
            //如果当前是隐藏的、无效的，那么会跳过
            if (!IsActive())
                return;

            //顶点标记为Dirty，并注册到布局重建队列中。也就是说，顶点标记为Dirty之后，会触发图形重建
            m_VertsDirty = true;
            CanvasUpdateRegistry.RegisterCanvasElementForGraphicRebuild(this);

            //顶点标记为Dirty的回调
            if (m_OnDirtyVertsCallback != null)
                m_OnDirtyVertsCallback();
        }

        /// <summary>
        /// Mark the material as dirty and needing rebuilt.
		/// 材质数据标记为Dirty，后面会触发图形重建
        /// </summary>
        /// <remarks>
        /// Send a OnDirtyMaterialCallback notification if any elements are registered. See RegisterDirtyMaterialCallback
        /// </remarks>
        public virtual void SetMaterialDirty()
        {
            if (!IsActive())
                return;

            m_MaterialDirty = true;
            CanvasUpdateRegistry.RegisterCanvasElementForGraphicRebuild(this);

            if (m_OnDirtyMaterialCallback != null)
                m_OnDirtyMaterialCallback();
        }
        
        /// <summary>
        /// 变更是否接受射线检测
        /// 方法内容与 raycastTarget 的Set方法一致
        /// </summary>
        public void SetRaycastDirty()
        {
            if (m_RaycastTargetCache != m_RaycastTarget)
            {
                if (m_RaycastTarget && isActiveAndEnabled)
                    GraphicRegistry.RegisterRaycastGraphicForCanvas(canvas, this);

                else if (!m_RaycastTarget)
                    GraphicRegistry.UnregisterRaycastGraphicForCanvas(canvas, this);
            }
            m_RaycastTargetCache = m_RaycastTarget;
        }

        /// <summary>
        /// RectTransform尺寸发生变化时调用
        /// 触发布局重建、图形重建
        /// </summary>
        protected override void OnRectTransformDimensionsChange()
        {
            //元素处于Active时才会处理
            if (gameObject.activeInHierarchy)
            {
                // prevent double dirtying...
                if (CanvasUpdateRegistry.IsRebuildingLayout())
                    SetVerticesDirty();
                else
                {
                    SetVerticesDirty();
                    SetLayoutDirty();
                }
            }
        }

        protected override void OnBeforeTransformParentChanged()
        {
            GraphicRegistry.UnregisterGraphicForCanvas(canvas, this);
            LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
        }

        /// <summary>
        /// 元素的父节点切换，也就是从一个Trs变更到另一个Trs上
        /// 会重新找一个最近的父Canvas，然后关联上。并且触发全部重建。
        /// </summary>
        protected override void OnTransformParentChanged()
        {
            base.OnTransformParentChanged();

            m_Canvas = null;

            //InActive时，不处理
            if (!IsActive())
                return;

            CacheCanvas();
            GraphicRegistry.RegisterGraphicForCanvas(canvas, this);
            SetAllDirty();
        }

        /// <summary>
        /// Absolute depth of the graphic, used by rendering and events -- lowest to highest.
        /// 元素的绝对深度值
        /// 是一个只读的、从CanvasRenderer读取出来的
        /// 深度值取决于它的第一个根Canvas，如下面的排列
        /// </summary>
        /// <example>
        /// The depth is relative to the first root canvas.
        ///
        /// Canvas
        ///  Graphic - 1
        ///  Graphic - 2
        ///  Nested Canvas
        ///     Graphic - 3
        ///     Graphic - 4
        ///  Graphic - 5
        ///
        /// This value is used to determine draw and event ordering.
        /// </example>
        public int depth { get { return canvasRenderer.absoluteDepth; } }

        /// <summary>
        /// The RectTransform component used by the Graphic. Cached for speed.
        /// 为了访问速度缓存的RectTransform
        /// </summary>
        public RectTransform rectTransform
        {
            get
            {
                // The RectTransform is a required component that must not be destroyed. Based on this assumption, a
                // null-reference check is sufficient.
                if (ReferenceEquals(m_RectTransform, null))
                {
                    m_RectTransform = GetComponent<RectTransform>();
                }
                return m_RectTransform;
            }
        }

        /// <summary>
        /// A reference to the Canvas this Graphic is rendering to.
        /// 元素归属的、最近的父Canvas
        /// </summary>
        /// <remarks>
        /// In the situation where the Graphic is used in a hierarchy with multiple Canvases, the Canvas closest to the root will be used.
        /// </remarks>
        public Canvas canvas
        {
            get
            {
                if (m_Canvas == null)
                    CacheCanvas();
                return m_Canvas;
            }
        }

        /// <summary>
        /// //找到最接近的、父节点的Canvas，然后赋值给 m_Canvas
        /// 如果没找到，m_Canvas = null
        /// </summary>
        private void CacheCanvas()
        {
            var list = ListPool<Canvas>.Get();
            gameObject.GetComponentsInParent(false, list);
            if (list.Count > 0)
            {
                // Find the first active and enabled canvas.
                for (int i = 0; i < list.Count; ++i)
                {
                    if (list[i].isActiveAndEnabled)
                    {
                        m_Canvas = list[i];
                        break;
                    }

                    // if we reached the end and couldn't find an active and enabled canvas, we should return null . case 1171433
                    if (i == list.Count - 1)
                        m_Canvas = null;
                }
            }
            else
            {
                m_Canvas = null;
            }

            ListPool<Canvas>.Release(list);
        }

        /// <summary>
        /// A reference to the CanvasRenderer populated by this Graphic.
        /// 元素身上的 CanvasRenderer ，如果没有会自动添加一个
        /// 因为这个 CanvasRenderer 是渲染图形必须的
        /// </summary>
        public CanvasRenderer canvasRenderer
        {
            get
            {
                // The CanvasRenderer is a required component that must not be destroyed. Based on this assumption, a
                // null-reference check is sufficient.
                if (ReferenceEquals(m_CanvasRenderer, null))
                {
                    m_CanvasRenderer = GetComponent<CanvasRenderer>();

                    if (ReferenceEquals(m_CanvasRenderer, null))
                    {
                        m_CanvasRenderer = gameObject.AddComponent<CanvasRenderer>();
                    }
                }
                return m_CanvasRenderer;
            }
        }

        /// <summary>
        /// Returns the default material for the graphic.
        /// UI元素的默认材质
        /// </summary>
        public virtual Material defaultMaterial
        {
            get { return defaultGraphicMaterial; }
        }

        /// <summary>
        /// The Material set by the user
        /// 元素的材质
        /// Get时，如果没有自定义材质、那么返回默认材质
        /// Set时，会设置材质Dirty、触发图形重建
        /// </summary>
        public virtual Material material
        {
            get
            {
                return (m_Material != null) ? m_Material : defaultMaterial;
            }
            set
            {
                if (m_Material == value)
                    return;

                m_Material = value;
                SetMaterialDirty();
            }
        }

        /// <summary>
        /// The material that will be sent for Rendering (Read only).
        /// 渲染真正使用的材质
        /// 会把元素组件上所有的 IMaterialModifier 都调用一遍，把原有的 material 修改一遍，然后用于渲染
        /// 每次图形重建都会调用一次
        /// </summary>
        /// <remarks>
        /// This is the material that actually gets sent to the CanvasRenderer. By default it's the same as [[Graphic.material]]. When extending Graphic you can override this to send a different material to the CanvasRenderer than the one set by Graphic.material. This is useful if you want to modify the user set material in a non destructive manner.
        /// </remarks>
        public virtual Material materialForRendering
        {
            get
            {
                var components = ListPool<IMaterialModifier>.Get();
                GetComponents<IMaterialModifier>(components);

                var currentMat = material;
                for (var i = 0; i < components.Count; i++)
                    currentMat = (components[i] as IMaterialModifier).GetModifiedMaterial(currentMat);
                ListPool<IMaterialModifier>.Release(components);
                return currentMat;
            }
        }

        /// <summary>
        /// The graphic's texture. (Read Only).
        /// 元素的纹理（可重载），默认就是纯白色
        /// 用于CanvasRenderer => 材质 => Shader中的 _MainTex
        /// 由于UGUI会进行合批操作，所以重载后这里的纹理最好使用图集中的纹理
        /// Image、Text、RawImage都重写了这个纹理
        /// </summary>
        /// <remarks>
        /// This is the Texture that gets passed to the CanvasRenderer, Material and then Shader _MainTex.
        ///
        /// When implementing your own Graphic you can override this to control which texture goes through the UI Rendering pipeline.
        ///
        /// Bear in mind that Unity tries to batch UI elements together to improve performance, so its ideal to work with atlas to reduce the number of draw calls.
        /// </remarks>
        public virtual Texture mainTexture
        {
            get
            {
                return s_WhiteTexture;
            }
        }

        /// <summary>
        /// Mark the Graphic and the canvas as having been changed.
        /// OnEnable时的元素处理流程
        /// 1.设置父节点Canvas
        /// 2.与父节点Canvas在GraphicCanvas中建立关系
        /// 3.把 s_WhiteTexture 设置为纯白色
        /// 4.SetAllDirty，触发布局、纹理重建
        /// </summary>
        protected override void OnEnable()
        {
            base.OnEnable();
            CacheCanvas();
            GraphicRegistry.RegisterGraphicForCanvas(canvas, this);

#if UNITY_EDITOR
            GraphicRebuildTracker.TrackGraphic(this);
#endif
            if (s_WhiteTexture == null)
                s_WhiteTexture = Texture2D.whiteTexture;

            SetAllDirty();
        }

        /// <summary>
        /// Clear references.
        /// OnDisable时的元素处理流程
        /// 1.与父Canvas取消关联-暂时
        /// 2.从所有重建队列中取消-暂时
        /// 3.CanvasRenderer 清理所有顶点数据
        /// 4.将它或者最近的、带有ILayoutGroup的组件加入到布局重建队列中，等待布局重建
        ///
        /// 都是调用Disable接口，实际上是把它们都放到IndexedSet的末尾上，而不是真正删除
        /// </summary>
        protected override void OnDisable()
        {
#if UNITY_EDITOR
            GraphicRebuildTracker.UnTrackGraphic(this);
#endif
            GraphicRegistry.DisableGraphicForCanvas(canvas, this);
            CanvasUpdateRegistry.DisableCanvasElementForRebuild(this);

            if (canvasRenderer != null)
                canvasRenderer.Clear();

            LayoutRebuilder.MarkLayoutForRebuild(rectTransform);

            base.OnDisable();
        }

        /// <summary>
        /// OnDestroy元素的处理流程
        /// 1.与父Canvas取消关联-直接删除
        /// 2.从所有重建队列中取消-直接删除
        /// 3.销毁网格数据
        ///
        /// 都是调用Unregister接口，是真正的从队列中删除
        /// </summary>
        protected override void OnDestroy()
        {
#if UNITY_EDITOR
            GraphicRebuildTracker.UnTrackGraphic(this);
#endif
            GraphicRegistry.UnregisterGraphicForCanvas(canvas, this);
            CanvasUpdateRegistry.UnRegisterCanvasElementForRebuild(this);
            if (m_CachedMesh)
                Destroy(m_CachedMesh);
            m_CachedMesh = null;

            base.OnDestroy();
        }

        /// <summary>
        /// 元素的归属Canvas切换，也就是从一个Canvas变更到另一个Canvas上
        /// </summary>
        /// <returns></returns>
        protected override void OnCanvasHierarchyChanged()
        {
            //先缓存当前的Canvas
            // Use m_Cavas so we dont auto call CacheCanvas
            Canvas currentCanvas = m_Canvas;

            // Clear the cached canvas. Will be fetched below if active.
            m_Canvas = null;

            if (!IsActive())
            {
                GraphicRegistry.UnregisterGraphicForCanvas(currentCanvas, this);
                return;
            }

            //重新找新的父Canvas
            CacheCanvas();

            //如果新的父节点Canvas与老的不一致，那么解除与老Canvas的关联、重新关联到新的Canvas上
            if (currentCanvas != m_Canvas)
            {
                GraphicRegistry.UnregisterGraphicForCanvas(currentCanvas, this);

                // Only register if we are active and enabled as OnCanvasHierarchyChanged can get called
                // during object destruction and we dont want to register ourself and then become null.
                if (IsActive())
                    GraphicRegistry.RegisterGraphicForCanvas(canvas, this);
            }
        }

        /// <summary>
        /// This method must be called when <c>CanvasRenderer.cull</c> is modified.
        /// 仅在 CanvasRenderer.cull 变更后调用，变更是否剔除
        /// </summary> 
        /// <remarks>
        /// This can be used to perform operations that were previously skipped because the <c>Graphic</c> was culled.
        /// </remarks>
        public virtual void OnCullingChanged()
        {
            //如果不进行剔除、并且有任意Dirty，那么进行图形重建
            if (!canvasRenderer.cull && (m_VertsDirty || m_MaterialDirty))
            {
                /// When we were culled, we potentially skipped calls to <c>Rebuild</c>.
                CanvasUpdateRegistry.RegisterCanvasElementForGraphicRebuild(this);
            }
        }

        /// <summary>
        /// Rebuilds the graphic geometry and its material on the PreRender cycle.
        /// 图形重建
        /// 在PreRender阶段执行，会更新几何图形、更新材质
        /// </summary>
        /// <param name="update">The current step of the rendering CanvasUpdate cycle.</param>
        /// <remarks>
        /// See CanvasUpdateRegistry for more details on the canvas update cycle.
        /// </remarks>
        public virtual void Rebuild(CanvasUpdate update)
        {
            //没有CanvasRenderer、或者被Clip掉了，那么不会绘制
            if (canvasRenderer == null || canvasRenderer.cull)
                return;

            switch (update)
            {
                case CanvasUpdate.PreRender:
                    if (m_VertsDirty)
                    {
                        UpdateGeometry();
                        m_VertsDirty = false;
                    }
                    if (m_MaterialDirty)
                    {
                        UpdateMaterial();
                        m_MaterialDirty = false;
                    }
                    break;
            }
        }

        /// <summary>
        /// 布局重建结束调用
        /// </summary>
        public virtual void LayoutComplete()
        {}

        /// <summary>
        /// 图形重建结束调用
        /// </summary>
        public virtual void GraphicUpdateComplete()
        {}

        /// <summary>
        /// Call to update the Material of the graphic onto the CanvasRenderer.
        /// 更新材质信息
        /// 把材质、主纹理写入到CanvasRenderer中
        /// </summary>
        protected virtual void UpdateMaterial()
        {
            if (!IsActive())
                return;

            canvasRenderer.materialCount = 1;
            canvasRenderer.SetMaterial(materialForRendering, 0);
            canvasRenderer.SetTexture(mainTexture);
        }

        /// <summary>
        /// Call to update the geometry of the Graphic onto the CanvasRenderer.
        /// 更新几何信息
        /// 实际上是把顶点信息写入到CanvasRenderer中
        /// </summary>
        protected virtual void UpdateGeometry()
        {
            if (useLegacyMeshGeneration)
            {
                DoLegacyMeshGeneration();
            }
            else
            {
                DoMeshGeneration();
            }
        }

        /// <summary>
        /// Mesh生成
        /// 最终将顶点信息写入到CanvasRenderer中
        /// </summary>
        private void DoMeshGeneration()
        {
            //RectTrs必须尺寸>=0的情况下才会绘制，否则将清空顶点
            if (rectTransform != null && rectTransform.rect.width >= 0 && rectTransform.rect.height >= 0)
                OnPopulateMesh(s_VertexHelper);
            else
                s_VertexHelper.Clear(); // clear the vertex helper so invalid graphics dont draw.

            //生成原始顶点信息后，使用IMeshModifier对顶点进行修改
            var components = ListPool<Component>.Get();
            GetComponents(typeof(IMeshModifier), components);

            for (var i = 0; i < components.Count; i++)
                ((IMeshModifier)components[i]).ModifyMesh(s_VertexHelper);

            ListPool<Component>.Release(components);

            //顶点信息从VertextHelper、写入到workerMesh中
            s_VertexHelper.FillMesh(workerMesh);
            //顶点信息写入到CanvasRenderer中
            canvasRenderer.SetMesh(workerMesh);
        }

        /// <summary>
        /// Mesh生成-旧
        /// </summary>
        private void DoLegacyMeshGeneration()
        {
            if (rectTransform != null && rectTransform.rect.width >= 0 && rectTransform.rect.height >= 0)
            {
#pragma warning disable 618
                OnPopulateMesh(workerMesh);
#pragma warning restore 618
            }
            else
            {
                workerMesh.Clear();
            }

            var components = ListPool<Component>.Get();
            GetComponents(typeof(IMeshModifier), components);

            for (var i = 0; i < components.Count; i++)
            {
#pragma warning disable 618
                ((IMeshModifier)components[i]).ModifyMesh(workerMesh);
#pragma warning restore 618
            }

            ListPool<Component>.Release(components);
            canvasRenderer.SetMesh(workerMesh);
        }

        //图形的Mesh信息
        protected static Mesh workerMesh
        {
            get
            {
                if (s_Mesh == null)
                {
                    s_Mesh = new Mesh();
                    s_Mesh.name = "Shared UI Mesh";
                }
                return s_Mesh;
            }
        }
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Use OnPopulateMesh instead.", true)]
        protected virtual void OnFillVBO(System.Collections.Generic.List<UIVertex> vbo) {}

        [Obsolete("Use OnPopulateMesh(VertexHelper vh) instead.", false)]
        /// <summary>
        /// Callback function when a UI element needs to generate vertices. Fills the vertex buffer data.
        /// </summary>
        /// <param name="m">Mesh to populate with UI data.</param>
        /// <remarks>
        /// Used by Text, UI.Image, and RawImage for example to generate vertices specific to their use case.
        /// </remarks>
        protected virtual void OnPopulateMesh(Mesh m)
        {
            OnPopulateMesh(s_VertexHelper);
            s_VertexHelper.FillMesh(m);
        }

        /// <summary>
        /// Callback function when a UI element needs to generate vertices. Fills the vertex buffer data.
        /// 顶点信息生成，写入到VertexHelper中
        /// 图形类子类都会重写这个方法，来实现自己的顶点布局
        /// </summary>
        /// <param name="vh">VertexHelper utility.</param>
        /// <remarks>
        /// Used by Text, UI.Image, and RawImage for example to generate vertices specific to their use case.
        /// </remarks>
        protected virtual void OnPopulateMesh(VertexHelper vh)
        {
            //获取像素适配后的图形区域
            var r = GetPixelAdjustedRect();
            //计算区域的左下、右上角
            var v = new Vector4(r.x, r.y, r.x + r.width, r.y + r.height);

            //绘制了一个矩形图形
            Color32 color32 = color;
            vh.Clear();
            //顶点环绕方向：顺时针，UV：左下角为原点
            //所以这里顶点的添加顺序是左下、左上、右上、右下，UV也是如此
            vh.AddVert(new Vector3(v.x, v.y), color32, new Vector2(0f, 0f));
            vh.AddVert(new Vector3(v.x, v.w), color32, new Vector2(0f, 1f));
            vh.AddVert(new Vector3(v.z, v.w), color32, new Vector2(1f, 1f));
            vh.AddVert(new Vector3(v.z, v.y), color32, new Vector2(1f, 0f));

            //三角形环绕是顺时针顺序，这里绘制了2个三角形
            vh.AddTriangle(0, 1, 2);
            vh.AddTriangle(2, 3, 0);
        }

#if UNITY_EDITOR
        /// <summary>
        /// Editor-only callback that is issued by Unity if a rebuild of the Graphic is required.
        /// Currently sent when an asset is reimported.
        /// </summary>
        public virtual void OnRebuildRequested()
        {
            // when rebuild is requested we need to rebuild all the graphics /
            // and associated components... The correct way to do this is by
            // calling OnValidate... Because MB's don't have a common base class
            // we do this via reflection. It's nasty and ugly... Editor only.
            m_SkipLayoutUpdate = true;
            var mbs = gameObject.GetComponents<MonoBehaviour>();
            foreach (var mb in mbs)
            {
                if (mb == null)
                    continue;
                var methodInfo = mb.GetType().GetMethod("OnValidate", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (methodInfo != null)
                    methodInfo.Invoke(mb, null);
            }
            m_SkipLayoutUpdate = false;
        }

        protected override void Reset()
        {
            SetAllDirty();
        }

#endif

        // Call from unity if animation properties have changed
        /// <summary>
        /// 当UI的动画属性应用时调用，图形、布局全部重建
        /// 所以动画的播放会触发重建，动静分离的重要性
        /// </summary>
        protected override void OnDidApplyAnimationProperties()
        {
            SetAllDirty();
        }

        /// <summary>
        /// Make the Graphic have the native size of its content.
        /// </summary>
        public virtual void SetNativeSize() {}

        /// <summary>
        /// When a GraphicRaycaster is raycasting into the scene it does two things. First it filters the elements using their RectTransform rect. Then it uses this Raycast function to determine the elements hit by the raycast.
        /// UI自己进行的射线检测接口，是在被射线命中以后进行自我判断的接口
        /// 之所以要自我检测一下，是因为有些UI可能有Mask，可能会影响命中结果
        /// 如果持续往上遍历，找到的Canvas是overrideSorting的Canvas，那么就停止继续往上了，因为它是重写排序的Canvas，不会再受上层Canvas的Mask影响了
        /// 如果Canvas不是重写排序的，那么就会一直往上持续遍历
        /// </summary>
        /// <param name="sp">Screen point being tested</param>
        /// <param name="eventCamera">Camera that is being used for the testing.</param>
        /// <returns>True if the provided point is a valid location for GraphicRaycaster raycasts.</returns>
        public virtual bool Raycast(Vector2 sp, Camera eventCamera)
        {
            if (!isActiveAndEnabled)
                return false;

            var t = transform;
            var components = ListPool<Component>.Get();

            bool ignoreParentGroups = false;
            bool continueTraversal = true;

            while (t != null)
            {
                t.GetComponents(components);
                for (var i = 0; i < components.Count; i++)
                {
                    var canvas = components[i] as Canvas;
                    //overrideSorting字段用来重写排序，使其不受父对象的Sorting控制
                    if (canvas != null && canvas.overrideSorting)
                        continueTraversal = false;

                    var filter = components[i] as ICanvasRaycastFilter;
                    //ICanvasRaycastFilter只有Mask的2个类继承了，所以如果没有Mask，那么射线就是命中了，就不用再看经过Mask裁剪后的区域是否命中了
                    if (filter == null)
                        continue;

                    //如果该组件有Mask，那么需要判断射线是否命中的Mask后的区域内
                    var raycastValid = true;

                    //如果组件上有CanvasGroup，这个组件是用来统一控制子节点的透明度、是否能交互、是否阻止射线、是否忽略父节点的Group等
                    //这里的代码貌似有问题，无论如何都会调用 filter.IsRaycastLocationValid()，那为何还要进行这么多判断
                    //filter.IsRaycastLocationValid()是各个实现接口的组件来处理，决定射线是否能成功打到该点上
                    var group = components[i] as CanvasGroup;
                    if (group != null)
                    {
                        if (!group.enabled)
                            continue;

                        if (ignoreParentGroups == false && group.ignoreParentGroups)
                        {
                            ignoreParentGroups = true;
                            raycastValid = filter.IsRaycastLocationValid(sp, eventCamera);
                        }
                        else if (!ignoreParentGroups)
                            raycastValid = filter.IsRaycastLocationValid(sp, eventCamera);
                    }
                    else
                    {
                        raycastValid = filter.IsRaycastLocationValid(sp, eventCamera);
                    }

                    if (!raycastValid)
                    {
                        ListPool<Component>.Release(components);
                        return false;
                    }
                }
                t = continueTraversal ? t.parent : null;
            }
            //如果没有被filter拦截，那么就是成功命中了该组件
            ListPool<Component>.Release(components);
            return true;
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            SetAllDirty();
        }

#endif

        ///<summary>
        ///Adjusts the given pixel to be pixel perfect.
        /// 传入一个Vector2，返回像素对齐后的点
        /// 与 GetPixelAdjustedRect() 同理
        ///</summary>
        ///<param name="point">Local space point.</param>
        ///<returns>Pixel perfect adjusted point.</returns>
        ///<remarks>
        ///Note: This is only accurate if the Graphic root Canvas is in Screen Space.
        ///</remarks>
        public Vector2 PixelAdjustPoint(Vector2 point)
        {
            if (!canvas || canvas.renderMode == RenderMode.WorldSpace || canvas.scaleFactor == 0.0f || !canvas.pixelPerfect)
                return point;
            else
            {
                return RectTransformUtility.PixelAdjustPoint(point, transform, canvas);
            }
        }

        /// <summary>
        /// Returns a pixel perfect Rect closest to the Graphic RectTransform.
        /// 获取经过像素适配后的Rect，世界空间尺寸，不是像素
        /// 非Canvas，与分辨率无关，直接返回Rect区域
        /// Canvas：非世界空间的、缩放不是0的、像素对齐的，需要返回像素适配后的Rect
        /// 需要注意的是，像素对齐会使得位置信息完全匹配像素，比如(1.2, 1.05)会强制变为(1, 1)，所以可能会导致位置失控
        /// </summary>
        /// <remarks>
        /// Note: This is only accurate if the Graphic root Canvas is in Screen Space.
        /// </remarks>
        /// <returns>A Pixel perfect Rect.</returns>
        public Rect GetPixelAdjustedRect()
        {
            if (!canvas || canvas.renderMode == RenderMode.WorldSpace || canvas.scaleFactor == 0.0f || !canvas.pixelPerfect)
                return rectTransform.rect;
            else
                return RectTransformUtility.PixelAdjustRect(rectTransform, canvas);
        }

        ///<summary>
        ///Tweens the CanvasRenderer color associated with this Graphic.
        /// 颜色淡入淡出
        ///</summary>
        ///<param name="targetColor">Target color.</param>
        ///<param name="duration">Tween duration.</param>
        ///<param name="ignoreTimeScale">Should ignore Time.scale?</param>
        ///<param name="useAlpha">Should also Tween the alpha channel?</param>
        public virtual void CrossFadeColor(Color targetColor, float duration, bool ignoreTimeScale, bool useAlpha)
        {
            CrossFadeColor(targetColor, duration, ignoreTimeScale, useAlpha, true);
        }

        ///<summary>
        ///Tweens the CanvasRenderer color associated with this Graphic.
        /// 颜色淡入淡出
        ///</summary>
        ///<param name="targetColor">Target color.</param>
        ///<param name="duration">Tween duration.</param>
        ///<param name="ignoreTimeScale">Should ignore Time.scale?</param>
        ///<param name="useAlpha">Should also Tween the alpha channel? 是否要控制透明度</param>
        /// <param name="useRGB">Should the color or the alpha be used to tween 是否要控制颜色</param>
        public virtual void CrossFadeColor(Color targetColor, float duration, bool ignoreTimeScale, bool useAlpha, bool useRGB)
        {
            //useAlpha、useRGB同时为false的话，代表什么也不控制，就跳过
            if (canvasRenderer == null || (!useRGB && !useAlpha))
                return;

            //获取当前颜色
            Color currentColor = canvasRenderer.GetColor();
            if (currentColor.Equals(targetColor))
            {
                m_ColorTweenRunner.StopTween();
                return;
            }

            ColorTween.ColorTweenMode mode = (useRGB && useAlpha ?
                ColorTween.ColorTweenMode.All :
                (useRGB ? ColorTween.ColorTweenMode.RGB : ColorTween.ColorTweenMode.Alpha));

            //执行动画
            var colorTween = new ColorTween {duration = duration, startColor = canvasRenderer.GetColor(), targetColor = targetColor};
            colorTween.AddOnChangedCallback(canvasRenderer.SetColor);
            colorTween.ignoreTimeScale = ignoreTimeScale;
            colorTween.tweenMode = mode;
            m_ColorTweenRunner.StartTween(colorTween);
        }

        /// <summary>
        /// 根据透明度创建颜色
        /// 创建的是黑色，然后设置透明度
        /// </summary>
        /// <param name="alpha"></param>
        /// <returns></returns>
        static private Color CreateColorFromAlpha(float alpha)
        {
            var alphaColor = Color.black;
            alphaColor.a = alpha;
            return alphaColor;
        }

        ///<summary>
        ///Tweens the alpha of the CanvasRenderer color associated with this Graphic.
        /// 透明度淡入淡出
        ///</summary>
        ///<param name="alpha">Target alpha.</param>
        ///<param name="duration">Duration of the tween in seconds.</param>
        ///<param name="ignoreTimeScale">Should ignore [[Time.scale]]?</param>
        public virtual void CrossFadeAlpha(float alpha, float duration, bool ignoreTimeScale)
        {
            CrossFadeColor(CreateColorFromAlpha(alpha), duration, ignoreTimeScale, true, false);
        }

        /// <summary>
        /// Add a listener to receive notification when the graphics layout is dirtied.
        /// 注册布局设置为Dirty时的回调
        /// </summary>
        /// <param name="action">The method to call when invoked.</param>
        public void RegisterDirtyLayoutCallback(UnityAction action)
        {
            m_OnDirtyLayoutCallback += action;
        }

        /// <summary>
        /// Remove a listener from receiving notifications when the graphics layout are dirtied
        /// 取消注册布局设置为Dirty时的回调
        /// </summary>
        /// <param name="action">The method to call when invoked.</param>
        public void UnregisterDirtyLayoutCallback(UnityAction action)
        {
            m_OnDirtyLayoutCallback -= action;
        }

        /// <summary>
        /// Add a listener to receive notification when the graphics vertices are dirtied.
        /// 注册顶点设置为Dirty时的回调
        /// </summary>
        /// <param name="action">The method to call when invoked.</param>
        public void RegisterDirtyVerticesCallback(UnityAction action)
        {
            m_OnDirtyVertsCallback += action;
        }

        /// <summary>
        /// Remove a listener from receiving notifications when the graphics vertices are dirtied
        /// 取消注册顶点设置为Dirty时的回调
        /// </summary>
        /// <param name="action">The method to call when invoked.</param>
        public void UnregisterDirtyVerticesCallback(UnityAction action)
        {
            m_OnDirtyVertsCallback -= action;
        }

        /// <summary>
        /// Add a listener to receive notification when the graphics material is dirtied.
        /// 注册材质设置为Dirty时的回调
        /// </summary>
        /// <param name="action">The method to call when invoked.</param>
        public void RegisterDirtyMaterialCallback(UnityAction action)
        {
            m_OnDirtyMaterialCallback += action;
        }

        /// <summary>
        /// Remove a listener from receiving notifications when the graphics material are dirtied
        /// 取消注册顶点设置为Dirty时的回调
        /// </summary>
        /// <param name="action">The method to call when invoked.</param>
        public void UnregisterDirtyMaterialCallback(UnityAction action)
        {
            m_OnDirtyMaterialCallback -= action;
        }
    }
}
