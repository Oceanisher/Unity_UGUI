namespace UnityEngine.EventSystems
{
    /// <summary>
    /// A class that can be used for sending simple events via the event system.
    /// UI事件抽象基类
    /// </summary>
    public abstract class AbstractEventData
    {
        //用于标记是否已经处理中，防止重复处理
        protected bool m_Used;

        /// <summary>
        /// Reset the event.
        /// 重置变量
        /// </summary>
        public virtual void Reset()
        {
            m_Used = false;
        }

        /// <summary>
        /// Use the event.
        /// </summary>
        /// <remarks>
        /// Internally sets a flag that can be checked via used to see if further processing should happen.
        /// </remarks>
        public virtual void Use()
        {
            m_Used = true;
        }

        /// <summary>
        /// Is the event used?
        /// </summary>
        public virtual bool used
        {
            get { return m_Used; }
        }
    }

    /// <summary>
    /// A class that contains the base event data that is common to all event types in the new EventSystem.
    /// UI事件基类
    /// 目前被轴向事件、指针事件继承
    /// </summary>
    public class BaseEventData : AbstractEventData
    {
        //事件系统
        private readonly EventSystem m_EventSystem;
        public BaseEventData(EventSystem eventSystem)
        {
            m_EventSystem = eventSystem;
        }

        /// <summary>
        /// >A reference to the BaseInputModule that sent this event.
        /// 输入组件
        /// </summary>
        public BaseInputModule currentInputModule
        {
            get { return m_EventSystem.currentInputModule; }
        }

        /// <summary>
        /// The object currently considered selected by the EventSystem.
        /// 当前选择的GO
        /// </summary>
        public GameObject selectedObject
        {
            get { return m_EventSystem.currentSelectedGameObject; }
            set { m_EventSystem.SetSelectedGameObject(value, this); }
        }
    }
}
