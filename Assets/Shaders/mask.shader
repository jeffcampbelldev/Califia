Shader "Unlit/mask"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
		_Tint ("Tint", Color) = (1.0,0.0,0.0,1.0)
		_ScrollPos ("Scroll Position", Float) = 0.5
		_ScrollWidth ("Scroll Multiplier", Float) = 2
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
		Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
			fixed _ScrollPos;
			fixed _ScrollWidth;
			fixed4 _Tint;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv)*_Tint;
				//fixed4 mask = tex2D(_MaskTex, i.uv);
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
				fixed ds = abs(i.uv.x-_ScrollPos);
				ds*=_ScrollWidth;
				col.a *= 1-ds;
				return col;
            }
            ENDCG
        }
    }
}
