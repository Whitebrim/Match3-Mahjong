Shader "UI/BiomeGradient"
{
    Properties
    {
        _TopColor1 ("Top Color 1", Color) = (1,1,1,1)
        _BottomColor1 ("Bottom Color 1", Color) = (0,0,0,1)
        _TopColor2 ("Top Color 2", Color) = (1,1,1,1)
        _BottomColor2 ("Bottom Color 2", Color) = (0,0,0,1)
        _BiomeBlend ("Biome Blend", Range(0,1)) = 0
        _EdgeDarkness ("Edge Darkness", Range(0,1)) = 0.3
        _EdgeSoftness ("Edge Softness", Range(0.01,1)) = 0.3
    }
    
    SubShader
    {
        Tags {"Queue"="Background" "RenderType"="Opaque" "PreviewType"="Plane"}
        LOD 100
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };
            
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };
            
            float4 _TopColor1;
            float4 _BottomColor1;
            float4 _TopColor2;
            float4 _BottomColor2;
            float _BiomeBlend;
            float _EdgeDarkness;
            float _EdgeSoftness;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                // Вертикальный градиент для первого биома
                float4 gradient1 = lerp(_BottomColor1, _TopColor1, i.uv.y);
                
                // Вертикальный градиент для второго биома
                float4 gradient2 = lerp(_BottomColor2, _TopColor2, i.uv.y);
                
                // Смешивание между биомами
                float4 col = lerp(gradient1, gradient2, _BiomeBlend);
                
                // Затемнение по краям (виньетка)
                float edgeFactor = 1.0;
                float distFromCenterX = abs(i.uv.x - 0.5) * 2.0;
                edgeFactor = 1.0 - smoothstep(1.0 - _EdgeSoftness, 1.0, distFromCenterX) * _EdgeDarkness;
                
                col.rgb *= edgeFactor;
                
                return col;
            }
            ENDCG
        }
    }
}