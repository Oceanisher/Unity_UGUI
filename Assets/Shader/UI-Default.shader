// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)
//Unity默认Shader
Shader "UI/Default"
{
    Properties
    {
        //主纹理
        //PerRendererData代表会使用MaterialPropertyBlock进行设置该变量，而不是修改材质
        //这样防止修改了材质导致每个UI一个材质，从而打断合批
        //使用 MaterialPropertyBlock 设置该值，不会修改材质，多个UI共享一个材质，从而不会打断合并
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        //主颜色
        _Color ("Tint", Color) = (1,1,1,1)

        //模板比较函数，默认总是通过
        _StencilComp ("Stencil Comparison", Float) = 8
        //模板参考值
        _Stencil ("Stencil ID", Float) = 0
        //模板测试通过后的操作
        _StencilOp ("Stencil Operation", Float) = 0
        //模板写入掩码
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        //模板读取掩码
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        //颜色通道写入掩码(RGBA)
        _ColorMask ("Color Mask", Float) = 15

        //是否使用UI的Alpha裁切，默认关闭
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane" //材质预览中为平面模式
            "CanUseSpriteAtlas"="True" //可使用图集
        }

        //模板缓冲区状态设置，常用于UI遮罩、裁剪等效果
        Stencil
        {
            Ref [_Stencil] //设置模板参考值为_Stencil的值
            Comp [_StencilComp]//设置模板比较函数为_StencilComp
            Pass [_StencilOp]//设置模板检测通过后的执行操作为_StencilOp
            ReadMask [_StencilReadMask]//设置模板读取掩码
            WriteMask [_StencilWriteMask]//设置模板写入掩码
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend One OneMinusSrcAlpha //混合模式是预乘以透明度
        ColorMask [_ColorMask] //颜色通道掩码，控制哪些颜色通道会被写入渲染目标

        Pass
        {
            Name "Default"
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            //启用多编译指令，用于生成处理不同UI功能的着色器变体（如矩形裁切等）
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT //RectMask矩形裁切
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP //透明度裁切

            //所以UGUI默认只需要顶点位置、颜色、第一套UV信息
            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID //支持GPU Instance渲染，更多的是为了shader的通用性，UGUI并不使用GPU Instance
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord  : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                float4  mask : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO //VR中立体渲染使用
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;//内置着色器变量，用于处理UI元素的纹理采样
            float4 _ClipRect;//裁切区域，4个变量分别代表左下右上的边界，如果为0，那么就是不裁切
            float4 _MainTex_ST;//主纹理的缩放和位移
            float _UIMaskSoftnessX;//Mask的边缘柔和-X轴
            float _UIMaskSoftnessY;//Mask的边缘柔和-Y轴
            int _UIVertexColorAlwaysGammaSpace;//是否在Gamma空间下进行颜色操作

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);//开启GPU Instance渲染，更多的是为了shader的通用性，UGUI并不使用GPU Instance
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT); //VR中立体渲染使用
                float4 vPosition = UnityObjectToClipPos(v.vertex);
                OUT.worldPosition = v.vertex;
                OUT.vertex = vPosition;

                //w代表深度值
                float2 pixelSize = vPosition.w;
                //_ScreenParams.xy是屏幕分辨率信息，使用mul将其转换到投影空间
                //pixelSize代表屏幕空间单位到像素的转换因子，代表在屏幕空间中，一个单位对应多少像素
                //用来确保在不同分辨率下，Mask的边缘柔和表现正确
                pixelSize /= float2(1, 1) * abs(mul((float2x2)UNITY_MATRIX_P, _ScreenParams.xy));

                float4 clampedRect = clamp(_ClipRect, -2e10, 2e10);
                float2 maskUV = (v.vertex.xy - clampedRect.xy) / (clampedRect.zw - clampedRect.xy);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord.xy, _MainTex);
                OUT.mask = float4(v.vertex.xy * 2 - clampedRect.xy - clampedRect.zw, 0.25 / (0.25 * half2(_UIMaskSoftnessX, _UIMaskSoftnessY) + abs(pixelSize.xy)));


                if (_UIVertexColorAlwaysGammaSpace)
                {
                    if(!IsGammaSpace())
                    {
                        v.color.rgb = UIGammaToLinear(v.color.rgb);
                    }
                }

                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                //Round up the alpha color coming from the interpolator (to 1.0/256.0 steps)
                //The incoming alpha could have numerical instability, which makes it very sensible to
                //HDR color transparency blend, when it blends with the world's texture.
                const half alphaPrecision = half(0xff);
                const half invAlphaPrecision = half(1.0/alphaPrecision);
                IN.color.a = round(IN.color.a * alphaPrecision)*invAlphaPrecision;

                half4 color = IN.color * (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd);

                //如果开启了矩形裁切，那么区域之外的像素的透明通道会被置为0
                #ifdef UNITY_UI_CLIP_RECT
                //saturate是将值限制的0~1内
                half2 m = saturate((_ClipRect.zw - _ClipRect.xy - abs(IN.mask.xy)) * IN.mask.zw);
                color.a *= m.x * m.y;
                #endif

                //如果开启了alpha裁切，那么透明度小于0.001的都要剔除掉
                #ifdef UNITY_UI_ALPHACLIP
                clip (color.a - 0.001);
                #endif

                //混合模式是Blend One OneMinusSrcAlpha，预乘以透明度的意思，所以这里的RGB需要乘以它的透明度，确保混合正确
                color.rgb *= color.a;

                return color;
            }
        ENDCG
        }
    }
}
