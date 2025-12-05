namespace UnityEngine.EventSystems
{
    /// <summary>
    /// Base behaviour that has protected implementations of Unity lifecycle functions.
    /// UI的Mono类
    /// MonoBehaviour的子类
    /// </summary>
    public abstract class UIBehaviour : MonoBehaviour
    {
        protected virtual void Awake()
        {}

        protected virtual void OnEnable()
        {}

        protected virtual void Start()
        {}

        protected virtual void OnDisable()
        {}

        protected virtual void OnDestroy()
        {}

        /// <summary>
        /// Returns true if the GameObject and the Component are active.
        /// </summary>
        public virtual bool IsActive()
        {
            return isActiveAndEnabled;
        }

#if UNITY_EDITOR
        /// <summary>
        /// 每次组件在Inspector中发生值变化时会调用
        /// </summary>
        protected virtual void OnValidate()
        {}

        /// <summary>
        /// 首次添加组件、或者Inspector点击Reset的时候调用
        /// </summary>
        protected virtual void Reset()
        {}
#endif
        /// <summary>
        /// This callback is called when the dimensions of an associated RectTransform change. It is always called before Awake, OnEnable, or Start. The call is also made to all child RectTransforms, regardless of whether their dimensions change (which depends on how they are anchored).
        /// RectTransform 尺寸发生改变时调用
        /// </summary>
        protected virtual void OnRectTransformDimensionsChange()
        {}

        /// <summary>
        /// 父节点发生变更前调用
        /// </summary>
        protected virtual void OnBeforeTransformParentChanged()
        {}

        /// <summary>
        /// 父节点发生变更后调用
        /// </summary>
        protected virtual void OnTransformParentChanged()
        {}

        /// <summary>
        /// 当UI的动画属性应用时调用
        /// </summary>
        protected virtual void OnDidApplyAnimationProperties()
        {}

        /// <summary>
        /// CanvasGroup发生变更时调用
        /// </summary>
        protected virtual void OnCanvasGroupChanged()
        {}

        /// <summary>
        /// Called when the state of the parent Canvas is changed.
        /// 父Canvas变更时调用，比如Canvas禁用等
        /// </summary>
        protected virtual void OnCanvasHierarchyChanged()
        {}

        /// <summary>
        /// Returns true if the native representation of the behaviour has been destroyed.
        /// </summary>
        /// <remarks>
        /// When a parent canvas is either enabled, disabled or a nested canvas's OverrideSorting is changed this function is called. You can for example use this to modify objects below a canvas that may depend on a parent canvas - for example, if a canvas is disabled you may want to halt some processing of a UI element.
        /// </remarks>
        public bool IsDestroyed()
        {
            // Workaround for Unity native side of the object
            // having been destroyed but accessing via interface
            // won't call the overloaded ==
            return this == null;
        }
    }
}
