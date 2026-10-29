Shader "CellDefense/SpriteHitFlash"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
        [HideInInspector] _RendererColor ("Renderer Color", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
        [PerRendererData] _FlashAmount ("Hit Flash", Range(0,1)) = 0
        [PerRendererData] _StateTint ("Animated State Tint", Color) = (1,1,1,1)
        [PerRendererData] _ImpactKick ("Local visual recoil", Float) = 0
        [PerRendererData] _ImpactSquash ("Local visual compression", Float) = 0
        [PerRendererData] _ImpactDirection ("Local impact direction", Vector) = (1,0,0,0)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"
            "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex ImpactVert
            #pragma fragment FlashFrag
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma multi_compile_local _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"
            float _FlashAmount;
            fixed4 _StateTint;
            float _ImpactKick, _ImpactSquash;
            float4 _ImpactDirection;
            v2f ImpactVert(appdata_t input)
            {
                float2 direction = _ImpactDirection.xy;
                direction *= rsqrt(max(dot(direction, direction), 0.0001));
                float2 across = float2(-direction.y, direction.x);
                float along = dot(input.vertex.xy, direction);
                float side = dot(input.vertex.xy, across);
                input.vertex.xy += -direction * (along * _ImpactSquash + _ImpactKick)
                    + across * side * _ImpactSquash * 0.55;
                return SpriteVert(input);
            }
            fixed4 FlashFrag(v2f input) : SV_Target
            {
                fixed4 color = SampleSpriteTexture(input.texcoord) * input.color;
                color.rgb *= _StateTint.rgb;
                color.rgb = lerp(color.rgb, input.color.rgb, saturate(_FlashAmount));
                color.rgb *= color.a;
                return color;
            }
            ENDCG
        }
    }
}
