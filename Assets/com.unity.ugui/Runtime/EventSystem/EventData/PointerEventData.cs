using System;
using System.Text;
using System.Collections.Generic;

namespace UnityEngine.EventSystems
{
    /// <summary>
    /// Each touch event creates one of these containing all the relevant information.
    /// 指针事件
    /// 鼠标/触摸都会触发
    /// </summary>
    public class PointerEventData : BaseEventData
    {
        /// <summary>
        /// Input press tracking.
        /// 指针类型：左键、右键、中键
        /// </summary>
        public enum InputButton
        {
            /// <summary>
            /// Left button
            /// 左键
            /// </summary>
            Left = 0,

            /// <summary>
            /// Right button.
            /// 右键
            /// </summary>
            Right = 1,

            /// <summary>
            /// Middle button
            /// 中键
            /// </summary>
            Middle = 2
        }

        /// <summary>
        /// The state of a press for the given frame.
        /// 帧中的指针状态
        /// </summary>
        public enum FramePressState
        {
            /// <summary>
            /// Button was pressed this frame.
            /// 该帧中指针按下
            /// </summary>
            Pressed,

            /// <summary>
            /// Button was released this frame.
            /// 该帧中指针释放
            /// </summary>
            Released,

            /// <summary>
            /// Button was pressed and released this frame.
            /// 该帧中指针按下又释放
            /// </summary>
            PressedAndReleased,

            /// <summary>
            /// Same as last frame.
            /// 该帧中指针不变
            /// </summary>
            NotChanged
        }

        /// <summary>
        /// The object that received 'OnPointerEnter'.
        /// 接收到指针进入事件的GO
        /// </summary>
        public GameObject pointerEnter { get; set; }

        // The object that received OnPointerDown
        //接收按下事件的物体，有可能不是当前物体
        private GameObject m_PointerPress;

        /// <summary>
        /// The raw GameObject for the last press event. This means that it is the 'pressed' GameObject even if it can not receive the press event itself.
        /// 上次指针按下的GO，即使这个GO本身并不接收Press事件
        /// </summary>
        public GameObject lastPress { get; private set; }

        /// <summary>
        /// The object that the press happened on even if it can not handle the press event.
        /// 指针按下的原始GO，等同于射线命中后的最近的GO
        /// 即使这个GO本身并不接收Press事件
        /// </summary>
        public GameObject rawPointerPress { get; set; }

        /// <summary>
        /// The object that is receiving 'OnDrag'.
        /// 接收到拖拽的GO
        /// </summary>
        public GameObject pointerDrag { get; set; }

        /// <summary>
        /// The object that should receive the 'OnPointerClick' event.
        /// 接收到指针Click的GO
        /// </summary>
        public GameObject pointerClick { get; set; }

        /// <summary>
        /// RaycastResult associated with the current event.
        /// 射线当前命中的结果
        /// </summary>
        public RaycastResult pointerCurrentRaycast { get; set; }

        /// <summary>
        /// RaycastResult associated with the pointer press.
        /// 指针按下时的射线结果
        /// </summary>
        public RaycastResult pointerPressRaycast { get; set; }

        //指针路过悬停的GO列表
        public List<GameObject> hovered = new List<GameObject>();

        /// <summary>
        /// Is it possible to click this frame
        /// 该帧有可能是点击
        /// 因为一帧里可以是按下、释放、按下并释放，所以如果发现按下了，那么这里就是true，因为这一帧里有可能会释放
        /// </summary>
        public bool eligibleForClick { get; set; }

        /// <summary>
        /// The index of the display that this pointer event comes from.
        /// 指针所在的显示器序号
        /// </summary>
        public int displayIndex { get; set; }

        /// <summary>
        /// Id of the pointer (touch id).
        /// 指针ID
        /// 对于鼠标来说，对应的是 kMouseLeftId、kMouseRightId、kMouseMiddleId
        /// 对于触控来说，对应的是 touchId
        /// </summary>
        public int pointerId { get; set; }

        /// <summary>
        /// Current pointer position.
        /// Canvas空间中的相对坐标
        /// </summary>
        public Vector2 position { get; set; }

        /// <summary>
        /// Pointer delta since last update.
        /// 鼠标移动的Delta
        /// </summary>
        public Vector2 delta { get; set; }

        /// <summary>
        /// Position of the press.
        /// 当该帧状态是按下时，按下的位置
        /// 应该是只要一直是按下状态，那么每帧都会改变这个值
        /// </summary>
        public Vector2 pressPosition { get; set; }

        /// <summary>
        /// World-space position where a ray cast into the screen hits something
        /// </summary>

        [Obsolete("Use either pointerCurrentRaycast.worldPosition or pointerPressRaycast.worldPosition")]
        public Vector3 worldPosition { get; set; }

        /// <summary>
        /// World-space normal where a ray cast into the screen hits something
        /// </summary>
        [Obsolete("Use either pointerCurrentRaycast.worldNormal or pointerPressRaycast.worldNormal")]
        public Vector3 worldNormal { get; set; }

        /// <summary>
        /// The last time a click event was sent. Used for double click
        /// 上一次点击时间
        /// 用来判断是否进行了双击
        /// </summary>
        public float clickTime { get; set; }

        /// <summary>
        /// Number of clicks in a row.
        /// Click次数
        /// </summary>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// using UnityEngine;
        /// using System.Collections;
        /// using UnityEngine.UI;
        /// using UnityEngine.EventSystems;// Required when using Event data.
        ///
        /// public class ExampleClass : MonoBehaviour, IPointerDownHandler
        /// {
        ///     public void OnPointerDown(PointerEventData eventData)
        ///     {
        ///         //Grab the number of consecutive clicks and assign it to an integer varible.
        ///         int i = eventData.clickCount;
        ///         //Display the click count.
        ///         Debug.Log(i);
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
        public int clickCount { get; set; }

        /// <summary>
        /// The amount of scroll since the last update.
        /// </summary>
        public Vector2 scrollDelta { get; set; }

        /// <summary>
        /// Should a drag threshold be used?
        /// 是否需要使用拖拽阈值
        /// 也就是说移动距离超过某个距离，才算是拖拽
        /// </summary>
        /// <remarks>
        /// If you do not want a drag threshold set this to false in IInitializePotentialDragHandler.OnInitializePotentialDrag.
        /// </remarks>
        public bool useDragThreshold { get; set; }

        /// <summary>
        /// Is a drag operation currently occuring.
        /// 是否是拖拽中
        /// </summary>
        public bool dragging { get; set; }

        /// <summary>
        /// The EventSystems.PointerEventData.InputButton for this event.
        /// 输入的按键类型，只有3个，左中右
        /// 触控按键默认都设置为左按键
        /// </summary>
        public InputButton button { get; set; }


        /// <summary>
        /// The amount of pressure currently applied by a touch.
        /// 触控时，当前按下的压力
        /// </summary>
        /// <remarks>
        /// If the device does not report pressure, the value of this property is 1.0f.
        /// </remarks>
        /// <seealso cref="UnityEngine.UIElements.IPointerEvent" />
        public float pressure { get; set; }
        /// <summary>
        /// The pressure applied to an additional pressure-sensitive control on the stylus.
        /// </summary>
        /// <seealso cref="UnityEngine.UIElements.IPointerEvent" />
        public float tangentialPressure { get; set; }
        /// <summary>
        /// The angle of the stylus relative to the surface, in radians
        /// 触笔相对于屏幕的弧度，0代表平行，π/2代表垂直
        /// </summary>
        /// <remarks>
        /// A value of 0 indicates that the stylus is parallel to the surface. A value of pi/2 indicates that it is perpendicular to the surface.
        /// </remarks>
        /// <seealso cref="UnityEngine.UIElements.IPointerEvent" />
        public float altitudeAngle { get; set; }
        /// <summary>
        /// The angle of the stylus relative to the x-axis, in radians.
        /// 触笔相对于X轴的弧度，0代表触笔指向X轴方向
        /// </summary>
        /// <remarks>
        /// A value of 0 indicates that the stylus is pointed along the x-axis of the device.
        /// </remarks>
        /// <seealso cref="UnityEngine.UIElements.IPointerEvent" />
        public float azimuthAngle { get; set; }
        /// <summary>
        /// The rotation of the stylus around its axis, in radians.
        /// </summary>
        /// <seealso cref="UnityEngine.UIElements.IPointerEvent" />
        public float twist { get; set; }
        /// <summary>
        /// Specifies the angle of the pen relative to the X & Y axis, in radians.
        /// </summary>
        /// <seealso cref="UnityEngine.UIElements.IPointerEvent" />
        public Vector2 tilt { get; set; }
        /// <summary>
        /// Specifies the state of the pen. For example, whether the pen is in contact with the screen or tablet, whether the pen is inverted, and whether buttons are pressed.
        /// </summary>
        /// <seealso cref="UnityEngine.UIElements.IPointerEvent" />
        public PenStatus penStatus { get; set; }
        /// <summary>
        /// An estimate of the radius of a touch.
        /// 估计的触摸半径，也就是说能判断是用指尖按下的、还是用指腹按压的
        /// 也就是按下的那个地方的区域半径大小，因为不像鼠标点击是一个像素点，触摸点击是一个区域
        /// </summary>
        /// <remarks>
        /// Add `radiusVariance` to get the maximum touch radius, subtract it to get the minimum touch radius.
        /// </remarks>
        /// <seealso cref="UnityEngine.UIElements.IPointerEvent" />
        public Vector2 radius { get; set; }
        /// <summary>
        /// The accuracy of the touch radius.
        /// 估计的触摸半径的方差
        /// radius加上这个值就是最大触摸半径，减去这个值就是最小触摸半径
        /// </summary>
        /// <remarks>
        /// Add this value to the radius to get the maximum touch radius, subtract it to get the minimum touch radius.
        /// </remarks>
        public Vector2 radiusVariance { get; set; }
        /// <summary>
        /// Specifies in the case of a pointer exit if the pointer has fully exited the area or if it has just entered a child.
        /// 指针是完全退出原先悬停的区域，还是只是进入了一个新的子节点
        /// 缓存值，每次遍历中每一次循环都会修改
        /// </summary>
        public bool fullyExited { get; set; }
        /// <summary>
        /// Specifies in the case of a pointer enter if the pointer has entered a new area or if it has just reentered a parent after leaving a child.
        /// 是否是从一个子物体，进入到了父节点物体中
        /// 缓存值，每次遍历中每一次循环都会修改
        /// </summary>
        public bool reentered { get; set; }
        /// <seealso cref="UnityEngine.UIElements.IPointerEvent" />

        public PointerEventData(EventSystem eventSystem) : base(eventSystem)
        {
            eligibleForClick = false;

            displayIndex = 0;
            pointerId = -1;
            position = Vector2.zero; // Current position of the mouse or touch event
            delta = Vector2.zero; // Delta since last update
            pressPosition = Vector2.zero; // Delta since the event started being tracked
            clickTime = 0.0f; // The last time a click event was sent out (used for double-clicks)
            clickCount = 0; // Number of clicks in a row. 2 for a double-click for example.

            scrollDelta = Vector2.zero;
            useDragThreshold = true;
            dragging = false;
            button = InputButton.Left;

            pressure = 0f;
            tangentialPressure = 0f;
            altitudeAngle = 0f;
            azimuthAngle = 0f;
            twist = 0f;
            tilt = new Vector2(0f, 0f);
            penStatus = PenStatus.None;
            radius = Vector2.zero;
            radiusVariance = Vector2.zero;
        }

        /// <summary>
        /// Is the pointer moving.
        /// 鼠标是否移动过
        /// </summary>
        public bool IsPointerMoving()
        {
            return delta.sqrMagnitude > 0.0f;
        }

        /// <summary>
        /// Is scroll being used on the input device.
        /// 鼠标是否滚动过
        /// </summary>
        public bool IsScrolling()
        {
            return scrollDelta.sqrMagnitude > 0.0f;
        }

        /// <summary>
        /// The camera associated with the last OnPointerEnter event.
        /// </summary>
        public Camera enterEventCamera
        {
            get { return pointerCurrentRaycast.module == null ? null : pointerCurrentRaycast.module.eventCamera; }
        }

        /// <summary>
        /// The camera associated with the last OnPointerPress event.
        /// </summary>
        public Camera pressEventCamera
        {
            get { return pointerPressRaycast.module == null ? null : pointerPressRaycast.module.eventCamera; }
        }

        /// <summary>
        /// The GameObject that received the OnPointerDown.
        /// 接收按下事件的物体，有可能不是当前物体
        /// </summary>
        public GameObject pointerPress
        {
            get { return m_PointerPress; }
            set
            {
                if (m_PointerPress == value)
                    return;

                lastPress = m_PointerPress;
                m_PointerPress = value;
            }
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine("<b>Position</b>: " + position);
            sb.AppendLine("<b>delta</b>: " + delta);
            sb.AppendLine("<b>eligibleForClick</b>: " + eligibleForClick);
            sb.AppendLine("<b>pointerEnter</b>: " + pointerEnter);
            sb.AppendLine("<b>pointerPress</b>: " + pointerPress);
            sb.AppendLine("<b>lastPointerPress</b>: " + lastPress);
            sb.AppendLine("<b>pointerDrag</b>: " + pointerDrag);
            sb.AppendLine("<b>Use Drag Threshold</b>: " + useDragThreshold);
            sb.AppendLine("<b>Current Raycast:</b>");
            sb.AppendLine(pointerCurrentRaycast.ToString());
            sb.AppendLine("<b>Press Raycast:</b>");
            sb.AppendLine(pointerPressRaycast.ToString());
            sb.AppendLine("<b>Display Index:</b>");
            sb.AppendLine(displayIndex.ToString());
            sb.AppendLine("<b>pressure</b>: " + pressure);
            sb.AppendLine("<b>tangentialPressure</b>: " + tangentialPressure);
            sb.AppendLine("<b>altitudeAngle</b>: " + altitudeAngle);
            sb.AppendLine("<b>azimuthAngle</b>: " + azimuthAngle);
            sb.AppendLine("<b>twist</b>: " + twist);
            sb.AppendLine("<b>tilt</b>: " + tilt);
            sb.AppendLine("<b>penStatus</b>: " + penStatus);
            sb.AppendLine("<b>radius</b>: " + radius);
            sb.AppendLine("<b>radiusVariance</b>: " + radiusVariance);
            return sb.ToString();
        }
    }
}
