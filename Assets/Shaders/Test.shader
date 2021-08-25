// Upgrade NOTE: commented out 'float3 _WorldSpaceCameraPos', a built-in variable

Shader "Custom/Test"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
		_Power ("Rim power", Float) = 1
		_Threshold ("Outline Thresh", Float) = 0.9
		_OutlineColor ("Outline Color", Color) = (0,0,0,0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
			float3 viewDir;
			float3 worldNormal;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
		fixed _Power;
		fixed _Threshold;
		fixed4 _OutlineColor;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Albedo comes from a texture tinted by color
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex+fixed2(_Time.y,0)) * _Color;
			fixed dt = dot(IN.worldNormal,IN.viewDir);
			dt = pow(1-abs(dt),_Power);
			dt = step(_Threshold,dt);
			//Albedo color set
			//dt = 0 means front facing pixels
			//dt = 1 means outward facing pixels
			//c = main color
			//_OutlineColor is outline color
			//multiply c by 1-dt gives color where dt is 0
			//gives black where dt=1
			//need outline color where dt=1
			o.Albedo = c*(1-dt)+_OutlineColor*dt;
			//o.Albedo = lerp(c,_OutlineColor,dt);
			o.Emission = dt*_OutlineColor;
			//o.Albedo=dt;
            // Metallic and smoothness come from slider variables
            //o.Metallic = _Metallic;
			o.Metallic = 1;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
