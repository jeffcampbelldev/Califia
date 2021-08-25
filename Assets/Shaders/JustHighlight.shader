Shader "Custom/JustHighlight"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
		_RimColor ("Rim Color", Color) = (1,1,1,1)
		_OutlineThickness ("Outline thickness", Float) = 0.1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

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
				fixed4 _RimColor;

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
					return _RimColor;
				}

				ENDCG
			}
    }
}
