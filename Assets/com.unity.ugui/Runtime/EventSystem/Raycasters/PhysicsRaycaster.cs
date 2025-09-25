using System.Collections.Generic;
using UnityEngine.UI;

namespace UnityEngine.EventSystems
{
    /// <summary>
    /// Simple event system using physics raycasts.
    /// 3D物理射线类
    /// </summary>
    [AddComponentMenu("Event/Physics Raycaster")]
    [RequireComponent(typeof(Camera))]
    /// <summary>
    /// Raycaster for casting against 3D Physics components.
    /// </summary>
    public class PhysicsRaycaster : BaseRaycaster
    {
        /// <summary>
        /// Const to use for clarity when no event mask is set
        /// 阻挡射线的Mask默认值，默认是不阻挡
        /// </summary>
        protected const int kNoEventMaskSet = -1;

        //使用的相机
        protected Camera m_EventCamera;

        /// <summary>
        /// Layer mask used to filter events. Always combined with the camera's culling mask if a camera is used.
        /// 阻挡射线的Mask
        /// </summary>
        [SerializeField]
        protected LayerMask m_EventMask = kNoEventMaskSet;

        /// <summary>
        /// The max number of intersections allowed. 0 = allocating version anything else is non alloc.
        /// 最大可射线检测的GO数量
        /// 为0则无限制
        /// </summary>
        [SerializeField]
        protected int m_MaxRayIntersections = 0;
        //上次设置的最大可射线检测的GO数量
        protected int m_LastMaxRayIntersections = 0;

#if PACKAGE_PHYSICS
        RaycastHit[] m_Hits;
#endif

        protected PhysicsRaycaster()
        {}

        //射线使用的相机，优先从射线类所在的GO上获取、获取不到就取主相机
        public override Camera eventCamera
        {
            get
            {
                if (m_EventCamera == null)
                    m_EventCamera = GetComponent<Camera>();

                if (m_EventCamera == null)
                    return Camera.main;

                return m_EventCamera ;
            }
        }


        /// <summary>
        /// Depth used to determine the order of event processing.
        /// 深度值使用相机的深度
        /// </summary>
        public virtual int depth
        {
            get { return (eventCamera != null) ? (int)eventCamera.depth : 0xFFFFFF; }
        }

        /// <summary>
        /// Event mask used to determine which objects will receive events.
        /// 射线最终使用的Mask，是射线上配置的Mask+相机上的Mask的交集
        /// 没有相机的话，就没有Mask
        /// </summary>
        public int finalEventMask
        {
            get { return (eventCamera != null) ? eventCamera.cullingMask & m_EventMask : kNoEventMaskSet; }
        }

        /// <summary>
        /// Layer mask used to filter events. Always combined with the camera's culling mask if a camera is used.
        /// 射线自己设置的Mask
        /// </summary>
        public LayerMask eventMask
        {
            get { return m_EventMask; }
            set { m_EventMask = value; }
        }

        /// <summary>
        /// Max number of ray intersection allowed to be found.
        /// 最大可射线检测的GO数量
        /// 为0则无限制
        /// </summary>
        /// <remarks>
        /// A value of zero will represent using the allocating version of the raycast function where as any other value will use the non allocating version.
        /// </remarks>
        public int maxRayIntersections
        {
            get { return m_MaxRayIntersections; }
            set { m_MaxRayIntersections = value; }
        }

        /// <summary>
        /// Returns a ray going from camera through the event position and the distance between the near and far clipping planes along that ray.
        /// 计算射线从相机到事件位置的方向，在近切面、远切面之间的距离，以及Ray数据
        /// </summary>
        /// <param name="eventData">The pointer event for which we will cast a ray.</param>
        /// <param name="ray">The ray to use.</param>
        /// <param name="eventDisplayIndex">The display index used.</param>
        /// <param name="distanceToClipPlane">The distance between the near and far clipping planes along the ray.</param>
        /// <returns>True if the operation was successful. false if it was not possible to compute, such as the eventPosition being outside of the view.</returns>
        protected bool ComputeRayAndDistance(PointerEventData eventData, ref Ray ray, ref int eventDisplayIndex, ref float distanceToClipPlane)
        {
            //没有相机就不处理
            if (eventCamera == null)
                return false;

            //多显示器情况下，获取事件位置在指定显示器上的位置，返回值的xy坐标代表事件在游戏内像素坐标位置，Z代表相机
            var eventPosition = MultipleDisplayUtilities.RelativeMouseAtScaled(eventData.position, eventData.displayIndex);
            //如果有值，代表能成功命中
            if (eventPosition != Vector3.zero)
            {
                // We support multiple display and display identification based on event position.
                eventDisplayIndex = (int)eventPosition.z;

                // Discard events that are not part of this display so the user does not interact with multiple displays at once.
                //如果命中的显示器不是当前相机所在的显示器，那么不处理
                if (eventDisplayIndex != eventCamera.targetDisplay)
                    return false;
            }
            //没有值，那就还是使用原始的事件坐标
            else
            {
                // The multiple display system is not supported on all platforms, when it is not supported the returned position
                // will be all zeros so when the returned index is 0 we will default to the event data to be safe.
                eventPosition = eventData.position;
            }

            //如果坐标像素位置超出相机的像素区域，那么不处理
            // Cull ray casts that are outside of the view rect. (case 636595)
            if (!eventCamera.pixelRect.Contains(eventPosition))
                return false;

            //把屏幕像素位置转换为Ray数据
            ray = eventCamera.ScreenPointToRay(eventPosition);
            // compensate far plane distance - see MouseEvents.cs
            //计算射线在近切面、远切面内的长度
            float projectionDirection = ray.direction.z;
            distanceToClipPlane = Mathf.Approximately(0.0f, projectionDirection)
                ? Mathf.Infinity
                : Mathf.Abs((eventCamera.farClipPlane - eventCamera.nearClipPlane) / projectionDirection);
            return true;
        }

        /// <summary>
        /// 射线方法
        /// 这里由于是物理射线，所以仅当安装了3D物理包，才会执行
        /// </summary>
        /// <param name="eventData"></param>
        /// <param name="resultAppendList"></param>
        public override void Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList)
        {
#if PACKAGE_PHYSICS
            Ray ray = new Ray();
            int displayIndex = 0;
            float distanceToClipPlane = 0;
            if (!ComputeRayAndDistance(eventData, ref ray, ref displayIndex, ref distanceToClipPlane))
                return;

            int hitCount = 0;

            if (m_MaxRayIntersections == 0)
            {
                if (ReflectionMethodsCache.Singleton.raycast3DAll == null)
                    return;

                m_Hits = ReflectionMethodsCache.Singleton.raycast3DAll(ray, distanceToClipPlane, finalEventMask);
                hitCount = m_Hits.Length;
            }
            else
            {
                if (ReflectionMethodsCache.Singleton.getRaycastNonAlloc == null)
                    return;
                if (m_LastMaxRayIntersections != m_MaxRayIntersections)
                {
                    m_Hits = new RaycastHit[m_MaxRayIntersections];
                    m_LastMaxRayIntersections = m_MaxRayIntersections;
                }

                hitCount = ReflectionMethodsCache.Singleton.getRaycastNonAlloc(ray, m_Hits, distanceToClipPlane, finalEventMask);
            }

            if (hitCount != 0)
            {
                if (hitCount > 1)
                    System.Array.Sort(m_Hits, 0, hitCount, RaycastHitComparer.instance);

                for (int b = 0, bmax = hitCount; b < bmax; ++b)
                {
                    var result = new RaycastResult
                    {
                        gameObject = m_Hits[b].collider.gameObject,
                        module = this,
                        distance = m_Hits[b].distance,
                        worldPosition = m_Hits[b].point,
                        worldNormal = m_Hits[b].normal,
                        screenPosition = eventData.position,
                        displayIndex = displayIndex,
                        index = resultAppendList.Count,
                        sortingLayer = 0,
                        sortingOrder = 0
                    };
                    resultAppendList.Add(result);
                }
            }
#endif
        }

#if PACKAGE_PHYSICS
        private class RaycastHitComparer : IComparer<RaycastHit>
        {
            public static RaycastHitComparer instance = new RaycastHitComparer();
            public int Compare(RaycastHit x, RaycastHit y)
            {
                return x.distance.CompareTo(y.distance);
            }
        }
#endif
    }
}
