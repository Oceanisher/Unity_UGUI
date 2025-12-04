using System;
using System.Collections;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace UnityEngine.UI
{
    [AddComponentMenu("UI/Scrollbar", 36)]
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    /// <summary>
    /// A standard scrollbar with a variable sized handle that can be dragged between 0 and 1.
    /// 滚动条
    /// </summary>
    /// <remarks>
    /// The slider component is a Selectable that controls a handle which follow the current value and is sized according to the size property.
    /// The anchors of the handle RectTransforms are driven by the Scrollbar. The handle can be a direct child of the GameObject with the Scrollbar, or intermediary RectTransforms can be placed in between for additional control.
    /// When a change to the scrollbar value occurs, a callback is sent to any registered listeners of onValueChanged.
    /// </remarks>
    public class Scrollbar : Selectable, IBeginDragHandler, IDragHandler, IInitializePotentialDragHandler, ICanvasElement
    {
        /// <summary>
        /// Setting that indicates one of four directions the scrollbar will travel.
        /// 滚动方向
        /// </summary>
        public enum Direction
        {
            /// <summary>
            /// Starting position is the Left.
            /// 左到右
            /// </summary>
            LeftToRight,

            /// <summary>
            /// Starting position is the Right
            /// 右到左
            /// </summary>
            RightToLeft,

            /// <summary>
            /// Starting position is the Bottom.
            /// 下到上
            /// </summary>
            BottomToTop,

            /// <summary>
            /// Starting position is the Top.
            /// 上到下
            /// </summary>
            TopToBottom,
        }

        [Serializable]
        /// <summary>
        /// UnityEvent callback for when a scrollbar is scrolled.
        /// 滚动条回调
        /// </summary>
        public class ScrollEvent : UnityEvent<float> {}

        //滑块RectTrs
        [SerializeField]
        private RectTransform m_HandleRect;

        /// <summary>
        /// The RectTransform to use for the handle.
        /// 滑块RectTrs
        /// </summary>
        public RectTransform handleRect { get { return m_HandleRect; } set { if (SetPropertyUtility.SetClass(ref m_HandleRect, value)) { UpdateCachedReferences(); UpdateVisuals(); } } }

        // Direction of movement.
        //滚动方向
        [SerializeField]
        private Direction m_Direction = Direction.LeftToRight;

        /// <summary>
        /// The direction of the scrollbar from minimum to maximum value.
        /// 滚动方向
        /// </summary>
        public Direction direction { get { return m_Direction; } set { if (SetPropertyUtility.SetStruct(ref m_Direction, value)) UpdateVisuals(); } }

        protected Scrollbar()
        {}

        //滚动条的值，0~1
        [Range(0f, 1f)]
        [SerializeField]
        private float m_Value;

        /// <summary>
        /// The current value of the scrollbar, between 0 and 1.
        /// 滚动条值，0~1
        /// </summary>
        public float value
        {
            get
            {
                float val = m_Value;
                if (m_NumberOfSteps > 1)
                    val = Mathf.Round(val * (m_NumberOfSteps - 1)) / (m_NumberOfSteps - 1);
                return val;
            }
            set
            {
                Set(value);
            }
        }

        /// <summary>
        /// Set the value of the scrollbar without invoking onValueChanged callback.
        /// </summary>
        /// <param name="input">The new value for the scrollbar.</param>
        public virtual void SetValueWithoutNotify(float input)
        {
            Set(input, false);
        }

        //滑块自身的大小，是相对于Container的比例，为1则是跟Container一样大小
        [Range(0f, 1f)]
        [SerializeField]
        private float m_Size = 0.2f;

        /// <summary>
        /// The size of the scrollbar handle where 1 means it fills the entire scrollbar.
        /// 滑块自身的大小，是相对于Container的比例，为1则是跟Container一样大小
        /// </summary>
        public float size { get { return m_Size; } set { if (SetPropertyUtility.SetStruct(ref m_Size, Mathf.Clamp01(value))) UpdateVisuals(); } }

        //总移动步长
        //如果设置了>1，那么移动范围0~1就是在这个总步长的比例上进行移动，离散型移动，不再是无极移动
        [Range(0, 11)]
        [SerializeField]
        private int m_NumberOfSteps = 0;

        /// <summary>
        /// The number of steps to use for the value. A value of 0 disables use of steps.
        /// 总移动步长
        /// </summary>
        public int numberOfSteps { get { return m_NumberOfSteps; } set { if (SetPropertyUtility.SetStruct(ref m_NumberOfSteps, value)) { Set(m_Value); UpdateVisuals(); } } }

        [Space(6)]

        //值变更回调
        [SerializeField]
        private ScrollEvent m_OnValueChanged = new ScrollEvent();

        /// <summary>
        /// Handling for when the scrollbar value is changed.
        /// </summary>
        /// <remarks>
        /// Allow for delegate-based subscriptions for faster events than 'eventReceiver', and allowing for multiple receivers.
        /// </remarks>
        public ScrollEvent onValueChanged { get { return m_OnValueChanged; } set { m_OnValueChanged = value; } }

        // Private fields
        //Handle滑块的父节点，也就是SlidingArea
        private RectTransform m_ContainerRect;

        //开始拖拽事件的时候，指针所在的位置相对于滑块区域中心的偏移量
        //赋值的前提是开始拖拽事件指针不在滑块上、而是在非滑块区域
        // The offset from handle position to mouse down position
        private Vector2 m_Offset = Vector2.zero;

        // Size of each step.
        float stepSize { get { return (m_NumberOfSteps > 1) ? 1f / (m_NumberOfSteps - 1) : 0.1f; } }

        // field is never assigned warning
        #pragma warning disable 649
        private DrivenRectTransformTracker m_Tracker;
        #pragma warning restore 649
        private Coroutine m_PointerDownRepeat;
        //是否指针按下但是没有开始拖拽
        private bool isPointerDownAndNotDragging = false;

        // This "delayed" mechanism is required for case 1037681.
        private bool m_DelayedUpdateVisuals = false;

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();

            m_Size = Mathf.Clamp01(m_Size);

            //This can be invoked before OnEnabled is called. So we shouldn't be accessing other objects, before OnEnable is called.
            if (IsActive())
            {
                UpdateCachedReferences();
                Set(m_Value, false);
                // Update rects (in next update) since other things might affect them even if value didn't change.
                m_DelayedUpdateVisuals = true;
            }

            if (!UnityEditor.PrefabUtility.IsPartOfPrefabAsset(this) && !Application.isPlaying)
                CanvasUpdateRegistry.RegisterCanvasElementForLayoutRebuild(this);
        }

#endif // if UNITY_EDITOR

        public virtual void Rebuild(CanvasUpdate executing)
        {
#if UNITY_EDITOR
            if (executing == CanvasUpdate.Prelayout)
                onValueChanged.Invoke(value);
#endif
        }

        /// <summary>
        /// See ICanvasElement.LayoutComplete.
        /// </summary>
        public virtual void LayoutComplete()
        {}

        /// <summary>
        /// See ICanvasElement.GraphicUpdateComplete.
        /// </summary>
        public virtual void GraphicUpdateComplete()
        {}

        protected override void OnEnable()
        {
            base.OnEnable();
            UpdateCachedReferences();
            Set(m_Value, false);
            // Update rects since they need to be initialized correctly.
            UpdateVisuals();
        }

        protected override void OnDisable()
        {
            m_Tracker.Clear();
            base.OnDisable();
        }

        /// <summary>
        /// Update the rect based on the delayed update visuals.
        /// Got around issue of calling sendMessage from onValidate.
        /// </summary>
        protected virtual void Update()
        {
            if (m_DelayedUpdateVisuals)
            {
                m_DelayedUpdateVisuals = false;
                UpdateVisuals();
            }
        }

        /// <summary>
        /// 更新Handle父节点，也就是SlidingArea
        /// </summary>
        void UpdateCachedReferences()
        {
            //获取滑块的父节点，也就是SlidingArea
            if (m_HandleRect && m_HandleRect.parent != null)
                m_ContainerRect = m_HandleRect.parent.GetComponent<RectTransform>();
            else
                m_ContainerRect = null;
        }

        void Set(float input, bool sendCallback = true)
        {
            float currentValue = m_Value;

            // bugfix (case 802330) clamp01 input in callee before calling this function, this allows inertia from dragging content to go past extremities without being clamped
            m_Value = input;

            // If the stepped value doesn't match the last one, it's time to update
            if (currentValue == value)
                return;

            UpdateVisuals();
            if (sendCallback)
            {
                UISystemProfilerApi.AddMarker("Scrollbar.value", this);
                m_OnValueChanged.Invoke(value);
            }
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();

            //This can be invoked before OnEnabled is called. So we shouldn't be accessing other objects, before OnEnable is called.
            if (!IsActive())
                return;

            UpdateVisuals();
        }

        enum Axis
        {
            Horizontal = 0,
            Vertical = 1
        }

        //将滑动方向转换成轴向，只有横竖两个轴向
        Axis axis { get { return (m_Direction == Direction.LeftToRight || m_Direction == Direction.RightToLeft) ? Axis.Horizontal : Axis.Vertical; } }
        bool reverseValue { get { return m_Direction == Direction.RightToLeft || m_Direction == Direction.TopToBottom; } }

        // Force-update the scroll bar. Useful if you've changed the properties and want it to update visually.
        private void UpdateVisuals()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                UpdateCachedReferences();
#endif
            m_Tracker.Clear();

            if (m_ContainerRect != null)
            {
                m_Tracker.Add(this, m_HandleRect, DrivenTransformProperties.Anchors);
                Vector2 anchorMin = Vector2.zero;
                Vector2 anchorMax = Vector2.one;

                float movement = Mathf.Clamp01(value) * (1 - size);
                if (reverseValue)
                {
                    anchorMin[(int)axis] = 1 - movement - size;
                    anchorMax[(int)axis] = 1 - movement;
                }
                else
                {
                    anchorMin[(int)axis] = movement;
                    anchorMax[(int)axis] = movement + size;
                }

                m_HandleRect.anchorMin = anchorMin;
                m_HandleRect.anchorMax = anchorMax;
            }
        }

        /// <summary>
        /// 处理拖拽事件
        /// 更新滑块位置、更新滚动值
        /// </summary>
        /// <param name="eventData"></param>
        // Update the scroll bar's position based on the mouse.
        void UpdateDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            if (m_ContainerRect == null)
                return;

            Vector2 position = Vector2.zero;
            //如果拖拽事件、鼠标按下事件，不再同一个显示器，那么不处理拖拽事件
            if (!MultipleDisplayUtilities.GetRelativeMousePositionForDrag(eventData, ref position))
                return;

            //计算点击相对于Container的本地位置，如果点击位置不在整个滚动区域内，那么不处理
            Vector2 localCursor;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(m_ContainerRect, position, eventData.pressEventCamera, out localCursor))
                return;

            //如果指针是在Handle中，那么这里相当于先把位置转换到handle的中心，然后再计算出相对于Container中心的偏移；这样计算的目的是为了当鼠标点击Handle的任意位置的时候，都相当于点击了Handle的正中心
            //如果指针是不在Handle，那么这里m_offset是0，直接就是计算出当前指针位置相对于Container的偏移
            //这个偏移相当于是滑块中心应该在Container中的相对位置
            Vector2 handleCenterRelativeToContainerCorner = localCursor - m_Offset - m_ContainerRect.rect.position;
            //(m_HandleRect.rect.size - m_HandleRect.sizeDelta) * 0.5f 是半个滑块的大小
            //把当前指针位置相对于Container的偏移减去半个滑块的大小，就相当于计算出当前位置下，滑块左下角相对于Container的偏移
            Vector2 handleCorner = handleCenterRelativeToContainerCorner - (m_HandleRect.rect.size - m_HandleRect.sizeDelta) * 0.5f;

            //根据滚动轴向，计算出Container滑动尺寸，看是取高度、还是宽度
            float parentSize = axis == 0 ? m_ContainerRect.rect.width : m_ContainerRect.rect.height;
            //计算出Container减去滑块自身大小后，还剩余可滑动的尺寸
            float remainingSize = parentSize * (1 - size);
            if (remainingSize <= 0)
                return;

            DoUpdateDrag(handleCorner, remainingSize);
        }

        /// <summary>
        /// 根据计算出来的当前Handle左下角的位置、以及Container减去Handle尺寸后可滑动区域大小，移动Handle滑块
        /// </summary>
        /// <param name="handleCorner"></param>
        /// <param name="remainingSize"></param>
        //this function is testable, it is found using reflection in ScrollbarClamp test
        private void DoUpdateDrag(Vector2 handleCorner, float remainingSize)
        {
            switch (m_Direction)
            {
                case Direction.LeftToRight:
                    Set(Mathf.Clamp01(handleCorner.x / remainingSize));
                    break;
                case Direction.RightToLeft:
                    Set(Mathf.Clamp01(1f - (handleCorner.x / remainingSize)));
                    break;
                case Direction.BottomToTop:
                    Set(Mathf.Clamp01(handleCorner.y / remainingSize));
                    break;
                case Direction.TopToBottom:
                    Set(Mathf.Clamp01(1f - (handleCorner.y / remainingSize)));
                    break;
            }
        }

        /// <summary>
        /// 判断是否是可以拖拽的状态
        /// 有效、可交互、是左键
        /// </summary>
        /// <param name="eventData"></param>
        /// <returns></returns>
        private bool MayDrag(PointerEventData eventData)
        {
            return IsActive() && IsInteractable() && eventData.button == PointerEventData.InputButton.Left;
        }

        /// <summary>
        /// Handling for when the scrollbar value is begin being dragged.
        /// 拖拽开始事件处理
        /// </summary>
        public virtual void OnBeginDrag(PointerEventData eventData)
        {
            isPointerDownAndNotDragging = false;

            if (!MayDrag(eventData))
                return;

            if (m_ContainerRect == null)
                return;

            //先重置偏移量
            m_Offset = Vector2.zero;
            
            //开始拖拽的时候判断下鼠标点是不是在Handle区域内
            //如果在Hanle范围内，那么就是普通拖拽；如果不再，那就是先把滑块瞬移到指定位置、再开启拖拽
            //这里是先计算一下指针到滑块中心点的相对偏移
            if (RectTransformUtility.RectangleContainsScreenPoint(m_HandleRect, eventData.pointerPressRaycast.screenPosition, eventData.enterEventCamera))
            {
                Vector2 localMousePos;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(m_HandleRect, eventData.pointerPressRaycast.screenPosition, eventData.pressEventCamera, out localMousePos))
                    m_Offset = localMousePos - m_HandleRect.rect.center;
            }
        }

        /// <summary>
        /// Handling for when the scrollbar value is dragged.
        /// 处理拖拽事件
        /// </summary>
        public virtual void OnDrag(PointerEventData eventData)
        {
            if (!MayDrag(eventData))
                return;

            if (m_ContainerRect != null)
                UpdateDrag(eventData);
        }

        /// <summary>
        /// Event triggered when pointer is pressed down on the scrollbar.
        /// </summary>
        public override void OnPointerDown(PointerEventData eventData)
        {
            if (!MayDrag(eventData))
                return;

            base.OnPointerDown(eventData);
            isPointerDownAndNotDragging = true;
            m_PointerDownRepeat = StartCoroutine(ClickRepeat(eventData.pointerPressRaycast.screenPosition, eventData.enterEventCamera));
        }

        protected IEnumerator ClickRepeat(PointerEventData eventData)
        {
            return ClickRepeat(eventData.pointerPressRaycast.screenPosition, eventData.enterEventCamera);
        }

        /// <summary>
        /// Coroutine function for handling continual press during Scrollbar.OnPointerDown.
        /// </summary>
        protected IEnumerator ClickRepeat(Vector2 screenPosition, Camera camera)
        {
            while (isPointerDownAndNotDragging)
            {
                if (!RectTransformUtility.RectangleContainsScreenPoint(m_HandleRect, screenPosition, camera))
                {
                    Vector2 localMousePos;
                    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(m_HandleRect, screenPosition, camera, out localMousePos))
                    {
                        var axisCoordinate = axis == 0 ? localMousePos.x : localMousePos.y;

                        // modifying value depending on direction, fixes (case 925824)

                        float change = axisCoordinate < 0 ? size : -size;
                        value += reverseValue ? change : -change;
                        value = Mathf.Clamp01(value);
                        // Only keep 4 decimals of precision
                        value = Mathf.Round(value * 10000f) / 10000f;
                    }
                }
                yield return new WaitForEndOfFrame();
            }
            StopCoroutine(m_PointerDownRepeat);
        }

        /// <summary>
        /// Event triggered when pointer is released after pressing on the scrollbar.
        /// </summary>
        public override void OnPointerUp(PointerEventData eventData)
        {
            base.OnPointerUp(eventData);
            isPointerDownAndNotDragging = false;
        }

        /// <summary>
        /// Handling for movement events.
        /// </summary>
        public override void OnMove(AxisEventData eventData)
        {
            if (!IsActive() || !IsInteractable())
            {
                base.OnMove(eventData);
                return;
            }

            switch (eventData.moveDir)
            {
                case MoveDirection.Left:
                    if (axis == Axis.Horizontal && FindSelectableOnLeft() == null)
                        Set(Mathf.Clamp01(reverseValue ? value + stepSize : value - stepSize));
                    else
                        base.OnMove(eventData);
                    break;
                case MoveDirection.Right:
                    if (axis == Axis.Horizontal && FindSelectableOnRight() == null)
                        Set(Mathf.Clamp01(reverseValue ? value - stepSize : value + stepSize));
                    else
                        base.OnMove(eventData);
                    break;
                case MoveDirection.Up:
                    if (axis == Axis.Vertical && FindSelectableOnUp() == null)
                        Set(Mathf.Clamp01(reverseValue ? value - stepSize : value + stepSize));
                    else
                        base.OnMove(eventData);
                    break;
                case MoveDirection.Down:
                    if (axis == Axis.Vertical && FindSelectableOnDown() == null)
                        Set(Mathf.Clamp01(reverseValue ? value + stepSize : value - stepSize));
                    else
                        base.OnMove(eventData);
                    break;
            }
        }

        /// <summary>
        /// Prevents selection if we we move on the Horizontal axis. See Selectable.FindSelectableOnLeft.
        /// </summary>
        public override Selectable FindSelectableOnLeft()
        {
            if (navigation.mode == Navigation.Mode.Automatic && axis == Axis.Horizontal)
                return null;
            return base.FindSelectableOnLeft();
        }

        /// <summary>
        /// Prevents selection if we we move on the Horizontal axis.  See Selectable.FindSelectableOnRight.
        /// </summary>
        public override Selectable FindSelectableOnRight()
        {
            if (navigation.mode == Navigation.Mode.Automatic && axis == Axis.Horizontal)
                return null;
            return base.FindSelectableOnRight();
        }

        /// <summary>
        /// Prevents selection if we we move on the Vertical axis. See Selectable.FindSelectableOnUp.
        /// </summary>
        public override Selectable FindSelectableOnUp()
        {
            if (navigation.mode == Navigation.Mode.Automatic && axis == Axis.Vertical)
                return null;
            return base.FindSelectableOnUp();
        }

        /// <summary>
        /// Prevents selection if we we move on the Vertical axis. See Selectable.FindSelectableOnDown.
        /// </summary>
        public override Selectable FindSelectableOnDown()
        {
            if (navigation.mode == Navigation.Mode.Automatic && axis == Axis.Vertical)
                return null;
            return base.FindSelectableOnDown();
        }

        /// <summary>
        /// See: IInitializePotentialDragHandler.OnInitializePotentialDrag
        /// </summary>
        public virtual void OnInitializePotentialDrag(PointerEventData eventData)
        {
            eventData.useDragThreshold = false;
        }

        /// <summary>
        /// Set the direction of the scrollbar, optionally setting the layout as well.
        /// </summary>
        /// <param name="direction">The direction of the scrollbar.</param>
        /// <param name="includeRectLayouts">Should the layout be flipped together with the direction?</param>
        public void SetDirection(Direction direction, bool includeRectLayouts)
        {
            Axis oldAxis = axis;
            bool oldReverse = reverseValue;
            this.direction = direction;

            if (!includeRectLayouts)
                return;

            if (axis != oldAxis)
                RectTransformUtility.FlipLayoutAxes(transform as RectTransform, true, true);

            if (reverseValue != oldReverse)
                RectTransformUtility.FlipLayoutOnAxis(transform as RectTransform, (int)axis, true, true);
        }
    }
}
