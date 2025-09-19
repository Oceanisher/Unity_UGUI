using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

namespace UnityEngine.UI
{
    [AddComponentMenu("Event/Graphic Raycaster")]
    [RequireComponent(typeof(Canvas))]
    /// <summary>
    /// A derived BaseRaycaster to raycast against Graphic elements.
    /// 对于UI Graphic使用的射线类
    /// 与Canvas同在一个GO上，所以是每个GraphicRaycaster类都只检测自己的Canvas
    /// </summary>
    public class GraphicRaycaster : BaseRaycaster
    {
        protected const int kNoEventMaskSet = -1;

        /// <summary>
        /// Type of raycasters to check against to check for canvas blocking elements.
        /// 射线检测可以被哪些物体阻断，也就是不能穿透
        /// 比如设置上TwoD的话，那么UI元素前面如果放置了带有2D碰撞体的元素，那么将阻挡射线
        /// </summary>
        public enum BlockingObjects
        {
            /// <summary>
            /// Perform no raycasts.
            /// </summary>
            None = 0,
            /// <summary>
            /// Perform a 2D raycast check to check for blocking 2D elements
            /// </summary>
            TwoD = 1,
            /// <summary>
            /// Perform a 3D raycast check to check for blocking 3D elements
            /// </summary>
            ThreeD = 2,
            /// <summary>
            /// Perform a 2D and a 3D raycasts to check for blocking 2D and 3D elements.
            /// </summary>
            All = 3,
        }

        /// <summary>
        /// Priority of the raycaster based upon sort order.
        /// 排序优先级
        /// 数字越小、优先级越高
        /// 通常Graphic射线具有最高优先级、物理射线优先级较低
        ///
        /// </summary>
        /// <returns>
        /// The sortOrder priority.
        /// </returns>
        public override int sortOrderPriority
        {
            get
            {
                //对于Overlay的Canvas，优先级返回Canvas本身的排序（Overlay的Canvas的sortOrderPriority=0）
                // We need to return the sorting order here as distance will all be 0 for overlay.
                if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                    return canvas.sortingOrder;

                return base.sortOrderPriority;
            }
        }

        /// <summary>
        /// Priority of the raycaster based upon render order.
        /// 渲染优先级
        /// </summary>
        /// <returns>
        /// The renderOrder priority.
        /// </returns>
        public override int renderOrderPriority
        {
            get
            {
                //对于Overlay的Canvas，优先级返回Canvas本身的排序（Overlay的Canvas的sortOrderPriority=0）
                // We need to return the sorting order here as distance will all be 0 for overlay.
                if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                    return canvas.rootCanvas.renderOrder;

                return base.renderOrderPriority;
            }
        }

        //否忽略非正面的图形，用于射线检测时忽略背面UI的射线命中检测
        [FormerlySerializedAs("ignoreReversedGraphics")]
        [SerializeField]
        private bool m_IgnoreReversedGraphics = true;
        //是否设置2D或者3D物体可阻挡射线检测
        [FormerlySerializedAs("blockingObjects")]
        [SerializeField]
        private BlockingObjects m_BlockingObjects = BlockingObjects.None;

        /// <summary>
        /// Whether Graphics facing away from the raycaster are checked for raycasts.
        /// 是否忽略非正面的图形，用于射线检测时忽略背面UI的射线命中检测
        /// </summary>
        public bool ignoreReversedGraphics { get {return m_IgnoreReversedGraphics; } set { m_IgnoreReversedGraphics = value; } }

        /// <summary>
        /// The type of objects that are checked to determine if they block graphic raycasts.
        /// 是否设置2D或者3D物体可阻挡射线检测
        /// </summary>
        public BlockingObjects blockingObjects { get {return m_BlockingObjects; } set { m_BlockingObjects = value; } }

        //阻挡射线的Mask层级
        [SerializeField]
        protected LayerMask m_BlockingMask = kNoEventMaskSet;

        /// <summary>
        /// The type of objects specified through LayerMask that are checked to determine if they block graphic raycasts.
        /// </summary>
        public LayerMask blockingMask { get { return m_BlockingMask; } set { m_BlockingMask = value; } }

        private Canvas m_Canvas;

        protected GraphicRaycaster()
        {}

        /// <summary>
        /// 获取射线类脚本所在GO上的Canvas
        /// </summary>
        private Canvas canvas
        {
            get
            {
                if (m_Canvas != null)
                    return m_Canvas;

                m_Canvas = GetComponent<Canvas>();
                return m_Canvas;
            }
        }

        //射线命中的UI缓存
        [NonSerialized] private List<Graphic> m_RaycastResults = new List<Graphic>();

        /// <summary>
        /// Perform the raycast against the list of graphics associated with the Canvas.
        /// 发出射线，对于本射线类所在的Canvas进行射线检测
        /// </summary>
        /// <param name="eventData">Current event data</param>
        /// <param name="resultAppendList">List of hit objects to append new results to.</param>
        public override void Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList)
        {
            //没有Canvas，就不执行射线检测
            if (canvas == null)
                return;

            //获取本Canvas下所有可以接受射线检测的元素
            var canvasGraphics = GraphicRegistry.GetRaycastableGraphicsForCanvas(canvas);
            if (canvasGraphics == null || canvasGraphics.Count == 0)
                return;

            int displayIndex;
            var currentEventCamera = eventCamera; // Property can call Camera.main, so cache the reference

            //如果是Overlay模式，或者事件相机是空的，那么目标显示器是Canvas上的显示器
            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay || currentEventCamera == null)
                displayIndex = canvas.targetDisplay;
            //否则是拿取相机上的目标显示器
            else
                displayIndex = currentEventCamera.targetDisplay;

            //获取经过调整计算后的事件位置
            Vector3 eventPosition = MultipleDisplayUtilities.GetRelativeMousePositionForRaycast(eventData);

            // Discard events that are not part of this display so the user does not interact with multiple displays at once.
            //如果事件不是在当前屏幕上，那么不处理。也就是说多屏幕下，切换屏幕的话，事件会有一次失效。
            if ((int) eventPosition.z != displayIndex)
                return;

            // Convert to view space
            //视口空间坐标，屏幕左下角为(0,0)，右上角为(1,1)，也就是归一化的屏幕空间位置
            Vector2 pos;
            //如果没有事件相机，那么视口空间坐标直接计算出来
            if (currentEventCamera == null)
            {
                // Multiple display support only when not the main display. For display 0 the reported
                // resolution is always the desktops resolution since its part of the display API,
                // so we use the standard none multiple display method. (case 741751)
                float w = Screen.width;
                float h = Screen.height;
                if (displayIndex > 0 && displayIndex < Display.displays.Length)
                {
#if UNITY_ANDROID
                    // Changed for UITK to be coherent for Android which passes display relative rendering coordinates
                    w = Display.displays[displayIndex].renderingWidth;
                    h = Display.displays[displayIndex].renderingHeight;
#else
                    w = Display.displays[displayIndex].systemWidth;
                    h = Display.displays[displayIndex].systemHeight;
#endif
                }
                pos = new Vector2(eventPosition.x / w, eventPosition.y / h);
            }
            //有相机，那么调用相机的ScreenToViewportPoint接口
            else
                pos = currentEventCamera.ScreenToViewportPoint(eventPosition);

            //如果计算出来的视口坐标超出了归一化的坐标区域，那么不处理
            // If it's outside the camera's viewport, do nothing
            if (pos.x < 0f || pos.x > 1f || pos.y < 0f || pos.y > 1f)
                return;

            //射线命中时点距离Ray原点的距离，用于3D2D物体命中时的距离缓存
            float hitDistance = float.MaxValue;

            //射线，内部包含射线原点坐标、射线方向
            Ray ray = new Ray();

            //如果有相机，那么直接使用相机将事件坐标转换为射线
            if (currentEventCamera != null)
                ray = currentEventCamera.ScreenPointToRay(eventPosition);

            //如果Canvas的模式是Overlay、并且有阻挡设置
            if (canvas.renderMode != RenderMode.ScreenSpaceOverlay && blockingObjects != BlockingObjects.None)
            {
                float distanceToClipPlane = 100.0f;

                if (currentEventCamera != null)
                {
                    //如果投影方向完全不是面向Z轴的，那么到切平面的距离设置为无穷大；否则，距离设置为摄像机远近屏幕的距离 / 投影方向Z的值
                    float projectionDirection = ray.direction.z;
                    distanceToClipPlane = Mathf.Approximately(0.0f, projectionDirection)
                        ? Mathf.Infinity
                        : Mathf.Abs((currentEventCamera.farClipPlane - currentEventCamera.nearClipPlane) / projectionDirection);
                }
#if PACKAGE_PHYSICS
                //如果有3D物理阻挡设置，也就是说使用了3D物理包，那么需要检测该射线方向上是不是会命中物理碰撞盒
                if (blockingObjects == BlockingObjects.ThreeD || blockingObjects == BlockingObjects.All)
                {
                    if (ReflectionMethodsCache.Singleton.raycast3D != null)
                    {
                        RaycastHit hit;
                        if (ReflectionMethodsCache.Singleton.raycast3D(ray, out hit, distanceToClipPlane, (int)m_BlockingMask))
                        {
                            hitDistance = hit.distance;
                        }
                    }
                }
#endif
#if PACKAGE_PHYSICS2D
                //如果有2D物理阻挡设置，也就是说使用了2D物理包，那么需要检测该射线方向上是不是会命中物理碰撞盒
                if (blockingObjects == BlockingObjects.TwoD || blockingObjects == BlockingObjects.All)
                {
                    if (ReflectionMethodsCache.Singleton.raycast2D != null)
                    {
                        var hits = ReflectionMethodsCache.Singleton.getRayIntersectionAll(ray, distanceToClipPlane, (int)m_BlockingMask);
                        //获取第一个命中的物体的距离
                        if (hits.Length > 0)
                            hitDistance = hits[0].distance;
                    }
                }
#endif
            }

            m_RaycastResults.Clear();

            //实际发出射线并得到返回所有命中的UI结果
            Raycast(canvas, currentEventCamera, eventPosition, canvasGraphics, m_RaycastResults);

            //遍历命中的UI结果，看是否是需要被剔除出去
            int totalCount = m_RaycastResults.Count;
            for (var index = 0; index < totalCount; index++)
            {
                var go = m_RaycastResults[index].gameObject;
                bool appendGraphic = true;

                //如果忽略背面UI，那么需要计算一下该UI是否正面面向射线方向，实际上就是看UI元素的方向与射线方向的夹角是否小于90度；使用点乘计算即可
                if (ignoreReversedGraphics)
                {
                    if (currentEventCamera == null)
                    {
                        //如果没有相机，那么就用UI的朝向、和Vector3.forward做比较
                        // If we dont have a camera we know that we should always be facing forward
                        var dir = go.transform.rotation * Vector3.forward;
                        appendGraphic = Vector3.Dot(Vector3.forward, dir) > 0;
                    }
                    else
                    {
                        //如果有相机，那么要和相机的朝向进行比较
                        //相机的朝向，是相机的近切面中心、到UI中心的朝向；
                        //这样如果近切面是0，那么就是等同于从相机原点发出的射线
                        //如果近切面不为0，且UI小于近切面，也就是说UI没有在摄像机的照射范围内，那么照样计算出来的结果就是负的，就是需要被剔除的UI元素
                        // If we have a camera compare the direction against the cameras forward.
                        var cameraForward = currentEventCamera.transform.rotation * Vector3.forward * currentEventCamera.nearClipPlane;
                        appendGraphic = Vector3.Dot(go.transform.position - currentEventCamera.transform.position - cameraForward, go.transform.forward) >= 0;
                    }
                }

                //如果是确实命中的UI
                if (appendGraphic)
                {
                    float distance = 0;
                    Transform trans = go.transform;
                    Vector3 transForward = trans.forward;

                    if (currentEventCamera == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                        distance = 0;
                    else
                    {
                        //如果UI在摄像机后面，那么剔除掉
                        // http://geomalgorithms.com/a06-_intersect-2.html
                        distance = (Vector3.Dot(transForward, trans.position - ray.origin) / Vector3.Dot(transForward, ray.direction));

                        // Check to see if the go is behind the camera.
                        if (distance < 0)
                            continue;
                    }

                    //如果命中的UI在2D3D物体命中之后，那么剔除
                    if (distance >= hitDistance)
                        continue;

                    //如果都剔除不了，那么放入到最终的命中结果中
                    var castResult = new RaycastResult
                    {
                        gameObject = go,
                        module = this,
                        distance = distance,
                        screenPosition = eventPosition,
                        displayIndex = displayIndex,
                        index = resultAppendList.Count,
                        depth = m_RaycastResults[index].depth,
                        sortingLayer = canvas.sortingLayerID,
                        sortingOrder = canvas.sortingOrder,
                        worldPosition = ray.origin + ray.direction * distance,
                        worldNormal = -transForward
                    };
                    resultAppendList.Add(castResult);
                }
            }
        }

        /// <summary>
        /// The camera that will generate rays for this raycaster.
        /// UI射线使用的相机
        /// 如果渲染模式是Overlay、或者模式是Camera但是又没有设置相机，那么直接返回null
        /// 其他模式、或者Camera模式下有相机，那么返回Canvas上设置的相机，
        /// 否则使用主相机
        /// </summary>
        /// <returns>
        /// - Null if Camera mode is ScreenSpaceOverlay or ScreenSpaceCamera and has no camera.
        /// - canvas.worldCanvas if not null
        /// - Camera.main.
        /// </returns>
        public override Camera eventCamera
        {
            get
            {
                var canvas = this.canvas;
                var renderMode = canvas.renderMode;
                if (renderMode == RenderMode.ScreenSpaceOverlay
                    || (renderMode == RenderMode.ScreenSpaceCamera && canvas.worldCamera == null))
                    return null;

                return canvas.worldCamera ?? Camera.main;
            }
        }

        /// <summary>
        /// Perform a raycast into the screen and collect all graphics underneath it.
        /// 在指定的Canvas中发出射线，并得到所有命中的UI结果，用来缓存的
        /// </summary>
        [NonSerialized] static readonly List<Graphic> s_SortedGraphics = new List<Graphic>();
        private static void Raycast(Canvas canvas, Camera eventCamera, Vector2 pointerPosition, IList<Graphic> foundGraphics, List<Graphic> results)
        {
            // Necessary for the event system
            //每个可以接受射线的UI都进行检测
            int totalCount = foundGraphics.Count;
            for (int i = 0; i < totalCount; ++i)
            {
                Graphic graphic = foundGraphics[i];

                // -1 means it hasn't been processed by the canvas, which means it isn't actually drawn
                //如果一个UI不接受射线、或者被剔除了、或者没有被绘制，那么跳过
                //-1代表这个UI没有被canvas处理、没有真正进行绘制
                if (!graphic.raycastTarget || graphic.canvasRenderer.cull || graphic.depth == -1)
                    continue;

                //使用RectangleContainsScreenPoint接口，判断射线点的位置，有没有在这个UI的矩形范围内
                if (!RectTransformUtility.RectangleContainsScreenPoint(graphic.rectTransform, pointerPosition, eventCamera, graphic.raycastPadding))
                    continue;

                //如果UI已经超出相机的远切平面了，那么剔除
                //但是感觉这样是有问题的，如果UI有旋转，可能会有一部分进入相机的范围内
                if (eventCamera != null && eventCamera.WorldToScreenPoint(graphic.rectTransform.position).z > eventCamera.farClipPlane)
                    continue;

                //UI自己再判断下是否能够被射线命中，主要是
                if (graphic.Raycast(pointerPosition, eventCamera))
                {
                    s_SortedGraphics.Add(graphic);
                }
            }

            //对射线检测结果进行排序，按照深度反向排序。因为视觉上越靠前的UI，深度越高。
            s_SortedGraphics.Sort((g1, g2) => g2.depth.CompareTo(g1.depth));
            totalCount = s_SortedGraphics.Count;
            for (int i = 0; i < totalCount; ++i)
                results.Add(s_SortedGraphics[i]);

            s_SortedGraphics.Clear();
        }
    }
}
