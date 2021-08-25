Shader "Custom/HighlightTransparent"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
	_RimColor ("Rim Color", Color) = (1,1,1,1)
	_OutlineThickness ("Rim Power (Outline Thickness)", Float) = 2
	_EmissionColor ("Emission color", Color) = (1,1,1,1)
	_EmissionTex ("Emission texture", 2D) = "white" {}
	_EmissionPower ("Emission scalar", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent""Queue"="Transparent" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows alpha:blend

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;
		sampler2D _EmissionTex;

        struct Input
        {
            float2 uv_MainTex;
			float2 uv_EmissionTex;
		fixed3 viewDir;
		fixed3 worldNormal;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
		fixed4 _EmissionColor;
	fixed4 _RimColor;
	fixed _OutlineThickness;
	fixed _EmissionPower;

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
		fixed rim = abs(dot(IN.viewDir,IN.worldNormal));
            //o.Albedo = lerp(_RimColor,c.rgb,pow(rim,_OutlineThickness));
			o.Albedo=c.rgb;
			o.Emission.rgb+=_RimColor*rim*_OutlineThickness;
            // Metallic and smoothness come from slider variables
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
			o.Emission += tex2D(_EmissionTex, IN.uv_EmissionTex) *_EmissionColor*_EmissionPower;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
