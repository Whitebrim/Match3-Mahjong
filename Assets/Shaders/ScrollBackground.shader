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
            #pragma target 2.0

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
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _ColorTop;
            fixed4 _ColorBottom;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float _VignetteIntensity;
            float _VignetteSoftness;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = v.texcoord;
                OUT.color = v.color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                // Вертикальный градиент
                fixed4 gradientColor = lerp(_ColorBottom, _ColorTop, IN.texcoord.y);

                // Вычисляем виньетирование по бокам
                float2 center = float2(0.5, 0.5);
                float2 uv = IN.texcoord - center;
                
                // Горизонтальное затемнение (только по X)
                float horizontalDist = abs(uv.x) * 2.0;
                
                // Используем smoothstep для плавного перехода
                float vignetteStart = 1.0 - _VignetteSoftness;
                float vignette = 1.0 - smoothstep(vignetteStart, 1.0, horizontalDist);
                
                // Применяем интенсивность виньетирования
                vignette = lerp(1.0, vignette, _VignetteIntensity);
                
                // Комбинируем градиент с виньетированием
                fixed4 finalColor = gradientColor;
                finalColor.rgb *= vignette;
                
                // Применяем цвет вершины и альфу
                finalColor *= IN.color;
                
                // Отсечение для UI
                finalColor.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                
                // Убираем полностью прозрачные пиксели
                #ifdef UNITY_UI_CLIP_RECT
                clip(finalColor.a - 0.001);
                #endif

                return finalColor;
            }
            ENDCG
        }
    }
}