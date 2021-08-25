Shader "Custom/yFade"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
		_XBounds ("X bounds", Vector) = (0,0,0,0)
		_YBounds ("Y bounds", Vector) = (0,0,0,0)
		_ZBounds ("Z bounds", Vector) = (0,0,0,0)
		_FadeAxis("Fade Axis", Vector) = (0,1,0,0)
    }
    SubShader
    {
		Pass{
				ZWrite On
				Colormask 0
		}
        Tags { "RenderType"="Transparent" "Queue"="Transparent"} 
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows alpha:blend

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
			float3 worldPos;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
		fixed4 _XBounds;
		fixed4 _YBounds;
		fixed4 _ZBounds;
		fixed4 _FadeAxis;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Albedo comes from a texture tinted by color
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            // Metallic and smoothness come from slider variables
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
			fixed xa = smoothstep(_XBounds.x,_XBounds.y,IN.worldPos.x);
			fixed ya = smoothstep(_YBounds.x,_YBounds.y,IN.worldPos.y);
			fixed za = smoothstep(_ZBounds.x,_ZBounds.y,IN.worldPos.z);
            o.Alpha = _FadeAxis.x*xa+_FadeAxis.y*ya+_FadeAxis.z*za + _FadeAxis.w*1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
