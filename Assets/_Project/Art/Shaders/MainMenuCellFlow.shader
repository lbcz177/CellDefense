Shader "CellDefense/UI/MainMenuCellFlow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _FlowTime ("Flow Time", Float) = 0
        _UvRect ("Sprite UV bounds", Vector) = (0,0,1,1)
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"
            "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp]
            ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            struct appdata { float4 vertex:POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; };
            struct v2f { float4 vertex:SV_POSITION; fixed4 color:COLOR; float2 uv:TEXCOORD0; float4 local:TEXCOORD1; };
            sampler2D _MainTex;
            fixed4 _Color, _TextureSampleAdd;
            float4 _ClipRect, _UvRect, _MainTex_TexelSize;
            float _FlowTime;
            v2f vert(appdata v)
            {
                v2f o;
                o.local=v.vertex;
                o.vertex=UnityObjectToClipPos(v.vertex);
                o.uv=v.uv; o.color=v.color*_Color;
                return o;
            }
            float2 CellDrift(float2 uv, float2 center, float2 radius, float phase)
            {
                // A flat inner mask translates the complete silhouette as one body.
                // Only the surrounding tissue blends back into its stationary pose.
                float mask=1-smoothstep(1.0,1.45,length((uv-center)/radius));
                float angle=_FlowTime*(6.2831853/5.4)+phase;
                return mask*float2(sin(angle)*.0024,cos(angle)*.0028);
            }
            fixed4 SampleMovingCell(float2 uv)
            {
                // Subpixel interpolation avoids whole-texel jumps with Point artwork.
                // The original texture's import settings and stationary UI stay intact.
                float2 pixel=uv*_MainTex_TexelSize.zw-.5;
                float2 fraction=frac(pixel);
                float2 baseUV=(floor(pixel)+.5)*_MainTex_TexelSize.xy;
                fixed4 a=tex2D(_MainTex,baseUV);
                fixed4 b=tex2D(_MainTex,baseUV+float2(_MainTex_TexelSize.x,0));
                fixed4 c=tex2D(_MainTex,baseUV+float2(0,_MainTex_TexelSize.y));
                fixed4 d=tex2D(_MainTex,baseUV+_MainTex_TexelSize.xy);
                return lerp(lerp(a,b,fraction.x),lerp(c,d,fraction.x),fraction.y);
            }
            float NucleusActivity(float2 uv,float2 center,float2 radius,float phase)
            {
                float area=1-smoothstep(.3,1,length((uv-center)/radius));
                float pulse=pow(saturate(.5+.5*sin(_FlowTime*2.1+phase)),4);
                return area*pulse;
            }
            fixed4 frag(v2f i):SV_Target
            {
                float2 uv=(i.uv-_UvRect.xy)/_UvRect.zw;
                float2 motion=CellDrift(uv,float2(.55,.72),float2(.085,.125),0);
                motion+=CellDrift(uv,float2(.875,.57),float2(.09,.13),1.8);
                motion+=CellDrift(uv,float2(.797,.30),float2(.15,.20),3.6);
                motion+=CellDrift(uv,float2(.445,.415),float2(.060,.085),5.1);
                // The title, all left buttons and the outer edges remain fixed.
                float protectedArea=smoothstep(.39,.42,uv.x)*(1-smoothstep(.945,.99,uv.x))
                    *smoothstep(.025,.07,uv.y)*(1-smoothstep(.94,.99,uv.y));
                protectedArea*=1-(1-smoothstep(.448,.47,uv.x))*smoothstep(.60,.64,uv.y);
                float2 sampleUV=saturate(uv-motion*protectedArea)*_UvRect.zw+_UvRect.xy;
                fixed4 sampled=tex2D(_MainTex,i.uv);
                if(protectedArea>0 && dot(motion,motion)>.0000000001)
                    sampled=SampleMovingCell(sampleUV);
                float activity=NucleusActivity(uv,float2(.55,.72),float2(.025,.04),0);
                activity+=NucleusActivity(uv,float2(.875,.57),float2(.035,.055),1.8);
                activity+=NucleusActivity(uv,float2(.84,.24),float2(.035,.055),3.6);
                activity+=NucleusActivity(uv,float2(.445,.415),float2(.022,.035),5.1);
                sampled.rgb+=float3(.025,.075,.09)*activity;
                fixed4 color=(sampled+_TextureSampleAdd)*i.color;
                #ifdef UNITY_UI_CLIP_RECT
                color.a*=UnityGet2DClipping(i.local.xy,_ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a-.001);
                #endif
                return color;
            }
            ENDCG
        }
    }
}
