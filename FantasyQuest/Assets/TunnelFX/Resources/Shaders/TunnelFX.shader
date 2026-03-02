// Tunnel FX shader (C) 2016-2017 by Kronnect

Shader "TunnelEffect/TunnelFX" {
	Properties {
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_TunnelTex1 ("Tunnel Tex 1 (RGB)", 2D) = "black" {}
		_TunnelTex2 ("Tunnel Tex 2 (RGB)", 2D) = "black" {}
		_TunnelTex3 ("Tunnel Tex 3 (RGB)", 2D) = "black" {}
		_TunnelTex4 ("Tunnel Tex 4 (RGB)", 2D) = "black" {}
		_Params1 ("Params 1", Vector) = (1.5, 0.5, 0.1, 0.12)
		_Params2 ("Params 2", Vector) = (1.5, 0.5, 0.1, 0.12)
		_Params3 ("Params 3", Vector) = (1.5, 0.5, 0.1, 0.12)
		_Params4 ("Params 4", Vector) = (1.5, 0.5, 0.1, 0.12)
		_Params5 ("Params 5", Vector) = (0.533328, 0.26664, 0.13332, 0.06666)
		_MixParams ("Mix Params", Vector) = (0,0,4,1)
		_TransitionColor ("Transition Color", Color) = (1,1,1)
		_BackgroundColor ("Background Color", Color) = (0,0,0,0)
		_MixParams2 ("Mix Params 2", Vector) = (10, 0, 1, 1)
		[IgnoreInInspector] _TunnelDownsampled ("Tunnel Downsampled", 2D) = "black" {}
	}
        	
	CGINCLUDE
	#pragma fragmentoption ARB_precision_hint_fastest
	#pragma target 3.0
	#include "UnityCG.cginc"
	
	sampler2D _MainTex;
	half4 _MainTex_TexelSize;
	float4 _MainTex_ST;
	sampler2D_float _CameraDepthTexture;
	sampler2D _TunnelTex1, _TunnelTex2, _TunnelTex3, _TunnelTex4;
	sampler2D _TunnelDownsampled;
	float4x4 _ViewRot;

	float4 _Params1; // x = travel speed, y = rotation speed, z = twist, w = brightness
	float4 _Params2; // x = travel speed, y = rotation speed, z = twist, w = brightness
	float4 _Params3; // x = travel speed, y = rotation speed, z = twist, w = brightness
	float4 _Params4; // x = travel speed, y = rotation speed, z = twist, w = brightness
	float4 _Params5; // x = contribution1, y = contribution 2, z = contribution 3, w = contribution 4
	half4  _MixParams; // x = center x offset, y = center y offset, z = layer count, w = global blend
	half4  _TransitionColor;
	half4  _BackgroundColor;
	half4  _MixParams2; // x = fallOff, y = blend in order, z = global alpha, w = hyperspeed effect;
		
    struct appdata {
    	float4 vertex : POSITION;
		float2 texcoord : TEXCOORD0;
    };
    
	struct v2f {
	    float4 pos : SV_POSITION;
	    float4 uv: TEXCOORD0;
	    float4 uv2: TEXCOORD1;
	};
	
	    
	struct v2fDepth {
	    float4 pos : SV_POSITION;
	    float4 uv: TEXCOORD0;
    	float2 depthUV : TEXCOORD1;
	    float4 uv2: TEXCOORD2;
	};

v2f vert(appdata v) {
    v2f o;
    o.pos = UnityObjectToClipPos(v.vertex);
    o.uv = float4(UnityStereoScreenSpaceUVAdjust(v.texcoord, _MainTex_ST), 1.0, 0);
    float2 uv2 = v.texcoord;
	uv2.xy += _MixParams.xy;
	uv2.x  *= _MainTex_TexelSize.y / _MainTex_TexelSize.x;
	#if UNITY_SINGLE_PASS_STEREO
	uv2.x *= 0.5;
	#endif
	o.uv2 = float4(uv2, _MixParams2.w, 0);
    return o;
}

v2fDepth vertDepth(appdata v) {
    v2fDepth o;
    o.pos = UnityObjectToClipPos(v.vertex);
    o.depthUV = UnityStereoScreenSpaceUVAdjust(v.texcoord, _MainTex_ST);
    o.uv = float4(o.depthUV, 1.0, 0);
    float2 uv2 = v.texcoord;
	uv2.xy += _MixParams.xy;
	uv2.x  *= _MainTex_TexelSize.y / _MainTex_TexelSize.x;
	#if UNITY_SINGLE_PASS_STEREO
	uv2.x *= 0.5;
	#endif
	o.uv2 = float4(uv2, _MixParams2.w, 0);

    #if UNITY_UV_STARTS_AT_TOP
    if (_MainTex_TexelSize.y < 0) {
       // Depth texture is inverted WRT the main texture
       o.depthUV.y = 1 - o.depthUV.y;
    }
    #endif
   
    return o;
}

	half4 getColorSquare(sampler2D tex, float3 rp, float sp, float4 params) {
//		rp.z += _Time.y * params.x * sp;
		rp.z += params.x * sp;
		float2 uv2 = float2((rp.x + rp.y) * 1.2732395259 + rp.z * params.z * sp, rp.z);
    	half4 color = tex2Dlod(tex, float4(uv2 * sp * 0.5, 0, 0));
    	return half4(color.rgb * params.www * color.aaa, color.a);
	}
	
	half4 buildTunnelSquare(float4 uv) {

		float3 rd = normalize(uv.xyz);
		#if TUNNEL_CAMERA_ROTATES
    	rd = mul(float4(rd.xyz, 0), _ViewRot).xyz;
    	#endif
		    
    	float t = dot(rd.xy, rd.xy);
    	t = 1.414213 / (sqrt(t) + 0.00001);
    	
    	t = max(abs(rd.x), abs(rd.y));
    	t = 1.0 / t;
    	
		rd *= t;

		#if TUNNEL_BLEND_IN_ORDER
			half4 f = half4(0,0,0,0);
			if (_MixParams.z>3.0) f += (1.0-f.a) * _Params5.w * getColorSquare(_TunnelTex4, rd, 4.0, _Params4);
	   		if (_MixParams.z>2.0) f += (1.0-f.a) * _Params5.z * getColorSquare(_TunnelTex3, rd, 2.0, _Params3);
			if (_MixParams.z>1.0) f += (1.0-f.a) * _Params5.y * getColorSquare(_TunnelTex2, rd, 1.0, _Params2);
   			f += (1.0-f.a) * _Params5.x * getColorSquare(_TunnelTex1, rd, 0.5, _Params1); 
			return lerp(_BackgroundColor, f,  saturate(_MixParams2.x / t));
		#else
			half4 f = _Params5.x * getColorSquare(_TunnelTex1, rd, 0.5, _Params1); 
			if (_MixParams.z>1.0) f += _Params5.y * getColorSquare(_TunnelTex2, rd, 1.0, _Params2);
   			if (_MixParams.z>2.0) f += _Params5.z * getColorSquare(_TunnelTex3, rd, 2.0, _Params3);
			if (_MixParams.z>3.0) f += _Params5.w * getColorSquare(_TunnelTex4, rd, 4.0, _Params4);
			return lerp(_BackgroundColor, f,  saturate(_MixParams2.x / t));
		#endif

	}
	
			
	half4 getColorCirc(sampler2D tex, float3 rp, float sp, float4 params) {
		rp.z += params.x * sp;
		float2 uv2 = float2(atan2(rp.y, rp.x) * 1.2732395259 + (params.y + rp.z * params.z) * sp, rp.z);
    	half4 color = tex2Dlod(tex, float4(uv2 * sp * 0.5, 0, 0));
    	return half4(color.rgb * params.www * color.aaa, color.a);
	}


	half4 buildTunnelCirc(half4 uv) {

		float3 rd = normalize(uv.xyz);
		#if TUNNEL_CAMERA_ROTATES
    	rd = mul(float4(rd.xyz, 0), _ViewRot).xyz;
    	#endif
		    
    	float t = dot(rd.xy, rd.xy);
    	t = 1.414213 / (sqrt(t) + 0.00001);
		rd *= t;

		#if TUNNEL_BLEND_IN_ORDER
			half4 f = half4(0,0,0,0);
			if (_MixParams.z>3.0) f += (1.0-f.a) * _Params5.w * getColorCirc(_TunnelTex4, rd, 4.0, _Params4);
	   		if (_MixParams.z>2.0) f += (1.0-f.a) * _Params5.z * getColorCirc(_TunnelTex3, rd, 2.0, _Params3);
			if (_MixParams.z>1.0) f += (1.0-f.a) * _Params5.y * getColorCirc(_TunnelTex2, rd, 1.0, _Params2);
   			f += (1.0-f.a) * _Params5.x * getColorCirc(_TunnelTex1, rd, 0.5, _Params1); 
			return lerp(_BackgroundColor, f,  saturate(_MixParams2.x / t));
		#else
			half4 f = _Params5.x * getColorCirc(_TunnelTex1, rd, 0.5, _Params1); 
			if (_MixParams.z>1.0) f += _Params5.y * getColorCirc(_TunnelTex2, rd, 1.0, _Params2);
   			if (_MixParams.z>2.0) f += _Params5.z * getColorCirc(_TunnelTex3, rd, 2.0, _Params3);
			if (_MixParams.z>3.0) f += _Params5.w * getColorCirc(_TunnelTex4, rd, 4.0, _Params4);
			return lerp(_BackgroundColor, f,  saturate(_MixParams2.x / t));
		#endif
	}


	half4 trans(float4 uv, float4 uv2, half t) {
		half4 co;
		if (t<1.0) {
			co = tex2Dlod(_MainTex, uv.xyww);
			co = lerp(co, _TransitionColor, t * _MixParams2.z);
		} else {
			#if TUNNEL_SQUARE
			co = buildTunnelSquare(uv2);
			#else
			co = buildTunnelCirc(uv2);
			#endif
			co = lerp(_TransitionColor, co, clamp(t, 0, 2) - 1.0);
			if (_MixParams2.z<1.0) {
				half4 bg = tex2Dlod(_MainTex, uv.xyww);
				co = lerp(bg, co, _MixParams2.z);
			}	
			
		}
		return co;
	}
	
	half4 transDownsampled(float4 uv, half t) {
		half4 co;
		if (t<1.0) {
			co = tex2Dlod(_MainTex, uv.xyww);
			co = lerp(co, _TransitionColor, t * _MixParams2.z);
		} else {
			co = tex2Dlod(_TunnelDownsampled, uv.xyww);
			co = lerp( _TransitionColor, co, clamp(t,0,2) - 1.0 );
			if (_MixParams2.z<1.0) {
				half4 bg = tex2Dlod(_MainTex, uv.xyww);
				co = lerp(bg, co, _MixParams2.z);
			}	
		}    
		return co;
	}
	
	
	
	// Fragment Shaders
	half4 fragDepth (v2fDepth i) : SV_Target {
		float depth = Linear01Depth(UNITY_SAMPLE_DEPTH(tex2D(_CameraDepthTexture, i.depthUV)));
		if (depth<0.999) {
			return tex2Dlod(_MainTex, i.uv.xyww);
		} else {
    		return trans(i.uv, i.uv2, _MixParams.w);
    	}
	}

	half4 frag (v2f i) : SV_Target {
    	return trans(i.uv, i.uv2, _MixParams.w);
	}

	half4 fragJustTunnel (v2f i) : SV_Target {
		#if TUNNEL_SQUARE
		return buildTunnelSquare(i.uv2);
		#else
		return buildTunnelCirc(i.uv2);
		#endif
	}
		
	half4 fragDepthUpsampling (v2fDepth i) : SV_Target {
		float depth = Linear01Depth(UNITY_SAMPLE_DEPTH(tex2D(_CameraDepthTexture, i.depthUV)));
		if (depth<0.999) {
			return tex2Dlod(_MainTex, i.uv.xyww);
		} else {
			return transDownsampled(i.uv, _MixParams.w);
		}
	}

		
	half4 fragUpsampling (v2fDepth i) : SV_Target {
		return transDownsampled(i.uv, _MixParams.w);
	}
	
	ENDCG
	
	SubShader {
       	ZTest Always Cull Off ZWrite Off
       	Fog { Mode Off }
		
		Pass { // 0: no downsampling w/ depth check
	        CGPROGRAM
			#pragma vertex vertDepth
			#pragma fragment fragDepth
			#pragma multi_compile TUNNEL_CIRCULAR TUNNEL_SQUARE
			#pragma multi_compile __ TUNNEL_BLEND_IN_ORDER
			#pragma multi_compile __ TUNNEL_CAMERA_ROTATES
			ENDCG
        }
        
		Pass { // 1: no downsampling / ignore depth
	        CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile TUNNEL_CIRCULAR TUNNEL_SQUARE
			#pragma multi_compile __ TUNNEL_BLEND_IN_ORDER
			#pragma multi_compile __ TUNNEL_CAMERA_ROTATES
			ENDCG
        }
        
		Pass { // 2: just tunnel
	        CGPROGRAM
			#pragma vertex vert
			#pragma fragment fragJustTunnel
			#pragma multi_compile TUNNEL_CIRCULAR TUNNEL_SQUARE
			#pragma multi_compile __ TUNNEL_BLEND_IN_ORDER
			#pragma multi_compile __ TUNNEL_CAMERA_ROTATES
			ENDCG
        }
        
		Pass { // 3: upsampling w/depth check
	        CGPROGRAM
			#pragma vertex vertDepth
			#pragma fragment fragDepthUpsampling
			ENDCG
        }        
        
		Pass { // 4: upsampling / ignore depth
	        CGPROGRAM
			#pragma vertex vert
			#pragma fragment fragUpsampling
			ENDCG
        }        
	}
	FallBack Off
}	