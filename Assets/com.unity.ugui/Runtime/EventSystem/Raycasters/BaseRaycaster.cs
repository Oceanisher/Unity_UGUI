using System;
using System.Collections.Generic;

namespace UnityEngine.EventSystems
{
    /// <summary>
    /// Base class for any RayCaster.
    /// 射线基类
    ///
    /// 用于判断光标是否在某个元素上，默认有PhysicsRaycaster, Physics2DRaycaster, GraphicRaycaster(UI专用)
    /// </summary>
    /// <remarks>
    /// A Raycaster is responsible for raycasting against scene elements to determine if the cursor is over them. Default Raycasters include PhysicsRaycaster, Physics2DRaycaster, GraphicRaycaster.
    /// Custom raycasters can be added by extending this class.
    /// </remarks>
    public abstract class BaseRaycaster : UIBehaviour
    {
        private BaseRaycaster m_RootRaycaster;

        /// <summary>
        /// Raycast against the scene.
        /// 发出射线
        /// </summary>
        /// <param name="eventData">Current event data.</param>
        /// <param name="resultAppendList">List of hit Objects.</param>
        public abstract void Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList);

        /// <summary>
        /// The camera that will generate rays for this raycaster.
        /// 该射线使用的相机
        /// </summary>
        public abstract Camera eventCamera { get; }

        [Obsolete("Please use sortOrderPriority and renderOrderPriority", false)]
        public virtual int priority
        {
            get { return 0; }
        }

        /// <summary>
        /// Priority of the raycaster based upon sort order.
        /// 射线的优先级
        /// 数字越小、优先级越高
        /// 通常Graphic射线具有最高优先级、物理射线优先级较低
        /// </summary>
        public virtual int sortOrderPriority
        {
            get { return int.MinValue; }
        }

        /// <summary>
        /// Priority of the raycaster based upon render order.
        /// </summary>
        public virtual int renderOrderPriority
        {
            get { return int.MinValue; }
        }

        /// <summary>
        /// Raycaster on root canvas
        /// </summary>
        public BaseRaycaster rootRaycaster
        {
            get
            {
                if (m_RootRaycaster == null)
                {
                    var baseRaycasters = GetComponentsInParent<BaseRaycaster>();
                    if (baseRaycasters.Length != 0)
                        m_RootRaycaster = baseRaycasters[baseRaycasters.Length - 1];
                }

                return m_RootRaycaster;
            }
        }

        public override string ToString()
        {
            return "Name: " + gameObject + "\n" +
                "eventCamera: " + eventCamera + "\n" +
                "sortOrderPriority: " + sortOrderPriority + "\n" +
                "renderOrderPriority: " + renderOrderPriority;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            RaycasterManager.AddRaycaster(this);
        }

        protected override void OnDisable()
        {
            RaycasterManager.RemoveRaycasters(this);
            base.OnDisable();
        }

        protected override void OnCanvasHierarchyChanged()
        {
            base.OnCanvasHierarchyChanged();
            m_RootRaycaster = null;
        }

        protected override void OnTransformParentChanged()
        {
            base.OnTransformParentChanged();
            m_RootRaycaster = null;
        }
    }
}
