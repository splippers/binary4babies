Shader "NibBall/Overlay"
{
    Properties { _MainTex ("Font Texture", 2D) = "white" {} _Color ("Color", Color) = (1,1,1,1) _AlphaOnly ("Alpha only (font atlas)", Float) = 0 }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex; float4 _MainTex_ST; fixed4 _Color; float _AlphaOnly;
            struct appdata { float4 vertex : POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; };
            v2f vert (appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.color = v.color * _Color; o.uv = TRANSFORM_TEX(v.uv, _MainTex); return o; }
            fixed4 frag (v2f i) : SV_Target { fixed4 t = tex2D(_MainTex, i.uv); return lerp(i.color * t, fixed4(i.color.rgb, i.color.a * t.a), _AlphaOnly); }
            ENDCG
        }
    }
}
