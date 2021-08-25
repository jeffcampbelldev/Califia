Shader "Custom/CHtube"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _FlowTex ("Flow Texture (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
		_BloodColor ("Blood Color", Color) = (1,1,1,1)
		_BloodColorB ("Blood Color B", Color) = (1,1,1,1)
		_RimTube ("Rim Tube", Float) = 2
		_BloodFill ("Blood flow", Range(0,1)) = 0
		_BloodFill2 ("Blood flow (second)", Range(0,1)) = 1
		_HighlightCol ("Highlight Color", Color) = (1,1,1,1)
		_RimPower ("Highlight power", Float) = 2	
		_OutlineThickness ("Outline thickness", Float) = 0.1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry"}
        LOD 200

		Stencil {
			Ref 2
			Comp Always
			Pass Replace
		}

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows 

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _FlowTex;

        struct Input
        {
            float2 uv_MainTex;
            float2 uv_FlowTex;
		fixed3 viewDir;
		fixed3 worldNormal;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
	fixed4 _BloodColor;
	fixed4 _BloodColorB;
	fixed _RimTube;
	fixed _BloodFill;
	fixed _BloodFill2;
	fixed4 _HighlightCol;
	fixed _RimPower;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
			fixed rimRaw= abs(dot(IN.viewDir,IN.worldNormal));
			fixed rim = pow(rimRaw,_RimTube);
			rim *= step(IN.uv_MainTex.y,_BloodFill);
			rim += step(_BloodFill2,IN.uv_MainTex.y);
			rim = min(rim,1);
			fixed3 mainCol=lerp(_BloodColorB,tex2D(_FlowTex,IN.uv_FlowTex)*_BloodColor,rim);
			fixed label = step(_Color.a,IN.uv_MainTex.x);
			label+=step(_Color.a*.5,abs(.5-IN.uv_MainTex.y));
			//combines both end label and strip label
			label = step(.9,label);
			fixed3 baseCol = lerp(mainCol,_Color.rgb,label);
			fixed hRim = pow(rimRaw,_RimPower);
			o.Albedo = lerp(_HighlightCol,baseCol,hRim); 
			// Metallic and smoothness come from slider variables
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;
			label+=rim;
			label = step(.9,label);
				o.Alpha = lerp(_Color.a,1,label);
        }
        ENDCG
			Pass{
				Cull Front
				Blend SrcAlpha OneMinusSrcAlpha
				CGPROGRAM

				#include "UnityCG.cginc"
				#pragma vertex vert
				#pragma fragment frag

				struct appdata{
					float4 vertex : POSITION;
					float3 normal : NORMAL;
				};

				struct v2f{
					float4 position : SV_POSITION;
				};

				fixed _OutlineThickness;

				v2f vert(appdata v){
					v2f o;
					float3 normal = normalize(v.normal);
					float3 outlineOffset = normal*_OutlineThickness;
					float3 position = v.vertex+outlineOffset;
					o.position = UnityObjectToClipPos(position);
					return o;
				}
				fixed4 frag(v2f i) : SV_TARGET{
					clip(_OutlineThickness-0.0001);
					return fixed4(0,1,1,1);
				}

				ENDCG
			}
    }
    FallBack "Diffuse"
}
