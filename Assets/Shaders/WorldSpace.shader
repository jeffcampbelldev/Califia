Shader "Custom/WorldSpace"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
		_GridColor ("GridColor", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
		_GridWidth("GridWidth", Range(0,0.1)) = 0.01
		_IsFloor("Is Floor", Range(0,1)) = 0.0
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
		fixed4 _MainTex_ST;

        struct Input
        {
            float2 uv_MainTex;
			float3 worldPos;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
        fixed4 _GridColor;
		fixed _GridWidth;
		fixed _IsFloor;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Albedo comes from a texture tinted by color
			fixed3 wallPos = fixed3((IN.worldPos.x+IN.worldPos.z)*_MainTex_ST.x+_MainTex_ST.z,IN.worldPos.y*_MainTex_ST.y+_MainTex_ST.w,0);
			fixed3 floorPos = fixed3((IN.worldPos.x)*_MainTex_ST.x+_MainTex_ST.z,IN.worldPos.z*_MainTex_ST.y+_MainTex_ST.w,0);
			fixed3 pos = lerp(wallPos,floorPos,_IsFloor);
            fixed4 c = tex2D (_MainTex, pos.xy) * _Color;
			o.Albedo=c.rgb;
            // Metallic and smoothness come from slider variables
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
