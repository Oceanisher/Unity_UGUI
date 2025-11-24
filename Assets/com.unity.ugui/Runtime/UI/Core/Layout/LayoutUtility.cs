using UnityEngine.Pool;

namespace UnityEngine.UI
{
    /// <summary>
    /// Utility functions for querying layout elements for their minimum, preferred, and flexible sizes.
    /// 布局工具类
    /// 主要是用来获取指定元素上 ILayoutElement 的对应属性的值
    /// </summary>
    public static class LayoutUtility
    {
        /// <summary>
        /// Returns the minimum size of the layout element.
        /// 获取元素最小尺寸
        /// 实际上是获取所有子元素的总最小尺寸
        /// </summary>
        /// <param name="rect">The RectTransform of the layout element to query.</param>
        /// <param name="axis">The axis to query. This can be 0 or 1.</param>
        /// <remarks>All components on the GameObject that implement the ILayoutElement are queried. The one with the highest priority which has a value for this setting is used. If multiple componets have this setting and have the same priority, the maximum value out of those is used.</remarks>
        public static float GetMinSize(RectTransform rect, int axis)
        {
            return axis == 0 ? GetMinWidth(rect) : GetMinHeight(rect);
        }

        /// <summary>
        /// Returns the preferred size of the layout element.
        /// 获取元素理想尺寸
        /// 实际上是获取所有子元素的总理想尺寸
        /// </summary>
        /// <param name="rect">The RectTransform of the layout element to query.</param>
        /// <param name="axis">The axis to query. This can be 0 or 1.</param>
        /// <remarks>
        /// All components on the GameObject that implement the ILayoutElement are queried. The one with the highest priority which has a value for this setting is used. If multiple componets have this setting and have the same priority, the maximum value out of those is used.
        /// </remarks>
        public static float GetPreferredSize(RectTransform rect, int axis)
        {
            return axis == 0 ? GetPreferredWidth(rect) : GetPreferredHeight(rect);
        }

        /// <summary>
        /// Returns the flexible size of the layout element.
        /// 获取元素灵活尺寸
        /// </summary>
        /// <remarks>
        /// All components on the GameObject that implement the ILayoutElement are queried. The one with the highest priority which has a value for this setting is used. If multiple componets have this setting and have the same priority, the maximum value out of those is used.
        /// </remarks>
        /// <param name="rect">The RectTransform of the layout element to query.</param>
        /// <param name="axis">The axis to query. This can be 0 or 1.</param>
        public static float GetFlexibleSize(RectTransform rect, int axis)
        {
            return axis == 0 ? GetFlexibleWidth(rect) : GetFlexibleHeight(rect);
        }

        /// <summary>
        /// Returns the minimum width of the layout element.
        /// 获取元素最小宽度，实际上是获取了所有子元素的总最小尺寸
        /// </summary>
        /// <param name="rect">The RectTransform of the layout element to query.</param>
        /// <remarks>
        /// All components on the GameObject that implement the ILayoutElement are queried. The one with the highest priority which has a value for this setting is used. If multiple componets have this setting and have the same priority, the maximum value out of those is used.
        /// </remarks>
        public static float GetMinWidth(RectTransform rect)
        {
            return GetLayoutProperty(rect, e => e.minWidth, 0);
        }

        /// <summary>
        /// Returns the preferred width of the layout element.
        /// 获取元素最佳宽度，实际上是获取了所有子元素的总最佳尺寸
        /// </summary>
        /// <param name="rect">The RectTransform of the layout element to query.</param>
        /// <returns>
        /// All components on the GameObject that implement the ILayoutElement are queried. The one with the highest priority which has a value for this setting is used. If multiple componets have this setting and have the same priority, the maximum value out of those is used.
        /// </returns>
        public static float GetPreferredWidth(RectTransform rect)
        {
            return Mathf.Max(GetLayoutProperty(rect, e => e.minWidth, 0), GetLayoutProperty(rect, e => e.preferredWidth, 0));
        }

        /// <summary>
        /// Returns the flexible width of the layout element.
        /// 获取元素灵活宽度，实际上是获取了所有子元素的总灵活尺寸
        /// </summary>
        /// <remarks>
        /// All components on the GameObject that implement the ILayoutElement are queried. The one with the highest priority which has a value for this setting is used. If multiple componets have this setting and have the same priority, the maximum value out of those is used
        /// </remarks>
        /// <param name="rect">The RectTransform of the layout element to query.</param>
        public static float GetFlexibleWidth(RectTransform rect)
        {
            return GetLayoutProperty(rect, e => e.flexibleWidth, 0);
        }

        /// <summary>
        /// Returns the minimum height of the layout element.
        /// 获取元素最小高度，实际上是获取了所有子元素的总最小尺寸
        /// </summary>
        /// <param name="rect">The RectTransform of the layout element to query.</param>
        /// <remarks>
        /// All components on the GameObject that implement the ILayoutElement are queried. The one with the highest priority which has a value for this setting is used. If multiple componets have this setting and have the same priority, the maximum value out of those is used.
        /// </remarks>
        public static float GetMinHeight(RectTransform rect)
        {
            return GetLayoutProperty(rect, e => e.minHeight, 0);
        }

        /// <summary>
        /// Returns the preferred height of the layout element.
        /// 获取元素最佳高度，实际上是获取了所有子元素的总最佳尺寸
        /// </summary>
        /// <param name="rect">The RectTransform of the layout element to query.</param>
        /// <remarks>
        /// All components on the GameObject that implement the ILayoutElement are queried. The one with the highest priority which has a value for this setting is used. If multiple componets have this setting and have the same priority, the maximum value out of those is used.
        /// </remarks>
        public static float GetPreferredHeight(RectTransform rect)
        {
            return Mathf.Max(GetLayoutProperty(rect, e => e.minHeight, 0), GetLayoutProperty(rect, e => e.preferredHeight, 0));
        }

        /// <summary>
        /// Returns the flexible height of the layout element.
        /// 获取元素灵活高度，实际上是获取了所有子元素的总灵活尺寸
        /// </summary>
        /// <remarks>
        /// All components on the GameObject that implement the ILayoutElement are queried. The one with the highest priority which has a value for this setting is used. If multiple componets have this setting and have the same priority, the maximum value out of those is used.
        /// </remarks>
        /// <param name="rect">The RectTransform of the layout element to query.</param>
        public static float GetFlexibleHeight(RectTransform rect)
        {
            return GetLayoutProperty(rect, e => e.flexibleHeight, 0);
        }

        /// <summary>
        /// Gets a calculated layout property for the layout element with the given RectTransform.
        /// 获取元素指定的变量数值
        /// </summary>
        /// <param name="rect">The RectTransform of the layout element to get a property for.</param>
        /// <param name="property">The property to calculate.</param>
        /// <param name="defaultValue">The default value to use if no component on the layout element supplies the given property</param>
        /// <returns>The calculated value of the layout property.</returns>
        public static float GetLayoutProperty(RectTransform rect, System.Func<ILayoutElement, float> property, float defaultValue)
        {
            ILayoutElement dummy;
            return GetLayoutProperty(rect, property, defaultValue, out dummy);
        }

        /// <summary>
        /// Gets a calculated layout property for the layout element with the given RectTransform.
        /// 获取元素指定的变量数值
        ///
        /// 1.从自己本元素上获取所有实现了 ILayoutElement 的组件
        /// 2.遍历所有的 ILayoutElement，选择优先级更大的 ILayoutElement、或者值更大的
        /// 3.对于Layout来说，它自己的ILayoutElement组件就是Layout布局组件，而布局组件
        ///
        /// 也就是说，如果组件有多个 ILayoutElement，那么取值最大的那个；
        /// 如果没有 ILayoutElement，那么返回传入的默认值；目前传入的默认值都是0
        /// </summary>
        /// <param name="rect">The RectTransform of the layout element to get a property for.</param>
        /// <param name="property">The property to calculate.</param>
        /// <param name="defaultValue">The default value to use if no component on the layout element supplies the given property</param>
        /// <param name="source">Optional out parameter to get the component that supplied the calculated value.</param>
        /// <returns>The calculated value of the layout property.</returns>
        public static float GetLayoutProperty(RectTransform rect, System.Func<ILayoutElement, float> property, float defaultValue, out ILayoutElement source)
        {
            source = null;
            if (rect == null)
                return 0;
            float min = defaultValue;
            int maxPriority = System.Int32.MinValue;
            var components = ListPool<Component>.Get();
            rect.GetComponents(typeof(ILayoutElement), components);

            var componentsCount = components.Count;
            for (int i = 0; i < componentsCount; i++)
            {
                var layoutComp = components[i] as ILayoutElement;
                if (layoutComp is Behaviour && !((Behaviour)layoutComp).isActiveAndEnabled)
                    continue;

                int priority = layoutComp.layoutPriority;
                // If this layout components has lower priority than a previously used, ignore it.
                if (priority < maxPriority)
                    continue;
                float prop = property(layoutComp);
                // If this layout property is set to a negative value, it means it should be ignored.
                if (prop < 0)
                    continue;

                // If this layout component has higher priority than all previous ones,
                // overwrite with this one's value.
                if (priority > maxPriority)
                {
                    min = prop;
                    maxPriority = priority;
                    source = layoutComp;
                }
                // If the layout component has the same priority as a previously used,
                // use the largest of the values with the same priority.
                else if (prop > min)
                {
                    min = prop;
                    source = layoutComp;
                }
            }

            ListPool<Component>.Release(components);
            return min;
        }
    }
}
