namespace UnityEngine.EventSystems
{
    /// <summary>
    /// Base class that all EventSystem events inherit from.
    /// 各类UI事件处理器的基类
    /// </summary>
    public interface IEventSystemHandler
    {
    }

    /// <summary>
    /// Interface to implement if you wish to receive OnPointerMove callbacks.
    /// 指针移动事件处理器
    /// </summary>
    /// <remarks>
    /// Criteria for this event is implementation dependent. For example see StandAloneInputModule.
    /// </remarks>
    public interface IPointerMoveHandler : IEventSystemHandler
    {
        /// <summary>
        /// Use this callback to detect pointer move events
        /// </summary>
        void OnPointerMove(PointerEventData eventData);
    }

    /// <summary>
    /// Interface to implement if you wish to receive OnPointerEnter callbacks.
    /// 指针进入事件处理器
    /// </summary>
    /// <remarks>
    /// Criteria for this event is implementation dependent. For example see StandAloneInputModule.
    /// </remarks>
    public interface IPointerEnterHandler : IEventSystemHandler
    {
        /// <summary>
        /// Use this callback to detect pointer enter events
        /// </summary>
        void OnPointerEnter(PointerEventData eventData);
    }

    /// <summary>
    /// Interface to implement if you wish to receive OnPointerExit callbacks.
    /// 指针离开事件处理器
    /// </summary>
    /// <remarks>
    /// Criteria for this event is implementation dependent. For example see StandAloneInputModule.
    /// </remarks>
    public interface IPointerExitHandler : IEventSystemHandler
    {
        /// <summary>
        /// Use this callback to detect pointer exit events
        /// </summary>
        void OnPointerExit(PointerEventData eventData);
    }

    /// <summary>
    /// Interface to implement if you wish to receive OnPointerDown callbacks.
    /// 指针按下事件处理器
    /// </summary>
    /// <remarks>
    /// Criteria for this event is implementation dependent. For example see StandAloneInputModule.
    /// </remarks>
    public interface IPointerDownHandler : IEventSystemHandler
    {
        /// <summary>
        /// Use this callback to detect pointer down events.
        /// </summary>
        void OnPointerDown(PointerEventData eventData);
    }

    /// <summary>
    /// Interface to implement if you wish to receive OnPointerUp callbacks.
    /// 指针松开事件处理器
    /// 要接受指针松开事件，必须接受指针按下事件
    /// Note: In order to receive OnPointerUp callbacks, you must also implement the EventSystems.IPointerDownHandler|IPointerDownHandler interface
    /// </summary>
    /// <remarks>
    /// Criteria for this event is implementation dependent. For example see StandAloneInputModule.
    /// </remarks>
    public interface IPointerUpHandler : IEventSystemHandler
    {
        /// <summary>
        /// Use this callback to detect pointer up events.
        /// </summary>
        void OnPointerUp(PointerEventData eventData);
    }

    /// <summary>
    /// Interface to implement if you wish to receive OnPointerClick callbacks.
    /// 指针点击事件处理器
    /// </summary>
    /// <remarks>
    /// Criteria for this event is implementation dependent. For example see StandAloneInputModule.
    /// </remarks>
    /// <remarks>
    /// Use the IPointerClickHandler Interface to handle click input using OnPointerClick callbacks. Ensure an Event System exists in the Scene to allow click detection. For click detection on non-UI GameObjects, ensure a EventSystems.PhysicsRaycaster is attached to the Camera.
    /// </remarks>
    /// <example>
    /// <code>
    /// <![CDATA[
    /// using UnityEngine;
    /// using UnityEngine.EventSystems;
    ///
    /// public class Example : MonoBehaviour, IPointerClickHandler
    /// {
    ///     //Detect if a click occurs
    ///     public void OnPointerClick(PointerEventData pointerEventData)
    ///     {
    ///         //Output to console the clicked GameObject's name and the following message. You can replace this with your own actions for when clicking the GameObject.
    ///         Debug.Log(name + " Game Object Clicked!");
    ///     }
    /// }
    /// ]]>
    ///</code>
    /// </example>
    public interface IPointerClickHandler : IEventSystemHandler
    {
        /// <summary>
        /// Use this callback to detect clicks.
        /// </summary>
        void OnPointerClick(PointerEventData eventData);
    }

    /// <summary>
    /// Interface to implement if you wish to receive OnBeginDrag callbacks.
    /// Note: You need to implement IDragHandler in addition to IBeginDragHandler.
    /// 拖拽开始事件处理器
    /// 还需要实现拖拽事件处理器
    /// </summary>
    /// <remarks>
    /// Criteria for this event is implementation dependent. For example see StandAloneInputModule.
    /// </remarks>
    public interface IBeginDragHandler : IEventSystemHandler
    {
        /// <summary>
        /// Called by a BaseInputModule before a drag is started.
        /// </summary>
        void OnBeginDrag(PointerEventData eventData);
    }

    /// <summary>
    /// Interface to implement if you wish to receive OnInitializePotentialDrag callbacks.
    /// 按下还没开始拖拽的时候的事件处理器
    /// </summary>
    /// <remarks>
    /// Criteria for this event is implementation dependent. For example see StandAloneInputModule.
    /// </remarks>
    public interface IInitializePotentialDragHandler : IEventSystemHandler
    {
        /// <summary>
        /// Called by a BaseInputModule when a drag has been found but before it is valid to begin the drag.
        /// </summary>
        void OnInitializePotentialDrag(PointerEventData eventData);
    }

    /// <summary>
    /// Interface to implement if you wish to receive OnDrag callbacks.
    /// 拖拽事件处理器
    /// </summary>
    /// <remarks>
    /// Criteria for this event is implementation dependent. For example see StandAloneInputModule.
    /// </remarks>
    /// <example>
    /// <code>
    /// <![CDATA[
    /// using UnityEngine;
    /// using UnityEngine.EventSystems;
    /// using UnityEngine.UI;
    ///
    /// [RequireComponent(typeof(Image))]
    /// public class DragMe : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    /// {
    ///     public bool dragOnSurfaces = true;
    ///
    ///     private GameObject m_DraggingIcon;
    ///     private RectTransform m_DraggingPlane;
    ///
    ///     public void OnBeginDrag(PointerEventData eventData)
    ///     {
    ///         var canvas = FindInParents<Canvas>(gameObject);
    ///         if (canvas == null)
    ///             return;
    ///
    ///         // We have clicked something that can be dragged.
    ///         // What we want to do is create an icon for this.
    ///         m_DraggingIcon = new GameObject("icon");
    ///
    ///         m_DraggingIcon.transform.SetParent(canvas.transform, false);
    ///         m_DraggingIcon.transform.SetAsLastSibling();
    ///
    ///         var image = m_DraggingIcon.AddComponent<Image>();
    ///
    ///         image.sprite = GetComponent<Image>().sprite;
    ///         image.SetNativeSize();
    ///
    ///         if (dragOnSurfaces)
    ///             m_DraggingPlane = transform as RectTransform;
    ///         else
    ///             m_DraggingPlane = canvas.transform as RectTransform;
    ///
    ///         SetDraggedPosition(eventData);
    ///     }
    ///
    ///     public void OnDrag(PointerEventData data)
    ///     {
    ///         if (m_DraggingIcon != null)
    ///             SetDraggedPosition(data);
    ///     }
    ///
    ///     private void SetDraggedPosition(PointerEventData data)
    ///     {
    ///         if (dragOnSurfaces && data.pointerEnter != null && data.pointerEnter.transform as RectTransform != null)
    ///             m_DraggingPlane = data.pointerEnter.transform as RectTransform;
    ///
    ///         var rt = m_DraggingIcon.GetComponent<RectTransform>();
    ///         Vector3 globalMousePos;
    ///         if (RectTransformUtility.ScreenPointToWorldPointInRectangle(m_DraggingPlane, data.position, data.pressEventCamera, out globalMousePos))
    ///         {
    ///             rt.position = globalMousePos;
    ///             rt.rotation = m_DraggingPlane.rotation;
    ///         }
    ///     }
    ///
    ///     public void OnEndDrag(PointerEventData eventData)
    ///     {
    ///         if (m_DraggingIcon != null)
    ///             Destroy(m_DraggingIcon);
    ///     }
    ///
    ///     static public T FindInParents<T>(GameObject go) where T : Component
    ///     {
    ///         if (go == null) return null;
    ///         var comp = go.GetComponent<T>();
    ///
    ///         if (comp != null)
    ///             return comp;
    ///
    ///         Transform t = go.transform.parent;
    ///         while (t != null && comp == null)
    ///         {
    ///             comp = t.gameObject.GetComponent<T>();
    ///             t = t.parent;
    ///         }
    ///         return comp;
    ///     }
    /// }
    /// ]]>
    ///</code>
    /// </example>
    public interface IDragHandler : IEventSystemHandler
    {
        /// <summary>
        /// When dragging is occurring this will be called every time the cursor is moved.
        /// </summary>
        void OnDrag(PointerEventData eventData);
    }

    /// <summary>
    /// Interface to implement if you wish to receive OnEndDrag callbacks.
    /// Note: You need to implement IDragHandler in addition to IEndDragHandler.
    /// 拖拽结束事件处理器
    /// </summary>
    /// <remarks>
    /// Criteria for this event is implementation dependent. For example see StandAloneInputModule.
    /// </remarks>
    public interface IEndDragHandler : IEventSystemHandler
    {
        /// <summary>
        /// Called by a BaseInputModule when a drag is ended.
        /// </summary>
        void OnEndDrag(PointerEventData eventData);
    }

    /// <summary>
    /// Interface to implement if you wish to receive OnDrop callbacks.
    /// 放下事件处理器
    /// </summary>
    /// <example>
    /// <code>
    /// <![CDATA[
    /// using UnityEngine;
    /// using UnityEngine.EventSystems;
    ///
    /// public class DropMe : MonoBehaviour, IDropHandler
    /// {
    ///     public void OnDrop(PointerEventData data)
    ///     {
    ///         if (data.pointerDrag != null)
    ///         {
    ///             Debug.Log ("Dropped object was: "  + data.pointerDrag);
    ///         }
    ///     }
    /// }
    /// ]]>
    ///</code>
    /// </example>
    /// <remarks>
    /// Criteria for this event is implementation dependent. For example see StandAloneInputModule.
    /// </remarks>
    public interface IDropHandler : IEventSystemHandler
    {
        /// <summary>
        /// Called by a BaseInputModule on a target that can accept a drop.
        /// </summary>
        void OnDrop(PointerEventData eventData);
    }

    /// <summary>
    /// Interface to implement if you wish to receive OnScroll callbacks.
    /// 滚轮滚动事件处理器
    /// </summary>
    /// <remarks>
    /// Criteria for this event is implementation dependent. For example see StandAloneInputModule.
    /// </remarks>
    public interface IScrollHandler : IEventSystemHandler
    {
        /// <summary>
        /// Use this callback to detect scroll events.
        /// </summary>
        void OnScroll(PointerEventData eventData);
    }

    /// <summary>
    /// Interface to implement if you wish to receive OnUpdateSelected callbacks.
    /// 更新所选对象事件处理器
    /// </summary>
    /// <remarks>
    /// Criteria for this event is implementation dependent. For example see StandAloneInputModule.
    /// </remarks>
    public interface IUpdateSelectedHandler : IEventSystemHandler
    {
        /// <summary>
        /// Called by the EventSystem when the object associated with this EventTrigger is updated.
        /// </summary>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// using UnityEngine;
        /// using UnityEngine.EventSystems;
        ///
        /// public class UpdateSelectedExample : MonoBehaviour, IUpdateSelectedHandler
        /// {
        ///     public void OnUpdateSelected(BaseEventData data)
        ///     {
        ///         Debug.Log("OnUpdateSelected called.");
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
        void OnUpdateSelected(BaseEventData eventData);
    }

    /// <summary>
    /// Interface to implement if you wish to receive OnSelect callbacks.
    /// 选择对象时的事件处理器
    /// </summary>
    /// <remarks>
    /// Criteria for this event is implementation dependent. For example see StandAloneInputModule.
    /// </remarks>
    public interface ISelectHandler : IEventSystemHandler
    {
        void OnSelect(BaseEventData eventData);
    }

    /// <summary>
    /// Interface to implement if you wish to receive OnDeselect callbacks.
    /// 取消选择对象时的事件处理器
    /// </summary>
    /// <remarks>
    /// Criteria for this event is implementation dependent. For example see StandAloneInputModule.
    /// </remarks>
    public interface IDeselectHandler : IEventSystemHandler
    {
        /// <summary>
        /// Called by the EventSystem when a new object is being selected.
        /// </summary>
        void OnDeselect(BaseEventData eventData);
    }

    /// <summary>
    /// Interface to implement if you wish to receive OnMove callbacks.
    /// 按下上下左右键时的事件处理器
    /// 这时候就没有对象了，事件里只是按键数据
    /// </summary>
    /// <remarks>
    /// Criteria for this event is implementation dependent. For example see StandAloneInputModule.
    /// </remarks>
    public interface IMoveHandler : IEventSystemHandler
    {
        /// <summary>
        /// Called by a BaseInputModule when a move event occurs.
        /// </summary>
        void OnMove(AxisEventData eventData);
    }

    /// <summary>
    /// Interface to implement if you wish to receive OnSubmit callbacks.
    /// 按下回车键时的事件处理器
    /// </summary>
    /// <remarks>
    /// Criteria for this event is implementation dependent. For example see StandAloneInputModule.
    /// </remarks>
    public interface ISubmitHandler : IEventSystemHandler
    {
        void OnSubmit(BaseEventData eventData);
    }

    /// <summary>
    /// Interface to implement if you wish to receive OnCancel callbacks.
    /// 按下取消键时的事件处理器
    /// </summary>
    /// <remarks>
    /// Criteria for this event is implementation dependent. For example see StandAloneInputModule.
    /// </remarks>
    public interface ICancelHandler : IEventSystemHandler
    {
        void OnCancel(BaseEventData eventData);
    }
}
