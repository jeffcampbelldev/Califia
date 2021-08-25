Shader "Custom/Skin"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
		_RimColor("Rim color", Color) = (1,1,1,1)
		_RimPower("Rim power", Float) = 1
		_ZBounds ("Z bounds", Vector) = (0,0,0,0)
    }
    SubShader
    {
        Tags { "RenderType"="TreeLeaf" "Queue"="Geometry"} 
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard noshadow vertex:vert

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
			fixed3 viewDir;
			fixed3 worldNormal;
			fixed3 localPos;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
		fixed4 _RimColor;
		fixed _RimPower;
		fixed4 _ZBounds;

		void vert (inout appdata_full v, out Input o) {
			UNITY_INITIALIZE_OUTPUT(Input,o);
			o.localPos = v.vertex.xyz;
		}

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Albedo comes from a texture tinted by color
			clip(_ZBounds.x-IN.localPos.y);
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
			fixed rim = abs(dot(IN.viewDir,IN.worldNormal));
            o.Albedo = lerp(_RimColor,c.rgb,pow(rim,_RimPower));
            // Metallic and smoothness come from slider variables
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
        }
        ENDCG
    }
}
