namespace UnityEngine.EventSystems
{
    /// <summary>
    /// Event Data associated with Axis Events (Controller / Keyboard).
    /// 轴事件
    /// 比如手柄的摇杆、键盘的上下左右
    /// </summary>
    public class AxisEventData : BaseEventData
    {
        /// <summary>
        /// Raw input vector associated with this event.
        /// 原始轴输入信息
        /// </summary>
        public Vector2 moveVector { get; set; }

        /// <summary>
        /// MoveDirection for this event.
        /// 移动方向信息
        /// </summary>
        public MoveDirection moveDir { get; set; }

        public AxisEventData(EventSystem eventSystem)
            : base(eventSystem)
        {
            moveVector = Vector2.zero;
            moveDir = MoveDirection.None;
        }
    }
}
