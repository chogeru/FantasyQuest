Shader "BoZo/BakeTexture"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _NormalMap ("NormalMap", 2D) = "bump" {}

        _ShowNormalMap("ShowNormalMap", Range(0, 1)) = 0

        [Header(Colors Options)]
        [Space(10)]
        _Color_1("Color_1", Color) = (1, 0, 0, 0)
        _Color_2("Color_2", Color) = (0, 1, 0, 0)
        _Color_3("Color_3", Color) = (0, 0, 1, 0)
        _Color_4("Color_4", Color) = (0.9727613, 1, 0, 0)
        _Color_5("Color_5", Color) = (0, 0.9845986, 1, 0)
        _Color_6("Color_6", Color) = (1, 0, 0.988061, 0)
        _Color_7("Color_7", Color) = (1, 1, 1, 0)
        _Color_8("Color_8", Color) = (0.5031446, 0.5031446, 0.5031446, 0)
        _Color_9("Color_9", Color) = (0, 0, 0, 0)

        [Header(Decal Options)]
        [Space(10)]
        [NoScaleOffset]_DecalMap("DecalMap", 2D) = "black" {}
        _DecalUVSet("DecalUVSet", Range(0, 1)) = 0
        _DecalBlend("DecalBlend", Range(0, 1)) = 0
        _DecalScale("DecalScale", Vector) = (1, 1, 0, 0)
        _DecalColor_1("DecalColor_1", Color) = (0, 0, 0, 0)
        _DecalColor_2("DecalColor_2", Color) = (0, 0, 0, 0)
        _DecalColor_3("DecalColor_3", Color) = (0, 0, 0, 0)

        [Header(Pattern Options)]
        [Space(10)]
        [NoScaleOffset]_PatternMap("PatternMap", 2D) = "black" {}
        _PatternUVSet("PatternUVSet", Range(0, 1)) = 0
        _PatternBlend("PatternBlend", Range(0, 1)) = 0
        _PatternScale("PatternScale", Vector) = (1, 1, 0, 0)
        _PatternColor_1("PatternColor_1", Color) = (0, 0, 0, 0)
        _PatternColor_2("PatternColor_2", Color) = (0, 0, 0, 0)
        _PatternColor_3("PatternColor_3", Color) = (0, 0, 0, 0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Name "BakeTexture"
            Tags { "LightMode" = "UniversalForward" }
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);

            float _ShowNormalMap;

            //BaseColor
            float4 _Color_1;
            float4 _Color_2;
            float4 _Color_3;
            float4 _Color_4;
            float4 _Color_5;
            float4 _Color_6;
            float4 _Color_7;
            float4 _Color_8;
            float4 _Color_9;

            //decal
            sampler2D _DecalMap;
            float4 _DecalMap_ST;
            float  _DecalUVSet;
            float  _DecalBlend;
            float4 _DecalScale;
            float4 _DecalColor_1;
            float4 _DecalColor_2;
            float4 _DecalColor_3;

            //pattern
            sampler2D _PatternMap;
            float4 _PatternMap_ST;
            float  _PatternUVSet;
            float  _PatternBlend;
            float4 _PatternScale;
            float4 _PatternColor_1;
            float4 _PatternColor_2;
            float4 _PatternColor_3;

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                float4 vertexColor : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                float4 vertexColor : COLOR;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float2 uv = IN.uv;
                uv.y = 1.0 - uv.y; // Flip Y so the texture appears upright in the baked output
                OUT.positionHCS = float4(uv * 2.0 - 1.0, 0.0, 1.0);
                OUT.uv = IN.uv;
                OUT.uv1 = IN.uv1;
                OUT.vertexColor = IN.vertexColor;
                return OUT;
            }

            float4 UnpackNormalCustom(float4 packedNormal)
            {
                float3 normal;
                normal.x = packedNormal.a * 2.0 - 1.0;
                normal.y = packedNormal.g * 2.0 - 1.0;
                normal.z = sqrt(1.0 - saturate(dot(normal.xy, normal.xy)));
                
                return float4 (normal,1);
            }

            float4 ApplyPattern(float4 tex, float flatTexture, float mask, Varyings i)
            {
                float2 uv = lerp(i.uv, i.uv1, _PatternUVSet);
                float2 scaleduv = uv * _PatternScale + ((_PatternScale * -1) / 2) + 0.5;

                float4 pattern = tex2D(_PatternMap, scaleduv);


                float4 color1 = lerp(0, _PatternColor_1, pattern.r);
                float4 color2 = lerp(0, _PatternColor_2, pattern.g);
                float4 color3 = lerp(0, _PatternColor_3, pattern.b);
                
                float4 combine = color1 + color2 + color3;

                float steppedMask = step(0.01, mask);
                steppedMask = steppedMask  * i.vertexColor.x;
                steppedMask = steppedMask * pattern.a;
                steppedMask = lerp(pattern.a, steppedMask, _PatternBlend);
                float4 blend  = lerp(combine, flatTexture * combine, _PatternBlend);

                float4 final = lerp(tex, blend, pattern.w * steppedMask);
                return float4(final.rgb, tex.a + pattern.a);
            }

            float4 ApplyDecal(float4 tex, float flatTexture, float mask, Varyings i)
            {
                float2 uv = lerp(i.uv, i.uv1, _DecalUVSet);
                float2 scaleduv = uv * _DecalScale + ((_DecalScale * -1) / 2) + 0.5;

                float4 decal = tex2D(_DecalMap, scaleduv);

                float4 color1 = lerp(0, _DecalColor_1, decal.r);
                float4 color2 = lerp(0, _DecalColor_2, decal.g);
                float4 color3 = lerp(0, _DecalColor_3, decal.b);

                float4 combine = color1 + color2 + color3;

                float steppedMask = step(0.06, mask);

                float4 blend  = lerp(combine, flatTexture * combine, _DecalBlend);

                float4 final = lerp(tex, blend, decal.a);
                return float4(final.rgb, tex.a);
            }

            float4 CustomColors(float4 tex, float4 vertexColors)
            {
               float4 color1 = lerp(0, _Color_1, tex.x);
               float4 color2 = lerp(0, _Color_2, tex.y);
               float4 color3 = lerp(0, _Color_3, tex.z);
               float4 color4 = lerp(0, _Color_4, tex.x);
               float4 color5 = lerp(0, _Color_5, tex.y);
               float4 color6 = lerp(0, _Color_6, tex.z);
               float4 color7 = lerp(0, _Color_7, tex.x);
               float4 color8 = lerp(0, _Color_8, tex.y);
               float4 color9 = lerp(0, _Color_9, tex.z);

               float4 combine1 = color1 + color2 + color3;
               float4 combine2 = color4 + color5 + color6;
               float4 combine3 = color7 + color8 + color9;

               float4 layer1 = lerp(0, combine1, vertexColors.x);
               float4 layer2 = lerp(layer1, combine2, vertexColors.y);
               float4 layer3 = lerp(layer2, combine3, vertexColors.z);

               return float4(layer3.rgb, tex.a);
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 map = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                half4 flat = map.x + map.y + map.z;
                half4 tex = CustomColors(map, IN.vertexColor); 
                tex = ApplyPattern(tex,flat,map.r,IN);
                tex = ApplyDecal(tex,flat,map.r,IN);

                float4 packedNormal = SAMPLE_TEXTURE2D(_NormalMap,sampler_NormalMap, IN.uv);
                float4 n = UnpackNormalCustom(packedNormal);
                n = float4(n * 0.5 + 0.5);

                float4 final = lerp(tex, n, _ShowNormalMap);

                return final;
            }
            ENDHLSL
        }
    }
}
