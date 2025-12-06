using System;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace UnityEngine.UI
{
    [AddComponentMenu("UI/Slider", 34)]
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    /// <summary>
    /// A standard slider that can be moved between a minimum and maximum value.
    /// 进度条、滑动条
    /// 原理与ScrollBar类似，细节上有差别
    /// </summary>
    /// <remarks>
    /// The slider component is a Selectable that controls a fill, a handle, or both. The fill, when used, spans from the minimum value to the current value while the handle, when used, follow the current value.
    /// The anchors of the fill and handle RectTransforms are driven by the Slider. The fill and handle can be direct children of the GameObject with the Slider, or intermediary RectTransforms can be placed in between for additional control.
    /// When a change to the slider value occurs, a callback is sent to any registered listeners of UI.Slider.onValueChanged.
    /// </remarks>
    public class Slider : Selectable, IDragHandler, IInitializePotentialDragHandler, ICanvasElement
    {
        /// <summary>
        /// Setting that indicates one of four directions.
        /// 滑动条滑动方向
        /// </summary>
        public enum Direction
        {
            /// <summary>
            /// From the left to the right
            /// </summary>
            LeftToRight,

            /// <summary>
            /// From the right to the left
            /// </summary>
            RightToLeft,

            /// <summary>
            /// From the bottom to the top.
            /// </summary>
            BottomToTop,

            /// <summary>
            /// From the top to the bottom.
            /// </summary>
            TopToBottom,
        }

        [Serializable]
        /// <summary>
        /// Event type used by the UI.Slider.
        /// 滑动条值变更事件
        /// </summary>
        public class SliderEvent : UnityEvent<float> {}

        //进度条节点
        [SerializeField]
        private RectTransform m_FillRect;

        /// <summary>
        /// Optional RectTransform to use as fill for the slider.
        /// 进度条节点
        /// </summary>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// using UnityEngine;
        /// using System.Collections;
        /// using UnityEngine.UI;  // Required when Using UI elements.
        ///
        /// public class Example : MonoBehaviour
        /// {
        ///     public Slider mainSlider;
        ///     //Reference to new "RectTransform"(Child of FillArea).
        ///     public RectTransform newFillRect;
        ///
        ///     //Deactivates the old FillRect and assigns a new one.
        ///     void Start()
        ///     {
        ///         mainSlider.fillRect.gameObject.SetActive(false);
        ///         mainSlider.fillRect = newFillRect;
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
        public RectTransform fillRect { get { return m_FillRect; } set { if (SetPropertyUtility.SetClass(ref m_FillRect, value)) {UpdateCachedReferences(); UpdateVisuals(); } } }

        //滑块节点
        [SerializeField]
        private RectTransform m_HandleRect;

        /// <summary>
        /// Optional RectTransform to use as a handle for the slider.
        /// 滑块节点
        /// </summary>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// using UnityEngine;
        /// using System.Collections;
        /// using UnityEngine.UI; // Required when Using UI elements.
        ///
        /// public class Example : MonoBehaviour
        /// {
        ///     public Slider mainSlider;
        ///     //Reference to new "RectTransform" (Child of "Handle Slide Area").
        ///     public RectTransform handleHighlighted;
        ///
        ///     //Deactivates the old Handle, then assigns and enables the new one.
        ///     void Start()
        ///     {
        ///         mainSlider.handleRect.gameObject.SetActive(false);
        ///         mainSlider.handleRect = handleHighlighted;
        ///         mainSlider.handleRect.gameObject.SetActive(true);
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
        public RectTransform handleRect { get { return m_HandleRect; } set { if (SetPropertyUtility.SetClass(ref m_HandleRect, value)) { UpdateCachedReferences(); UpdateVisuals(); } } }

        [Space]

        //滑动方向
        [SerializeField]
        private Direction m_Direction = Direction.LeftToRight;

        /// <summary>
        /// The direction of the slider, from minimum to maximum value.
        /// 滑动方向
        /// </summary>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// using UnityEngine;
        /// using System.Collections;
        /// using UnityEngine.UI; // Required when Using UI elements.
        ///
        /// public class Example : MonoBehaviour
        /// {
        ///     public Slider mainSlider;
        ///
        ///     public void Start()
        ///     {
        ///         //Changes the direction of the slider.
        ///         if (mainSlider.direction == Slider.Direction.BottomToTop)
        ///         {
        ///             mainSlider.direction = Slider.Direction.TopToBottom;
        ///         }
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
        public Direction direction { get { return m_Direction; } set { if (SetPropertyUtility.SetStruct(ref m_Direction, value)) UpdateVisuals(); } }

        [SerializeField]
        private float m_MinValue = 0;

        /// <summary>
        /// The minimum allowed value of the slider.
        /// 进度条最小值
        /// </summary>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// using UnityEngine;
        /// using System.Collections;
        /// using UnityEngine.UI; // Required when Using UI elements.
        ///
        /// public class Example : MonoBehaviour
        /// {
        ///     public Slider mainSlider;
        ///
        ///     void Start()
        ///     {
        ///         // Changes the minimum value of the slider to 10;
        ///         mainSlider.minValue = 10;
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
        public float minValue { get { return m_MinValue; } set { if (SetPropertyUtility.SetStruct(ref m_MinValue, value)) { Set(m_Value); UpdateVisuals(); } } }

        //进度条最大值
        [SerializeField]
        private float m_MaxValue = 1;

        /// <summary>
        /// The maximum allowed value of the slider.
        /// 进度条最大值
        /// </summary>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// using UnityEngine;
        /// using System.Collections;
        /// using UnityEngine.UI; // Required when Using UI elements.
        ///
        /// public class Example : MonoBehaviour
        /// {
        ///     public Slider mainSlider;
        ///
        ///     void Start()
        ///     {
        ///         // Changes the max value of the slider to 20;
        ///         mainSlider.maxValue = 20;
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
        public float maxValue { get { return m_MaxValue; } set { if (SetPropertyUtility.SetStruct(ref m_MaxValue, value)) { Set(m_Value); UpdateVisuals(); } } }

        //进度条是否是整数型的，只能一格一格的移动
        [SerializeField]
        private bool m_WholeNumbers = false;

        /// <summary>
        /// Should the value only be allowed to be whole numbers?
        /// 进度条是否是整数型的，只能一格一格的移动
        /// </summary>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// using UnityEngine;
        /// using System.Collections;
        /// using UnityEngine.UI; // Required when Using UI elements.
        ///
        /// public class Example : MonoBehaviour
        /// {
        ///     public Slider mainSlider;
        ///
        ///     public void Start()
        ///     {
        ///         //sets the slider's value to accept whole numbers only.
        ///         mainSlider.wholeNumbers = true;
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
        public bool wholeNumbers { get { return m_WholeNumbers; } set { if (SetPropertyUtility.SetStruct(ref m_WholeNumbers, value)) { Set(m_Value); UpdateVisuals(); } } }

        //进度值
        [SerializeField]
        protected float m_Value;

        /// <summary>
        /// The current value of the slider.
        /// 进度值
        /// </summary>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// using UnityEngine;
        /// using System.Collections;
        /// using UnityEngine.UI; // Required when Using UI elements.
        ///
        /// public class Example : MonoBehaviour
        /// {
        ///     public Slider mainSlider;
        ///
        ///     //Invoked when a submit button is clicked.
        ///     public void SubmitSliderSetting()
        ///     {
        ///         //Displays the value of the slider in the console.
        ///         Debug.Log(mainSlider.value);
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
        public virtual float value
        {
            get
            {
                return wholeNumbers ? Mathf.Round(m_Value) : m_Value;
            }
            set
            {
                Set(value);
            }
        }

        /// <summary>
        /// Set the value of the slider without invoking onValueChanged callback.
        /// 设置进度值，并且不触发值变更回调
        /// </summary>
        /// <param name="input">The new value for the slider.</param>
        public virtual void SetValueWithoutNotify(float input)
        {
            Set(input, false);
        }

        /// <summary>
        /// The current value of the slider normalized into a value between 0 and 1.
        /// 当前进度值标准化，0~1
        /// </summary>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// using UnityEngine;
        /// using System.Collections;
        /// using UnityEngine.UI; // Required when Using UI elements.
        ///
        /// public class Example : MonoBehaviour
        /// {
        ///     public Slider mainSlider;
        ///
        ///     //Set to invoke when "OnValueChanged" method is called.
        ///     void CheckNormalisedValue()
        ///     {
        ///         //Displays the normalised value of the slider everytime the value changes.
        ///         Debug.Log(mainSlider.normalizedValue);
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
        public float normalizedValue
        {
            get
            {
                if (Mathf.Approximately(minValue, maxValue))
                    return 0;
                //逆向插值，返回百分比
                return Mathf.InverseLerp(minValue, maxValue, value);
            }
            set
            {
                this.value = Mathf.Lerp(minValue, maxValue, value);
            }
        }

        [Space]

        //值变更回调
        [SerializeField]
        private SliderEvent m_OnValueChanged = new SliderEvent();

        /// <summary>
        /// Callback executed when the value of the slider is changed.
        /// 值变更回调
        /// </summary>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// using UnityEngine;
        /// using System.Collections;
        /// using UnityEngine.UI; // Required when Using UI elements.
        ///
        /// public class Example : MonoBehaviour
        /// {
        ///     public Slider mainSlider;
        ///
        ///     public void Start()
        ///     {
        ///         //Adds a listener to the main slider and invokes a method when the value changes.
        ///         mainSlider.onValueChanged.AddListener(delegate {ValueChangeCheck(); });
        ///     }
        ///
        ///     // Invoked when the value of the slider changes.
        ///     public void ValueChangeCheck()
        ///     {
        ///         Debug.Log(mainSlider.value);
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
        public SliderEvent onValueChanged { get { return m_OnValueChanged; } set { m_OnValueChanged = value; } }

        // Private fields

        //进度条使用的图片
        private Image m_FillImage;
        //进度条Trs
        private Transform m_FillTransform;
        //进度条父节点Container
        private RectTransform m_FillContainerRect;
        //滑块Trs
        private Transform m_HandleTransform;
        //滑块父节点Container
        private RectTransform m_HandleContainerRect;

        // The offset from handle position to mouse down position
        //鼠标点击的偏移值，相对于滑块中心的偏移向量
        private Vector2 m_Offset = Vector2.zero;

        // field is never assigned warning
        #pragma warning disable 649
        private DrivenRectTransformTracker m_Tracker;
        #pragma warning restore 649

        //延迟更新视觉，主要用在编辑器模式下
        // This "delayed" mechanism is required for case 1037681.
        private bool m_DelayedUpdateVisuals = false;

        //移动的步长，如果设置为整数型移动，那么每次移动1；否则默认每次移动0.1个百分比距离
        // Size of each step.
        float stepSize { get { return wholeNumbers ? 1 : (maxValue - minValue) * 0.1f; } }

        protected Slider()
        {}

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();

            if (wholeNumbers)
            {
                m_MinValue = Mathf.Round(m_MinValue);
                m_MaxValue = Mathf.Round(m_MaxValue);
            }

            //Onvalidate is called before OnEnabled. We need to make sure not to touch any other objects before OnEnable is run.
            if (IsActive())
            {
                UpdateCachedReferences();
                // Update rects in next update since other things might affect them even if value didn't change.
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
        /// See ICanvasElement.LayoutComplete
        /// </summary>
        public virtual void LayoutComplete()
        {}

        /// <summary>
        /// See ICanvasElement.GraphicUpdateComplete
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
                Set(m_Value, false);
                UpdateVisuals();
            }
        }

        protected override void OnDidApplyAnimationProperties()
        {
            // Has value changed? Various elements of the slider have the old normalisedValue assigned, we can use this to perform a comparison.
            // We also need to ensure the value stays within min/max.
            m_Value = ClampValue(m_Value);
            float oldNormalizedValue = normalizedValue;
            if (m_FillContainerRect != null)
            {
                if (m_FillImage != null && m_FillImage.type == Image.Type.Filled)
                    oldNormalizedValue = m_FillImage.fillAmount;
                else
                    oldNormalizedValue = (reverseValue ? 1 - m_FillRect.anchorMin[(int)axis] : m_FillRect.anchorMax[(int)axis]);
            }
            else if (m_HandleContainerRect != null)
                oldNormalizedValue = (reverseValue ? 1 - m_HandleRect.anchorMin[(int)axis] : m_HandleRect.anchorMin[(int)axis]);

            UpdateVisuals();

            if (oldNormalizedValue != normalizedValue)
            {
                UISystemProfilerApi.AddMarker("Slider.value", this);
                onValueChanged.Invoke(m_Value);
            }
            // UUM-34170 Apparently, some properties on slider such as IsInteractable and Normalcolor Animation is broken.
            // We need to call base here to render the animation on Scene
            base.OnDidApplyAnimationProperties();
        }

        /// <summary>
        /// 更新所有缓存的Trs或者其他组件信息
        /// </summary>
        void UpdateCachedReferences()
        {
            if (m_FillRect && m_FillRect != (RectTransform)transform)
            {
                m_FillTransform = m_FillRect.transform;
                m_FillImage = m_FillRect.GetComponent<Image>();
                if (m_FillTransform.parent != null)
                    m_FillContainerRect = m_FillTransform.parent.GetComponent<RectTransform>();
            }
            else
            {
                m_FillRect = null;
                m_FillContainerRect = null;
                m_FillImage = null;
            }

            if (m_HandleRect && m_HandleRect != (RectTransform)transform)
            {
                m_HandleTransform = m_HandleRect.transform;
                if (m_HandleTransform.parent != null)
                    m_HandleContainerRect = m_HandleTransform.parent.GetComponent<RectTransform>();
            }
            else
            {
                m_HandleRect = null;
                m_HandleContainerRect = null;
            }
        }

        /// <summary>
        /// input输入值Clamp到minValue、maxValue之间
        /// 如果是整数模式，还要四舍五入一下
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        float ClampValue(float input)
        {
            float newValue = Mathf.Clamp(input, minValue, maxValue);
            if (wholeNumbers)
                newValue = Mathf.Round(newValue);
            return newValue;
        }

        /// <summary>
        /// Set the value of the slider.
        /// 设置Slider的值
        /// 如果值变更，会更新视觉
        /// 所以有些设置最大值、是否是整数型等，在调用了这个Set之后，还需要手动调用下更新视觉方法，因为此时进度值没变
        /// </summary>
        /// <param name="input">The new value for the slider.</param>
        /// <param name="sendCallback">If the OnValueChanged callback should be invoked.</param>
        /// <remarks>
        /// Process the input to ensure the value is between min and max value. If the input is different set the value and send the callback is required.
        /// </remarks>
        protected virtual void Set(float input, bool sendCallback = true)
        {
            //输入值需要clamp一下，限制值范围
            // Clamp the input
            float newValue = ClampValue(input);

            // If the stepped value doesn't match the last one, it's time to update
            if (m_Value == newValue)
                return;

            m_Value = newValue;
            UpdateVisuals();
            if (sendCallback)
            {
                UISystemProfilerApi.AddMarker("Slider.value", this);
                m_OnValueChanged.Invoke(newValue);
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

        //滑动方向，与ScrollBar一致
        enum Axis
        {
            Horizontal = 0,
            Vertical = 1
        }

        //滑动方向，与ScrollBar一致
        Axis axis { get { return (m_Direction == Direction.LeftToRight || m_Direction == Direction.RightToLeft) ? Axis.Horizontal : Axis.Vertical; } }
        //是否是翻转方向，与ScrollBar一致
        bool reverseValue { get { return m_Direction == Direction.RightToLeft || m_Direction == Direction.TopToBottom; } }

        /// <summary>
        /// 更新视觉信息
        /// </summary>
        // Force-update the slider. Useful if you've changed the properties and want it to update visually.
        private void UpdateVisuals()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                UpdateCachedReferences();
#endif

            m_Tracker.Clear();

            //修改进度条
            if (m_FillContainerRect != null)
            {
                m_Tracker.Add(this, m_FillRect, DrivenTransformProperties.Anchors);
                Vector2 anchorMin = Vector2.zero;
                Vector2 anchorMax = Vector2.one;

                //修改进度条中图片的进度，图片必须是Filled模式
                if (m_FillImage != null && m_FillImage.type == Image.Type.Filled)
                {
                    m_FillImage.fillAmount = normalizedValue;
                }
                //如果没有图片、或者图片模式不是Filled模式，那么就通过修改anchor的方式，模拟图片的进度展示
                else
                {
                    if (reverseValue)
                        anchorMin[(int)axis] = 1 - normalizedValue;
                    else
                        anchorMax[(int)axis] = normalizedValue;
                }

                //这里的anchor在有进度图片、并且模式是Filled模式时，min=(0,0)，max=(1,1)
                m_FillRect.anchorMin = anchorMin;
                m_FillRect.anchorMax = anchorMax;
            }

            //修改滑块，滑块在竖直方向是stretch对齐，所以它的移动轴上的anchor值是一样的
            if (m_HandleContainerRect != null)
            {
                m_Tracker.Add(this, m_HandleRect, DrivenTransformProperties.Anchors);
                Vector2 anchorMin = Vector2.zero;
                Vector2 anchorMax = Vector2.one;
                anchorMin[(int)axis] = anchorMax[(int)axis] = (reverseValue ? (1 - normalizedValue) : normalizedValue);
                m_HandleRect.anchorMin = anchorMin;
                m_HandleRect.anchorMax = anchorMax;
            }
        }

        /// <summary>
        /// 根据鼠标事件，更新滑块位置
        /// 看上去只更新了值，但是实际上最终调用Set方法设置值，而Set方法中有更新视觉的方法
        /// </summary>
        /// <param name="eventData"></param>
        /// <param name="cam"></param>
        // Update the slider's position based on the mouse.
        void UpdateDrag(PointerEventData eventData, Camera cam)
        {
            RectTransform clickRect = m_HandleContainerRect ?? m_FillContainerRect;
            if (clickRect != null && clickRect.rect.size[(int)axis] > 0)
            {
                Vector2 position = Vector2.zero;
                if (!MultipleDisplayUtilities.GetRelativeMousePositionForDrag(eventData, ref position))
                    return;
                
                Vector2 localCursor;
                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(clickRect, position, cam, out localCursor))
                    return;
                //rect.position，是矩形左下角相对于矩形中心的位置，本地坐标
                //将localCursor，减去rect.position，就能将localCursor转换为相对于clickRect左下角的本地坐标
                localCursor -= clickRect.rect.position;

                //用这个本地坐标减去鼠标按到滑块时的偏移，就能得出鼠标相对于clickRect左下角的真实偏移向量
                //然后根据当前的移动轴向，取这个真实移动的值，对比clickRect在这个轴向上的大小，就得到了进度
                float val = Mathf.Clamp01((localCursor - m_Offset)[(int)axis] / clickRect.rect.size[(int)axis]);
                //根据是否翻转正向，就能得到最终的进度值
                normalizedValue = (reverseValue ? 1f - val : val);
            }
        }

        /// <summary>
        /// 是否可以触发拖拽事件
        /// 有效、可交互、左键
        /// </summary>
        /// <param name="eventData"></param>
        /// <returns></returns>
        private bool MayDrag(PointerEventData eventData)
        {
            return IsActive() && IsInteractable() && eventData.button == PointerEventData.InputButton.Left;
        }

        /// <summary>
        /// 鼠标按下处理
        /// </summary>
        /// <param name="eventData"></param>
        public override void OnPointerDown(PointerEventData eventData)
        {
            if (!MayDrag(eventData))
                return;

            base.OnPointerDown(eventData);

            m_Offset = Vector2.zero;
            //如果鼠标按到了滑块上，那么记录一下鼠标当前位置（转换为相对于滑块中心的本地偏移量）
            if (m_HandleContainerRect != null && RectTransformUtility.RectangleContainsScreenPoint(m_HandleRect, eventData.pointerPressRaycast.screenPosition, eventData.enterEventCamera))
            {
                Vector2 localMousePos;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(m_HandleRect, eventData.pointerPressRaycast.screenPosition, eventData.pressEventCamera, out localMousePos))
                    m_Offset = localMousePos;
            }
            //如果没有按到滑块上、而是按到了滑块之外的区域，滑块需要跳转到当前指针位置
            //与ScrollBar不同的是，ScrollBar是使用每帧移动一个滑块的距离的方式，移动到鼠标位置；而进度条是瞬间跳转
            else
            {
                // Outside the slider handle - jump to this point instead
                UpdateDrag(eventData, eventData.pressEventCamera);
            }
        }

        /// <summary>
        /// 拖拽事件
        /// 直接根据鼠标位置更新进度
        /// </summary>
        /// <param name="eventData"></param>
        public virtual void OnDrag(PointerEventData eventData)
        {
            if (!MayDrag(eventData))
                return;
            UpdateDrag(eventData, eventData.pressEventCamera);
        }

        /// <summary>
        /// 处理导航事件，如左右按键、摇杆等
        /// 与ScrollBar类似
        /// </summary>
        /// <param name="eventData"></param>
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
                        Set(reverseValue ? value + stepSize : value - stepSize);
                    else
                        base.OnMove(eventData);
                    break;
                case MoveDirection.Right:
                    if (axis == Axis.Horizontal && FindSelectableOnRight() == null)
                        Set(reverseValue ? value - stepSize : value + stepSize);
                    else
                        base.OnMove(eventData);
                    break;
                case MoveDirection.Up:
                    if (axis == Axis.Vertical && FindSelectableOnUp() == null)
                        Set(reverseValue ? value - stepSize : value + stepSize);
                    else
                        base.OnMove(eventData);
                    break;
                case MoveDirection.Down:
                    if (axis == Axis.Vertical && FindSelectableOnDown() == null)
                        Set(reverseValue ? value + stepSize : value - stepSize);
                    else
                        base.OnMove(eventData);
                    break;
            }
        }

        /// <summary>
        /// See Selectable.FindSelectableOnLeft
        /// 与ScrollBar类似
        /// </summary>
        public override Selectable FindSelectableOnLeft()
        {
            if (navigation.mode == Navigation.Mode.Automatic && axis == Axis.Horizontal)
                return null;
            return base.FindSelectableOnLeft();
        }

        /// <summary>
        /// See Selectable.FindSelectableOnRight
        /// 与ScrollBar类似
        /// </summary>
        public override Selectable FindSelectableOnRight()
        {
            if (navigation.mode == Navigation.Mode.Automatic && axis == Axis.Horizontal)
                return null;
            return base.FindSelectableOnRight();
        }

        /// <summary>
        /// See Selectable.FindSelectableOnUp
        /// 与ScrollBar类似
        /// </summary>
        public override Selectable FindSelectableOnUp()
        {
            if (navigation.mode == Navigation.Mode.Automatic && axis == Axis.Vertical)
                return null;
            return base.FindSelectableOnUp();
        }

        /// <summary>
        /// See Selectable.FindSelectableOnDown
        /// 与ScrollBar类似
        /// </summary>
        public override Selectable FindSelectableOnDown()
        {
            if (navigation.mode == Navigation.Mode.Automatic && axis == Axis.Vertical)
                return null;
            return base.FindSelectableOnDown();
        }

        /// <summary>
        /// 在按下、并还没开始拖拽的时候，取消拖拽阈值，也就是说只要移动就是拖拽，而不是超过某个距离才算拖拽
        /// </summary>
        /// <param name="eventData"></param>
        public virtual void OnInitializePotentialDrag(PointerEventData eventData)
        {
            eventData.useDragThreshold = false;
        }

        /// <summary>
        /// Sets the direction of this slider, optionally changing the layout as well.
        /// 与ScrollBar类似
        /// </summary>
        /// <param name="direction">The direction of the slider</param>
        /// <param name="includeRectLayouts">Should the layout be flipped together with the slider direction</param>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// using UnityEngine;
        /// using System.Collections;
        /// using UnityEngine.UI; // Required when Using UI elements.
        ///
        /// public class Example : MonoBehaviour
        /// {
        ///     public Slider mainSlider;
        ///
        ///     public void Start()
        ///     {
        ///         mainSlider.SetDirection(Slider.Direction.LeftToRight, false);
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
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
