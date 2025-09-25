using System.Collections;
using UnityEngine.Events;

namespace UnityEngine.UI.CoroutineTween
{
    // Base interface for tweeners,
    // using an interface instead of
    // an abstract class as we want the
    // tweens to be structs.
    //Tween结构体所实现的接口
    //使用接口而不是抽象类，这样Tween就可以用结构体了
    internal interface ITweenValue
    {
        /// <summary>
        /// Tween百分比
        /// 传入一个百分比，然后对应的Tween就播放到对应的进入，通常是每帧调用
        /// </summary>
        /// <param name="floatPercentage"></param>
        void TweenValue(float floatPercentage);
        /// <summary>
        /// 是否忽略时间缩放
        /// </summary>
        bool ignoreTimeScale { get; }
        /// <summary>
        /// 持续时间
        /// </summary>
        float duration { get; }
        /// <summary>
        /// 动画目标是否有效
        /// </summary>
        /// <returns></returns>
        bool ValidTarget();
    }

    // Color tween class, receives the
    // TweenValue callback and then sets
    // the value on the target.
    //颜色Tween动画数据
    internal struct ColorTween : ITweenValue
    {
        /// <summary>
        /// 颜色动画控制方式
        /// </summary>
        public enum ColorTweenMode
        {
            All,//全部控制
            RGB,//只控制RGB
            Alpha //控制透明通道
        }

        //颜色Tween动画回调
        public class ColorTweenCallback : UnityEvent<Color> {}
        //颜色Tween动画回调
        private ColorTweenCallback m_Target;
        //动画起始颜色
        private Color m_StartColor;
        //动画目标颜色
        private Color m_TargetColor;
        //颜色控制模式
        private ColorTweenMode m_TweenMode;
        //动画时长
        private float m_Duration;
        //是否忽略时间缩放
        private bool m_IgnoreTimeScale;

        //动画起始颜色
        public Color startColor
        {
            get { return m_StartColor; }
            set { m_StartColor = value; }
        }

        //动画目标颜色
        public Color targetColor
        {
            get { return m_TargetColor; }
            set { m_TargetColor = value; }
        }

        //颜色控制模式
        public ColorTweenMode tweenMode
        {
            get { return m_TweenMode; }
            set { m_TweenMode = value; }
        }

        //动画时长
        public float duration
        {
            get { return m_Duration; }
            set { m_Duration = value; }
        }

        //是否忽略时间缩放
        public bool ignoreTimeScale
        {
            get { return m_IgnoreTimeScale; }
            set { m_IgnoreTimeScale = value; }
        }

        /// <summary>
        /// 动画处理
        /// 这里是每帧被调用一次，传入一个进度百分比
        /// 使用这个百分比，在初始颜色、目标颜色之间lerp一下，就能得到当前帧应该的颜色
        /// </summary>
        /// <param name="floatPercentage"></param>
        public void TweenValue(float floatPercentage)
        {
            if (!ValidTarget())
                return;

            var newColor = Color.Lerp(m_StartColor, m_TargetColor, floatPercentage);

            if (m_TweenMode == ColorTweenMode.Alpha)
            {
                newColor.r = m_StartColor.r;
                newColor.g = m_StartColor.g;
                newColor.b = m_StartColor.b;
            }
            else if (m_TweenMode == ColorTweenMode.RGB)
            {
                newColor.a = m_StartColor.a;
            }
            m_Target.Invoke(newColor);
        }

        /// <summary>
        /// 设置颜色变更回调
        /// </summary>
        /// <param name="callback"></param>
        public void AddOnChangedCallback(UnityAction<Color> callback)
        {
            if (m_Target == null)
                m_Target = new ColorTweenCallback();

            m_Target.AddListener(callback);
        }

        public bool GetIgnoreTimescale()
        {
            return m_IgnoreTimeScale;
        }

        public float GetDuration()
        {
            return m_Duration;
        }

        public bool ValidTarget()
        {
            return m_Target != null;
        }
    }

    // Float tween class, receives the
    // TweenValue callback and then sets
    // the value on the target.
    /// <summary>
    /// Float值变更动画
    /// 同颜色变更动画
    /// </summary>
    internal struct FloatTween : ITweenValue
    {
        public class FloatTweenCallback : UnityEvent<float> {}

        private FloatTweenCallback m_Target;
        private float m_StartValue;
        private float m_TargetValue;

        private float m_Duration;
        private bool m_IgnoreTimeScale;

        public float startValue
        {
            get { return m_StartValue; }
            set { m_StartValue = value; }
        }

        public float targetValue
        {
            get { return m_TargetValue; }
            set { m_TargetValue = value; }
        }

        public float duration
        {
            get { return m_Duration; }
            set { m_Duration = value; }
        }

        public bool ignoreTimeScale
        {
            get { return m_IgnoreTimeScale; }
            set { m_IgnoreTimeScale = value; }
        }

        public void TweenValue(float floatPercentage)
        {
            if (!ValidTarget())
                return;

            var newValue = Mathf.Lerp(m_StartValue, m_TargetValue, floatPercentage);
            m_Target.Invoke(newValue);
        }

        public void AddOnChangedCallback(UnityAction<float> callback)
        {
            if (m_Target == null)
                m_Target = new FloatTweenCallback();

            m_Target.AddListener(callback);
        }

        public bool GetIgnoreTimescale()
        {
            return m_IgnoreTimeScale;
        }

        public float GetDuration()
        {
            return m_Duration;
        }

        public bool ValidTarget()
        {
            return m_Target != null;
        }
    }

    // Tween runner, executes the given tween.
    // The coroutine will live within the given
    // behaviour container.
    //执行动画
    //使用协程方式进行动画播放，协程会使用给定的Mono进行
    internal class TweenRunner<T> where T : struct, ITweenValue
    {
        //跑协程的Mono，一般是哪个UI要动画，这里就是哪个UI的组件
        protected MonoBehaviour m_CoroutineContainer;
        //动画协程
        protected IEnumerator m_Tween;

        // utility function for starting the tween
        private static IEnumerator Start(T tweenInfo)
        {
            //如果目标不再有效，那么终止协程
            if (!tweenInfo.ValidTarget())
                yield break;

            var elapsedTime = 0.0f;
            while (elapsedTime < tweenInfo.duration)
            {
                elapsedTime += tweenInfo.ignoreTimeScale ? Time.unscaledDeltaTime : Time.deltaTime;
                var percentage = Mathf.Clamp01(elapsedTime / tweenInfo.duration);
                //每帧调用动画
                tweenInfo.TweenValue(percentage);
                yield return null;
            }
            //超过时长了，直接把动画拉到100%，并停止
            tweenInfo.TweenValue(1.0f);
        }

        public void Init(MonoBehaviour coroutineContainer)
        {
            m_CoroutineContainer = coroutineContainer;
        }

        /// <summary>
        /// 开始动画协程
        /// 参数是个Tween的结构体
        /// </summary>
        /// <param name="info"></param>
        public void StartTween(T info)
        {
            if (m_CoroutineContainer == null)
            {
                Debug.LogWarning("Coroutine container not configured... did you forget to call Init?");
                return;
            }

            //先停止原先的动画
            StopTween();

            //如果跑协程的Mono未激活，那么直接把动画拉到100%进度
            if (!m_CoroutineContainer.gameObject.activeInHierarchy)
            {
                info.TweenValue(1.0f);
                return;
            }

            //开始协程
            m_Tween = Start(info);
            m_CoroutineContainer.StartCoroutine(m_Tween);
        }

        /// <summary>
        /// 终止协程
        /// 动画不会恢复到最初形态
        /// </summary>
        public void StopTween()
        {
            if (m_Tween != null)
            {
                m_CoroutineContainer.StopCoroutine(m_Tween);
                m_Tween = null;
            }
        }
    }
}
