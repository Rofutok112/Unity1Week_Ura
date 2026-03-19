Shader "UI/PolarityTransition"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Center ("Center", Vector) = (0.5, 0.5, 0, 0)
        _Radius ("Radius", Float) = 0
        _EdgeSoftness ("Edge Softness", Float) = 0.03
        _Aspect ("Aspect", Float) = 1
        _EffectEnabled ("Effect Enabled", Float) = 0
        _BaseInvert ("Base Invert", Float) = 0
        _TargetInvert ("Target Invert", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float2 _Center;
            float _Radius;
            float _EdgeSoftness;
            float _Aspect;
            float _EffectEnabled;
            float _BaseInvert;
            float _TargetInvert;

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 texcoord : TEXCOORD0;
            };

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 color = tex2D(_MainTex, i.texcoord);
                fixed3 baseColor = lerp(color.rgb, 1.0 - color.rgb, saturate(_BaseInvert));

                if (_EffectEnabled < 0.5)
                {
                    color.rgb = baseColor;
                    return color;
                }

                float2 delta = i.texcoord - _Center;
                delta.x *= _Aspect;
                float edge = max(_EdgeSoftness, 0.0001);
                float distanceToCenter = length(delta);
                float mask = 1.0 - smoothstep(_Radius - edge, _Radius, distanceToCenter);
                fixed3 targetColor = lerp(color.rgb, 1.0 - color.rgb, saturate(_TargetInvert));
                color.rgb = lerp(baseColor, targetColor, saturate(mask));
                return color;
            }
            ENDCG
        }
    }
}
