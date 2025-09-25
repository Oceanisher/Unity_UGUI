using UnityEngine.EventSystems;

namespace UnityEngine.UI
{
    /// <summary>
    /// 多显示器工具类
    /// </summary>
    internal static class MultipleDisplayUtilities
    {
        /// <summary>
        /// Converts the current drag position into a relative position for the display.
        /// </summary>
        /// <param name="eventData"></param>
        /// <param name="position"></param>
        /// <returns>Returns true except when the drag operation is not on the same display as it originated</returns>
        public static bool GetRelativeMousePositionForDrag(PointerEventData eventData, ref Vector2 position)
        {
            #if UNITY_EDITOR
            position = eventData.position;
            #else
            int pressDisplayIndex = eventData.pointerPressRaycast.displayIndex;
            var relativePosition = RelativeMouseAtScaled(eventData.position, eventData.displayIndex);
            int currentDisplayIndex = (int)relativePosition.z;

            // Discard events on a different display.
            if (currentDisplayIndex != pressDisplayIndex)
                return false;

            // If we are not on the main display then we must use the relative position.
            position = pressDisplayIndex != 0 ? (Vector2)relativePosition : eventData.position;
            #endif
            return true;
        }

        /// <summary>
        /// 获取指针事件相对于Canvas的相对坐标
        /// Canvas是从左下角为(0,0)，Y向上X向右的相对坐标系
        /// 由于Canvas基本上与游戏渲染分辨率一致，所以相对坐标可以说是屏幕位置，但是要根据一些具体情况进行调整
        /// </summary>
        /// <param name="eventData"></param>
        /// <returns></returns>
        internal static Vector3 GetRelativeMousePositionForRaycast(PointerEventData eventData)
        {
            // The multiple display system is not supported on all platforms, when it is not supported the returned position
            // will be all zeros so when the returned index is 0 we will default to the event data to be safe.
            //根据是否支持多屏幕、是否全屏、是否分辨率一致等情况，获取计算后的鼠标事件位置
            Vector3 eventPosition = RelativeMouseAtScaled(eventData.position, eventData.displayIndex);
            //某些情况下返回0，那么此时直接使用原始位置
            if (eventPosition == Vector3.zero)
            {
                eventPosition = eventData.position;
#if UNITY_EDITOR
                eventPosition.z = Display.activeEditorGameViewTarget;
#endif
                // We don't really know in which display the event occurred. We will process the event assuming it occurred in our display.
            }

            // We support multiple display on some platforms. When supported:
            //  - InputSystem will set eventData.displayIndex
            //  - Old Input System will set eventPosition.z
            //
            // If the event is on the main display, both displayIndex and eventPosition.z
            // will be 0 so in that case we can leave the eventPosition untouched (see UUM-47650).
#if ENABLE_INPUT_SYSTEM && PACKAGE_INPUTSYSTEM
            if (eventData.displayIndex > 0)
                eventPosition.z = eventData.displayIndex;
#endif

            return eventPosition;
        }

        /// <summary>
        /// A version of Display.RelativeMouseAt that scales the position when the main display has a different rendering resolution to the system resolution.
        /// By default, the mouse position is relative to the main render area, we need to adjust this so it is relative to the system resolution
        /// in order to correctly determine the position on other displays.
        /// 获取指针事件在指定显示器上的位置
        /// 由于Display本身的分辨率systemWidth、systemHeight可能与真实渲染的分辨率不同，也就是renderingWidth、renderingHeight，所以需要有一些缩放处理
        /// 比如显示器是4K的，但是游戏用2K分辨率运行，此时就是2个分辨率
        /// 还有就是如果游戏和屏幕的宽高比不一致，那么实际游戏运行时会有黑边、相当于有个padding，那么此时也要重新计算鼠标的位置
        ///
        /// 返回值中的x/y代表屏幕空间中的像素坐标，z代表显示器序号displayIndex
        /// 由于canvas大小与游戏分辨率一致（Overlay模式下完全匹配，Camera模式下间接匹配、可能会有裁剪或者缩放），所以可以认为xy就是像素位置
        /// </summary>
        /// <returns></returns>
        public static Vector3 RelativeMouseAtScaled(Vector2 position, int displayIndex)
        {
            //非编辑器下，要根据情况计算坐标位置
            #if !UNITY_EDITOR && !UNITY_WSA
            //主显示器
            // If the main display is not the same resolution as the system then we need to scale the mouse position. (case 1141732)
            var display = Display.main;

#if ENABLE_INPUT_SYSTEM && PACKAGE_INPUTSYSTEM && UNITY_ANDROID
            // On Android with the new input system passed position are always relative to a surface and scaled accordingly to the rendering resolution.
            //安卓或者其他条件下，如果屏幕不是全屏，那么直接返回入参的位置
            display = Display.displays[displayIndex];
            if (!Screen.fullScreen)
            {
                // If not in fullscreen, assume UaaL multi-view multi-screen multi-touch scenario, where the position is already in the correct scaled coordinates for the displayIndex
                return new Vector3(position.x, position.y, displayIndex);
            }
            // in full screen, we need to account for some padding which may be added to the rendering area ? (Behavior unchanged for main display, untested for non-main displays)
#endif
            //如果游戏分辨率与显示器分辨率不同
            if (display.renderingWidth != display.systemWidth || display.renderingHeight != display.systemHeight)
            {
                // The system will add padding when in full-screen and using a non-native aspect ratio. (case UUM-7893)
                // For example Rendering 1920x1080 with a systeem resolution of 3440x1440 would create black bars on each side that are 330 pixels wide.
                // we need to account for this or it will offset our coordinates when we are not on the main display.
                //计算显示器分辨率宽高比
                var systemAspectRatio = display.systemWidth / (float)display.systemHeight;
                //全屏模式下把游戏渲染分辨率按照显示器的宽高比加黑边之后的完整渲染尺寸、包含黑边的
                //非全屏就是渲染区域大小，游戏分辨率下的大小
                var sizePlusPadding = new Vector2(display.renderingWidth, display.renderingHeight);
                //游戏渲染分辨率下的padding大小
                var padding = Vector2.zero;
                //如果是全屏，那么要根据实际游戏分辨率和屏幕分辨率的不同，在上下左右加黑边。这就是为什么有的游戏以某个分辨率运行时会有左右两边的黑色区域
                //加黑边的办法是，根据屏幕的宽高比、游戏设定的渲染宽高比，决定是往上下加黑边，还是左右加黑边
                if (Screen.fullScreen)
                {
                    //当前游戏分辨率的宽高比
                    var aspectRatio = Screen.width / (float)Screen.height; // This assumes aspectRatio is the same for all displays
                    //如果当前显示器的宽高比大于游戏宽高比，也就是说显示器更宽，那么需要在左右两侧加padding尺寸
                    if (display.systemHeight * aspectRatio < display.systemWidth)
                    {
                        //先计算以屏幕的宽高比拉伸的话，游戏渲染的宽应该是多少，也就是加上了两个黑边的宽
                        //水平两侧的padding大小，是(计算后的渲染宽度-原始游戏渲染分辨率的宽度)*0.5
                        // Horizontal padding
                        sizePlusPadding.x = display.renderingHeight * systemAspectRatio;
                        padding.x = (sizePlusPadding.x - display.renderingWidth) * 0.5f;
                    }
                    //如果宽高比一致、或者更小（也就是屏幕分辨率的高比游戏分辨率的高要大），那么就在上下两侧加入padding
                    else
                    {
                        //先计算以屏幕的宽高比拉伸的话，游戏渲染的高应该是多少，也就是加上了两个黑边的高
                        //垂直两侧的padding大小，是(计算后的渲染高度-原始游戏渲染分辨率的高度)*0.5
                        // Vertical padding
                        sizePlusPadding.y = display.renderingWidth / systemAspectRatio;
                        padding.y = (sizePlusPadding.y - display.renderingHeight) * 0.5f;
                    }
                }

                //这里是用计算出来的游戏渲染分辨率之后，减去单边的padding
                //之所以是只减去单边的padding，是为了后面计算鼠标是否在游戏渲染区域内部的时候方便计算
                var sizePlusPositivePadding = sizePlusPadding - padding;

                // If we are not inside of the main display then we must adjust the mouse position so it is scaled by
                // the main display and adjusted for any padding that may have been added due to different aspect ratios.
                //这里是为了计算鼠标是否在游戏渲染区域内部，因为传进来的鼠标位置是以Canvas为相对坐标的，所以如果鼠标在黑边区域内，那么就是不在Canvas里。
                //所以可能会有负值。那么如果鼠标坐标小于padding的负值、或者大于渲染区域+单边黑边，那么就是脱离游戏渲染区域了
                //如果鼠标脱离游戏渲染区域，那么需要将鼠标位置转换为屏幕分辨率位置，然后调用Display.RelativeMouseAt接口来获取相对位置
                //如果超出屏幕、比如进入第二块屏幕，那么Display.RelativeMouseAt会判断是否支持多屏幕，如果不支持，那么返回(0,0,0)
                //Unity默认只开启一台显示器，也就是说游戏只展示在一个屏幕上，如果要开启多屏幕显示，那么要使用Display.displays[i].Activate()来进行激活
                //脱离游戏渲染空间后，返回的坐标是屏幕空间下的坐标
                if (position.y < -padding.y || position.y > sizePlusPositivePadding.y ||
                     position.x < -padding.x || position.x > sizePlusPositivePadding.x)
                {
                    var adjustedPosition = position;

                    if (!Screen.fullScreen)
                    {
                        // When in windowed mode, the window will be centered with the 0,0 coordinate at the top left, we need to adjust so it is relative to the screen instead.
                        //窗口模式下，此时将Canvas空间下的鼠标坐标，转换为屏幕分辨率空间下的坐标
                        //这里的计算感觉有些问题，它是默认游戏窗口位于屏幕的中心位置，才能这么算，如果移动了游戏窗口，计算的值就不对了
                        //假如游戏窗口位于屏幕中心位置，那么转换为屏幕分辨率空间的位置，就是加上两者差值的一半即可
                        adjustedPosition.x -= (display.renderingWidth - display.systemWidth) * 0.5f;
                        adjustedPosition.y -= (display.renderingHeight - display.systemHeight) * 0.5f;
                    }
                    else
                    {
                        // Scale the mouse position to account for the black bars when in a non-native aspect ratio.
                        //全屏模式下，此时将Canvas空间下的鼠标坐标，转换为屏幕分辨率空间下的坐标
                        //原理是先把鼠标从Canvas空间转换到屏幕空间、也就是加上单边padding，然后根据百分比转换到屏幕分辨率空间下
                        adjustedPosition += padding;
                        adjustedPosition.x *= display.systemWidth / sizePlusPadding.x;
                        adjustedPosition.y *= display.systemHeight / sizePlusPadding.y;
                    }

#if ENABLE_INPUT_SYSTEM && PACKAGE_INPUTSYSTEM && (UNITY_STANDALONE_WIN || UNITY_ANDROID)
                    var relativePos = new Vector3(adjustedPosition.x, adjustedPosition.y, displayIndex);
#else
                    //将屏幕分辨率空间下的位置，传入Display.RelativeMouseAt中进行计算，得到相对位置
                    //如果鼠标与游戏不在同一块屏幕、并且游戏没有开启多屏幕支持，这里会返回(0,0,0)
                    var relativePos = Display.RelativeMouseAt(adjustedPosition);
#endif

                    // If we are not on the main display then return the adjusted position.
                    //如果没有开启多屏幕支持，那么即使在同一屏幕上，这里的z也会是0
                    if (relativePos.z != 0)
                        return relativePos;
                }

                // We are using the main display.
#if ENABLE_INPUT_SYSTEM && PACKAGE_INPUTSYSTEM && UNITY_ANDROID
                // On Android, in all cases, it is a surface associated to a given displayIndex, so we need to use the display index
                return new Vector3(position.x, position.y, displayIndex);
#else
                //默认返回原始位置
                return new Vector3(position.x, position.y, 0);
#endif
            }
#endif
#if ENABLE_INPUT_SYSTEM && PACKAGE_INPUTSYSTEM && (UNITY_STANDALONE_WIN || UNITY_ANDROID)
            return new Vector3(position.x, position.y, displayIndex);
#else
            return Display.RelativeMouseAt(position);
#endif
        }
    }
}
