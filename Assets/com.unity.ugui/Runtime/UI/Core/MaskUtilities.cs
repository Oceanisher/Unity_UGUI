using System.Collections.Generic;
using UnityEngine.Pool;

namespace UnityEngine.UI
{
    /// <summary>
    /// Mask related utility class. This class provides masking-specific utility functions.
    /// </summary>
    public class MaskUtilities
    {
        /// <summary>
        /// Notify all IClippables under the given component that they need to recalculate clipping.
        /// 通知在某个Mask下的所有可裁剪的元素，进行裁剪计算
        /// </summary>
        /// <param name="mask">The object thats changed for whose children should be notified.</param>
        public static void Notify2DMaskStateChanged(Component mask)
        {
            var components = ListPool<Component>.Get();
            mask.GetComponentsInChildren(components);
            for (var i = 0; i < components.Count; i++)
            {
                if (components[i] == null || components[i].gameObject == mask.gameObject)
                    continue;

                var toNotify = components[i] as IClippable;
                if (toNotify != null)
                    toNotify.RecalculateClipping();
            }
            ListPool<Component>.Release(components);
        }

        /// <summary>
        /// Notify all IMaskable under the given component that they need to recalculate masking.
        /// </summary>
        /// <param name="mask">The object thats changed for whose children should be notified.</param>
        public static void NotifyStencilStateChanged(Component mask)
        {
            var components = ListPool<Component>.Get();
            mask.GetComponentsInChildren(components);
            for (var i = 0; i < components.Count; i++)
            {
                if (components[i] == null || components[i].gameObject == mask.gameObject)
                    continue;

                var toNotify = components[i] as IMaskable;
                if (toNotify != null)
                    toNotify.RecalculateMasking();
            }
            ListPool<Component>.Release(components);
        }

        /// <summary>
        /// Find a root Canvas.
        /// 往上找，找到最根节点的Canvas、或者第一个重写了排序的Canvas
        /// </summary>
        /// <param name="start">Transform to start the search at going up the hierarchy.</param>
        /// <returns>Finds either the most root canvas, or the first canvas that overrides sorting.</returns>
        public static Transform FindRootSortOverrideCanvas(Transform start)
        {
            var canvasList = ListPool<Canvas>.Get();
            start.GetComponentsInParent(false, canvasList);
            Canvas canvas = null;

            for (int i = 0; i < canvasList.Count; ++i)
            {
                canvas = canvasList[i];

                // We found the canvas we want to use break
                if (canvas.overrideSorting)
                    break;
            }
            ListPool<Canvas>.Release(canvasList);

            return canvas != null ? canvas.transform : null;
        }

        /// <summary>
        /// Find the stencil depth for a given element.
        /// 计算元素模板深度值
        /// 给一个Trs，然后往上进行遍历、找到每一个有Mask组件的元素，一直到指定的Trs停止
        /// 每找到一个Mask，那么入参的Trs的模板深度值+1
        /// 也就是说，按照从上往下的顺序，每个Mask的深度+1，同层的Mask深度值一致
        /// </summary>
        /// <param name="transform">The starting transform to search.</param>
        /// <param name="stopAfter">Where the search of parents should stop</param>
        /// <returns>What the proper stencil buffer index should be.</returns>
        public static int GetStencilDepth(Transform transform, Transform stopAfter)
        {
            var depth = 0;
            if (transform == stopAfter)
                return depth;

            var t = transform.parent;
            var components = ListPool<Mask>.Get();
            while (t != null)
            {
                t.GetComponents<Mask>(components);
                for (var i = 0; i < components.Count; ++i)
                {
                    if (components[i] != null && components[i].MaskEnabled() && components[i].graphic.IsActive())
                    {
                        ++depth;
                        break;
                    }
                }

                if (t == stopAfter)
                    break;

                t = t.parent;
            }
            ListPool<Mask>.Release(components);
            return depth;
        }

        /// <summary>
        /// Helper function to determine if the child is a descendant of father or is father.
        /// 判断某个B是否是A的子节点、或者B=A
        /// </summary>
        /// <param name="father">The transform to compare against.</param>
        /// <param name="child">The starting transform to search up the hierarchy.</param>
        /// <returns>Is child equal to father or is a descendant.</returns>
        public static bool IsDescendantOrSelf(Transform father, Transform child)
        {
            if (father == null || child == null)
                return false;

            if (father == child)
                return true;

            while (child.parent != null)
            {
                if (child.parent == father)
                    return true;

                child = child.parent;
            }

            return false;
        }

        /// <summary>
        /// Find the correct RectMask2D for a given IClippable.
        /// 对于一个给定的有IClippable的元素，往上遍历寻找某个带有RectMask2D的父节点
        /// </summary>
        /// <param name="clippable">Clippable to search from.</param>
        /// <returns>The Correct RectMask2D</returns>
        public static RectMask2D GetRectMaskForClippable(IClippable clippable)
        {
            List<RectMask2D> rectMaskComponents = ListPool<RectMask2D>.Get();
            List<Canvas> canvasComponents = ListPool<Canvas>.Get();
            RectMask2D componentToReturn = null;

            //找到所有带有RectMask2D的父节点
            clippable.gameObject.GetComponentsInParent(false, rectMaskComponents);

            if (rectMaskComponents.Count > 0)
            {
                for (int rmi = 0; rmi < rectMaskComponents.Count; rmi++)
                {
                    componentToReturn = rectMaskComponents[rmi];
                    //父节点不能是入参的节点
                    if (componentToReturn.gameObject == clippable.gameObject)
                    {
                        componentToReturn = null;
                        continue;
                    }
                    if (!componentToReturn.isActiveAndEnabled)
                    {
                        componentToReturn = null;
                        continue;
                    }
                    //找到所有带有Canvas父节点
                    clippable.gameObject.GetComponentsInParent(false, canvasComponents);
                    for (int i = canvasComponents.Count - 1; i >= 0; i--)
                    {
                        //如果找到的有RectMask2D的父节点，跟入参的节点的不在同一个Canvas下，并且该Canvas是一个重写了排序的Canvas，那么直接跳出
                        if (!IsDescendantOrSelf(canvasComponents[i].transform, componentToReturn.transform) && canvasComponents[i].overrideSorting)
                        {
                            componentToReturn = null;
                            break;
                        }
                    }
                    break;
                }
            }

            ListPool<RectMask2D>.Release(rectMaskComponents);
            ListPool<Canvas>.Release(canvasComponents);

            return componentToReturn;
        }

        /// <summary>
        /// Search for all RectMask2D that apply to the given RectMask2D (includes self).
        /// 找到RectMask2D的所有上级RectMask2D，包含它自己
        /// </summary>
        /// <param name="clipper">Starting clipping object.</param>
        /// <param name="masks">The list of Rect masks</param>
        public static void GetRectMasksForClip(RectMask2D clipper, List<RectMask2D> masks)
        {
            masks.Clear();

            List<Canvas> canvasComponents = ListPool<Canvas>.Get();
            List<RectMask2D> rectMaskComponents = ListPool<RectMask2D>.Get();
            clipper.transform.GetComponentsInParent(false, rectMaskComponents);

            if (rectMaskComponents.Count > 0)
            {
                clipper.transform.GetComponentsInParent(false, canvasComponents);
                for (int i = rectMaskComponents.Count - 1; i >= 0; i--)
                {
                    if (!rectMaskComponents[i].IsActive())
                        continue;
                    bool shouldAdd = true;
                    for (int j = canvasComponents.Count - 1; j >= 0; j--)
                    {
                        if (!IsDescendantOrSelf(canvasComponents[j].transform, rectMaskComponents[i].transform) && canvasComponents[j].overrideSorting)
                        {
                            shouldAdd = false;
                            break;
                        }
                    }
                    if (shouldAdd)
                        masks.Add(rectMaskComponents[i]);
                }
            }

            ListPool<RectMask2D>.Release(rectMaskComponents);
            ListPool<Canvas>.Release(canvasComponents);
        }
    }
}
