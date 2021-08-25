Shader "Unlit/timeOfDay"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
		_DayGradient ("Day Gradient", 2D) = "white" {}
		_Brightness ("Brightness", Float) = 1.0
		_Light ("Light color", Color) = (1,1,1,1)
		_Dark ("Dark color", Color) = (0,0,0,0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

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
			sampler2D _DayGradient;
            float4 _MainTex_ST;
			float _Brightness;
			fixed4 _Light;
			fixed4 _Dark;

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
                fixed4 col = tex2D(_MainTex, i.uv)*_Brightness;
				//fixed4 tint = lerp(_Light, _Dark,_Brightness);
				//col.rgb = fixed3(col.r*tint.r,col.g*tint.g,col.b*tint.b);
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
