Shader "Effects/TraceSlider"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
		_ScrollOffset ("Scroll Offset", Float) = 0 
		_MyST ("Scale Translate", Vector) = (1,1,0,0)
		_Directions ("Blur Directions", Float) = 16.0
		_Quality ("Blur Quality", Float) = 3.0
		_Size ("Blur Size", Float) = 8.0
		_Pi ("2 Pi", Float) = 6.28318530718
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always
		Blend SrcAlpha OneMinusSrcAlpha

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
			fixed4 _MyST;
			fixed _Size;
			fixed _Quality;
			fixed _Directions;
			fixed _Pi;

            fixed4 frag (v2f i) : SV_Target
            {
				fixed2 coords = fixed2(i.uv.x*_MyST.x-_MyST.z,i.uv.y*_MyST.y-_MyST.w);
				//I think this controls the sweep line width
				clip(coords.x+0.3);
				clip(5-coords.x);
				clip(.5 - abs(coords.y-.5));
				//first we gotta see what it looks like with data
				fixed4 col = tex2D(_MainTex,coords);
				//gaussian blur
				/*
				fixed2 radius = _Size/_ScreenParams.xy;
				for(fixed d=0.0; d<_Pi; d+=_Pi/_Directions){
					for(float j=1.0/_Quality; j<=1.0; j+=1.0/_Quality){
						col+=tex2D(_MainTex, i.uv+fixed2(cos(d),sin(d))*radius*j);
					}
				}
				col /= _Quality *_Directions - 15.0;
				*/
				col.a=1;
				fixed rightEdge = step(1,coords.x);
                col = lerp(col,fixed4(0,0,0,1),rightEdge);
		
                return col;
            }
            ENDCG
        }
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
			fixed _ScrollOffset;

            fixed4 frag (v2f i) : SV_Target
            {
				//offset uv coords and re-sample
				fixed2 coords = fixed2(i.uv.x+_ScrollOffset,i.uv.y);
				//cut off any coords with u>1 or u<0
				fixed edge = step(coords.x,1);
				//edge*=step(0,coords.x);
				fixed4 col = (1-edge)*fixed4(0,0,0,1)+edge*tex2D(_MainTex,coords);
				return col;
            }
            ENDCG
        }
    }
}
