using System;
using System.Collections.Generic;
using UnityEngine.UI.Collections;

namespace UnityEngine.UI
{
    /// <summary>
    /// Values of 'update' called on a Canvas update.
    /// </summary>
    /// <remarks> If modifying also modify m_CanvasUpdateProfilerStrings to match.</remarks>
    public enum CanvasUpdate
    {
        /// <summary>
        /// Called before layout.
        /// </summary>
        Prelayout = 0,
        /// <summary>
        /// Called for layout.
        /// </summary>
        Layout = 1,
        /// <summary>
        /// Called after layout.
        /// </summary>
        PostLayout = 2,
        /// <summary>
        /// Called before rendering.
        /// </summary>
        PreRender = 3,
        /// <summary>
        /// Called late, before render.
        /// </summary>
        LatePreRender = 4,
        /// <summary>
        /// Max enum value. Always last.
        /// </summary>
        MaxUpdateValue = 5
    }

    /// <summary>
    /// This is an element that can live on a Canvas.
    /// UI基础元素接口
    /// </summary>
    public interface ICanvasElement
    {
        /// <summary>
        /// Rebuild the element for the given stage.
        /// 元素重建接口，被 CanvasUpdateRegistry 的 PerformUpdate() 调用
        /// 入参是重建阶段
        /// </summary>
        /// <param name="executing">The current CanvasUpdate stage being rebuild.</param>
        void Rebuild(CanvasUpdate executing);

        /// <summary>
        /// Get the transform associated with the ICanvasElement.
        /// </summary>
        Transform transform { get; }

        /// <summary>
        /// Callback sent when this ICanvasElement has completed layout.
        /// 布局重建完成后调用
        /// </summary>
        void LayoutComplete();

        /// <summary>
        /// Callback sent when this ICanvasElement has completed Graphic rebuild.
        /// 图形重建完成后调用
        /// </summary>
        void GraphicUpdateComplete();

        /// <summary>
        /// Used if the native representation has been destroyed.
        /// </summary>
        /// <returns>Return true if the element is considered destroyed.</returns>
        bool IsDestroyed();
    }

    /// <summary>
    /// A place where CanvasElements can register themselves for rebuilding.
    /// UI元素更新注册中心-单例
    /// UI元素将自身注册到这里，然后从这里被调用重建
    /// </summary>
    public class CanvasUpdateRegistry
    {
        private static CanvasUpdateRegistry s_Instance;

        //是否在布局重建中
        private bool m_PerformingLayoutUpdate;
        //是否在图形重建中
        private bool m_PerformingGraphicUpdate;

        // This list matches the CanvasUpdate enum above. Keep in sync
        private string[] m_CanvasUpdateProfilerStrings = new string[] { "CanvasUpdate.Prelayout", "CanvasUpdate.Layout", "CanvasUpdate.PostLayout", "CanvasUpdate.PreRender", "CanvasUpdate.LatePreRender" };
        private const string m_CullingUpdateProfilerString = "ClipperRegistry.Cull";

        //布局重建队列
        private readonly IndexedSet<ICanvasElement> m_LayoutRebuildQueue = new IndexedSet<ICanvasElement>();
        //图形重建队列
        private readonly IndexedSet<ICanvasElement> m_GraphicRebuildQueue = new IndexedSet<ICanvasElement>();

        protected CanvasUpdateRegistry()
        {
            //Canvas渲染时调用，不开放源码
            Canvas.willRenderCanvases += PerformUpdate;
        }

        /// <summary>
        /// Get the singleton registry instance.
        /// </summary>
        public static CanvasUpdateRegistry instance
        {
            get
            {
                if (s_Instance == null)
                    s_Instance = new CanvasUpdateRegistry();
                return s_Instance;
            }
        }

        /// <summary>
        /// 判断元素是否能够更新重建
        /// 不能为null、必须是Unity的Object并且不是null
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        private bool ObjectValidForUpdate(ICanvasElement element)
        {
            var valid = element != null;

            var isUnityObject = element is Object;
            if (isUnityObject)
                valid = (element as Object) != null; //Here we make use of the overloaded UnityEngine.Object == null, that checks if the native object is alive.

            return valid;
        }

        /// <summary>
        /// 对布局、Graphic队列中的无效元素进行剔除或者跳过重建
        /// </summary>
        private void CleanInvalidItems()
        {
            // So MB's override the == operator for null equality, which checks
            // if they are destroyed. This is fine if you are looking at a concrete
            // mb, but in this case we are looking at a list of ICanvasElement
            // this won't forward the == operator to the MB, but just check if the
            // interface is null. IsDestroyed will return if the backend is destroyed.

            //布局重建队列处理
            var layoutRebuildQueueCount = m_LayoutRebuildQueue.Count;
            for (int i = layoutRebuildQueueCount - 1; i >= 0; --i)
            {
                var item = m_LayoutRebuildQueue[i];
                //删除不存在的
                if (item == null)
                {
                    m_LayoutRebuildQueue.RemoveAt(i);
                    continue;
                }

                //已经销毁的直接调用布局完成，也就是不经过布局处理
                //从队列中删除，因为队列中的元素会在PerformUpdate()中调用LayoutComplete()
                if (item.IsDestroyed())
                {
                    m_LayoutRebuildQueue.RemoveAt(i);
                    item.LayoutComplete();
                }
            }

            //Graphic重建队列处理
            var graphicRebuildQueueCount = m_GraphicRebuildQueue.Count;
            for (int i = graphicRebuildQueueCount - 1; i >= 0; --i)
            {
                var item = m_GraphicRebuildQueue[i];
                //删除不存在的
                if (item == null)
                {
                    m_GraphicRebuildQueue.RemoveAt(i);
                    continue;
                }
                //已经销毁的直接调用布局完成，也就是不经过Graphic绘制处理
                //从队列中删除，因为队列中的元素会在PerformUpdate()中调用GraphicUpdateComplete()
                if (item.IsDestroyed())
                {
                    m_GraphicRebuildQueue.RemoveAt(i);
                    item.GraphicUpdateComplete();
                }
            }
        }

        //按照元素的深度排序，越深越靠后
        private static readonly Comparison<ICanvasElement> s_SortLayoutFunction = SortLayoutList;
        
        /// <summary>
        /// Canvas.willRenderCanvases 渲染前调用
        /// </summary>
        private void PerformUpdate()
        {
            UISystemProfilerApi.BeginSample(UISystemProfilerApi.SampleType.Layout);
            //布局、Graphic队列整理
            CleanInvalidItems();

            m_PerformingLayoutUpdate = true;

            m_LayoutRebuildQueue.Sort(s_SortLayoutFunction);

            //布局重建
            for (int i = 0; i <= (int)CanvasUpdate.PostLayout; i++)
            {
                UnityEngine.Profiling.Profiler.BeginSample(m_CanvasUpdateProfilerStrings[i]);

                for (int j = 0; j < m_LayoutRebuildQueue.Count; j++)
                {
                    //待重建的元素
                    var rebuild = m_LayoutRebuildQueue[j];
                    try
                    {
                        if (ObjectValidForUpdate(rebuild))
                            rebuild.Rebuild((CanvasUpdate)i);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e, rebuild.transform);
                    }
                }
                UnityEngine.Profiling.Profiler.EndSample();
            }
            
            //布局重建完成之后，调用元素的布局重建完成接口
            for (int i = 0; i < m_LayoutRebuildQueue.Count; ++i)
                m_LayoutRebuildQueue[i].LayoutComplete();

            m_LayoutRebuildQueue.Clear();
            m_PerformingLayoutUpdate = false;
            UISystemProfilerApi.EndSample(UISystemProfilerApi.SampleType.Layout);
            UISystemProfilerApi.BeginSample(UISystemProfilerApi.SampleType.Render);

            //裁剪
            // now layout is complete do culling...
            UnityEngine.Profiling.Profiler.BeginSample(m_CullingUpdateProfilerString);
            ClipperRegistry.instance.Cull();
            UnityEngine.Profiling.Profiler.EndSample();

            m_PerformingGraphicUpdate = true;

            //图形重建
            for (var i = (int)CanvasUpdate.PreRender; i < (int)CanvasUpdate.MaxUpdateValue; i++)
            {
                UnityEngine.Profiling.Profiler.BeginSample(m_CanvasUpdateProfilerStrings[i]);
                for (var k = 0; k < m_GraphicRebuildQueue.Count; k++)
                {
                    try
                    {
                        var element = m_GraphicRebuildQueue[k];
                        if (ObjectValidForUpdate(element))
                            element.Rebuild((CanvasUpdate)i);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e, m_GraphicRebuildQueue[k].transform);
                    }
                }
                UnityEngine.Profiling.Profiler.EndSample();
            }

            //图形重建完成后，调用图形重建完成接口
            for (int i = 0; i < m_GraphicRebuildQueue.Count; ++i)
                m_GraphicRebuildQueue[i].GraphicUpdateComplete();

            m_GraphicRebuildQueue.Clear();
            m_PerformingGraphicUpdate = false;
            UISystemProfilerApi.EndSample(UISystemProfilerApi.SampleType.Render);
        }

        /// <summary>
        /// 计算的是父节点层级的多少
        /// 每有一层父节点，数字就+1
        /// </summary>
        /// <param name="child"></param>
        /// <returns></returns>
        private static int ParentCount(Transform child)
        {
            if (child == null)
                return 0;

            var parent = child.parent;
            int count = 0;
            while (parent != null)
            {
                count++;
                parent = parent.parent;
            }
            return count;
        }
        
        /// <summary>
        /// UI元素排序
        /// 层级越深（父节点越多），越靠后
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        private static int SortLayoutList(ICanvasElement x, ICanvasElement y)
        {
            Transform t1 = x.transform;
            Transform t2 = y.transform;

            return ParentCount(t1) - ParentCount(t2);
        }

        /// <summary>
        /// Try and add the given element to the layout rebuild list.
        /// Will not return if successfully added.
        /// 元素注册到布局重建队列
        /// </summary>
        /// <param name="element">The element that is needing rebuilt.</param>
        public static void RegisterCanvasElementForLayoutRebuild(ICanvasElement element)
        {
            instance.InternalRegisterCanvasElementForLayoutRebuild(element);
        }

        /// <summary>
        /// Try and add the given element to the layout rebuild list.
        /// 元素注册到布局重建队列
        /// </summary>
        /// <param name="element">The element that is needing rebuilt.</param>
        /// <returns>
        /// True if the element was successfully added to the rebuilt list.
        /// False if either already inside a Graphic Update loop OR has already been added to the list.
        /// </returns>
        public static bool TryRegisterCanvasElementForLayoutRebuild(ICanvasElement element)
        {
            return instance.InternalRegisterCanvasElementForLayoutRebuild(element);
        }

        /// <summary>
        /// 元素注册到布局重建队列
        /// 如果已经注册过，那么跳过
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        private bool InternalRegisterCanvasElementForLayoutRebuild(ICanvasElement element)
        {
            if (m_LayoutRebuildQueue.Contains(element))
                return false;

            /* TODO: this likely should be here but causes the error to show just resizing the game view (case 739376)
            if (m_PerformingLayoutUpdate)
            {
                Debug.LogError(string.Format("Trying to add {0} for layout rebuild while we are already inside a layout rebuild loop. This is not supported.", element));
                return false;
            }*/

            return m_LayoutRebuildQueue.AddUnique(element);
        }

        /// <summary>
        /// Try and add the given element to the rebuild list.
        /// Will not return if successfully added.
        /// 元素注册到图形重建队列
        /// </summary>
        /// <param name="element">The element that is needing rebuilt.</param>
        public static void RegisterCanvasElementForGraphicRebuild(ICanvasElement element)
        {
            instance.InternalRegisterCanvasElementForGraphicRebuild(element);
        }

        /// <summary>
        /// Try and add the given element to the rebuild list.
        /// 元素注册到图形重建队列
        /// </summary>
        /// <param name="element">The element that is needing rebuilt.</param>
        /// <returns>
        /// True if the element was successfully added to the rebuilt list.
        /// False if either already inside a Graphic Update loop OR has already been added to the list.
        /// </returns>
        public static bool TryRegisterCanvasElementForGraphicRebuild(ICanvasElement element)
        {
            return instance.InternalRegisterCanvasElementForGraphicRebuild(element);
        }

        /// <summary>
        /// 元素注册到图形重建队列
        /// 如果已经注册过，那么跳过
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        private bool InternalRegisterCanvasElementForGraphicRebuild(ICanvasElement element)
        {
            if (m_PerformingGraphicUpdate)
            {
                Debug.LogError(string.Format("Trying to add {0} for graphic rebuild while we are already inside a graphic rebuild loop. This is not supported.", element));
                return false;
            }

            return m_GraphicRebuildQueue.AddUnique(element);
        }

        /// <summary>
        /// Remove the given element from both the graphic and the layout rebuild lists.
        /// 元素从2个重建队列中移除
        /// </summary>
        /// <param name="element"></param>
        public static void UnRegisterCanvasElementForRebuild(ICanvasElement element)
        {
            instance.InternalUnRegisterCanvasElementForLayoutRebuild(element);
            instance.InternalUnRegisterCanvasElementForGraphicRebuild(element);
        }

        /// <summary>
        /// Disable the given element from both the graphic and the layout rebuild lists.
        /// 元素从2个重建队列中移除
        /// </summary>
        /// <param name="element"></param>
        public static void DisableCanvasElementForRebuild(ICanvasElement element)
        {
            instance.InternalDisableCanvasElementForLayoutRebuild(element);
            instance.InternalDisableCanvasElementForGraphicRebuild(element);
        }

        /// <summary>
        /// 元素从布局重建队列中移除
        /// 如果正在布局重建中，那么禁止移除，会报错
        /// 先调用元素的 LayoutComplete()，然后再从布局队列中移除
        /// </summary>
        /// <param name="element"></param>
        private void InternalUnRegisterCanvasElementForLayoutRebuild(ICanvasElement element)
        {
            if (m_PerformingLayoutUpdate)
            {
                Debug.LogError(string.Format("Trying to remove {0} from rebuild list while we are already inside a rebuild loop. This is not supported.", element));
                return;
            }

            element.LayoutComplete();
            instance.m_LayoutRebuildQueue.Remove(element);
        }

        /// <summary>
        /// 元素从图形重建队列中移除
        /// 如果正在图形重建中，那么禁止移除，会报错
        /// 先调用元素的 GraphicUpdateComplete()，然后再从图形队列中移除
        /// </summary>
        /// <param name="element"></param>
        private void InternalUnRegisterCanvasElementForGraphicRebuild(ICanvasElement element)
        {
            if (m_PerformingGraphicUpdate)
            {
                Debug.LogError(string.Format("Trying to remove {0} from rebuild list while we are already inside a rebuild loop. This is not supported.", element));
                return;
            }
            element.GraphicUpdateComplete();
            instance.m_GraphicRebuildQueue.Remove(element);
        }

        /// <summary>
        /// 元素被置为无效，被移动到布局队列末尾，但是不会被删除
        /// 移动前先调用布局重建完成
        /// </summary>
        /// <param name="element"></param>
        private void InternalDisableCanvasElementForLayoutRebuild(ICanvasElement element)
        {
            if (m_PerformingLayoutUpdate)
            {
                Debug.LogError(string.Format("Trying to remove {0} from rebuild list while we are already inside a rebuild loop. This is not supported.", element));
                return;
            }

            element.LayoutComplete();
            instance.m_LayoutRebuildQueue.DisableItem(element);
        }

        /// <summary>
        /// 元素被置为无效，被移动到图形队列末尾，但是不会被删除
        /// 移动前先调用图形重建完成
        /// </summary>
        /// <param name="element"></param>
        private void InternalDisableCanvasElementForGraphicRebuild(ICanvasElement element)
        {
            if (m_PerformingGraphicUpdate)
            {
                Debug.LogError(string.Format("Trying to remove {0} from rebuild list while we are already inside a rebuild loop. This is not supported.", element));
                return;
            }
            element.GraphicUpdateComplete();
            instance.m_GraphicRebuildQueue.DisableItem(element);
        }

        /// <summary>
        /// Are graphics layouts currently being calculated..
        /// 是否在布局重建中
        /// </summary>
        /// <returns>True if the rebuild loop is CanvasUpdate.Prelayout, CanvasUpdate.Layout or CanvasUpdate.Postlayout</returns>
        public static bool IsRebuildingLayout()
        {
            return instance.m_PerformingLayoutUpdate;
        }

        /// <summary>
        /// Are graphics currently being rebuild.
        /// 是否在图形重建中
        /// </summary>
        /// <returns>True if the rebuild loop is CanvasUpdate.PreRender or CanvasUpdate.Render</returns>
        public static bool IsRebuildingGraphics()
        {
            return instance.m_PerformingGraphicUpdate;
        }
    }
}
