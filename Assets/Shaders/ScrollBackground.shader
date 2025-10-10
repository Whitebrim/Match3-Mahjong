Shader "UI/ScrollBackground"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _ColorTop ("Top Color", Color) = (0.2, 0.3, 0.5, 1)
        _ColorBottom ("Bottom Color", Color) = (0.1, 0.15, 0.25, 1)
        _VignetteIntensity ("Vignette Intensity", Range(0, 1)) = 0.3
        _VignetteSoftness ("Vignette Softness", Range(0, 1)) = 0.5
        
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                float4 screenPos : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _ColorTop;
            fixed4 _ColorBottom;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float _VignetteIntensity;
            float _VignetteSoftness;

            // Функция для генерации псевдослучайного шума (дизеринг)
            float rand(float2 co)
            {
                return frac(sin(dot(co.xy, float2(12.9898, 78.233))) * 43758.5453);
            }

            // Улучшенная функция дизеринга
            float dither(float2 screenPos)
            {
                float noise = rand(screenPos);
                return (noise - 0.5) / 255.0; // Нормализуем для 8-bit цвета
            }

            // Smooth gradient функция (использует smoothstep для еще более плавного перехода)
            float3 smoothGradient(float3 colorA, float3 colorB, float t)
            {
                // Применяем smoothstep для устранения линейности
                float smoothT = smoothstep(0.0, 1.0, t);
                return lerp(colorA, colorB, smoothT);
            }

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.screenPos = ComputeScreenPos(OUT.vertex);
                OUT.texcoord = v.texcoord;
                OUT.color = v.color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                // Вычисляем screen-space координаты для дизеринга
                float2 screenUV = IN.screenPos.xy / IN.screenPos.w;
                screenUV *= _ScreenParams.xy;

                // Создаем плавный вертикальный градиент с дизерингом
                float gradientT = IN.texcoord.y;
                
                // Добавляем дизеринг для устранения полос
                float ditherValue = dither(screenUV);
                gradientT += ditherValue;
                gradientT = saturate(gradientT);

                // Используем улучшенную функцию градиента
                float3 gradientColor = smoothGradient(_ColorBottom.rgb, _ColorTop.rgb, gradientT);

                // Вычисляем виньетирование по горизонтали и внизу
                float2 uv = IN.texcoord;
                
                // Расстояние от центра по горизонтали (0 в центре, 1 на краях)
                float horizontalDist = abs(uv.x - 0.5) * 2.0;
                
                // Расстояние снизу (0 внизу, 1 вверху) - умножаем на 2 чтобы эффект был в 2 раза меньше
                float bottomDist = uv.y * 2.0;
                
                // Создаем плавное виньетирование с улучшенной формулой
                float vignetteEdge = 1.0 - _VignetteSoftness * 0.8;
                
                // Горизонтальное виньетирование (слева и справа)
                float vignetteHorizontal = 1.0 - smoothstep(vignetteEdge, 1.0, horizontalDist);
                vignetteHorizontal = pow(vignetteHorizontal, 2.0);
                
                // Вертикальное виньетирование (только снизу)
                float vignetteBottom = smoothstep(0.0, _VignetteSoftness * 0.5, bottomDist);
                vignetteBottom = pow(vignetteBottom, 1.5);
                
                // Комбинируем горизонтальное и вертикальное виньетирование
                float vignette = vignetteHorizontal * vignetteBottom;
                
                // Интерполируем между затемненным и незатемненным состоянием
                vignette = lerp(1.0, vignette, _VignetteIntensity);
                
                // Применяем виньетирование к градиенту
                float3 finalColor = gradientColor * vignette;
                
                // Добавляем небольшой дополнительный дизеринг к финальному цвету
                finalColor += ditherValue * 0.5;
                
                // Создаем финальный цвет с альфа-каналом
                fixed4 result = fixed4(finalColor, 1.0);
                
                // Применяем цвет вершины
                result *= IN.color;
                
                // Отсечение для UI
                result.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                
                #ifdef UNITY_UI_CLIP_RECT
                clip(result.a - 0.001);
                #endif

                return result;
            }
            ENDCG
        }
    }
}