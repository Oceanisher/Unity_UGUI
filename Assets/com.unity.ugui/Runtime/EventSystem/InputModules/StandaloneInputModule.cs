using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace UnityEngine.EventSystems
{
    [AddComponentMenu("Event/Standalone Input Module")]
    /// <summary>
    /// A BaseInputModule designed for mouse / keyboard / controller input.
    /// </summary>
    /// <remarks>
    /// Input module for working with, mouse, keyboard, or controller.
    /// </remarks>
    public class StandaloneInputModule : PointerInputModule
    {
        private float m_PrevActionTime;
        private Vector2 m_LastMoveVector;
        private int m_ConsecutiveMoveCount = 0;

        private Vector2 m_LastMousePosition;
        private Vector2 m_MousePosition;

        //当前处理的GO，比如鼠标在它上面，且它是最前面的一个，那么这里就是这个GO
        private GameObject m_CurrentFocusedGameObject;

        //当前处理的指针事件
        private PointerEventData m_InputPointerEvent;

        private const float doubleClickTime = 0.3f;

        protected StandaloneInputModule()
        {
        }

        [Obsolete("Mode is no longer needed on input module as it handles both mouse and keyboard simultaneously.", false)]
        public enum InputMode
        {
            Mouse,
            Buttons
        }

        [Obsolete("Mode is no longer needed on input module as it handles both mouse and keyboard simultaneously.", false)]
        public InputMode inputMode
        {
            get { return InputMode.Mouse; }
        }

        [SerializeField]
        private string m_HorizontalAxis = "Horizontal";

        /// <summary>
        /// Name of the vertical axis for movement (if axis events are used).
        /// </summary>
        [SerializeField]
        private string m_VerticalAxis = "Vertical";

        /// <summary>
        /// Name of the submit button.
        /// </summary>
        [SerializeField]
        private string m_SubmitButton = "Submit";

        /// <summary>
        /// Name of the submit button.
        /// </summary>
        [SerializeField]
        private string m_CancelButton = "Cancel";

        [SerializeField]
        private float m_InputActionsPerSecond = 10;

        [SerializeField]
        private float m_RepeatDelay = 0.5f;

        [SerializeField]
        [FormerlySerializedAs("m_AllowActivationOnMobileDevice")]
        [HideInInspector]
        private bool m_ForceModuleActive;

        [Obsolete("allowActivationOnMobileDevice has been deprecated. Use forceModuleActive instead (UnityUpgradable) -> forceModuleActive")]
        public bool allowActivationOnMobileDevice
        {
            get { return m_ForceModuleActive; }
            set { m_ForceModuleActive = value; }
        }

        /// <summary>
        /// Force this module to be active.
        /// </summary>
        /// <remarks>
        /// If there is no module active with higher priority (ordered in the inspector) this module will be forced active even if valid enabling conditions are not met.
        /// </remarks>

        [Obsolete("forceModuleActive has been deprecated. There is no need to force the module awake as StandaloneInputModule works for all platforms")]
        public bool forceModuleActive
        {
            get { return m_ForceModuleActive; }
            set { m_ForceModuleActive = value; }
        }

        /// <summary>
        /// Number of keyboard / controller inputs allowed per second.
        /// </summary>
        public float inputActionsPerSecond
        {
            get { return m_InputActionsPerSecond; }
            set { m_InputActionsPerSecond = value; }
        }

        /// <summary>
        /// Delay in seconds before the input actions per second repeat rate takes effect.
        /// </summary>
        /// <remarks>
        /// If the same direction is sustained, the inputActionsPerSecond property can be used to control the rate at which events are fired. However, it can be desirable that the first repetition is delayed, so the user doesn't get repeated actions by accident.
        /// </remarks>
        public float repeatDelay
        {
            get { return m_RepeatDelay; }
            set { m_RepeatDelay = value; }
        }

        /// <summary>
        /// Name of the horizontal axis for movement (if axis events are used).
        /// </summary>
        public string horizontalAxis
        {
            get { return m_HorizontalAxis; }
            set { m_HorizontalAxis = value; }
        }

        /// <summary>
        /// Name of the vertical axis for movement (if axis events are used).
        /// </summary>
        public string verticalAxis
        {
            get { return m_VerticalAxis; }
            set { m_VerticalAxis = value; }
        }

        /// <summary>
        /// Maximum number of input events handled per second.
        /// </summary>
        public string submitButton
        {
            get { return m_SubmitButton; }
            set { m_SubmitButton = value; }
        }

        /// <summary>
        /// Input manager name for the 'cancel' button.
        /// </summary>
        public string cancelButton
        {
            get { return m_CancelButton; }
            set { m_CancelButton = value; }
        }

        /// <summary>
        /// 在失去焦点时，是否要忽略事件
        /// </summary>
        /// <returns></returns>
        private bool ShouldIgnoreEventsOnNoFocus()
        {
#if UNITY_EDITOR
            //编辑器下，如果是用的某个客户端包、而不是Editor，那么在失去焦点时也不会忽略事件
            return !UnityEditor.EditorApplication.isRemoteConnected;
#else
            return true;
#endif
        }

        public override void UpdateModule()
        {
            if (!eventSystem.isFocused && ShouldIgnoreEventsOnNoFocus())
            {
                if (m_InputPointerEvent != null && m_InputPointerEvent.pointerDrag != null && m_InputPointerEvent.dragging)
                {
                    ReleaseMouse(m_InputPointerEvent, m_InputPointerEvent.pointerCurrentRaycast.gameObject);
                }

                m_InputPointerEvent = null;

                return;
            }

            m_LastMousePosition = m_MousePosition;
            m_MousePosition = input.mousePosition;
        }

        /// <summary>
        /// 释放鼠标按键
        /// 如果在这一帧发生了鼠标释放、或者点击后又释放事件，那么会执行这个
        /// </summary>
        /// <param name="pointerEvent"></param>
        /// <param name="currentOverGo"></param>
        private void ReleaseMouse(PointerEventData pointerEvent, GameObject currentOverGo)
        {
            //首先对前面接收到按下事件的物体，执行释放事件
            ExecuteEvents.Execute(pointerEvent.pointerPress, pointerEvent, ExecuteEvents.pointerUpHandler);
            
            var pointerClickHandler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentOverGo);

            // PointerClick and Drop events
            //如果当前命中的物体，有Click处理器、并且与当前接收Click事件的物体一致，并且之前的处理中把事件标记为了有可能发生Click，那么这里就确实是发生了Click
            if (pointerEvent.pointerClick == pointerClickHandler && pointerEvent.eligibleForClick)
            {
                ExecuteEvents.Execute(pointerEvent.pointerClick, pointerEvent, ExecuteEvents.pointerClickHandler);
            }
            //如果有关注拖拽的物体、并且已经在拖拽中了，那么这里就Drop放下了
            if (pointerEvent.pointerDrag != null && pointerEvent.dragging)
            {
                ExecuteEvents.ExecuteHierarchy(currentOverGo, pointerEvent, ExecuteEvents.dropHandler);
            }

            //清空点击、按下的信息
            pointerEvent.eligibleForClick = false;
            pointerEvent.pointerPress = null;
            pointerEvent.rawPointerPress = null;
            pointerEvent.pointerClick = null;

            //如果有关注拖拽的物体、并且已经在拖拽中了，那么除了执行Drop事件，还要执行拖拽结束事件
            if (pointerEvent.pointerDrag != null && pointerEvent.dragging)
                ExecuteEvents.Execute(pointerEvent.pointerDrag, pointerEvent, ExecuteEvents.endDragHandler);

            //清空拖拽信息
            pointerEvent.dragging = false;
            pointerEvent.pointerDrag = null;

            // redo pointer enter / exit to refresh state
            // so that if we moused over something that ignored it before
            // due to having pressed on something else
            // it now gets it.
            //如果当前射线命中物体，不是当前指针进入的物体了，那么对新老鼠标进入物体执行进入、退出事件
            //当鼠标按下时，会忽略其他物体的进入退出，所以释放时要重新处理下进入退出事件
            if (currentOverGo != pointerEvent.pointerEnter)
            {
                HandlePointerExitAndEnter(pointerEvent, null);
                HandlePointerExitAndEnter(pointerEvent, currentOverGo);
            }

            m_InputPointerEvent = pointerEvent;
        }

        public override bool ShouldActivateModule()
        {
            if (!base.ShouldActivateModule())
                return false;

            var shouldActivate = m_ForceModuleActive;
            shouldActivate |= input.GetButtonDown(m_SubmitButton);
            shouldActivate |= input.GetButtonDown(m_CancelButton);
            shouldActivate |= !Mathf.Approximately(input.GetAxisRaw(m_HorizontalAxis), 0.0f);
            shouldActivate |= !Mathf.Approximately(input.GetAxisRaw(m_VerticalAxis), 0.0f);
            shouldActivate |= (m_MousePosition - m_LastMousePosition).sqrMagnitude > 0.0f;
            shouldActivate |= input.GetMouseButtonDown(0);

            if (input.touchCount > 0)
                shouldActivate = true;

            return shouldActivate;
        }

        /// <summary>
        /// See BaseInputModule.
        /// </summary>
        public override void ActivateModule()
        {
            if (!eventSystem.isFocused && ShouldIgnoreEventsOnNoFocus())
                return;

            base.ActivateModule();
            m_MousePosition = input.mousePosition;
            m_LastMousePosition = input.mousePosition;

            var toSelect = eventSystem.currentSelectedGameObject;
            if (toSelect == null)
                toSelect = eventSystem.firstSelectedGameObject;

            eventSystem.SetSelectedGameObject(toSelect, GetBaseEventData());
        }

        /// <summary>
        /// See BaseInputModule.
        /// </summary>
        public override void DeactivateModule()
        {
            base.DeactivateModule();
            ClearSelection();
        }

        /// <summary>
        /// 被EventSystem每帧调用
        /// </summary>
        public override void Process()
        {
            //如果失去焦点、且在失去焦点时忽略事件，那么跳过
            if (!eventSystem.isFocused && ShouldIgnoreEventsOnNoFocus())
                return;

            //是否在该帧对选中的GO发送了更新事件
            bool usedEvent = SendUpdateEventToSelectedObject();

            // case 1004066 - touch / mouse events should be processed before navigation events in case
            // they change the current selected gameobject and the submit button is a touch / mouse button.

            // touch needs to take precedence because of the mouse emulation layer
            //优先执行触控事件，如果没有，那么再执行鼠标事件
            if (!ProcessTouchEvents() && input.mousePresent)
                ProcessMouseEvent();

            if (eventSystem.sendNavigationEvents)
            {
                if (!usedEvent)
                    usedEvent |= SendMoveEventToSelectedObject();

                if (!usedEvent)
                    SendSubmitEventToSelectedObject();
            }
        }

        /// <summary>
        /// 处理触控事件
        /// </summary>
        /// <returns>如果没有任何触控输入，那么返回false</returns>
        private bool ProcessTouchEvents()
        {
            //这里touchCount是代表几根手指输入
            for (int i = 0; i < input.touchCount; ++i)
            {
                Touch touch = input.GetTouch(i);

                //跳过非直接接触的手指输入
                if (touch.type == TouchType.Indirect)
                    continue;

                bool released;
                bool pressed;
                //获取当前触摸的事件
                var pointer = GetTouchPointerEventData(touch, out pressed, out released);

                ProcessTouchPress(pointer, pressed, released);

                //如果没有释放，处理移动、拖拽
                if (!released)
                {
                    ProcessMove(pointer);
                    ProcessDrag(pointer);
                }
                //如果有释放，那么执行移除，直接删除缓存，没有执行其他的事件
                else
                    RemovePointerData(pointer);
            }
            return input.touchCount > 0;
        }

        /// <summary>
        /// This method is called by Unity whenever a touch event is processed. Override this method with a custom implementation to process touch events yourself.
        /// 处理触控按压事件
        /// 处理方式与 鼠标处理 ProcessMousePress() 基本一致，可以参考 ProcessMousePress() 方法
        /// </summary>
        /// <param name="pointerEvent">Event data relating to the touch event, such as position and ID to be passed to the touch event destination object.</param>
        /// <param name="pressed">This is true for the first frame of a touch event, and false thereafter. This can therefore be used to determine the instant a touch event occurred.</param>
        /// <param name="released">This is true only for the last frame of a touch event.</param>
        /// <remarks>
        /// This method can be overridden in derived classes to change how touch press events are handled.
        /// </remarks>
        protected void ProcessTouchPress(PointerEventData pointerEvent, bool pressed, bool released)
        {
            var currentOverGo = pointerEvent.pointerCurrentRaycast.gameObject;

            // PointerDown notification
            if (pressed)
            {
                pointerEvent.eligibleForClick = true;
                pointerEvent.delta = Vector2.zero;
                pointerEvent.dragging = false;
                pointerEvent.useDragThreshold = true;
                pointerEvent.pressPosition = pointerEvent.position;
                pointerEvent.pointerPressRaycast = pointerEvent.pointerCurrentRaycast;

                DeselectIfSelectionChanged(currentOverGo, pointerEvent);

                if (pointerEvent.pointerEnter != currentOverGo)
                {
                    // send a pointer enter to the touched element if it isn't the one to select...
                    HandlePointerExitAndEnter(pointerEvent, currentOverGo);
                    pointerEvent.pointerEnter = currentOverGo;
                }

                var resetDiffTime = Time.unscaledTime - pointerEvent.clickTime;
                if (resetDiffTime >= doubleClickTime)
                {
                    pointerEvent.clickCount = 0;
                }

                // search for the control that will receive the press
                // if we can't find a press handler set the press
                // handler to be what would receive a click.
                var newPressed = ExecuteEvents.ExecuteHierarchy(currentOverGo, pointerEvent, ExecuteEvents.pointerDownHandler);

                var newClick = ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentOverGo);

                // didnt find a press handler... search for a click handler
                if (newPressed == null)
                    newPressed = newClick;

                // Debug.Log("Pressed: " + newPressed);

                float time = Time.unscaledTime;

                if (newPressed == pointerEvent.lastPress)
                {
                    var diffTime = time - pointerEvent.clickTime;
                    if (diffTime < doubleClickTime)
                        ++pointerEvent.clickCount;
                    else
                        pointerEvent.clickCount = 1;

                    pointerEvent.clickTime = time;
                }
                else
                {
                    pointerEvent.clickCount = 1;
                }

                pointerEvent.pointerPress = newPressed;
                pointerEvent.rawPointerPress = currentOverGo;
                pointerEvent.pointerClick = newClick;

                pointerEvent.clickTime = time;

                // Save the drag handler as well
                pointerEvent.pointerDrag = ExecuteEvents.GetEventHandler<IDragHandler>(currentOverGo);

                if (pointerEvent.pointerDrag != null)
                    ExecuteEvents.Execute(pointerEvent.pointerDrag, pointerEvent, ExecuteEvents.initializePotentialDrag);
            }

            // PointerUp notification
            if (released)
            {
                // Debug.Log("Executing pressup on: " + pointer.pointerPress);
                ExecuteEvents.Execute(pointerEvent.pointerPress, pointerEvent, ExecuteEvents.pointerUpHandler);

                // Debug.Log("KeyCode: " + pointer.eventData.keyCode);

                // see if we mouse up on the same element that we clicked on...
                var pointerClickHandler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentOverGo);

                // PointerClick and Drop events
                if (pointerEvent.pointerClick == pointerClickHandler && pointerEvent.eligibleForClick)
                {
                    ExecuteEvents.Execute(pointerEvent.pointerClick, pointerEvent, ExecuteEvents.pointerClickHandler);
                }

                if (pointerEvent.pointerDrag != null && pointerEvent.dragging)
                {
                    ExecuteEvents.ExecuteHierarchy(currentOverGo, pointerEvent, ExecuteEvents.dropHandler);
                }

                pointerEvent.eligibleForClick = false;
                pointerEvent.pointerPress = null;
                pointerEvent.rawPointerPress = null;
                pointerEvent.pointerClick = null;

                if (pointerEvent.pointerDrag != null && pointerEvent.dragging)
                    ExecuteEvents.Execute(pointerEvent.pointerDrag, pointerEvent, ExecuteEvents.endDragHandler);

                pointerEvent.dragging = false;
                pointerEvent.pointerDrag = null;

                // send exit events as we need to simulate this on touch up on touch device
                ExecuteEvents.ExecuteHierarchy(pointerEvent.pointerEnter, pointerEvent, ExecuteEvents.pointerExitHandler);
                pointerEvent.pointerEnter = null;
            }

            m_InputPointerEvent = pointerEvent;
        }

        /// <summary>
        /// Calculate and send a submit event to the current selected object.
        /// </summary>
        /// <returns>If the submit event was used by the selected object.</returns>
        protected bool SendSubmitEventToSelectedObject()
        {
            if (eventSystem.currentSelectedGameObject == null)
                return false;

            var data = GetBaseEventData();
            if (input.GetButtonDown(m_SubmitButton))
                ExecuteEvents.Execute(eventSystem.currentSelectedGameObject, data, ExecuteEvents.submitHandler);

            if (input.GetButtonDown(m_CancelButton))
                ExecuteEvents.Execute(eventSystem.currentSelectedGameObject, data, ExecuteEvents.cancelHandler);
            return data.used;
        }

        private Vector2 GetRawMoveVector()
        {
            Vector2 move = Vector2.zero;
            move.x = input.GetAxisRaw(m_HorizontalAxis);
            move.y = input.GetAxisRaw(m_VerticalAxis);

            if (input.GetButtonDown(m_HorizontalAxis))
            {
                if (move.x < 0)
                    move.x = -1f;
                if (move.x > 0)
                    move.x = 1f;
            }
            if (input.GetButtonDown(m_VerticalAxis))
            {
                if (move.y < 0)
                    move.y = -1f;
                if (move.y > 0)
                    move.y = 1f;
            }
            return move;
        }

        /// <summary>
        /// Calculate and send a move event to the current selected object.
        /// </summary>
        /// <returns>If the move event was used by the selected object.</returns>
        protected bool SendMoveEventToSelectedObject()
        {
            float time = Time.unscaledTime;

            Vector2 movement = GetRawMoveVector();
            if (Mathf.Approximately(movement.x, 0f) && Mathf.Approximately(movement.y, 0f))
            {
                m_ConsecutiveMoveCount = 0;
                return false;
            }

            bool similarDir = (Vector2.Dot(movement, m_LastMoveVector) > 0);

            // If direction didn't change at least 90 degrees, wait for delay before allowing consequtive event.
            if (similarDir && m_ConsecutiveMoveCount == 1)
            {
                if (time <= m_PrevActionTime + m_RepeatDelay)
                    return false;
            }
            // If direction changed at least 90 degree, or we already had the delay, repeat at repeat rate.
            else
            {
                if (time <= m_PrevActionTime + 1f / m_InputActionsPerSecond)
                    return false;
            }

            var axisEventData = GetAxisEventData(movement.x, movement.y, 0.6f);

            if (axisEventData.moveDir != MoveDirection.None)
            {
                ExecuteEvents.Execute(eventSystem.currentSelectedGameObject, axisEventData, ExecuteEvents.moveHandler);
                if (!similarDir)
                    m_ConsecutiveMoveCount = 0;
                m_ConsecutiveMoveCount++;
                m_PrevActionTime = time;
                m_LastMoveVector = movement;
            }
            else
            {
                m_ConsecutiveMoveCount = 0;
            }

            return axisEventData.used;
        }

        /// <summary>
        /// 处理鼠标事件
        /// </summary>
        protected void ProcessMouseEvent()
        {
            //每帧都会传入0
            ProcessMouseEvent(0);
        }

        [Obsolete("This method is no longer checked, overriding it with return true does nothing!")]
        protected virtual bool ForceAutoSelect()
        {
            return false;
        }

        /// <summary>
        /// Process all mouse events.
        /// 处理所有鼠标事件
        /// Id每帧都会传入0
        /// 这个ID最终没什么用
        /// </summary>
        protected void ProcessMouseEvent(int id)
        {
            //获取鼠标状态，里面包含3个鼠标按键的数据
            var mouseData = GetMousePointerEventData(id);
            //从左键事件中拿出信息进行处理，因为3个按键的基础信息都是依赖左键信息的
            var leftButtonData = mouseData.GetButtonState(PointerEventData.InputButton.Left).eventData;
            //当前最先命中的UI元素GO
            m_CurrentFocusedGameObject = leftButtonData.buttonData.pointerCurrentRaycast.gameObject;

            //先处理左键事件，处理按下、移动、拖拽事件
            //只有左键需要处理Move事件，因为其实3个鼠标事件的移动、滚动参数是一样的
            // Process the first mouse button fully
            ProcessMousePress(leftButtonData);
            ProcessMove(leftButtonData.buttonData);
            ProcessDrag(leftButtonData.buttonData);

            //再处理右键、中键，处理按下、拖拽事件
            // Now process right / middle clicks
            ProcessMousePress(mouseData.GetButtonState(PointerEventData.InputButton.Right).eventData);
            ProcessDrag(mouseData.GetButtonState(PointerEventData.InputButton.Right).eventData.buttonData);
            ProcessMousePress(mouseData.GetButtonState(PointerEventData.InputButton.Middle).eventData);
            ProcessDrag(mouseData.GetButtonState(PointerEventData.InputButton.Middle).eventData.buttonData);

            //如果有滚动数据，那么对当前物体执行级联的滚动事件调用
            if (!Mathf.Approximately(leftButtonData.buttonData.scrollDelta.sqrMagnitude, 0.0f))
            {
                var scrollHandler = ExecuteEvents.GetEventHandler<IScrollHandler>(leftButtonData.buttonData.pointerCurrentRaycast.gameObject);
                ExecuteEvents.ExecuteHierarchy(scrollHandler, leftButtonData.buttonData, ExecuteEvents.scrollHandler);
            }
        }

        /// <summary>
        /// 如果当前有选中的GO，那么每帧都会给它调用IUpdateSelectedHandler
        /// </summary>
        /// <returns>返回在该帧是否对选中的物体执行了Update事件</returns>
        protected bool SendUpdateEventToSelectedObject()
        {
            if (eventSystem.currentSelectedGameObject == null)
                return false;

            var data = GetBaseEventData();
            ExecuteEvents.Execute(eventSystem.currentSelectedGameObject, data, ExecuteEvents.updateSelectedHandler);
            return data.used;
        }

        /// <summary>
        /// Calculate and process any mouse button state changes.
        /// 处理鼠标按钮按下或者释放事件
        /// 根据事件中当前按钮是按下、还是释放，进行不同的处理
        /// </summary>
        protected void ProcessMousePress(MouseButtonEventData data)
        {
            var pointerEvent = data.buttonData;
            //事件的当前GO
            var currentOverGo = pointerEvent.pointerCurrentRaycast.gameObject;

            //如果该帧里，按钮按下了
            // PointerDown notification
            if (data.PressedThisFrame())
            {
                pointerEvent.eligibleForClick = true;
                pointerEvent.delta = Vector2.zero;
                pointerEvent.dragging = false;
                pointerEvent.useDragThreshold = true;
                pointerEvent.pressPosition = pointerEvent.position;
                pointerEvent.pointerPressRaycast = pointerEvent.pointerCurrentRaycast;

                //取消旧对象的选择
                DeselectIfSelectionChanged(currentOverGo, pointerEvent);

                //如果当前Click的时间超过了双击时间，那么Click次数归零
                var resetDiffTime = Time.unscaledTime - pointerEvent.clickTime;
                if (resetDiffTime >= doubleClickTime)
                {
                    pointerEvent.clickCount = 0;
                }

                // search for the control that will receive the press
                // if we can't find a press handler set the press
                // handler to be what would receive a click.
                //找到可以接收按下事件的子物体，如果自己就能接受按下事件，那么就是本物体
                var newPressed = ExecuteEvents.ExecuteHierarchy(currentOverGo, pointerEvent, ExecuteEvents.pointerDownHandler);
                //本物体Click处理器
                var newClick = ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentOverGo);

                // didnt find a press handler... search for a click handler
                //如果没有找到任何能接受按下的GO，那么接受按下的GO就是本物体
                if (newPressed == null)
                    newPressed = newClick;

                // Debug.Log("Pressed: " + newPressed);

                float time = Time.unscaledTime;

                //如果接收按下事件的物体，是上一个按下的物体
                if (newPressed == pointerEvent.lastPress)
                {
                    //如果在双击时间内，那么点击次数+1
                    var diffTime = time - pointerEvent.clickTime;
                    if (diffTime < doubleClickTime)
                        ++pointerEvent.clickCount;
                    //否则，点击数重置为1
                    else
                        pointerEvent.clickCount = 1;

                    //记录下本次点击事件
                    pointerEvent.clickTime = time;
                }
                //如果点击事件已经切换物体了，那么重置点击次数为1
                else
                {
                    pointerEvent.clickCount = 1;
                }

                pointerEvent.pointerPress = newPressed;
                pointerEvent.rawPointerPress = currentOverGo;
                pointerEvent.pointerClick = newClick;

                pointerEvent.clickTime = time;

                //找到当前物体的拖拽处理器，因为现在是按下了，所以有可能是拖拽，所以先执行潜在的拖拽事件，并且对拖拽赋值
                // Save the drag handler as well
                pointerEvent.pointerDrag = ExecuteEvents.GetEventHandler<IDragHandler>(currentOverGo);

                if (pointerEvent.pointerDrag != null)
                    ExecuteEvents.Execute(pointerEvent.pointerDrag, pointerEvent, ExecuteEvents.initializePotentialDrag);

                m_InputPointerEvent = pointerEvent;
            }

            //如果该帧里，按钮释放了
            // PointerUp notification
            if (data.ReleasedThisFrame())
            {
                ReleaseMouse(pointerEvent, currentOverGo);
            }
        }

        protected GameObject GetCurrentFocusedGameObject()
        {
            return m_CurrentFocusedGameObject;
        }
    }
}
