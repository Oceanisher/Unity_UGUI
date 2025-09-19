using System;
using System.Collections.Generic;

namespace UnityEngine.EventSystems
{
    [RequireComponent(typeof(EventSystem))]
    /// <summary>
    /// A base module that raises events and sends them to GameObjects.
    /// 输入模块，要跟EventSystem组件放在同一个GO上
    /// 生成事件、发送事件
    /// </summary>
    /// <remarks>
    /// An Input Module is a component of the EventSystem that is responsible for raising events and sending them to GameObjects for handling. The BaseInputModule is a class that all Input Modules in the EventSystem inherit from. Examples of provided modules are TouchInputModule and StandaloneInputModule, if these are inadequate for your project you can create your own by extending from the BaseInputModule.
    /// </remarks>
    /// <example>
    /// <code>
    /// <![CDATA[
    /// using UnityEngine;
    /// using UnityEngine.EventSystems;
    ///
    /// /**
    ///  * Create a module that every tick sends a 'Move' event to
    ///  * the target object
    ///  */
    /// public class MyInputModule : BaseInputModule
    /// {
    ///     public GameObject m_TargetObject;
    ///
    ///     public override void Process()
    ///     {
    ///         if (m_TargetObject == null)
    ///             return;
    ///         ExecuteEvents.Execute (m_TargetObject, new BaseEventData (eventSystem), ExecuteEvents.moveHandler);
    ///     }
    /// }
    /// ]]>
    ///</code>
    /// </example>
    public abstract class BaseInputModule : UIBehaviour
    {
        //射线结果缓存
        [NonSerialized]
        protected List<RaycastResult> m_RaycastResultCache = new List<RaycastResult>();

        /// <summary>
        /// True if pointer hover events will be sent to the parent
        /// 如果鼠标悬停事件需要发送给父节点，也就说悬停事件是否能够穿透
        /// </summary>
        [SerializeField] private bool m_SendPointerHoverToParent = true;
        //This is needed for testing
        internal bool sendPointerHoverToParent { get { return m_SendPointerHoverToParent; } set { m_SendPointerHoverToParent = value; } }

        private AxisEventData m_AxisEventData;

        private EventSystem m_EventSystem;
        private BaseEventData m_BaseEventData;

        protected BaseInput m_InputOverride;
        private BaseInput m_DefaultInput;

        /// <summary>
        /// The current BaseInput being used by the input module.
        /// </summary>
        public BaseInput input
        {
            get
            {
                if (m_InputOverride != null)
                    return m_InputOverride;

                if (m_DefaultInput == null)
                {
                    var inputs = GetComponents<BaseInput>();
                    foreach (var baseInput in inputs)
                    {
                        // We dont want to use any classes that derrive from BaseInput for default.
                        if (baseInput != null && baseInput.GetType() == typeof(BaseInput))
                        {
                            m_DefaultInput = baseInput;
                            break;
                        }
                    }

                    if (m_DefaultInput == null)
                        m_DefaultInput = gameObject.AddComponent<BaseInput>();
                }

                return m_DefaultInput;
            }
        }

        /// <summary>
        /// Used to override the default BaseInput for the input module.
        /// </summary>
        /// <remarks>
        /// With this it is possible to bypass the Input system with your own but still use the same InputModule. For example this can be used to feed fake input into the UI or interface with a different input system.
        /// </remarks>
        public BaseInput inputOverride
        {
            get { return m_InputOverride; }
            set { m_InputOverride = value; }
        }

        protected EventSystem eventSystem
        {
            get { return m_EventSystem; }
        }

        /// <summary>
        /// Enable时让EventSystem重新刷新Module列表
        /// </summary>
        protected override void OnEnable()
        {
            base.OnEnable();
            m_EventSystem = GetComponent<EventSystem>();
            m_EventSystem.UpdateModules();
        }

        /// <summary>
        /// Disable时让EventSystem重新刷新Module列表
        /// </summary>
        protected override void OnDisable()
        {
            m_EventSystem.UpdateModules();
            base.OnDisable();
        }

        /// <summary>
        /// Process the current tick for the module.
        /// 被EventSystem每帧调用
        /// </summary>
        public abstract void Process();

        /// <summary>
        /// Return the first valid RaycastResult.
        /// 找到射线列表中第一个GO有效的射线结果
        /// </summary>
        protected static RaycastResult FindFirstRaycast(List<RaycastResult> candidates)
        {
            var candidatesCount = candidates.Count;
            for (var i = 0; i < candidatesCount; ++i)
            {
                if (candidates[i].gameObject == null)
                    continue;

                return candidates[i];
            }
            return new RaycastResult();
        }

        /// <summary>
        /// Given an input movement, determine the best MoveDirection.
        /// 传入一个XY坐标，返回一个最佳移动方向
        /// 主要是触控和摇杆使用
        /// </summary>
        /// <param name="x">X movement.</param>
        /// <param name="y">Y movement.</param>
        protected static MoveDirection DetermineMoveDirection(float x, float y)
        {
            return DetermineMoveDirection(x, y, 0.6f);
        }

        /// <summary>
        /// Given an input movement, determine the best MoveDirection.
        /// 传入一个XY坐标，返回一个最佳移动方向，只返回4方向的移动
        /// 主要是触控和摇杆使用
        /// </summary>
        /// <param name="x">X movement.</param>
        /// <param name="y">Y movement.</param>
        /// <param name="deadZone">Dead zone.死区是指用户触摸到的最小距离后才能激活摇杆</param>
        protected static MoveDirection DetermineMoveDirection(float x, float y, float deadZone)
        {
            //如果移动距离在死区内，那么不响应
            // if vector is too small... just return
            if (new Vector2(x, y).sqrMagnitude < deadZone * deadZone)
                return MoveDirection.None;

            //主要看XY哪个大，然后看大的那个的方向
            if (Mathf.Abs(x) > Mathf.Abs(y))
            {
                return x > 0 ? MoveDirection.Right : MoveDirection.Left;
            }

            return y > 0 ? MoveDirection.Up : MoveDirection.Down;
        }

        /// <summary>
        /// Given 2 GameObjects, return a common root GameObject (or null).
        /// 找到2个GO的共同根节点，最近的根节点
        /// </summary>
        /// <param name="g1">GameObject to compare</param>
        /// <param name="g2">GameObject to compare</param>
        /// <returns></returns>
        protected static GameObject FindCommonRoot(GameObject g1, GameObject g2)
        {
            if (g1 == null || g2 == null)
                return null;

            var t1 = g1.transform;
            while (t1 != null)
            {
                var t2 = g2.transform;
                while (t2 != null)
                {
                    if (t1 == t2)
                        return t1.gameObject;
                    t2 = t2.parent;
                }
                t1 = t1.parent;
            }
            return null;
        }

        // walk up the tree till a common root between the last entered and the current entered is found
        // send exit events up to (but not including) the common root. Then send enter events up to
        // (but not including) the common root.
        // Send move events before exit, after enter, and on hovered objects when pointer data has changed.
        // 处理鼠标对UI的悬停事件（进入、移动、退出）
        //需要处理两种情况：悬停事件能否穿透
        //1:如果能够穿透，那么就是从命中节点开始，逐步向上遍历，把所有节点都执行悬停事件；那么退出的节点，也是需要逐步向上遍历，到新老节点的公共父节点结束，所有节点全部执行退出
        //2:如果不能穿透，那么就是从命中节点开始，逐步向上遍历，找到第一个能够接收悬停事件的对象就结束（中间所有的节点也需要执行）；那么退出节点，也是找到第一个接收悬停对象的结束，所有节点全部执行退出
        protected void HandlePointerExitAndEnter(PointerEventData currentPointerData, GameObject newEnterTarget)
        {
            // if we have no target / pointerEnter has been deleted
            // just send exit events to anything we are tracking
            // then exit
            //如果没有新物体、或者之前没有进入事件
            //这说明鼠标完全没有在可以接收悬停信息的UI上，因为每帧都会检测。所以如果没有新物体、或者没有当前的进入物体，那么就直接清空列表就可以了
            if (newEnterTarget == null || currentPointerData.pointerEnter == null)
            {
                //对所有原来悬停的物体执行退出事件
                //退出事件前先执行一遍Move事件
                var hoveredCount = currentPointerData.hovered.Count;
                for (var i = 0; i < hoveredCount; ++i)
                {
                    currentPointerData.fullyExited = true;
                    ExecuteEvents.Execute(currentPointerData.hovered[i], currentPointerData, ExecuteEvents.pointerMoveHandler);
                    ExecuteEvents.Execute(currentPointerData.hovered[i], currentPointerData, ExecuteEvents.pointerExitHandler);
                }

                //清空悬停列表
                currentPointerData.hovered.Clear();

                //如果没有新进入的物体，那么清空Enter的物体，并返回
                if (newEnterTarget == null)
                {
                    currentPointerData.pointerEnter = null;
                    return;
                }
            }

            //如果新的进入物体没有变化
            // if we have not changed hover target
            if (currentPointerData.pointerEnter == newEnterTarget && newEnterTarget)
            {
                //如果鼠标移动了，那么对所有已经悬停的物体执行鼠标移动事件
                if (currentPointerData.IsPointerMoving())
                {
                    var hoveredCount = currentPointerData.hovered.Count;
                    for (var i = 0; i < hoveredCount; ++i)
                        ExecuteEvents.Execute(currentPointerData.hovered[i], currentPointerData, ExecuteEvents.pointerMoveHandler);
                }
                return;
            }

            //如果有新进入的物体，并且新物体跟上一个进入的物体不一致，也就是说鼠标从一个UI移动到了另一个UI上
            //找到新旧物体的共同最近根节点
            GameObject commonRoot = FindCommonRoot(currentPointerData.pointerEnter, newEnterTarget);
            //找到新物体的父节点上的Exit处理器，如果没有这样的父节点的话，那这里就是空的
            GameObject pointerParent = ((Component)newEnterTarget.GetComponentInParent<IPointerExitHandler>())?.gameObject;

            //如果有老的进入的物体，需要执行退出
            // and we already an entered object from last time
            if (currentPointerData.pointerEnter != null)
            {
                // send exit handler call to all elements in the chain
                // until we reach the new target, or null!
                // ** or when !m_SendPointerEnterToParent, stop when meeting a gameobject with an exit event handler
                Transform t = currentPointerData.pointerEnter.transform;

                //对老的物体，往上对父节点层层遍历
                while (t != null)
                {
                    // if we reach the common root break out!
                    //如果遍历到了新老物体的共同父节点，并且悬停事件能够穿透，那么停止
                    //因为相当于鼠标从一个子节点切换到了另一个子节点，所以退出就只是老的子节点到公共节点之前的所有节点退出即可。
                    if (m_SendPointerHoverToParent && commonRoot != null && commonRoot.transform == t)
                        break;

                    // if we reach a PointerExitEvent break out!
                    //如果遍历到了新物体的有Exit处理器的父节点，并且悬停事件不能够穿透，那么停止
                    if (!m_SendPointerHoverToParent && pointerParent == t.gameObject)
                        break;

                    //中间物体进行处理
                    //是否是完全退出：如果向上遍历的物体还不是公共根节点、并且新老物体不一致，那么就是完全退出
                    currentPointerData.fullyExited = t.gameObject != commonRoot && currentPointerData.pointerEnter != newEnterTarget;
                    //对中间物体执行移动、退出事件
                    ExecuteEvents.Execute(t.gameObject, currentPointerData, ExecuteEvents.pointerMoveHandler);
                    ExecuteEvents.Execute(t.gameObject, currentPointerData, ExecuteEvents.pointerExitHandler);
                    //悬停列表删除中间物体
                    currentPointerData.hovered.Remove(t.gameObject);

                    //继续向上遍历
                    if (m_SendPointerHoverToParent) t = t.parent;

                    //如果已经到了公共节点了，那么停止
                    // if we reach the common root break out!
                    if (commonRoot != null && commonRoot.transform == t)
                        break;

                    //继续向上遍历
                    if (!m_SendPointerHoverToParent) t = t.parent;
                }
            }

            //处理新进入的物体
            //层层向上遍历
            // now issue the enter call up to but not including the common root
            var oldPointerEnter = currentPointerData.pointerEnter;
            //设置当前进入的物体为新物体
            currentPointerData.pointerEnter = newEnterTarget;
            if (newEnterTarget != null)
            {
                Transform t = newEnterTarget.transform;

                while (t != null)
                {
                    //是否是从一个子物体，重新进入到了父节点物体中
                    currentPointerData.reentered = t.gameObject == commonRoot && t.gameObject != oldPointerEnter;
                    // if we are sending the event to parent, they are already in hover mode at that point. No need to bubble up the event.
                    //如果是悬停事件需要发送给父节点，并且是重新进入父节点，那么跳过
                    if (m_SendPointerHoverToParent && currentPointerData.reentered)
                        break;

                    //对中间物体执行进入、移动事件，并且添加到悬停列表
                    ExecuteEvents.Execute(t.gameObject, currentPointerData, ExecuteEvents.pointerEnterHandler);
                    ExecuteEvents.Execute(t.gameObject, currentPointerData, ExecuteEvents.pointerMoveHandler);
                    currentPointerData.hovered.Add(t.gameObject);

                    //如果不需要发送事件给父节点，并且该中间物体有Enter处理器，那么跳过
                    // stop when encountering an object with the pointerEnterHandler
                    if (!m_SendPointerHoverToParent && t.gameObject.GetComponent<IPointerEnterHandler>() != null)
                        break;

                    //继续向上遍历
                    if (m_SendPointerHoverToParent) t = t.parent;

                    // if we reach the common root break out!
                    //如果到达了公共父节点，那么跳过
                    if (commonRoot != null && commonRoot.transform == t)
                        break;

                    //继续向上遍历
                    if (!m_SendPointerHoverToParent) t = t.parent;
                }
            }
        }

        /// <summary>
        /// Given some input data generate an AxisEventData that can be used by the event system.
        /// </summary>
        /// <param name="x">X movement.</param>
        /// <param name="y">Y movement.</param>
        /// <param name="deadZone">Dead zone.</param>
        protected virtual AxisEventData GetAxisEventData(float x, float y, float moveDeadZone)
        {
            if (m_AxisEventData == null)
                m_AxisEventData = new AxisEventData(eventSystem);

            m_AxisEventData.Reset();
            m_AxisEventData.moveVector = new Vector2(x, y);
            m_AxisEventData.moveDir = DetermineMoveDirection(x, y, moveDeadZone);
            return m_AxisEventData;
        }

        /// <summary>
        /// Generate a BaseEventData that can be used by the EventSystem.
        /// </summary>
        protected virtual BaseEventData GetBaseEventData()
        {
            if (m_BaseEventData == null)
                m_BaseEventData = new BaseEventData(eventSystem);

            m_BaseEventData.Reset();
            return m_BaseEventData;
        }

        /// <summary>
        /// If the module is pointer based, then override this to return true if the pointer is over an event system object.
        /// </summary>
        /// <param name="pointerId">Pointer ID</param>
        /// <returns>Is the given pointer over an event system object?</returns>
        public virtual bool IsPointerOverGameObject(int pointerId)
        {
            return false;
        }

        /// <summary>
        /// Should the module be activated.
        /// </summary>
        public virtual bool ShouldActivateModule()
        {
            return enabled && gameObject.activeInHierarchy;
        }

        /// <summary>
        /// Called when the module is deactivated. Override this if you want custom code to execute when you deactivate your module.
        /// </summary>
        public virtual void DeactivateModule()
        {}

        /// <summary>
        /// Called when the module is activated. Override this if you want custom code to execute when you activate your module.
        /// </summary>
        public virtual void ActivateModule()
        {}

        /// <summary>
        /// Update the internal state of the Module.
        /// </summary>
        public virtual void UpdateModule()
        {}

        /// <summary>
        /// Check to see if the module is supported. Override this if you have a platform specific module (eg. TouchInputModule that you do not want to activate on standalone.)
        /// </summary>
        /// <returns>Is the module supported.</returns>
        public virtual bool IsModuleSupported()
        {
            return true;
        }

        /// <summary>
        /// Returns Id of the pointer following <see cref="UnityEngine.UIElements.PointerId"/> convention.
        /// </summary>
        /// <param name="sourcePointerData">PointerEventData whose pointerId will be converted to UI Toolkit pointer convention.</param>
        /// <seealso cref="UnityEngine.UIElements.IPointerEvent" />
        public virtual int ConvertUIToolkitPointerId(PointerEventData sourcePointerData)
        {
#if PACKAGE_UITOOLKIT
            return sourcePointerData.pointerId < 0 ?
                UIElements.PointerId.mousePointerId :
                UIElements.PointerId.touchPointerIdBase + sourcePointerData.pointerId;
#else
            return -1;
#endif
        }
    }
}
