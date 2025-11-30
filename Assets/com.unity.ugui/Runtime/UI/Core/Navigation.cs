using System;
using UnityEngine.Serialization;

namespace UnityEngine.UI
{
    [Serializable]
    /// <summary>
    /// Structure storing details related to navigation.
    /// UI元素导航信息结构体
    /// 它控制当用户使用键盘方向键或游戏手柄摇杆时，UI 焦点如何在不同的 Selectable 元素（如 Button、Toggle、Slider 等）之间切换。
    /// </summary>
    public struct Navigation : IEquatable<Navigation>
    {
        /*
         * This looks like it's not flags, but it is flags,
         * the reason is that Automatic is considered horizontal
         * and verical mode combined
         */
        [Flags]
        /// <summary>
        /// Navigation mode enumeration.
        /// 导航模式
        /// </summary>
        /// <remarks>
        /// This looks like it's not flags, but it is flags, the reason is that Automatic is considered horizontal and vertical mode combined
        /// </remarks>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// using UnityEngine;
        /// using System.Collections;
        /// using UnityEngine.UI; // Required when Using UI elements.
        ///
        /// public class ExampleClass : MonoBehaviour
        /// {
        ///     public Button button;
        ///
        ///     void Start()
        ///     {
        ///         //Set the navigation to the mode "Vertical".
        ///         if (button.navigation.mode == Navigation.Mode.Vertical)
        ///         {
        ///             Debug.Log("The button's navigation mode is Vertical");
        ///         }
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
        public enum Mode
        {
            /// <summary>
            /// No navigation is allowed from this object.
            /// 此Go禁用导航
            /// </summary>
            None        = 0,

            /// <summary>
            /// Horizontal Navigation.
            /// 仅开启水平导航
            /// </summary>
            /// <remarks>
            /// Navigation should only be allowed when left / right move events happen.
            /// </remarks>
            Horizontal  = 1,

            /// <summary>
            /// Vertical navigation.
            /// 仅开启垂直导航
            /// </summary>
            /// <remarks>
            /// Navigation should only be allowed when up / down move events happen.
            /// </remarks>
            Vertical    = 2,

            /// <summary>
            /// Automatic navigation.
            /// 自动导航，会使用启发式算法寻找到最佳的下一个
            /// </summary>
            /// <remarks>
            /// Attempt to find the 'best' next object to select. This should be based on a sensible heuristic.
            /// </remarks>
            Automatic   = 3,

            /// <summary>
            /// Explicit navigation.
            /// 需要用户指定下一个
            /// </summary>
            /// <remarks>
            /// User should explicitly specify what is selected by each move event.
            /// </remarks>
            Explicit    = 4,
        }

        //使用的导航算法
        // Which method of navigation will be used.
        [SerializeField]
        private Mode m_Mode;

        //启用环绕式导航，也就是说可以当到最后一个时，可以跳到第一个，反之亦然；对于Automatic模式不生效
        [Tooltip("Enables navigation to wrap around from last to first or first to last element. Does not work for automatic grid navigation")]
        [SerializeField]
        private bool m_WrapAround;

        //用于Explicit模式下，摇杆向上时的选择对象
        // Game object selected when the joystick moves up. Used when navigation is set to "Explicit".
        [SerializeField]
        private Selectable m_SelectOnUp;

        //用于Explicit模式下，摇杆向下时的选择对象
        // Game object selected when the joystick moves down. Used when navigation is set to "Explicit".
        [SerializeField]
        private Selectable m_SelectOnDown;

        //用于Explicit模式下，摇杆向左时的选择对象
        // Game object selected when the joystick moves left. Used when navigation is set to "Explicit".
        [SerializeField]
        private Selectable m_SelectOnLeft;

        //用于Explicit模式下，摇杆向右时的选择对象
        // Game object selected when the joystick moves right. Used when navigation is set to "Explicit".
        [SerializeField]
        private Selectable m_SelectOnRight;

        /// <summary>
        /// Navigation mode.
        /// 导航模式
        /// </summary>
        public Mode       mode           { get { return m_Mode; } set { m_Mode = value; } }

        /// <summary>
        /// Enables navigation to wrap around from last to first or first to last element.
        /// Will find the furthest element from the current element in the opposite direction of movement.
        //  启用环绕式导航，也就是说可以当到最后一个时，可以跳到第一个，反之亦然；对于Automatic模式不生效
        /// </summary>
        /// <example>
        /// Note: If you have a grid of elements and you are on the last element in a row it will not wrap over to the next row it will pick the furthest element in the opposite direction.
        /// </example>
        public bool wrapAround { get { return m_WrapAround; } set { m_WrapAround = value; } }

        /// <summary>
        /// Specify a Selectable UI GameObject to highlight when the Up arrow key is pressed.
        /// </summary>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// using UnityEngine;
        /// using System.Collections;
        /// using UnityEngine.UI;  // Required when Using UI elements.
        ///
        /// public class HighlightOnKey : MonoBehaviour
        /// {
        ///     public Button btnSave;
        ///     public Button btnLoad;
        ///
        ///     public void Start()
        ///     {
        ///         // get the Navigation data
        ///         Navigation navigation = btnLoad.navigation;
        ///
        ///         // switch mode to Explicit to allow for custom assigned behavior
        ///         navigation.mode = Navigation.Mode.Explicit;
        ///
        ///         // highlight the Save button if the up arrow key is pressed
        ///         navigation.selectOnUp = btnSave;
        ///
        ///         // reassign the struct data to the button
        ///         btnLoad.navigation = navigation;
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
        public Selectable selectOnUp     { get { return m_SelectOnUp; } set { m_SelectOnUp = value; } }

        /// <summary>
        /// Specify a Selectable UI GameObject to highlight when the down arrow key is pressed.
        /// </summary>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// using UnityEngine;
        /// using System.Collections;
        /// using UnityEngine.UI;  // Required when Using UI elements.
        ///
        /// public class HighlightOnKey : MonoBehaviour
        /// {
        ///     public Button btnSave;
        ///     public Button btnLoad;
        ///
        ///     public void Start()
        ///     {
        ///         // get the Navigation data
        ///         Navigation navigation = btnLoad.navigation;
        ///
        ///         // switch mode to Explicit to allow for custom assigned behavior
        ///         navigation.mode = Navigation.Mode.Explicit;
        ///
        ///         // highlight the Save button if the down arrow key is pressed
        ///         navigation.selectOnDown = btnSave;
        ///
        ///         // reassign the struct data to the button
        ///         btnLoad.navigation = navigation;
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
        public Selectable selectOnDown   { get { return m_SelectOnDown; } set { m_SelectOnDown = value; } }

        /// <summary>
        /// Specify a Selectable UI GameObject to highlight when the left arrow key is pressed.
        /// </summary>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// using UnityEngine;
        /// using System.Collections;
        /// using UnityEngine.UI;  // Required when Using UI elements.
        ///
        /// public class HighlightOnKey : MonoBehaviour
        /// {
        ///     public Button btnSave;
        ///     public Button btnLoad;
        ///
        ///     public void Start()
        ///     {
        ///         // get the Navigation data
        ///         Navigation navigation = btnLoad.navigation;
        ///
        ///         // switch mode to Explicit to allow for custom assigned behavior
        ///         navigation.mode = Navigation.Mode.Explicit;
        ///
        ///         // highlight the Save button if the left arrow key is pressed
        ///         navigation.selectOnLeft = btnSave;
        ///
        ///         // reassign the struct data to the button
        ///         btnLoad.navigation = navigation;
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
        public Selectable selectOnLeft   { get { return m_SelectOnLeft; } set { m_SelectOnLeft = value; } }

        /// <summary>
        /// Specify a Selectable UI GameObject to highlight when the right arrow key is pressed.
        /// </summary>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// using UnityEngine;
        /// using System.Collections;
        /// using UnityEngine.UI;  // Required when Using UI elements.
        ///
        /// public class HighlightOnKey : MonoBehaviour
        /// {
        ///     public Button btnSave;
        ///     public Button btnLoad;
        ///
        ///     public void Start()
        ///     {
        ///         // get the Navigation data
        ///         Navigation navigation = btnLoad.navigation;
        ///
        ///         // switch mode to Explicit to allow for custom assigned behavior
        ///         navigation.mode = Navigation.Mode.Explicit;
        ///
        ///         // highlight the Save button if the right arrow key is pressed
        ///         navigation.selectOnRight = btnSave;
        ///
        ///         // reassign the struct data to the button
        ///         btnLoad.navigation = navigation;
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
        public Selectable selectOnRight  { get { return m_SelectOnRight; } set { m_SelectOnRight = value; } }

        /// <summary>
        /// Return a Navigation with sensible default values.
        /// 默认导航模式是自动、且关闭环绕模式
        /// </summary>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// using UnityEngine;
        /// using System.Collections;
        /// using UnityEngine.UI; // Required when Using UI elements.
        ///
        /// public class ExampleClass : MonoBehaviour
        /// {
        ///     public Button button;
        ///
        ///     void Start()
        ///     {
        ///         //Set the navigation to the default value. ("Automatic" is the default value).
        ///         button.navigation = Navigation.defaultNavigation;
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
        static public Navigation defaultNavigation
        {
            get
            {
                var defaultNav = new Navigation();
                defaultNav.m_Mode = Mode.Automatic;
                defaultNav.m_WrapAround = false;
                return defaultNav;
            }
        }

        public bool Equals(Navigation other)
        {
            return mode == other.mode &&
                selectOnUp == other.selectOnUp &&
                selectOnDown == other.selectOnDown &&
                selectOnLeft == other.selectOnLeft &&
                selectOnRight == other.selectOnRight;
        }
    }
}
