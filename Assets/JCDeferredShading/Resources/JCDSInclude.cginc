#ifndef __JCDS_INCLUDE__
#define __JCDS_INCLUDE__

uniform sampler2D _DiffuseBuffer;
uniform sampler2D _NormalBuffer;
uniform sampler2D _PositionBuffer;
uniform sampler2D _ResultBuffer;
uniform sampler2D _SSRBuffer;

uniform float4 _DirLightDir;
uniform fixed4 _DirLightColor;

uniform float3 _PointLightPos;
uniform fixed4 _PointLightColor;
uniform float4 _PointLightRange;

struct appdata
{
	float4 vertex : POSITION;
	float2 uv : TEXCOORD0;
};

struct v2f
{
	float2 uv : TEXCOORD0;
	float4 pos : TEXCOORD1;
	float4 vertex : SV_POSITION;
};

v2f vert_screen_quad(appdata v)
{
	v2f o;
	UNITY_INITIALIZE_OUTPUT(v2f, o);

	o.vertex = v.vertex;
	o.uv = v.uv;

	if (_ProjectionParams.x < 0)
		o.uv.y = 1 - o.uv.y;

	return o;
}

inline float2 pos_to_screen_uv(float4 pos)
{
	float2 screenUV = pos.xy / pos.w * 0.5 + 0.5;

#if UNITY_UV_STARTS_AT_TOP
	screenUV.y = 1.0 - screenUV.y;
#endif

	return screenUV;
}

#endif