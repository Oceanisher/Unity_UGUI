using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.UI
{
    /// <summary>
    /// Utility class that is used to help with Text update.
    /// Text组件更新辅助类
    /// Unity在重建字体纹理Atlas时，会有一个回调调用到该类上，所以Text组件可以注册到该组件，用来监听字体Atlas的变更事件
    /// </summary>
    /// <remarks>
    /// When Unity rebuilds a font atlas a callback is sent to the font. Using this class you can register your text as needing to be rebuilt if the font atlas is updated.
    /// </remarks>
    public static class FontUpdateTracker
    {
        //每种字体维护一个字体与Text组件的字典，这样每次这种字体的Atlas变更时，都能找到对应的组件进行通知
        static Dictionary<Font, HashSet<Text>> m_Tracked = new Dictionary<Font, HashSet<Text>>();

        /// <summary>
        /// Register a Text element for receiving texture atlas rebuild calls.
        /// 注册一个Text组件，监听对应的Font Atlas更新
        /// </summary>
        /// <param name="t">The Text object to track</param>
        public static void TrackText(Text t)
        {
            if (t.font == null)
                return;

            HashSet<Text> exists;
            m_Tracked.TryGetValue(t.font, out exists);
            if (exists == null)
            {
                //字体Atlas重建是个全局的，每次有新的Font注册进来，都进行监听
                // The textureRebuilt event is global for all fonts, so we add our delegate the first time we register *any* Text
                if (m_Tracked.Count == 0)
                    Font.textureRebuilt += RebuildForFont;

                exists = new HashSet<Text>();
                m_Tracked.Add(t.font, exists);
            }
            exists.Add(t);
        }

        /// <summary>
        /// 监听字体Atlas重建后的回调方法
        /// 调用每个使用该字体的Text组件的FontTextureChanged方法
        /// </summary>
        /// <param name="f"></param>
        private static void RebuildForFont(Font f)
        {
            HashSet<Text> texts;
            m_Tracked.TryGetValue(f, out texts);

            if (texts == null)
                return;
            
            foreach (var text in texts)
                text.FontTextureChanged();
        }

        /// <summary>
        /// Deregister a Text element from receiving texture atlas rebuild calls.
        /// 取消监听
        /// </summary>
        /// <param name="t">The Text object to no longer track</param>
        public static void UntrackText(Text t)
        {
            if (t.font == null)
                return;

            HashSet<Text> texts;
            m_Tracked.TryGetValue(t.font, out texts);

            if (texts == null)
                return;

            texts.Remove(t);

            if (texts.Count == 0)
            {
                m_Tracked.Remove(t.font);

                //如果该字体已经没有Text使用了，那么取消委托监听
                // There is a global textureRebuilt event for all fonts, so once the last Text reference goes away, remove our delegate
                if (m_Tracked.Count == 0)
                    Font.textureRebuilt -= RebuildForFont;
            }
        }
    }
}
