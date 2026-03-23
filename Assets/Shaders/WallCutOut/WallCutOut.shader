// WallCutOut.shader  –  HDRP custom opaque shader with a spherical cutout hole.
//
// ── HOW IT WORKS ────────────────────────────────────────────────────────────
// This shader uses per-material cutout properties. The WallCutOutController
// detects which objects are blocking the line of sight between the camera and
// the target, and swaps their materials with this WallCutOut material.
// The cutout parameters (position, radius, softness, enabled) are set on
// each material instance, allowing multiple walls to have cutouts at different
// positions simultaneously.
//
// ── SETUP ─────────────────────────────────────────────────────────────────────
//  1. Create a new Material and assign this shader: HDRP/Custom/WallCutOut
//  2. Assign this material to the Cutout Material field in WallCutOutController.
//  3. Set WallLayers in the controller to specify which objects should get cutouts.
//  4. The controller will automatically swap materials on blocking objects during Play Mode.
//
// ── CUTOUT PROPERTIES (set per-material) ────────────────────────────────
//   _WC_Position   — world-space centre of the hole  (float4, xyz used)
//   _WC_Radius     — inner hard-clip radius in world units
//   _WC_Softness   — width of the smooth transition zone at the edge
//   _WC_Enabled    — 1 = hole active, 0 = wall is fully solid

Shader "HDRP/Custom/WallCutOut"
{
    Properties
    {
        // ── Appearance ─────────────────────────────────────────────────────
        [MainColor]   _BaseColor        ("Base Color",        Color)        = (0.8, 0.8, 0.8, 1.0)
        [MainTexture] _BaseColorMap     ("Base Color Map",    2D)           = "white" {}
        _NormalMap                      ("Normal Map",        2D)           = "bump"  {}
        _NormalScale                    ("Normal Scale",      Float)        = 1.0
        _Smoothness                     ("Smoothness",        Range(0,1))   = 0.5
        _Metallic                       ("Metallic",          Range(0,1))   = 0.0
        _EmissiveColor                  ("Emissive Color",    Color)        = (0, 0, 0, 1)
        _Tiling                         ("UV Tiling (XY)",    Vector)       = (1, 1, 0, 0)

        // Read-only display of the global cutout state (Inspector only)
        // These are NOT used by the shader - the globals _WC_* are used.
        // They exist so you can see the live values in the Material Inspector
        // while the game is running (they are not driven anywhere).
        _WC_RadiusDisplay   ("Read-only Radius",   Float) = 1.5
        _WC_SoftnessDisplay ("Read-only Softness", Float) = 0.35
        
        [Header(Cutout Settings)]
        _WC_Position        ("Cutout Position",    Vector) = (0,0,0,0)
        _WC_Radius          ("Cutout Radius",      Float)  = 1.5
        _WC_Softness        ("Cutout Softness",    Float)  = 0.35
        _WC_Enabled         ("Cutout Enabled",     Float)  = 0.0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "HDRenderPipeline"
            "RenderType"     = "Opaque"
            // AlphaTest queue so Unity knows this shader uses clip()
            "Queue"          = "AlphaTest+0"
        }

        // ════════════════════════════════════════════════════════════════════
        // Shared HLSL — included into every pass via HLSLINCLUDE
        // ════════════════════════════════════════════════════════════════════
        HLSLINCLUDE

        #pragma multi_compile_instancing

        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

        // ── Per-material data (SRP Batcher compatible) ───────────────────
        CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor;
            float4 _BaseColorMap_ST;
            float  _NormalScale;
            float  _Smoothness;
            float  _Metallic;
            float4 _EmissiveColor;
            float4 _Tiling;
            // Cutout parameters - set per-material by WallCutOutController
            float4 _WC_Position;
            float  _WC_Radius;
            float  _WC_Softness;
            float  _WC_Enabled;
        CBUFFER_END

        TEXTURE2D(_BaseColorMap); SAMPLER(sampler_BaseColorMap);
        TEXTURE2D(_NormalMap);    SAMPLER(sampler_NormalMap);

        // ── Cutout helper ─────────────────────────────────────────────────
        // Returns 0 inside the hole (pixel is clipped) and 1 outside.
        float WC_Alpha(float3 worldPos)
        {
            if (_WC_Enabled < 0.5)
                return 1.0;

            // In HDRP, 'worldPos' is camera-relative if Camera Relative Rendering is enabled.
            // _WC_Position is set per-material and is in absolute world space.
            // We use GetAbsolutePositionWS to handle this conversion regardless of SRP settings.
            float3 absPos = GetAbsolutePositionWS(worldPos);

            float dist    = distance(absPos, _WC_Position.xyz);
            float outer   = _WC_Radius + max(_WC_Softness, 0.001);
            
            // Returns 0 inside the hole, 1 outside.
            return smoothstep(_WC_Radius, outer, dist);
        }

        // ── UV helper ─────────────────────────────────────────────────────
        float2 ApplyTiling(float2 uv)
        {
            return uv * _Tiling.xy + _BaseColorMap_ST.zw;
        }

        ENDHLSL

        // ════════════════════════════════════════════════════════════════════
        // Pass 1 — ForwardOnly  (main colour pass)
        // ════════════════════════════════════════════════════════════════════
        Pass
        {
            Name "ForwardOnly"
            Tags { "LightMode" = "ForwardOnly" }

            Cull   Back
            ZWrite On
            ZTest  LEqual
            Blend  Off

            HLSLPROGRAM
            #pragma vertex   Vert_Forward
            #pragma fragment Frag_Forward

            struct Attrs_Fwd
            {
                float4 posOS : POSITION;
                float3 norOS : NORMAL;
                float2 uv    : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Vary_Fwd
            {
                float4 posCS : SV_POSITION;
                float3 posWS : TEXCOORD0;
                float3 norWS : TEXCOORD1;
                float2 uv    : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Vary_Fwd Vert_Forward(Attrs_Fwd IN)
            {
                Vary_Fwd OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.posCS = TransformObjectToHClip(IN.posOS.xyz);
                OUT.posWS = TransformObjectToWorld(IN.posOS.xyz);
                OUT.norWS = TransformObjectToWorldNormal(IN.norOS);
                OUT.uv    = ApplyTiling(IN.uv);
                return OUT;
            }

            float4 Frag_Forward(Vary_Fwd IN) : SV_Target
            {
                float alpha = WC_Alpha(IN.posWS);
                
                // Discard pixels inside the cutout
                if (alpha < 0.01)
                    discard;

                float4 albedo  = SAMPLE_TEXTURE2D(_BaseColorMap, sampler_BaseColorMap, IN.uv)
                                 * _BaseColor;

                // Simple hemisphere ambient
                float3 norWS  = normalize(IN.norWS);
                float  skyT   = norWS.y * 0.5 + 0.5;
                float3 ambient = lerp(float3(0.08, 0.08, 0.08), float3(0.55, 0.55, 0.6), skyT);

                return float4(albedo.rgb * ambient + _EmissiveColor.rgb, 1.0);
            }
            ENDHLSL
        }

        // ════════════════════════════════════════════════════════════════════
        // Pass 2 — DepthForwardOnly  (HDRP depth pre-pass for forward objects)
        // ════════════════════════════════════════════════════════════════════
        Pass
        {
            Name "DepthForwardOnly"
            Tags { "LightMode" = "DepthForwardOnly" }

            Cull      Back
            ZWrite    On
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex   Vert_DepthFwd
            #pragma fragment Frag_DepthFwd

            struct Attrs_DFO { float4 posOS : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Vary_DFO  { float4 posCS : SV_POSITION; float3 posWS : TEXCOORD0; };

            Vary_DFO Vert_DepthFwd(Attrs_DFO IN)
            {
                Vary_DFO OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                OUT.posCS = TransformObjectToHClip(IN.posOS.xyz);
                OUT.posWS = TransformObjectToWorld(IN.posOS.xyz);
                return OUT;
            }

            void Frag_DepthFwd(Vary_DFO IN)
            {
                if (WC_Alpha(IN.posWS) < 0.01)
                    discard;
            }
            ENDHLSL
        }

        // ════════════════════════════════════════════════════════════════════
        // Pass 3 — DepthOnly  (shadow maps + SSAO depth)
        // ════════════════════════════════════════════════════════════════════
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            Cull      Back
            ZWrite    On
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex   Vert_Depth
            #pragma fragment Frag_Depth

            struct Attrs_DO { float4 posOS : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Vary_DO  { float4 posCS : SV_POSITION; float3 posWS : TEXCOORD0; };

            Vary_DO Vert_Depth(Attrs_DO IN)
            {
                Vary_DO OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                OUT.posCS = TransformObjectToHClip(IN.posOS.xyz);
                OUT.posWS = TransformObjectToWorld(IN.posOS.xyz);
                return OUT;
            }

            void Frag_Depth(Vary_DO IN)
            {
                if (WC_Alpha(IN.posWS) < 0.01)
                    discard;
            }
            ENDHLSL
        }

        // ════════════════════════════════════════════════════════════════════
        // Pass 4 — ShadowCaster  (directional / spot / point light shadows)
        // ════════════════════════════════════════════════════════════════════
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            Cull      Back
            ZWrite    On
            ZTest     LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex   Vert_Shadow
            #pragma fragment Frag_Shadow

            struct Attrs_Shad { float4 posOS : POSITION; float3 norOS : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Vary_Shad  { float4 posCS : SV_POSITION; float3 posWS : TEXCOORD0; };

            Vary_Shad Vert_Shadow(Attrs_Shad IN)
            {
                Vary_Shad OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                float3 posWS  = TransformObjectToWorld(IN.posOS.xyz);
                float3 norWS  = TransformObjectToWorldNormal(IN.norOS);
                posWS        += norWS * 0.005; // tiny normal bias to prevent shadow acne
                OUT.posCS     = TransformWorldToHClip(posWS);
                OUT.posWS     = posWS;
                return OUT;
            }

            void Frag_Shadow(Vary_Shad IN)
            {
                if (WC_Alpha(IN.posWS) < 0.01)
                    discard;
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/HDRP/FallbackError"
}
