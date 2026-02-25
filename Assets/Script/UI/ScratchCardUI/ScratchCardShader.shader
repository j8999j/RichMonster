// ScratchCardShader.shader
Shader "Custom/ScratchCard"
{
    Properties
    {
        _MainTex ("Cover Texture", 2D) = "white" {}
        _MaskTex ("Scratch Mask", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f    { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            sampler2D _MainTex;
            sampler2D _MaskTex;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 cover = tex2D(_MainTex, i.uv);
                fixed4 mask  = tex2D(_MaskTex, i.uv);
                // mask 越黑 → 封面越透明（被刮掉）
                cover.a *= mask.r;
                return cover;
            }
            ENDCG
        }
    }
}