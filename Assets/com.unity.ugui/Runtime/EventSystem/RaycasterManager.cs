using System.Collections.Generic;

namespace UnityEngine.EventSystems
{
    /// <summary>
    /// 射线管理器
    /// 用于管理挂在EventSystem、摄像机上的不同类型的射线发射类，射线发射类组件会自动注册、注销自己
    /// </summary>
    public static class RaycasterManager
    {
        private static readonly List<BaseRaycaster> s_Raycasters = new List<BaseRaycaster>();

        /// <summary>
        /// 射线发射组件在OnEnable()时自己注册进来
        /// </summary>
        /// <param name="baseRaycaster"></param>
        internal static void AddRaycaster(BaseRaycaster baseRaycaster)
        {
            if (s_Raycasters.Contains(baseRaycaster))
                return;

            s_Raycasters.Add(baseRaycaster);
        }

        /// <summary>
        /// List of BaseRaycasters that has been registered.
        /// 正在激活状态的射线发射组件列表
        /// 目前只被EventSystem调用，无论是什么类型的射线发射器，都是EventSystem来每帧调用
        /// </summary>
        public static List<BaseRaycaster> GetRaycasters()
        {
            return s_Raycasters;
        }

        /// <summary>
        /// 射线发射组件在OnDisable()时自己注销
        /// </summary>
        /// <param name="baseRaycaster"></param>
        internal static void RemoveRaycasters(BaseRaycaster baseRaycaster)
        {
            if (!s_Raycasters.Contains(baseRaycaster))
                return;
            s_Raycasters.Remove(baseRaycaster);
        }
    }
}
