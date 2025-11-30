using System;
using System.Collections.Generic;
using UnityEngine.Serialization;
using UnityEngine.EventSystems;

namespace UnityEngine.UI
{
    [AddComponentMenu("UI/Selectable", 35)]
    [ExecuteAlways]
    [SelectionBase]
    [DisallowMultipleComponent]
    /// <summary>
    /// Simple selectable object - derived from to create a selectable control.
    /// 可交互的组件基类
    /// 像Button、InputField这些可交互类的组件的基类
    /// 实现了鼠标进入、退出、按下、弹起、选中、取消选中接口
    /// </summary>
    public class Selectable
        :
        UIBehaviour,
        IMoveHandler,
        IPointerDownHandler, IPointerUpHandler,
        IPointerEnterHandler, IPointerExitHandler,
        ISelectHandler, IDeselectHandler
    {
        //所有可交互组件的静态数组存储，所有可交互组件都存放在这里
        protected static Selectable[] s_Selectables = new Selectable[10];
        //所有可交互组件的数量，其实是个指针，标明数组里现在到第几个位置了，每次放入一个元素都+1
        protected static int s_SelectableCount = 0;
        //是否已经调用过Enable了，防止多次调用Enable
        private bool m_EnableCalled = false;

        /// <summary>
        /// Copy of the array of all the selectable objects currently active in the scene.
        /// </summary>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// using UnityEngine;
        /// using System.Collections;
        /// using UnityEngine.UI; // required when using UI elements in scripts
        ///
        /// public class Example : MonoBehaviour
        /// {
        ///     //Displays the names of all selectable elements in the scene
        ///     public void GetNames()
        ///     {
        ///         foreach (Selectable selectableUI in Selectable.allSelectablesArray)
        ///         {
        ///             Debug.Log(selectableUI.name);
        ///         }
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
        public static Selectable[] allSelectablesArray
        {
            get
            {
                Selectable[] temp = new Selectable[s_SelectableCount];
                Array.Copy(s_Selectables, temp, s_SelectableCount);
                return temp;
            }
        }

        /// <summary>
        /// How many selectable elements are currently active.
        /// </summary>
        public static int allSelectableCount { get { return s_SelectableCount; } }

        /// <summary>
        /// A List instance of the allSelectablesArray to maintain API compatibility.
        /// </summary>

        [Obsolete("Replaced with allSelectablesArray to have better performance when disabling a element", false)]
        public static List<Selectable> allSelectables
        {
            get
            {
                return new List<Selectable>(allSelectablesArray);
            }
        }


        /// <summary>
        /// Non allocating version for getting the all selectables.
        /// If selectables.Length is less then s_SelectableCount only selectables.Length elments will be copied which
        /// could result in a incomplete list of elements.
        /// </summary>
        /// <param name="selectables">The array to be filled with current selectable objects</param>
        /// <returns>The number of element copied.</returns>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// using UnityEngine;
        /// using System.Collections;
        /// using UnityEngine.UI; // required when using UI elements in scripts
        ///
        /// public class Example : MonoBehaviour
        /// {
        ///     Selectable[] m_Selectables = new Selectable[10];
        ///
        ///     //Displays the names of all selectable elements in the scene
        ///     public void GetNames()
        ///     {
        ///         if (m_Selectables.Length < Selectable.allSelectableCount)
        ///             m_Selectables = new Selectable[Selectable.allSelectableCount];
        ///
        ///         int count = Selectable.AllSelectablesNoAlloc(ref m_Selectables);
        ///
        ///         for (int i = 0; i < count; ++i)
        ///         {
        ///             Debug.Log(m_Selectables[i].name);
        ///         }
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
        public static int AllSelectablesNoAlloc(Selectable[] selectables)
        {
            int copyCount = selectables.Length < s_SelectableCount ? selectables.Length : s_SelectableCount;

            Array.Copy(s_Selectables, selectables, copyCount);

            return copyCount;
        }

        //导航信息
        // Navigation information.
        [FormerlySerializedAs("navigation")]
        [SerializeField]
        private Navigation m_Navigation = Navigation.defaultNavigation;

        /// <summary>
        ///Transition mode for a Selectable.
        /// 可交互组件发生状态变更时所用的视觉效果转换方法
        /// </summary>
        public enum Transition
        {
            /// <summary>
            /// No Transition.
            /// </summary>
            None,

            /// <summary>
            /// Use an color tint transition.
            /// 使用颜色
            /// </summary>
            ColorTint,

            /// <summary>
            /// Use a sprite swap transition.
            /// 使用精灵
            /// </summary>
            SpriteSwap,

            /// <summary>
            /// Use an animation transition.
            /// 使用动画
            /// </summary>
            Animation
        }

        // Type of the transition that occurs when the button state changes.
        // 可交互组件发生状态变更时所用的视觉效果转换方法
        [FormerlySerializedAs("transition")]
        [SerializeField]
        private Transition m_Transition = Transition.ColorTint;

        // Colors used for a color tint-based transition.
        //不同状态下使用的颜色
        [FormerlySerializedAs("colors")]
        [SerializeField]
        private ColorBlock m_Colors = ColorBlock.defaultColorBlock;

        // Sprites used for a Image swap-based transition.
        //不同状态下使用的精灵图片
        [FormerlySerializedAs("spriteState")]
        [SerializeField]
        private SpriteState m_SpriteState;

        //不同状态下使用的动画触发器
        [FormerlySerializedAs("animationTriggers")]
        [SerializeField]
        private AnimationTriggers m_AnimationTriggers = new AnimationTriggers();

        //是否可交互
        [Tooltip("Can the Selectable be interacted with?")]
        [SerializeField]
        private bool m_Interactable = true;

        //自身的Graphic组件，Awake时会赋值
        //如果自身使用Sprite做状态切换，那么Graphic组件必须是Image才能生效
        //
        //Graphic有2个作用：
        //一个是接收射线检测、从而能够产生UI事件，没有Graphic的话无法被射线检测到；
        //第二个是提供事件的视觉效果，比如颜色变更、动画缩放等，没有Graphic也就没有这些视觉效果
        // Graphic that will be colored.
        [FormerlySerializedAs("highlightGraphic")]
        [FormerlySerializedAs("m_HighlightGraphic")]
        [SerializeField]
        private Graphic m_TargetGraphic;

        //逐级向上找CanvasGroup，是否允许交互
        private bool m_GroupsAllowInteraction = true;
        //当前组件在可交互组件列表中的位置
        protected int m_CurrentIndex = -1;

        /// <summary>
        /// The Navigation setting for this selectable object.
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
        public Navigation        navigation        { get { return m_Navigation; } set { if (SetPropertyUtility.SetStruct(ref m_Navigation, value))        OnSetProperty(); } }

        /// <summary>
        /// The type of transition that will be applied to the targetGraphic when the state changes.
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
        ///     public Button btnMain;
        ///
        ///     void SomeFunction()
        ///     {
        ///         //Sets the main button's transition setting to "Color Tint".
        ///         btnMain.transition = Selectable.Transition.ColorTint;
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
        public Transition        transition        { get { return m_Transition; } set { if (SetPropertyUtility.SetStruct(ref m_Transition, value))        OnSetProperty(); } }

        /// <summary>
        /// The ColorBlock for this selectable object.
        /// </summary>
        /// <remarks>
        /// Modifications will not be visible if  transition is not ColorTint.
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
        ///         //Resets the colors in the buttons transitions.
        ///         button.colors = ColorBlock.defaultColorBlock;
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
        public ColorBlock        colors            { get { return m_Colors; } set { if (SetPropertyUtility.SetStruct(ref m_Colors, value))            OnSetProperty(); } }

        /// <summary>
        /// The SpriteState for this selectable object.
        /// </summary>
        /// <remarks>
        /// Modifications will not be visible if transition is not SpriteSwap.
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
        ///     //Creates an instance of a sprite state (This includes the highlighted, pressed and disabled sprite.
        ///     public SpriteState sprState = new SpriteState();
        ///     public Button btnMain;
        ///
        ///
        ///     void Start()
        ///     {
        ///         //Assigns the new sprite states to the button.
        ///         btnMain.spriteState = sprState;
        ///     }
        /// }
        /// ]]>
        /// </code>
        /// </example>
        public SpriteState       spriteState       { get { return m_SpriteState; } set { if (SetPropertyUtility.SetStruct(ref m_SpriteState, value))       OnSetProperty(); } }

        /// <summary>
        /// The AnimationTriggers for this selectable object.
        /// </summary>
        /// <remarks>
        /// Modifications will not be visible if transition is not Animation.
        /// </remarks>
        public AnimationTriggers animationTriggers { get { return m_AnimationTriggers; } set { if (SetPropertyUtility.SetClass(ref m_AnimationTriggers, value)) OnSetProperty(); } }

        /// <summary>
        /// Graphic that will be transitioned upon.
        /// 自身的Graphic元素
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
        ///     public Image newImage;
        ///     public Button btnMain;
        ///
        ///     void SomeFunction()
        ///     {
        ///         //Displays the sprite transitions on the image when the transition to Highlighted,pressed or disabled is made.
        ///         btnMain.targetGraphic = newImage;
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
        public Graphic           targetGraphic     { get { return m_TargetGraphic; } set { if (SetPropertyUtility.SetClass(ref m_TargetGraphic, value))     OnSetProperty(); } }

        /// <summary>
        /// Is this object interactable.
        /// 是否可交互
        /// </summary>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// using UnityEngine;
        /// using System.Collections;
        /// using UnityEngine.UI; // required when using UI elements in scripts
        ///
        /// public class Example : MonoBehaviour
        /// {
        ///     public Button startButton;
        ///     public bool playersReady;
        ///
        ///
        ///     void Update()
        ///     {
        ///         // checks if the players are ready and if the start button is useable
        ///         if (playersReady == true && startButton.interactable == false)
        ///         {
        ///             //allows the start button to be used
        ///             startButton.interactable = true;
        ///         }
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
        public bool              interactable
        {
            get { return m_Interactable; }
            set
            {
                //当状态发生变更，并且当前选中的元素是自己，那么取消当前选中的元素
                if (SetPropertyUtility.SetStruct(ref m_Interactable, value))
                {
                    if (!m_Interactable && EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject)
                        EventSystem.current.SetSelectedGameObject(null);
                    OnSetProperty();
                }
            }
        }

        //鼠标是否进入了
        private bool             isPointerInside   { get; set; }
        //是否按下了
        private bool             isPointerDown     { get; set; }
        //是否已经选中了
        private bool             hasSelection      { get; set; }

        protected Selectable()
        {}

        /// <summary>
        /// Convenience function that converts the referenced Graphic to a Image, if possible.
        /// 从 m_TargetGraphic 转换为Image组件，在设置为图片切换状态时使用
        /// 所以如果不同状态下使用不同的Sprite，自身必须挂载Image组件
        /// </summary>
        public Image image
        {
            get { return m_TargetGraphic as Image; }
            set { m_TargetGraphic = value; }
        }

        /// <summary>
        /// Convenience function to get the Animator component on the GameObject.
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
        ///     private Animator buttonAnimator;
        ///     public Button button;
        ///
        ///     void Start()
        ///     {
        ///         //Assigns the "buttonAnimator" with the button's animator.
        ///         buttonAnimator = button.animator;
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
#if PACKAGE_ANIMATION
        public Animator animator
        {
            get { return GetComponent<Animator>(); }
        }
#endif

        protected override void Awake()
        {
            //Awake时，从自身找Grahpic对象
            if (m_TargetGraphic == null)
                m_TargetGraphic = GetComponent<Graphic>();
        }

        //CanvasGroup缓存数据，用于逐级查询CanvasGroup的时候减少开销
        private readonly List<CanvasGroup> m_CanvasGroupCache = new List<CanvasGroup>();
        
        //CanvasGroup变更时调用，重新检测下是否可以交互
        protected override void OnCanvasGroupChanged()
        {
            var parentGroupAllowsInteraction = ParentGroupAllowsInteraction();

            if (parentGroupAllowsInteraction != m_GroupsAllowInteraction)
            {
                m_GroupsAllowInteraction = parentGroupAllowsInteraction;
                OnSetProperty();
            }
        }
        
        /// <summary>
        /// 逐级向上查找CanvasGroup，看CanvasGroup是否允许交互
        /// 过程中，如果CanvasGroup不能交互、或者CanvasGroup忽略父CanvasGroup的交互设置，那么返回
        /// </summary>
        /// <returns></returns>
        bool ParentGroupAllowsInteraction()
        {
            Transform t = transform;
            while (t != null)
            {
                t.GetComponents(m_CanvasGroupCache);
                for (var i = 0; i < m_CanvasGroupCache.Count; i++)
                {
                    if (m_CanvasGroupCache[i].enabled && !m_CanvasGroupCache[i].interactable)
                        return false;

                    if (m_CanvasGroupCache[i].ignoreParentGroups)
                        return true;
                }

                t = t.parent;
            }

            return true;
        }

        /// <summary>
        /// Is the object interactable.
        /// 按钮是否可交互
        /// 是上层的CanvasGroup可交互、并且自身可交互
        /// </summary>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// using UnityEngine;
        /// using System.Collections;
        /// using UnityEngine.UI; // required when using UI elements in scripts
        ///
        /// public class Example : MonoBehaviour
        /// {
        ///     public Button startButton;
        ///
        ///     void Update()
        ///     {
        ///         if (!startButton.IsInteractable())
        ///         {
        ///             Debug.Log("Start Button has been Disabled");
        ///         }
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
        public virtual bool IsInteractable()
        {
            return m_GroupsAllowInteraction && m_Interactable;
        }

        // Call from unity if animation properties have changed
        protected override void OnDidApplyAnimationProperties()
        {
            OnSetProperty();
        }

        // Select on enable and add to the list.
        protected override void OnEnable()
        {
            //如果已经Enable过了，不再多次调用
            //Check to avoid multiple OnEnable() calls for each selectable
            if (m_EnableCalled)
                return;

            base.OnEnable();

            //静态可交互组件列表的扩容
            if (s_SelectableCount == s_Selectables.Length)
            {
                Selectable[] temp = new Selectable[s_Selectables.Length * 2];
                Array.Copy(s_Selectables, temp, s_Selectables.Length);
                s_Selectables = temp;
            }

            //如果当前选中了这个GO，那么标识为选中态
            if (EventSystem.current && EventSystem.current.currentSelectedGameObject == gameObject)
            {
                hasSelection = true;
            }

            //把当前组件放入到静态可交互组件列表的最后一个，并做一些数据的初始化
            m_CurrentIndex = s_SelectableCount;
            s_Selectables[m_CurrentIndex] = this;
            s_SelectableCount++;
            isPointerDown = false;
            m_GroupsAllowInteraction = ParentGroupAllowsInteraction();
            //状态流转
            DoStateTransition(currentSelectionState, true);

            m_EnableCalled = true;
        }

        protected override void OnTransformParentChanged()
        {
            base.OnTransformParentChanged();

            //父节点Transform变更的时候，重新检测下CanvasGroup
            // If our parenting changes figure out if we are under a new CanvasGroup.
            OnCanvasGroupChanged();
        }

        /// <summary>
        /// 每当数据变更、交互变更时，都开始进行状态流转
        /// </summary>
        private void OnSetProperty()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                DoStateTransition(currentSelectionState, true);
            else
#endif
            DoStateTransition(currentSelectionState, false);
        }

        //失效的时候把自己从可交互组件列表中剔除
        // Remove from the list.
        protected override void OnDisable()
        {
            //Check to avoid multiple OnDisable() calls for each selectable
            if (!m_EnableCalled)
                return;

            //计数-1
            s_SelectableCount--;

            //把列表中的最后一个挪到自己的这个位置上
            // Update the last elements index to be this index
            s_Selectables[s_SelectableCount].m_CurrentIndex = m_CurrentIndex;

            // Swap the last element and this element
            s_Selectables[m_CurrentIndex] = s_Selectables[s_SelectableCount];

            // null out last element.
            s_Selectables[s_SelectableCount] = null;

            //清理一下各类状态
            InstantClearState();
            base.OnDisable();

            m_EnableCalled = false;
        }

        void OnApplicationFocus(bool hasFocus)
        {
            //当用户切到其他应用时清除所有状态
            if (!hasFocus && IsPressed())
            {
                InstantClearState();
            }
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            m_Colors.fadeDuration = Mathf.Max(m_Colors.fadeDuration, 0.0f);

            // OnValidate can be called before OnEnable, this makes it unsafe to access other components
            // since they might not have been initialized yet.
            // OnSetProperty potentially access Animator or Graphics. (case 618186)
            if (isActiveAndEnabled)
            {
                if (!interactable && EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject)
                    EventSystem.current.SetSelectedGameObject(null);
                // Need to clear out the override image on the target...
                DoSpriteSwap(null);

                // If the transition mode got changed, we need to clear all the transitions, since we don't know what the old transition mode was.
                StartColorTween(Color.white, true);
                TriggerAnimation(m_AnimationTriggers.normalTrigger);

                // And now go to the right state.
                DoStateTransition(currentSelectionState, true);
            }
        }

        protected override void Reset()
        {
            m_TargetGraphic = GetComponent<Graphic>();
        }

#endif // if UNITY_EDITOR
        
        //当前的选中状态
        protected SelectionState currentSelectionState
        {
            get
            {
                if (!IsInteractable())
                    return SelectionState.Disabled;
                if (isPointerDown)
                    return SelectionState.Pressed;
                if (hasSelection)
                    return SelectionState.Selected;
                if (isPointerInside)
                    return SelectionState.Highlighted;
                return SelectionState.Normal;
            }
        }

        /// <summary>
        /// Clear any internal state from the Selectable (used when disabling).
        /// Disable的时候，或者App重新获取焦点并且自己不是被选中的时候，清理一下各类状态
        /// 清理颜色、动画、精灵状态
        /// </summary>
        protected virtual void InstantClearState()
        {
            string triggerName = m_AnimationTriggers.normalTrigger;

            isPointerInside = false;
            isPointerDown = false;
            hasSelection = false;

            switch (m_Transition)
            {
                case Transition.ColorTint:
                    StartColorTween(Color.white, true);
                    break;
                case Transition.SpriteSwap:
                    DoSpriteSwap(null);
                    break;
                case Transition.Animation:
                    TriggerAnimation(triggerName);
                    break;
            }
        }

        /// <summary>
        /// Transition the Selectable to the entered state.
        /// 可交互组件的状态流转
        /// 切换到不同的状态，颜色、精灵、动画触发器设置
        /// </summary>
        /// <param name="state">State to transition to</param>
        /// <param name="instant">Should the transition occur instantly.是否立即完成</param>
        protected virtual void DoStateTransition(SelectionState state, bool instant)
        {
            if (!gameObject.activeInHierarchy)
                return;

            Color tintColor;
            Sprite transitionSprite;
            string triggerName;

            switch (state)
            {
                case SelectionState.Normal:
                    tintColor = m_Colors.normalColor;
                    transitionSprite = null;
                    triggerName = m_AnimationTriggers.normalTrigger;
                    break;
                case SelectionState.Highlighted:
                    tintColor = m_Colors.highlightedColor;
                    transitionSprite = m_SpriteState.highlightedSprite;
                    triggerName = m_AnimationTriggers.highlightedTrigger;
                    break;
                case SelectionState.Pressed:
                    tintColor = m_Colors.pressedColor;
                    transitionSprite = m_SpriteState.pressedSprite;
                    triggerName = m_AnimationTriggers.pressedTrigger;
                    break;
                case SelectionState.Selected:
                    tintColor = m_Colors.selectedColor;
                    transitionSprite = m_SpriteState.selectedSprite;
                    triggerName = m_AnimationTriggers.selectedTrigger;
                    break;
                case SelectionState.Disabled:
                    tintColor = m_Colors.disabledColor;
                    transitionSprite = m_SpriteState.disabledSprite;
                    triggerName = m_AnimationTriggers.disabledTrigger;
                    break;
                default:
                    tintColor = Color.black;
                    transitionSprite = null;
                    triggerName = string.Empty;
                    break;
            }

            //根据不同的转换类型，来确认使用的是颜色、还是精灵、还是动画状态
            switch (m_Transition)
            {
                case Transition.ColorTint:
                    StartColorTween(tintColor * m_Colors.colorMultiplier, instant);
                    break;
                case Transition.SpriteSwap:
                    DoSpriteSwap(transitionSprite);
                    break;
                case Transition.Animation:
                    TriggerAnimation(triggerName);
                    break;
            }
        }

        /// <summary>
        /// An enumeration of selected states of objects
        /// 可交互组件的当前选中状态
        /// </summary>
        protected enum SelectionState
        {
            /// <summary>
            /// The UI object can be selected.
            /// 准备，可被交互
            /// </summary>
            Normal,

            /// <summary>
            /// The UI object is highlighted.
            /// 高亮，鼠标进入时
            /// </summary>
            Highlighted,

            /// <summary>
            /// The UI object is pressed.
            /// 按下
            /// </summary>
            Pressed,

            /// <summary>
            /// The UI object is selected
            /// 选择
            /// </summary>
            Selected,

            /// <summary>
            /// The UI object cannot be selected.
            /// 不可选中
            /// </summary>
            Disabled,
        }

        // Selection logic

        /// <summary>
        /// Finds the selectable object next to this one.
        /// 找到下一个可选择的对象
        /// </summary>
        /// <remarks>
        /// The direction is determined by a Vector3 variable.
        /// </remarks>
        /// <param name="dir">The direction in which to search for a neighbouring Selectable object.</param>
        /// <returns>The neighbouring Selectable object. Null if none found.</returns>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// using UnityEngine;
        /// using System.Collections;
        /// using UnityEngine.UI; // required when using UI elements in scripts
        ///
        /// public class ExampleClass : MonoBehaviour
        /// {
        ///     //Sets the direction as "Up" (Y is in positive).
        ///     public Vector3 direction = new Vector3(0, 1, 0);
        ///     public Button btnMain;
        ///
        ///     public void Start()
        ///     {
        ///         //Finds and assigns the selectable above the main button
        ///         Selectable newSelectable = btnMain.FindSelectable(direction);
        ///
        ///         Debug.Log(newSelectable.name);
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
        public Selectable FindSelectable(Vector3 dir)
        {
            //dir是世界坐标的方向
            //注意，由于导航是按照UI的旋转进行计算的，而不是固定屏幕方向，所以如果UI有旋转，那么原有的dir就会先被转换一下
            //比如用户输入了↑键，但是当前的UI元素右转了90°，那么会把这个用户输入转换一下，变成世界坐标的→方向
            //但是转换后还是世界方向
            dir = dir.normalized;
            //这里是把dir先转换到本地方向，因为传进来的是世界方向
            Vector3 localDir = Quaternion.Inverse(transform.rotation) * dir;
            //计算出在这个方向下，该RectTransform边缘上的点，并转换为世界坐标点
            Vector3 pos = transform.TransformPoint(GetPointOnRectEdge(transform as RectTransform, localDir));
            //遍历元素时，最佳元素的分数，用来寻找到最佳元素
            float maxScore = Mathf.NegativeInfinity;
            //遍历元素时，反方向最远元素的分数，用来寻找到反方向最远的元素，环绕式导航需要
            float maxFurthestScore = Mathf.NegativeInfinity;
            //当前分数
            float score = 0;

            //是否使用环绕式导航
            bool wantsWrapAround = navigation.wrapAround && (m_Navigation.mode == Navigation.Mode.Vertical || m_Navigation.mode == Navigation.Mode.Horizontal);

            //最佳选择
            Selectable bestPick = null;
            //反方向最远选择
            Selectable bestFurthestPick = null;

            //遍历所有的可交互组件，寻找下一个
            for (int i = 0; i < s_SelectableCount; ++i)
            {
                Selectable sel = s_Selectables[i];
                //跳过自己
                if (sel == this)
                    continue;
                //跳过不可交互、不可导航的
                if (!sel.IsInteractable() || sel.navigation.mode == Navigation.Mode.None)
                    continue;

#if UNITY_EDITOR
                // Apart from runtime use, FindSelectable is used by custom editors to
                // draw arrows between different selectables. For scene view cameras,
                // only selectables in the same stage should be considered.
                if (Camera.current != null && !UnityEditor.SceneManagement.StageUtility.IsGameObjectRenderedByCamera(sel.gameObject, Camera.current))
                    continue;
#endif

                //找到sel元素的中心店，并转换为世界坐标
                var selRect = sel.transform as RectTransform;
                Vector3 selCenter = selRect != null ? (Vector3)selRect.rect.center : Vector3.zero;
                //计算出sel元素与当前元素边缘点的相对位置向量
                Vector3 myVector = sel.transform.TransformPoint(selCenter) - pos;
                
                //方向与sel世界坐标中心点点乘，计算出方向在sel中心方向的投影长度
                // Value that is the distance out along the direction.
                float dot = Vector3.Dot(dir, myVector);

                //如果投影长度是负值，说明这个sel元素是反方向的元素，如果此时开启了环绕式、那么把它记录到最远元素中
                // If element is in wrong direction and we have wrapAround enabled check and cache it if furthest away.
                if (wantsWrapAround && dot < 0)
                {
                    //-dot越大，代表投影越大，那么就是反方向的元素越接近要选中的反方向
                    //myVector.sqrMagnitude越大，代表元素本身越远
                    //所以用这2个的乘积作为一个分数，分数越大，代表越应该选择这个元素
                    score = -dot * myVector.sqrMagnitude;

                    if (score > maxFurthestScore)
                    {
                        maxFurthestScore = score;
                        bestFurthestPick = sel;
                    }

                    continue;
                }

                //如果dot小于0，那么代表sel元素不在要选择的方向上，所以就不做考虑了
                // Skip elements that are in the wrong direction or which have zero distance.
                // This also ensures that the scoring formula below will not have a division by zero error.
                if (dot <= 0)
                    continue;

                // This scoring function has two priorities:
                // - Score higher for positions that are closer.
                // - Score higher for positions that are located in the right direction.
                // This scoring function combines both of these criteria.
                // It can be seen as this:
                //   Dot (dir, myVector.normalized) / myVector.magnitude
                // The first part equals 1 if the direction of myVector is the same as dir, and 0 if it's orthogonal.
                // The second part scores lower the greater the distance is by dividing by the distance.
                // The formula below is equivalent but more optimized.
                //
                // If a given score is chosen, the positions that evaluate to that score will form a circle
                // that touches pos and whose center is located along dir. A way to visualize the resulting functionality is this:
                // From the position pos, blow up a circular balloon so it grows in the direction of dir.
                // The first Selectable whose center the circular balloon touches is the one that's chosen.
                //距离越近、方向越接近，那么就越应该选择这个元素
                //所以这个计算分数的方法综合了这2个因素，来计算得分
                //方向越偏离，那么dot投影值越小，得分越小
                //距离越远，myVector.sqrMagnitude值越大，得分越小
                score = dot / myVector.sqrMagnitude;

                if (score > maxScore)
                {
                    maxScore = score;
                    bestPick = sel;
                }
            }

            //如果开启了环绕式，并且目标方向上没有要选择的元素，那么就返回最远的元素
            if (wantsWrapAround && null == bestPick) return bestFurthestPick;
            //否则，返回目标方向最近的元素
            return bestPick;
        }

        /// <summary>
        /// 计算在某个方向下，RectTransform边缘上的点的位置
        ///
        /// 使用无穷范数归一化|8方向归一化算法进行计算
        /// </summary>
        /// <param name="rect"></param>
        /// <param name="dir">世界坐标的方向</param>
        /// <returns>返回值是本地坐标的位置点</returns>
        private static Vector3 GetPointOnRectEdge(RectTransform rect, Vector2 dir)
        {
            if (rect == null)
                return Vector3.zero;
            //使用无穷范数归一化|8方向归一化算法进行方向归一化，使得dir在一个正方形范围内
            //这样可以利用这个方向计算边缘点位置
            if (dir != Vector2.zero)
                dir /= Mathf.Max(Mathf.Abs(dir.x), Mathf.Abs(dir.y));
            //Rect的中心点+ rect的尺寸按照dir进行缩放即可
            //Vector.Scale是逐分量相乘，相当于X/Y分别缩放
            dir = rect.rect.center + Vector2.Scale(rect.rect.size, dir * 0.5f);
            return dir;
        }

        // Convenience function -- change the selection to the specified object if it's not null and happens to be active.
        //设置导航UI
        void Navigate(AxisEventData eventData, Selectable sel)
        {
            if (sel != null && sel.IsActive())
                eventData.selectedObject = sel.gameObject;
        }

        /// <summary>
        /// Find the selectable object to the left of this one.
        /// 找到左方向的元素
        /// 注意自动导航是相对于UI元素本身的方向，而不是固定屏幕方向，所以虽然是用户按了↑键，但是如果UI本身有旋转，还是会把用户输入转换一下
        /// 比如用户输入了↑键，但是当前的UI元素右转了90°，那么会把这个用户输入转换一下，变成世界坐标的→方向
        /// 所以原本用户想选择↑，还是会变成→，这是UGUI的设计
        /// 注意转换后的方向，仍旧是世界坐标的方向
        /// </summary>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// using UnityEngine;
        /// using System.Collections;
        /// using UnityEngine.UI; // required when using UI elements in scripts
        ///
        /// public class ExampleClass : MonoBehaviour
        /// {
        ///     public Button btnMain;
        ///
        ///     // Disables the selectable UI element directly to the left of the Start Button
        ///     public void IgnoreSelectables()
        ///     {
        ///         //Finds the selectable UI element to the left the start button and assigns it to a variable of type "Selectable"
        ///         Selectable secondButton = startButton.FindSelectableOnLeft();
        ///         //Disables interaction with the selectable UI element
        ///         secondButton.interactable = false;
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
        public virtual Selectable FindSelectableOnLeft()
        {
            if (m_Navigation.mode == Navigation.Mode.Explicit)
            {
                return m_Navigation.selectOnLeft;
            }
            if ((m_Navigation.mode & Navigation.Mode.Horizontal) != 0)
            {
                return FindSelectable(transform.rotation * Vector3.left);
            }
            return null;
        }

        /// <summary>
        /// Find the selectable object to the right of this one.
        /// 找到右方向的元素
        /// 注意自动导航是相对于UI元素本身的方向，而不是固定屏幕方向，所以虽然是用户按了↑键，但是如果UI本身有旋转，还是会把用户输入转换一下
        /// 比如用户输入了↑键，但是当前的UI元素右转了90°，那么会把这个用户输入转换一下，变成世界坐标的→方向
        /// 所以原本用户想选择↑，还是会变成→，这是UGUI的设计
        /// 注意转换后的方向，仍旧是世界坐标的方向
        /// </summary>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// using UnityEngine;
        /// using System.Collections;
        /// using UnityEngine.UI; // required when using UI elements in scripts
        ///
        /// public class ExampleClass : MonoBehaviour
        /// {
        ///     public Button btnMain;
        ///
        ///     // Disables the selectable UI element directly to the right the Start Button
        ///     public void IgnoreSelectables()
        ///     {
        ///         //Finds the selectable UI element to the right the start button and assigns it to a variable of type "Selectable"
        ///         Selectable secondButton = startButton.FindSelectableOnRight();
        ///         //Disables interaction with the selectable UI element
        ///         secondButton.interactable = false;
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
        public virtual Selectable FindSelectableOnRight()
        {
            if (m_Navigation.mode == Navigation.Mode.Explicit)
            {
                return m_Navigation.selectOnRight;
            }
            if ((m_Navigation.mode & Navigation.Mode.Horizontal) != 0)
            {
                return FindSelectable(transform.rotation * Vector3.right);
            }
            return null;
        }

        /// <summary>
        /// The Selectable object above current
        /// 找到上方向的元素
        /// 注意自动导航是相对于UI元素本身的方向，而不是固定屏幕方向，所以虽然是用户按了↑键，但是如果UI本身有旋转，还是会把用户输入转换一下
        /// 比如用户输入了↑键，但是当前的UI元素右转了90°，那么会把这个用户输入转换一下，变成世界坐标的→方向
        /// 所以原本用户想选择↑，还是会变成→，这是UGUI的设计
        /// 注意转换后的方向，仍旧是世界坐标的方向
        /// </summary>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// using UnityEngine;
        /// using System.Collections;
        /// using UnityEngine.UI; // required when using UI elements in scripts
        ///
        /// public class ExampleClass : MonoBehaviour
        /// {
        ///     public Button btnMain;
        ///
        ///     // Disables the selectable UI element directly above the Start Button
        ///     public void IgnoreSelectables()
        ///     {
        ///         //Finds the selectable UI element above the start button and assigns it to a variable of type "Selectable"
        ///         Selectable secondButton = startButton.FindSelectableOnUp();
        ///         //Disables interaction with the selectable UI element
        ///         secondButton.interactable = false;
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
        public virtual Selectable FindSelectableOnUp()
        {
            if (m_Navigation.mode == Navigation.Mode.Explicit)
            {
                return m_Navigation.selectOnUp;
            }
            if ((m_Navigation.mode & Navigation.Mode.Vertical) != 0)
            {
                return FindSelectable(transform.rotation * Vector3.up);
            }
            return null;
        }

        /// <summary>
        /// Find the selectable object below this one.
        /// 找到下方向的元素
        /// 注意自动导航是相对于UI元素本身的方向，而不是固定屏幕方向，所以虽然是用户按了↑键，但是如果UI本身有旋转，还是会把用户输入转换一下
        /// 比如用户输入了↑键，但是当前的UI元素右转了90°，那么会把这个用户输入转换一下，变成世界坐标的→方向
        /// 所以原本用户想选择↑，还是会变成→，这是UGUI的设计
        /// 注意转换后的方向，仍旧是世界坐标的方向
        /// </summary>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// using UnityEngine;
        /// using System.Collections;
        /// using UnityEngine.UI; // required when using UI elements in scripts
        ///
        /// public class Example : MonoBehaviour
        /// {
        ///     public Button startButton;
        ///
        ///     // Disables the selectable UI element directly below the Start Button
        ///     public void IgnoreSelectables()
        ///     {
        ///         //Finds the selectable UI element below the start button and assigns it to a variable of type "Selectable"
        ///         Selectable secondButton = startButton.FindSelectableOnDown();
        ///         //Disables interaction with the selectable UI element
        ///         secondButton.interactable = false;
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
        public virtual Selectable FindSelectableOnDown()
        {
            if (m_Navigation.mode == Navigation.Mode.Explicit)
            {
                return m_Navigation.selectOnDown;
            }
            if ((m_Navigation.mode & Navigation.Mode.Vertical) != 0)
            {
                return FindSelectable(transform.rotation * Vector3.down);
            }
            return null;
        }

        /// <summary>
        /// Determine in which of the 4 move directions the next selectable object should be found.
        /// 监听导航事件，决定下一个被选择的元素
        /// </summary>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// using UnityEngine;
        /// using System.Collections;
        /// using UnityEngine.UI;
        /// using UnityEngine.EventSystems;// Required when using Event data.
        ///
        /// public class ExampleClass : MonoBehaviour, IMoveHandler
        /// {
        ///     //When the focus moves to another selectable object, Invoke this Method.
        ///     public void OnMove(AxisEventData eventData)
        ///     {
        ///         //Assigns the move direction and the raw input vector representing the direction from the event data.
        ///         MoveDirection moveDir = eventData.moveDir;
        ///         Vector2 moveVector = eventData.moveVector;
        ///
        ///         //Displays the information in the console
        ///         Debug.Log(moveDir + ", " + moveVector);
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
        public virtual void OnMove(AxisEventData eventData)
        {
            switch (eventData.moveDir)
            {
                case MoveDirection.Right:
                    Navigate(eventData, FindSelectableOnRight());
                    break;

                case MoveDirection.Up:
                    Navigate(eventData, FindSelectableOnUp());
                    break;

                case MoveDirection.Left:
                    Navigate(eventData, FindSelectableOnLeft());
                    break;

                case MoveDirection.Down:
                    Navigate(eventData, FindSelectableOnDown());
                    break;
            }
        }

        /// <summary>
        /// 开始颜色转换动画
        /// 必须有Graphic组件在身上
        /// </summary>
        /// <param name="targetColor"></param>
        /// <param name="instant"></param>
        void StartColorTween(Color targetColor, bool instant)
        {
            if (m_TargetGraphic == null)
                return;

            m_TargetGraphic.CrossFadeColor(targetColor, instant ? 0f : m_Colors.fadeDuration, true, true);
        }

        /// <summary>
        /// 切换精灵，自身必须有Image组件
        /// 注意这里是切换了Image组件的overrideSprite，而不是直接切换Sprite，好处是可以随时回复原有图片
        /// </summary>
        /// <param name="newSprite"></param>
        void DoSpriteSwap(Sprite newSprite)
        {
            if (image == null)
                return;

            image.overrideSprite = newSprite;
        }

        /// <summary>
        /// 触发Animator中的动画变量
        /// 不一定生效，得有动画组件才行
        /// </summary>
        /// <param name="triggername"></param>
        void TriggerAnimation(string triggername)
        {
#if PACKAGE_ANIMATION
            if (transition != Transition.Animation || animator == null || !animator.isActiveAndEnabled || !animator.hasBoundPlayables || string.IsNullOrEmpty(triggername))
                return;

            animator.ResetTrigger(m_AnimationTriggers.normalTrigger);
            animator.ResetTrigger(m_AnimationTriggers.highlightedTrigger);
            animator.ResetTrigger(m_AnimationTriggers.pressedTrigger);
            animator.ResetTrigger(m_AnimationTriggers.selectedTrigger);
            animator.ResetTrigger(m_AnimationTriggers.disabledTrigger);

            animator.SetTrigger(triggername);
#endif
        }

        /// <summary>
        /// Returns whether the selectable is currently 'highlighted' or not.
        /// 是否是被选择的元素
        /// </summary>
        /// <remarks>
        /// Use this to check if the selectable UI element is currently highlighted.
        /// </remarks>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// //Create a UI element. To do this go to Create>UI and select from the list. Attach this script to the UI GameObject to see this script working. The script also works with non-UI elements, but highlighting works better with UI.
        ///
        /// using UnityEngine;
        /// using UnityEngine.Events;
        /// using UnityEngine.EventSystems;
        /// using UnityEngine.UI;
        ///
        /// //Use the Selectable class as a base class to access the IsHighlighted method
        /// public class Example : Selectable
        /// {
        ///     //Use this to check what Events are happening
        ///     BaseEventData m_BaseEvent;
        ///
        ///     void Update()
        ///     {
        ///         //Check if the GameObject is being highlighted
        ///         if (IsHighlighted())
        ///         {
        ///             //Output that the GameObject was highlighted, or do something else
        ///             Debug.Log("Selectable is Highlighted");
        ///         }
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
        protected bool IsHighlighted()
        {
            if (!IsActive() || !IsInteractable())
                return false;
            return isPointerInside && !isPointerDown && !hasSelection;
        }

        /// <summary>
        /// Whether the current selectable is being pressed.
        /// 是否被按下了
        /// </summary>
        protected bool IsPressed()
        {
            if (!IsActive() || !IsInteractable())
                return false;
            return isPointerDown;
        }

        // Change the button to the correct state
        //转换到指定的状态，必须可交互、并且有效
        private void EvaluateAndTransitionToSelectionState()
        {
            if (!IsActive() || !IsInteractable())
                return;

            DoStateTransition(currentSelectionState, false);
        }

        /// <summary>
        /// Evaluate current state and transition to pressed state.
        /// 监听按下事件，只关注左键按下
        /// </summary>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// using UnityEngine;
        /// using System.Collections;
        /// using UnityEngine.UI;
        /// using UnityEngine.EventSystems;// Required when using Event data.
        ///
        /// public class ExampleClass : MonoBehaviour, IPointerDownHandler// required interface when using the OnPointerDown method.
        /// {
        ///     //Do this when the mouse is clicked over the selectable object this script is attached to.
        ///     public void OnPointerDown(PointerEventData eventData)
        ///     {
        ///         Debug.Log(this.gameObject.name + " Was Clicked.");
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
        public virtual void OnPointerDown(PointerEventData eventData)
        {
            //只关注鼠标左键按下
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            //如果开启了导航，那么会设置当前选中的元素
            // Selection tracking
            if (IsInteractable() && navigation.mode != Navigation.Mode.None && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(gameObject, eventData);

            isPointerDown = true;
            EvaluateAndTransitionToSelectionState();
        }

        /// <summary>
        /// Evaluate eventData and transition to appropriate state.
        /// 监听弹起事件
        /// 只关注鼠标左键
        /// </summary>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// using UnityEngine;
        /// using System.Collections;
        /// using UnityEngine.UI;
        /// using UnityEngine.EventSystems;// Required when using Event data.
        ///
        /// public class ExampleClass : MonoBehaviour, IPointerUpHandler, IPointerDownHandler// These are the interfaces the OnPointerUp method requires.
        /// {
        ///     //OnPointerDown is also required to receive OnPointerUp callbacks
        ///     public void OnPointerDown(PointerEventData eventData)
        ///     {
        ///     }
        ///
        ///     //Do this when the mouse click on this selectable UI object is released.
        ///     public void OnPointerUp(PointerEventData eventData)
        ///     {
        ///         Debug.Log("The mouse click was released");
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
        public virtual void OnPointerUp(PointerEventData eventData)
        {
            //只关注鼠标左键
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            isPointerDown = false;
            EvaluateAndTransitionToSelectionState();
        }

        /// <summary>
        /// Evaluate current state and transition to appropriate state.
        /// New state could be pressed or hover depending on pressed state.
        /// 监听鼠标进入事件
        /// </summary>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// using UnityEngine;
        /// using System.Collections;
        /// using UnityEngine.UI;
        /// using UnityEngine.EventSystems;// Required when using Event data.
        ///
        /// public class ExampleClass : MonoBehaviour, IPointerEnterHandler// required interface when using the OnPointerEnter method.
        /// {
        ///     //Do this when the cursor enters the rect area of this selectable UI object.
        ///     public void OnPointerEnter(PointerEventData eventData)
        ///     {
        ///         Debug.Log("The cursor entered the selectable UI element.");
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
        public virtual void OnPointerEnter(PointerEventData eventData)
        {
            isPointerInside = true;
            EvaluateAndTransitionToSelectionState();
        }

        /// <summary>
        /// Evaluate current state and transition to normal state.
        /// 监听鼠标离开事件
        /// </summary>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// using UnityEngine;
        /// using System.Collections;
        /// using UnityEngine.UI;
        /// using UnityEngine.EventSystems;// Required when using Event data.
        ///
        /// public class ExampleClass : MonoBehaviour, IPointerExitHandler// required interface when using the OnPointerExit method.
        /// {
        ///     //Do this when the cursor exits the rect area of this selectable UI object.
        ///     public void OnPointerExit(PointerEventData eventData)
        ///     {
        ///         Debug.Log("The cursor exited the selectable UI element.");
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
        public virtual void OnPointerExit(PointerEventData eventData)
        {
            isPointerInside = false;
            EvaluateAndTransitionToSelectionState();
        }

        /// <summary>
        /// Set selection and transition to appropriate state.
        /// 监听选中事件
        /// </summary>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// using UnityEngine;
        /// using System.Collections;
        /// using UnityEngine.UI;
        /// using UnityEngine.EventSystems;// Required when using Event data.
        ///
        /// public class ExampleClass : MonoBehaviour, ISelectHandler// required interface when using the OnSelect method.
        /// {
        ///     //Do this when the selectable UI object is selected.
        ///     public void OnSelect(BaseEventData eventData)
        ///     {
        ///         Debug.Log(this.gameObject.name + " was selected");
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
        public virtual void OnSelect(BaseEventData eventData)
        {
            hasSelection = true;
            EvaluateAndTransitionToSelectionState();
        }

        /// <summary>
        /// Unset selection and transition to appropriate state.
        /// 监听取消选择事件
        /// </summary>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// using UnityEngine;
        /// using System.Collections;
        /// using UnityEngine.UI;
        /// using UnityEngine.EventSystems;// Required when using Event data.
        ///
        /// public class ExampleClass : MonoBehaviour, IDeselectHandler //This Interface is required to receive OnDeselect callbacks.
        /// {
        ///     public void OnDeselect(BaseEventData data)
        ///     {
        ///         Debug.Log("Deselected");
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
        public virtual void OnDeselect(BaseEventData eventData)
        {
            hasSelection = false;
            EvaluateAndTransitionToSelectionState();
        }

        /// <summary>
        /// Selects this Selectable.
        /// 选中当前元素，下拉菜单中使用
        /// </summary>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// using UnityEngine;
        /// using System.Collections;
        /// using UnityEngine.UI; // required when using UI elements in scripts
        /// using UnityEngine.EventSystems;// Required when using Event data.
        ///
        /// public class ExampleClass : MonoBehaviour// required interface when using the OnSelect method.
        /// {
        ///     public InputField myInputField;
        ///
        ///     //Do this OnClick.
        ///     public void SaveGame()
        ///     {
        ///         //Makes the Input Field the selected UI Element.
        ///         myInputField.Select();
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
        public virtual void Select()
        {
            if (EventSystem.current == null || EventSystem.current.alreadySelecting)
                return;

            EventSystem.current.SetSelectedGameObject(gameObject);
        }
    }
}
