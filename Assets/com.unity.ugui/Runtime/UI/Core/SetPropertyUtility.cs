using System;
using System.Collections.Generic;
using UnityEngine.Events;

namespace UnityEngine.UI
{
    /// <summary>
    /// Property设置工具类
    /// </summary>
    internal static class SetPropertyUtility
    {
        /// <summary>
        /// 设置颜色
        /// 有变更才会返回True
        /// </summary>
        /// <param name="currentValue"></param>
        /// <param name="newValue"></param>
        /// <returns></returns>
        public static bool SetColor(ref Color currentValue, Color newValue)
        {
            if (currentValue.r == newValue.r && currentValue.g == newValue.g && currentValue.b == newValue.b && currentValue.a == newValue.a)
                return false;

            currentValue = newValue;
            return true;
        }

        /// <summary>
        /// 设置ref变量
        /// 主要是用于返回新老值是否一样，便于后面的调用
        /// </summary>
        /// <param name="currentValue"></param>
        /// <param name="newValue"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static bool SetStruct<T>(ref T currentValue, T newValue) where T : struct
        {
            //使用 EqualityComparer 进行判断
            //当T是引用类型，会使用Object.Equals()进行判断
            //当T是值类型，会使用EqualityComparer.Default进行判断
            if (EqualityComparer<T>.Default.Equals(currentValue, newValue))
                return false;

            currentValue = newValue;
            return true;
        }

        public static bool SetClass<T>(ref T currentValue, T newValue) where T : class
        {
            if ((currentValue == null && newValue == null) || (currentValue != null && currentValue.Equals(newValue)))
                return false;

            currentValue = newValue;
            return true;
        }
    }
}
