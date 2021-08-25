Shader "Effects/Blur"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
		_Bounds ("Bounds", Vector) = (0,0,1,0.5)
		_Directions ("Blur Directions", Float) = 16.0
		_Quality ("Blur Quality", Float) = 3.0
		_Size ("Blur Size", Float) = 8.0
		_Pi ("2 Pi", Float) = 6.28318530718
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

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

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
			fixed4 _Bounds;
			fixed _Directions;
			fixed _Quality;
			fixed _Size;
			fixed _Pi;

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 baseCol = tex2D(_MainTex, i.uv);
                // just invert the colors
				fixed blur=step(i.uv.y,_Bounds.w);
				blur *= step(_Bounds.y,i.uv.y);
				blur *= step(_Bounds.x, i.uv.x);
				blur *= step(i.uv.x,_Bounds.z);
				fixed4 col=baseCol;
				fixed2 radius = _Size/_ScreenParams.xy;
				for(fixed d=0.0; d<_Pi; d+=_Pi/_Directions){
					for(float j=1.0/_Quality; j<=1.0; j+=1.0/_Quality){
						col+=tex2D(_MainTex, i.uv+fixed2(cos(d),sin(d))*radius*j);
					}
				}
				col /= _Quality *_Directions - 15.0;
                col.rgb = blur*(col.rgb)+(1-blur)*baseCol.rgb;
                return col;
            }
            ENDCG
        }
    }
}
