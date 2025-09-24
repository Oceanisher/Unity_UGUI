using System.Collections.Generic;
using System.Text;
using UnityEngine.UI;

namespace UnityEngine.EventSystems
{
    /// <summary>
    /// A BaseInputModule for pointer input.
    /// 指针类型输入模块基类
    /// 触摸、鼠标或者手柄输入模块都是继承自该类
    /// </summary>
    public abstract class PointerInputModule : BaseInputModule
    {
        /// <summary>
        /// Id of the cached left mouse pointer event.
        /// 鼠标左键的指定ID
        /// </summary>
        public const int kMouseLeftId = -1;

        /// <summary>
        /// Id of the cached right mouse pointer event.
        /// 鼠标右键的指定ID
        /// </summary>
        public const int kMouseRightId = -2;

        /// <summary>
        /// Id of the cached middle mouse pointer event.
        /// 鼠标中键的指定ID
        /// </summary>
        public const int kMouseMiddleId = -3;

        /// <summary>
        /// Touch id for when simulating touches on a non touch device.
        /// </summary>
        public const int kFakeTouchesId = -4;

        //当前缓存的鼠标事件，key是对应的鼠标id或者触控手指id
        protected Dictionary<int, PointerEventData> m_PointerData = new Dictionary<int, PointerEventData>();

        /// <summary>
        /// Search the cache for currently active pointers, return true if found.
        /// 获取当前缓存的指定鼠标按键ID的鼠标事件
        /// 如果create，那么当不存在时会创建一个新的鼠标事件
        /// </summary>
        /// <param name="id">Touch ID</param>
        /// <param name="data">Found data</param>
        /// <param name="create">If not found should it be created</param>
        /// <returns>True if pointer is found.</returns>
        protected bool GetPointerData(int id, out PointerEventData data, bool create)
        {
            if (!m_PointerData.TryGetValue(id, out data) && create)
            {
                data = new PointerEventData(eventSystem)
                {
                    pointerId = id,
                };
                m_PointerData.Add(id, data);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Remove the PointerEventData from the cache.
        /// 移除指针事件
        /// </summary>
        protected void RemovePointerData(PointerEventData data)
        {
            m_PointerData.Remove(data.pointerId);
        }

        /// <summary>
        /// Given a touch populate the PointerEventData and return if we are pressed or released.
        /// 获取触控的单个手指的触控事件
        ///
        /// 手指触控有5个状态：触摸开始、触摸移动、触摸静止、触摸结束、触摸被取消（比如被来电中断）
        /// </summary>
        /// <param name="input">Touch being processed</param>
        /// <param name="pressed">Are we pressed this frame</param>
        /// <param name="released">Are we released this frame</param>
        /// <returns></returns>
        protected PointerEventData GetTouchPointerEventData(Touch input, out bool pressed, out bool released)
        {
            PointerEventData pointerData;
            //获取或者创建手指触控事件，这里的id传入的是触控设备的FingerId
            var created = GetPointerData(input.fingerId, out pointerData, true);

            //事件重置
            pointerData.Reset();

            //如果是新创建的触摸事件、或者触控阶段是开始，那么此时状态为按下
            pressed = created || (input.phase == TouchPhase.Began);
            //如果触摸阶段是取消或者停止，那么此时状态为release
            released = (input.phase == TouchPhase.Canceled) || (input.phase == TouchPhase.Ended);

            //如果是新建的，那么记录下创建时手指的位置
            if (created)
                pointerData.position = input.position;

            //如果是按下，那么Delta归0
            if (pressed)
                pointerData.delta = Vector2.zero;
            //如果是释放，那么Delta是从上一帧到这一帧的移动距离
            else
                pointerData.delta = input.position - pointerData.position;

            //每帧记录下当前的位置
            pointerData.position = input.position;
            //触控的按键类型都是左键
            pointerData.button = PointerEventData.InputButton.Left;

            //如果触控被中断了，那么当前射线命中目标设置为空的
            if (input.phase == TouchPhase.Canceled)
            {
                pointerData.pointerCurrentRaycast = new RaycastResult();
            }
            //否则，使用射线检测，获取当前命中的目标
            else
            {
                eventSystem.RaycastAll(pointerData, m_RaycastResultCache);

                var raycast = FindFirstRaycast(m_RaycastResultCache);
                pointerData.pointerCurrentRaycast = raycast;
                m_RaycastResultCache.Clear();
            }

            pointerData.pressure = input.pressure;
            pointerData.altitudeAngle = input.altitudeAngle;
            pointerData.azimuthAngle = input.azimuthAngle;
            pointerData.radius = Vector2.one * input.radius;
            pointerData.radiusVariance = Vector2.one * input.radiusVariance;

            return pointerData;
        }

        /// <summary>
        /// Copy one PointerEventData to another.
        /// 事件复制
        /// </summary>
        protected void CopyFromTo(PointerEventData @from, PointerEventData @to)
        {
            @to.position = @from.position;
            @to.delta = @from.delta;
            @to.scrollDelta = @from.scrollDelta;
            @to.pointerCurrentRaycast = @from.pointerCurrentRaycast;
            @to.pointerEnter = @from.pointerEnter;

            @to.pressure = @from.pressure;
            @to.tangentialPressure = @from.tangentialPressure;
            @to.altitudeAngle = @from.altitudeAngle;
            @to.azimuthAngle = @from.azimuthAngle;
            @to.twist = @from.twist;
            @to.radius = @from.radius;
            @to.radiusVariance = @from.radiusVariance;
        }

        /// <summary>
        /// Given a mouse button return the current state for the frame.
        /// 给一个鼠标ID，然后从InputSystem中获取它的状态
        /// </summary>
        /// <param name="buttonId">Mouse button ID</param>
        protected PointerEventData.FramePressState StateForMouseButton(int buttonId)
        {
            var pressed = input.GetMouseButtonDown(buttonId);
            var released = input.GetMouseButtonUp(buttonId);
            if (pressed && released)
                return PointerEventData.FramePressState.PressedAndReleased;
            if (pressed)
                return PointerEventData.FramePressState.Pressed;
            if (released)
                return PointerEventData.FramePressState.Released;
            return PointerEventData.FramePressState.NotChanged;
        }

        /// <summary>
        /// 按钮状态
        /// </summary>
        protected class ButtonState
        {
            //按钮类型
            private PointerEventData.InputButton m_Button = PointerEventData.InputButton.Left;

            public MouseButtonEventData eventData
            {
                get { return m_EventData; }
                set { m_EventData = value; }
            }

            public PointerEventData.InputButton button
            {
                get { return m_Button; }
                set { m_Button = value; }
            }

            //按钮事件
            private MouseButtonEventData m_EventData;
        }

        /// <summary>
        /// 鼠标状态
        /// 鼠标每个案件的状态都放在里面
        /// </summary>
        protected class MouseState
        {
            private List<ButtonState> m_TrackedButtons = new List<ButtonState>();

            public bool AnyPressesThisFrame()
            {
                var trackedButtonsCount = m_TrackedButtons.Count;
                for (int i = 0; i < trackedButtonsCount; i++)
                {
                    if (m_TrackedButtons[i].eventData.PressedThisFrame())
                        return true;
                }
                return false;
            }

            public bool AnyReleasesThisFrame()
            {
                var trackedButtonsCount = m_TrackedButtons.Count;
                for (int i = 0; i < trackedButtonsCount; i++)
                {
                    if (m_TrackedButtons[i].eventData.ReleasedThisFrame())
                        return true;
                }
                return false;
            }

            /// <summary>
            /// 获取鼠标指针状态，并放入到列表中
            /// </summary>
            /// <param name="button"></param>
            /// <returns></returns>
            public ButtonState GetButtonState(PointerEventData.InputButton button)
            {
                //如果缓存列表里有，就从列表里取
                ButtonState tracked = null;
                var trackedButtonsCount = m_TrackedButtons.Count;
                for (int i = 0; i < trackedButtonsCount; i++)
                {
                    if (m_TrackedButtons[i].button == button)
                    {
                        tracked = m_TrackedButtons[i];
                        break;
                    }
                }

                //否则就新增一个
                if (tracked == null)
                {
                    tracked = new ButtonState { button = button, eventData = new MouseButtonEventData() };
                    m_TrackedButtons.Add(tracked);
                }
                return tracked;
            }

            /// <summary>
            /// 设置鼠标状态
            /// </summary>
            /// <param name="button"></param>
            /// <param name="stateForMouseButton"></param>
            /// <param name="data"></param>
            public void SetButtonState(PointerEventData.InputButton button, PointerEventData.FramePressState stateForMouseButton, PointerEventData data)
            {
                //获取或者新增一个鼠标状态
                var toModify = GetButtonState(button);
                toModify.eventData.buttonState = stateForMouseButton;
                toModify.eventData.buttonData = data;
            }
        }

        /// <summary>
        /// Information about a mouse button event.
        /// 按钮事件
        /// 封装了指针事件、指针在该帧内的状态
        /// </summary>
        public class MouseButtonEventData
        {
            /// <summary>
            /// The state of the button this frame.
            /// 指针在该帧内的状态
            /// </summary>
            public PointerEventData.FramePressState buttonState;

            /// <summary>
            /// Pointer data associated with the mouse event.
            /// 指针事件数据
            /// </summary>
            public PointerEventData buttonData;

            /// <summary>
            /// Was the button pressed this frame?
            /// 该帧是否按下了
            /// </summary>
            public bool PressedThisFrame()
            {
                return buttonState == PointerEventData.FramePressState.Pressed || buttonState == PointerEventData.FramePressState.PressedAndReleased;
            }

            /// <summary>
            /// Was the button released this frame?
            /// 该帧是否释放了
            /// </summary>
            public bool ReleasedThisFrame()
            {
                return buttonState == PointerEventData.FramePressState.Released || buttonState == PointerEventData.FramePressState.PressedAndReleased;
            }
        }

        private readonly MouseState m_MouseState = new MouseState();

        /// <summary>
        /// Return the current MouseState. Using the default pointer.
        /// </summary>
        protected virtual MouseState GetMousePointerEventData()
        {
            return GetMousePointerEventData(0);
        }

        /// <summary>
        /// Return the current MouseState.
        /// 获取鼠标状态
        /// 实际上是分别对鼠标左键、鼠标右键、鼠标中间事件各自进行了一次处理
        /// 从 ProcessMouseEvent 过来，id = 0
        /// 这个id目前是没用的状态
        /// </summary>
        protected virtual MouseState GetMousePointerEventData(int id)
        {
            // Populate the left button...
            //先处理鼠标左键事件
            PointerEventData leftData;
            //获取或者创建左键事件
            var created = GetPointerData(kMouseLeftId, out leftData, true);

            //无论是新增还是从缓存里拿的，都重置一下。但是Reset()方法是抽象类里的，PointerEventData并没有重写
            leftData.Reset();

            //如果是新增的，那么写入当前鼠标的位置
            if (created)
                leftData.position = input.mousePosition;

            Vector2 pos = input.mousePosition;
            //如果当前鼠标是Locked状态（固定到屏幕中心、并且隐藏、无法移动）
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                //此时鼠标位置强制描述为-1，不让它产生任何效果
                // We don't want to do ANY cursor-based interaction when the mouse is locked
                leftData.position = new Vector2(-1.0f, -1.0f);
                leftData.delta = Vector2.zero;
            }
            //非Lock状态下，记录下当前位置、位置变化增量
            else
            {
                leftData.delta = pos - leftData.position;
                leftData.position = pos;
            }
            //滚动增量
            leftData.scrollDelta = input.mouseScrollDelta;
            leftData.button = PointerEventData.InputButton.Left;
            //使用该左键事件，发出射线，并找到第一个射线命中的结果放到事件中；之后清除命中缓存
            eventSystem.RaycastAll(leftData, m_RaycastResultCache);
            var raycast = FindFirstRaycast(m_RaycastResultCache);
            leftData.pointerCurrentRaycast = raycast;
            m_RaycastResultCache.Clear();

            //把鼠标左键的事件内容，Copy给鼠标右键事件
            // copy the apropriate data into right and middle slots
            PointerEventData rightData;
            GetPointerData(kMouseRightId, out rightData, true);
            rightData.Reset();

            CopyFromTo(leftData, rightData);
            rightData.button = PointerEventData.InputButton.Right;

            //把鼠标左键的事件内容，Copy给鼠标中键事件
            PointerEventData middleData;
            GetPointerData(kMouseMiddleId, out middleData, true);
            middleData.Reset();

            CopyFromTo(leftData, middleData);
            middleData.button = PointerEventData.InputButton.Middle;

            //把3个鼠标事件放到鼠标状态中
            m_MouseState.SetButtonState(PointerEventData.InputButton.Left, StateForMouseButton(0), leftData);
            m_MouseState.SetButtonState(PointerEventData.InputButton.Right, StateForMouseButton(1), rightData);
            m_MouseState.SetButtonState(PointerEventData.InputButton.Middle, StateForMouseButton(2), middleData);

            return m_MouseState;
        }

        /// <summary>
        /// Return the last PointerEventData for the given touch / mouse id.
        /// 获取最新的指针事件
        /// </summary>
        protected PointerEventData GetLastPointerEventData(int id)
        {
            PointerEventData data;
            GetPointerData(id, out data, false);
            return data;
        }

        /// <summary>
        /// 判断当前是否进行了拖拽
        /// 实际上是根据移动距离进行判断的，移动距离=按下鼠标的位置-当前鼠标位置
        /// </summary>
        /// <param name="pressPos"></param>
        /// <param name="currentPos"></param>
        /// <param name="threshold"></param>
        /// <param name="useDragThreshold"></param>
        /// <returns></returns>
        private static bool ShouldStartDrag(Vector2 pressPos, Vector2 currentPos, float threshold, bool useDragThreshold)
        {
            if (!useDragThreshold)
                return true;

            return (pressPos - currentPos).sqrMagnitude >= threshold * threshold;
        }

        /// <summary>
        /// Process movement for the current frame with the given pointer event.
        /// 处理鼠标移动事件
        /// 主要是鼠标的进入、退出事件
        /// </summary>
        protected virtual void ProcessMove(PointerEventData pointerEvent)
        {
            //如果当前鼠标在锁定状态，那么就没有新的进入的物体
            var targetGO = (Cursor.lockState == CursorLockMode.Locked ? null : pointerEvent.pointerCurrentRaycast.gameObject);
            HandlePointerExitAndEnter(pointerEvent, targetGO);
        }

        /// <summary>
        /// Process the drag for the current frame with the given pointer event.
        /// 处理鼠标拖拽事件
        /// </summary>
        protected virtual void ProcessDrag(PointerEventData pointerEvent)
        {
            //没有移动、鼠标处于锁定模式、没有拖拽对象，不处理
            if (!pointerEvent.IsPointerMoving() ||
                Cursor.lockState == CursorLockMode.Locked ||
                pointerEvent.pointerDrag == null)
                return;

            //如果没有在拖拽中，但是判断现在需要进行拖拽了，那么先执行拖拽开始事件
            if (!pointerEvent.dragging
                && ShouldStartDrag(pointerEvent.pressPosition, pointerEvent.position, eventSystem.pixelDragThreshold, pointerEvent.useDragThreshold))
            {
                ExecuteEvents.Execute(pointerEvent.pointerDrag, pointerEvent, ExecuteEvents.beginDragHandler);
                pointerEvent.dragging = true;
            }

            // Drag notification
            //如果当前在拖拽中
            if (pointerEvent.dragging)
            {
                // Before doing drag we should cancel any pointer down state
                // And clear selection!
                //如果拖拽中，并且当前拖拽对象和按下对象不是同一个，那么需要清理按下对象
                if (pointerEvent.pointerPress != pointerEvent.pointerDrag)
                {
                    //对按下的对象执行弹起事件，并且清空
                    ExecuteEvents.Execute(pointerEvent.pointerPress, pointerEvent, ExecuteEvents.pointerUpHandler);

                    pointerEvent.eligibleForClick = false;
                    pointerEvent.pointerPress = null;
                    pointerEvent.rawPointerPress = null;
                }
                //执行拖拽中事件
                ExecuteEvents.Execute(pointerEvent.pointerDrag, pointerEvent, ExecuteEvents.dragHandler);
            }
        }

        /// <summary>
        /// 指针是否经过某个GO
        /// 有对应的指针事件、并且指针事件中有指针进入的GO
        /// </summary>
        /// <param name="pointerId"></param>
        /// <returns></returns>
        public override bool IsPointerOverGameObject(int pointerId)
        {
            var lastPointer = GetLastPointerEventData(pointerId);
            if (lastPointer != null)
                return lastPointer.pointerEnter != null;
            return false;
        }

        /// <summary>
        /// Clear all pointers and deselect any selected objects in the EventSystem.
        /// 取消当前选择的所有UI
        /// </summary>
        protected void ClearSelection()
        {
            var baseEventData = GetBaseEventData();

            foreach (var pointer in m_PointerData.Values)
            {
                // clear all selection
                HandlePointerExitAndEnter(pointer, null);
            }

            m_PointerData.Clear();
            eventSystem.SetSelectedGameObject(null, baseEventData);
        }

        public override string ToString()
        {
            var sb = new StringBuilder("<b>Pointer Input Module of type: </b>" + GetType());
            sb.AppendLine();
            foreach (var pointer in m_PointerData)
            {
                if (pointer.Value == null)
                    continue;
                sb.AppendLine("<B>Pointer:</b> " + pointer.Key);
                sb.AppendLine(pointer.Value.ToString());
            }
            return sb.ToString();
        }

        /// <summary>
        /// Deselect the current selected GameObject if the currently pointed-at GameObject is different.
        /// 如果新的选择的对象变更了，那么把原有选的对象清空
        /// 只清空旧的，新的先不处理
        /// </summary>
        /// <param name="currentOverGo">The GameObject the pointer is currently over.</param>
        /// <param name="pointerEvent">Current event data.</param>
        protected void DeselectIfSelectionChanged(GameObject currentOverGo, BaseEventData pointerEvent)
        {
            //对新的选择的物体执行
            // Selection tracking
            var selectHandlerGO = ExecuteEvents.GetEventHandler<ISelectHandler>(currentOverGo);
            // if we have clicked something new, deselect the old thing
            // leave 'selection handling' up to the press event though.
            // 只清空旧的，新的先不处理
            if (selectHandlerGO != eventSystem.currentSelectedGameObject)
                eventSystem.SetSelectedGameObject(null, pointerEvent);
        }
    }
}
