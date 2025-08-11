using UnityEngine;
using System.Collections;

namespace UnityEngine.UI
{
    /// <summary>
    ///   A component is treated as a layout element by the auto layout system if it implements ILayoutElement.
    /// 布局元素
    /// 实现此接口的图形元素能够被自动布局管理系统进行管理，并且能够通过该接口实现一些自定义的布局策略。
    /// 布局管理系统通过调用 CalculateLayoutInputHorizontal 、CalculateLayoutInputVertical接口来进行布局计算。
    /// 部分UI元素实现的该接口，比如Image。但是Button没有实现。
    /// </summary>
    /// <remarks>
    /// The layout system will invoke CalculateLayoutInputHorizontal before querying minWidth, preferredWidth, and flexibleWidth. It can potentially save performance if these properties are cached when CalculateLayoutInputHorizontal is invoked, so they don't need to be recalculated every time the properties are queried.
    ///
    /// The layout system will invoke CalculateLayoutInputVertical before querying minHeight, preferredHeight, and flexibleHeight.It can potentially save performance if these properties are cached when CalculateLayoutInputVertical is invoked, so they don't need to be recalculated every time the properties are queried.
    ///
    /// The minWidth, preferredWidth, and flexibleWidth properties should not rely on any properties of the RectTransform of the layout element, otherwise the behavior will be non-deterministic.
    /// The minHeight, preferredHeight, and flexibleHeight properties may rely on horizontal aspects of the RectTransform, such as the width or the X component of the position.
    /// Any properties of the RectTransforms on child layout elements may always be relied on.
    /// </remarks>
    public interface ILayoutElement
    {
        /// <summary>
        /// 计算水平布局数据
        /// After this method is invoked, layout horizontal input properties should return up-to-date values.
        ///  Children will already have up-to-date layout horizontal inputs when this methods is called.
        /// </summary>
        void CalculateLayoutInputHorizontal();

        /// <summary>
        /// 计算竖直布局数据
        ///After this method is invoked, layout vertical input properties should return up-to-date values.
        ///Children will already have up-to-date layout vertical inputs when this methods is called.
        /// </summary>
        void CalculateLayoutInputVertical();

        /// <summary>
        /// 最小宽度
        /// The minimum width this layout element may be allocated.
        /// </summary>
        float minWidth { get; }

        /// <summary>
        /// 最佳宽度
        /// The preferred width this layout element should be allocated if there is sufficient space.
        /// </summary>
        /// <remarks>
        /// PreferredWidth can be set to -1 to remove the size.
        /// </remarks>
        float preferredWidth { get; }

        /// <summary>
        /// 灵活宽度
        /// 在父空间有剩余空间时使用，它是最终是一个百分比。也就是说，当父空间有剩余空间时，按照灵活宽度的比例，将剩余空间分配给该元素
        /// 设置为-1，则取消该值的使用
        /// The extra relative width this layout element should be allocated if there is additional available space.
        /// </summary>
        /// <remarks>
        /// Setting preferredWidth to -1 removed the preferredWidth.
        /// </remarks>
        /// <example>
        ///<code>
        ///<![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///using UnityEngine.UI; // Required when using UI elements.
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public Transform MyContentPanel;
        ///
        ///    //Sets the flexible height on on all children in the content panel.
        ///    public void Start()
        ///    {
        ///        //Assign all the children of the content panel to an array.
        ///        LayoutElement[] myLayoutElements = MyContentPanel.GetComponentsInChildren<LayoutElement>();
        ///
        ///        //For each child in the array change its LayoutElement's flexible width to 200.
        ///        foreach (LayoutElement element in myLayoutElements)
        ///        {
        ///            element.flexibleWidth = 200f;
        ///        }
        ///    }
        ///}
        ///]]>
        ///</code>
        ///</example>

        float flexibleWidth { get; }

        /// <summary>
        /// 最小高度
        /// The minimum height this layout element may be allocated.
        /// </summary>
        float minHeight { get; }

        /// <summary>
        /// 最佳高度
        /// The preferred height this layout element should be allocated if there is sufficient space.
        /// </summary>
        /// <remarks>
        /// PreferredHeight can be set to -1 to remove the size.
        /// </remarks>
        float preferredHeight { get; }

        /// <summary>
        /// 灵活高度
        /// The extra relative height this layout element should be allocated if there is additional available space.
        /// 在父空间有剩余空间时使用，它是最终是一个百分比。也就是说，当父空间有剩余空间时，按照灵活高度的比例，将剩余空间分配给该元素
        /// 设置为-1，则取消该值的使用
        /// </summary>
        ///<example>
        ///<code>
        ///<![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///using UnityEngine.UI; // Required when using UI elements.
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public Transform MyContentPanel;
        ///
        ///    //Sets the flexible height on on all children in the content panel.
        ///    public void Start()
        ///    {
        ///        //Assign all the children of the content panel to an array.
        ///        LayoutElement[] myLayoutElements = MyContentPanel.GetComponentsInChildren<LayoutElement>();
        ///
        ///        //For each child in the array change its LayoutElement's flexible height to 100.
        ///        foreach (LayoutElement element in myLayoutElements)
        ///        {
        ///            element.flexibleHeight = 100f;
        ///        }
        ///    }
        ///}
        ///]]>
        ///</code>
        ///</example>
        float flexibleHeight { get; }

        /// <summary>
        /// 在布局中该元素的优先级
        /// The layout priority of this component.
        /// </summary>
        /// <remarks>
        /// If multiple components on the same GameObject implement the ILayoutElement interface, the values provided by components that return a higher priority value are given priority. However, values less than zero are ignored. This way a component can override only select properties by leaving the remaning values to be -1 or other values less than zero.
        /// </remarks>
        int layoutPriority { get; }
    }

    /// <summary>
    /// Base interface to be implemented by components that control the layout of RectTransforms.
    /// 布局控制器基础接口
    ///
    /// 如果一个布局控制器能够控制它自己的Trs，那么需要实现 ILayoutSelfController 接口
    /// 如果一个布局控制器能够控制它子节点的Trs，那么需要实现 ILayoutGroup 接口
    /// </summary>
    /// <remarks>
    /// If a component is driving its own RectTransform it should implement the interface [[ILayoutSelfController]].
    /// If a component is driving the RectTransforms of its children, it should implement [[ILayoutGroup]].
    ///
    /// The layout system will first invoke SetLayoutHorizontal and then SetLayoutVertical.
    ///
    /// In the SetLayoutHorizontal call it is valid to call LayoutUtility.GetMinWidth, LayoutUtility.GetPreferredWidth, and LayoutUtility.GetFlexibleWidth on the RectTransform of itself or any of its children.
    /// In the SetLayoutVertical call it is valid to call LayoutUtility.GetMinHeight, LayoutUtility.GetPreferredHeight, and LayoutUtility.GetFlexibleHeight on the RectTransform of itself or any of its children.
    ///
    /// The component may use this information to determine the width and height to use for its own RectTransform or the RectTransforms of its children.
    /// </remarks>
    public interface ILayoutController
    {
        /// <summary>
        /// Callback invoked by the auto layout system which handles horizontal aspects of the layout.
        /// 布局控制器设置水平方向的布局，先被调用
        /// </summary>
        void SetLayoutHorizontal();

        /// <summary>
        /// Callback invoked by the auto layout system which handles vertical aspects of the layout.
        /// 布局控制器设置竖直方向的布局，后被调用
        /// </summary>
        void SetLayoutVertical();
    }

    /// <summary>
    /// ILayoutGroup is an ILayoutController that should drive the RectTransforms of its children.
    /// 布局控制器-控制所有子节点的Trs
    /// </summary>
    /// <remarks>
    /// ILayoutGroup derives from ILayoutController and requires the same members to be implemented.
    /// </remarks>
    public interface ILayoutGroup : ILayoutController
    {
    }

    /// <summary>
    /// ILayoutSelfController is an ILayoutController that should drive its own RectTransform.
    /// 布局控制器-控制自我节点的Trs
    /// </summary>
    /// <remarks>
    /// The iLayoutSelfController derives from the base controller [[ILayoutController]] and controls the layout of a RectTransform.
    ///
    /// Use the ILayoutSelfController to manipulate a GameObject’s own RectTransform component, which you attach in the Inspector.Use ILayoutGroup to manipulate RectTransforms belonging to the children of the GameObject.
    ///
    /// Call ILayoutController.SetLayoutHorizontal to handle horizontal parts of the layout, and call ILayoutController.SetLayoutVertical to handle vertical parts.
    /// You can change the height, width, position and rotation of the RectTransform.
    /// </remarks>
    /// <example>
    /// <code>
    /// <![CDATA[
    /// //This script shows how the GameObject’s own RectTransforms can be changed.
    /// //This creates a rectangle on the screen of the scale, positition and rotation you define in the Inspector.
    /// //Make sure to set the X and Y scale to be more than 0 to see it
    ///
    /// using UnityEngine;
    /// using UnityEngine.UI;
    /// using UnityEngine.EventSystems;
    ///
    /// public class Example : UIBehaviour, ILayoutSelfController
    /// {
    ///     //Fields in the inspector used to manipulate the RectTransform
    ///     public Vector3 m_Position;
    ///     public Vector3 m_Rotation;
    ///     public Vector2 m_Scale;
    ///
    ///     //This handles horizontal aspects of the layout (derived from ILayoutController)
    ///     public virtual void SetLayoutHorizontal()
    ///     {
    ///         //Move and Rotate the RectTransform appropriately
    ///         UpdateRectTransform();
    ///     }
    ///
    ///     //This handles vertical aspects of the layout
    ///     public virtual void SetLayoutVertical()
    ///     {
    ///         //Move and Rotate the RectTransform appropriately
    ///         UpdateRectTransform();
    ///     }
    ///
    ///     //This tells when there is a change in the inspector
    ///     #if UNITY_EDITOR
    ///     protected override void OnValidate()
    ///     {
    ///         Debug.Log("Validate");
    ///         //Update the RectTransform position, rotation and scale
    ///         UpdateRectTransform();
    ///     }
    ///
    ///     #endif
    ///
    ///     //This tells when there has been a change to the RectTransform's settings in the inspector
    ///     protected override void OnRectTransformDimensionsChange()
    ///     {
    ///         //Update the RectTransform position, rotation and scale
    ///         UpdateRectTransform();
    ///     }
    ///
    ///     void UpdateRectTransform()
    ///     {
    ///         //Fetch the RectTransform from the GameObject
    ///         RectTransform rectTransform = GetComponent<RectTransform>();
    ///
    ///         //Change the scale of the RectTransform using the fields in the inspector
    ///         rectTransform.localScale = new Vector3(m_Scale.x, m_Scale.y, 0);
    ///
    ///         //Change the position and rotation of the RectTransform
    ///         rectTransform.SetPositionAndRotation(m_Position, Quaternion.Euler(m_Rotation));
    ///     }
    /// }
    /// ]]>
    ///</code>
    /// </example>
    public interface ILayoutSelfController : ILayoutController
    {
    }

    /// <summary>
    /// A RectTransform will be ignored by the layout system if it has a component which implements ILayoutIgnorer.
    /// 布局忽略控制器
    /// 实现此接口可以设置是否要忽略布局对于元素的调整
    /// </summary>
    /// <remarks>
    /// A components that implements ILayoutIgnorer can be used to make a parent layout group component not consider this RectTransform part of the group. The RectTransform can then be manually positioned despite being a child GameObject of a layout group.
    /// </remarks>
    public interface ILayoutIgnorer
    {
        /// <summary>
        /// Should this RectTransform be ignored bvy the layout system?
        /// 是否要忽略父节点的布局控制器
        /// </summary>
        /// <remarks>
        /// Setting this property to true will make a parent layout group component not consider this RectTransform part of the group. The RectTransform can then be manually positioned despite being a child GameObject of a layout group.
        /// </remarks>
        bool ignoreLayout { get; }
    }
}
