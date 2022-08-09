Shader "Hidden/Onager/DataDisplay"
{
    Properties
    {
        [Enum(Onager.FXMesh.DataNames)]DisplayedData("Displayed Data", Float) = 0
        [Enum(Onager.FXMesh.ChannelNames)]Channel("Channel", Float) = 0

        [Toggle(WIREFRAME)]WIREFRAME("Show Wireframe", Float) = 0
        WireframeColor ("Wireframe Color", Color) = (0, 0, 0)
		WireframeSmoothing ("Wireframe Smoothing", Range(0, 10)) = 0.75
		WireframeThickness ("Wireframe Thickness", Range(0, 10)) = 0.5
        WireframeOpacity ("Wireframe Opacity", Range(0,1)) = 1
        
        ChannelMask ("Channel Mask", Vector) = (1,1,1,1)
    }
    SubShader
    {
        Name "Vertext"
        Tags { "RenderType"="Opaque" }

        Pass
        {
            Cull Off
            
            CGPROGRAM
            #pragma target 5.0
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag

            #pragma shader_feature WIREFRAME
            #pragma shader_feature RGB
            #pragma shader_feature MASK

            #define DATA float4 data[14]
            uniform uint DisplayedData, Channel;
            uniform float4 WireframeColor;
            uniform float WireframeSmoothing, WireframeThickness, WireframeOpacity;
            uniform float4 ChannelMask;

			struct appdata
			{
				float4 vertex  : POSITION;
				float4 color   : COLOR;
				float4 normal  : NORMAL;
				float4 tangent : TANGENT;
				float4 uv0     : TEXCOORD0;
				float4 uv1     : TEXCOORD1;
				float4 uv2     : TEXCOORD2;
				float4 uv3     : TEXCOORD3;
				float4 uv4     : TEXCOORD4;
				float4 uv5     : TEXCOORD5;
				float4 uv6     : TEXCOORD6;
				float4 uv7     : TEXCOORD7;
				uint   id      : SV_VertexID;
			};

			struct varyings
			{
				float4 vertex     : SV_POSITION;
				float4 data       : TEXCOORD0;
				float2 baryCoords : TEXCOORD1;
			}; 

            static const float2 baryCoords[3] =
            {
                float2(1, 0),
                float2(0, 1),
                float2(0, 0),
            };            

            float4 GetValue(DATA)
            {
                float4 value = data[DisplayedData];

                if(Channel == 0) return value;
                return value[Channel - 1];                
            }

            float GetWireframe(varyings i)
            {
                float3 barys;
                barys.xy = i.baryCoords;
                barys.z = 1 - barys.x - barys.y;

                float3 deltas = fwidth(barys);
                float3 smoothing = deltas * WireframeSmoothing;
                float3 thickness = deltas * WireframeThickness;
                barys = smoothstep(thickness, thickness + smoothing, barys);

                float minBary = min(barys.x, min(barys.y, barys.z));
                return minBary;
            }        

            varyings vert (appdata v)
            {
                DATA;
                data[0]  = v.color;
                data[1]  = v.normal;
                data[2]  = v.tangent;
                data[3]  = v.uv0;
                data[4]  = v.uv1;
                data[5]  = v.uv2;
                data[6]  = v.uv3;
                data[7]  = v.uv4;
                data[8]  = v.uv5;
                data[9]  = v.uv6;
                data[10] = v.uv7;
                data[11] = v.id;
                data[12] = mul(UNITY_MATRIX_M, v.vertex);
                data[13] = v.vertex;

                varyings o;
                o.vertex = v.vertex;
                o.data = GetValue(data);
                o.baryCoords = 0;
                return o;
            }

            [maxvertexcount(3)]
            void geom(triangle varyings input[3], inout TriangleStream<varyings> triStream)
            {
                varyings o;

                for(int i = 0; i < 3; i++)
                {
                    o.vertex = UnityObjectToClipPos(input[i].vertex);
                    o.data = input[i].data;
                    o.baryCoords = baryCoords[i];
                    triStream.Append(o);
                }
                triStream.RestartStrip();
            }   

            float4 frag (varyings i) : SV_Target
            {
                float4 final = 0;

                #ifdef WIREFRAME
                    float wireframe = GetWireframe(i);
                    final = lerp(WireframeColor, saturate(i.data), wireframe);
                    final = lerp(saturate(i.data), final, WireframeOpacity);
                #else
                    final = saturate(i.data);
                #endif

                #ifdef RGB
                    return final;
                #else
                    float4 mask = final * ChannelMask;
                    float channel = max(max(max(mask.r, mask.g), mask.b), mask.a);
                    return float4(channel.xxx, 1);
                #endif
            }

            ENDCG
        }
    }
    FallBack "Hidden/InternalErrorShader"
}