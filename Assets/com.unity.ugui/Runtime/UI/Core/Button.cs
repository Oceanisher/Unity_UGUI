using System;
using System.Collections;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

namespace UnityEngine.UI
{
    /// <summary>
    /// A standard button that sends an event when clicked.
    /// 按钮组件
    /// 比Selectable多实现了 IPointerClickHandler、ISubmitHandler
    /// </summary>
    [AddComponentMenu("UI/Button", 30)]
    public class Button : Selectable, IPointerClickHandler, ISubmitHandler
    {
        [Serializable]
        /// <summary>
        /// Function definition for a button click event.
        /// 点击事件触发
        /// </summary>
        public class ButtonClickedEvent : UnityEvent {}

        // Event delegates triggered on click.
        [FormerlySerializedAs("onClick")]
        [SerializeField]
        private ButtonClickedEvent m_OnClick = new ButtonClickedEvent();

        protected Button()
        {}

        /// <summary>
        /// UnityEvent that is triggered when the button is pressed.
        /// Note: Triggered on MouseUp after MouseDown on the same object.
        /// 点击事件处理
        /// </summary>
        ///<example>
        ///<code>
        /// <![CDATA[
        /// using UnityEngine;
        /// using UnityEngine.UI;
        /// using System.Collections;
        ///
        /// public class ClickExample : MonoBehaviour
        /// {
        ///     public Button yourButton;
        ///
        ///     void Start()
        ///     {
        ///         Button btn = yourButton.GetComponent<Button>();
        ///         btn.onClick.AddListener(TaskOnClick);
        ///     }
        ///
        ///     void TaskOnClick()
        ///     {
        ///         Debug.Log("You have clicked the button!");
        ///     }
        /// }
        /// ]]>
        ///</code>
        ///</example>
        public ButtonClickedEvent onClick
        {
            get { return m_OnClick; }
            set { m_OnClick = value; }
        }

        /// <summary>
        /// 处理点击事件
        /// 组件必须有效且可交互，会调用 m_OnClick 的所有监听器
        /// </summary>
        private void Press()
        {
            if (!IsActive() || !IsInteractable())
                return;

            UISystemProfilerApi.AddMarker("Button.onClick", this);
            m_OnClick.Invoke();
        }

        /// <summary>
        /// Call all registered IPointerClickHandlers.
        /// Register button presses using the IPointerClickHandler. You can also use it to tell what type of click happened (left, right etc.).
        /// Make sure your Scene has an EventSystem.
        /// 点击事件
        /// 只关注左键点击事件
        /// </summary>
        /// <param name="eventData">Pointer Data associated with the event. Typically by the event system.</param>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// //Attatch this script to a Button GameObject
        /// using UnityEngine;
        /// using UnityEngine.EventSystems;
        ///
        /// public class Example : MonoBehaviour, IPointerClickHandler
        /// {
        ///     //Detect if a click occurs
        ///     public void OnPointerClick(PointerEventData pointerEventData)
        ///     {
        ///             //Use this to tell when the user right-clicks on the Button
        ///         if (pointerEventData.button == PointerEventData.InputButton.Right)
        ///         {
        ///             //Output to console the clicked GameObject's name and the following message. You can replace this with your own actions for when clicking the GameObject.
        ///             Debug.Log(name + " Game Object Right Clicked!");
        ///         }
        ///
        ///         //Use this to tell when the user left-clicks on the Button
        ///         if (pointerEventData.button == PointerEventData.InputButton.Left)
        ///         {
        ///             Debug.Log(name + " Game Object Left Clicked!");
        ///         }
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>

        public virtual void OnPointerClick(PointerEventData eventData)
        {
            //只关注左键点击
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            //这里只处理了逻辑，不处理表现，是因为基类Selectable中已经处理了按键按下、按键弹起的表现，
            //所以点击事件这里只处理监听调用即可，表现可以交给父类
            Press();
        }

        /// <summary>
        /// Call all registered ISubmitHandler.
        /// 提交事件
        /// 就是通过提交按键触发的，比如假如定义了回车键是提交按键，那么就会触发这个
        /// 如果要修改提交按键，修改Edit->Project Settings->Input
        /// </summary>
        /// <param name="eventData">Associated data with the event. Typically by the event system.</param>
        /// <remarks>
        /// This detects when a Button has been selected via a "submit" key you specify (default is the return key).
        ///
        /// To change the submit key, either:
        ///
        /// 1. Go to Edit->Project Settings->Input.
        ///
        /// 2. Next, expand the Axes section and go to the Submit section if it exists.
        ///
        /// 3. If Submit doesn’t exist, add 1 number to the Size field. This creates a new section at the bottom. Expand the new section and change the Name field to “Submit”.
        ///
        /// 4. Change the Positive Button field to the key you want (e.g. space).
        ///
        ///
        /// Or:
        ///
        /// 1. Go to your EventSystem in your Project
        ///
        /// 2. Go to the Inspector window and change the Submit Button field to one of the sections in the Input Manager (e.g. "Submit"), or create your own by naming it what you like, then following the next few steps.
        ///
        /// 3. Go to Edit->Project Settings->Input to get to the Input Manager.
        ///
        /// 4. Expand the Axes section in the Inspector window. Add 1 to the number in the Size field. This creates a new section at the bottom.
        ///
        /// 5. Expand the new section and name it the same as the name you inserted in the Submit Button field in the EventSystem. Set the Positive Button field to the key you want (e.g. space)
        /// </remarks>

        public virtual void OnSubmit(BaseEventData eventData)
        {
            //按下了提交按键，直接会调用Press方法
            Press();

            // if we get set disabled during the press
            // don't run the coroutine.
            if (!IsActive() || !IsInteractable())
                return;
            
            //转换到按下状态
            //这里需要手动处理表现，是因为Submit事件是键盘输入单独触发，父类没有处理键盘的按下、弹起，所以需要手动调用
            DoStateTransition(SelectionState.Pressed, false);
            //按下结束后，恢复颜色
            StartCoroutine(OnFinishSubmit());
        }

        /// <summary>
        /// 按键结束后，奖状态
        /// TODO 这里设计有缺陷，这里只考虑了按钮使用颜色作为表现状态转换的方式，是不对的，还可能使用sprite、动画作为过渡方式
        /// </summary>
        /// <returns></returns>
        private IEnumerator OnFinishSubmit()
        {
            var fadeTime = colors.fadeDuration;
            var elapsedTime = 0f;

            while (elapsedTime < fadeTime)
            {
                elapsedTime += Time.unscaledDeltaTime;
                yield return null;
            }

            DoStateTransition(currentSelectionState, false);
        }
    }
}
