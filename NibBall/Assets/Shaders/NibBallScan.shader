Shader "NibBall/Scan"
{
    // The "digital twin" look for the scene mesh: a faint tint plus a metre-grid drawn in world space on every triangle,
    // so lumps and bumps read as relief even though the mesh has no UVs. Works in Built-in RP on Quest (no geometry shader).
    Properties { _Color ("Line colour", Color) = (0.2, 0.9, 1, 0.9) _Fill ("Fill alpha", Float) = 0.10 _Cell ("Cell size (m)", Float) = 0.2 _Line ("Line width (0..0.5)", Float) = 0.03 }
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
            fixed4 _Color; float _Fill, _Cell, _Line;
            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; };
            struct v2f { float4 pos : SV_POSITION; float3 wp : TEXCOORD0; float3 wn : TEXCOORD1; };
            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.wp = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.wn = UnityObjectToWorldNormal(v.normal);
                return o;
            }
            float grid (float2 p)
            {
                float2 f = abs(frac(p / _Cell + 0.5) - 0.5) * _Cell;   // metres from the nearest grid line
                float2 w = fwidth(p);
                float2 l = 1.0 - smoothstep(_Line * 0.5, _Line * 0.5 + max(w, 0.002), f);
                return max(l.x, l.y);
            }
            fixed4 frag (v2f i) : SV_Target
            {
                float3 n = abs(normalize(i.wn));
                // grid on the plane the surface mostly faces: floors get X/Z, walls get the horizontal axis and Y
                float g = n.y > n.x && n.y > n.z ? grid(i.wp.xz) : (n.x > n.z ? grid(i.wp.zy) : grid(i.wp.xy));
                return fixed4(_Color.rgb, lerp(_Fill, _Color.a, g));
            }
            ENDCG
        }
    }
}
