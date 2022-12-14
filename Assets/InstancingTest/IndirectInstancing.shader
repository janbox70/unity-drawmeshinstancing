Shader "Instanced/IndirectInstancing"
{
    SubShader{
        Tags { "RenderType" = "Opaque" }

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata_t {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
            };

            struct v2f {
                float4 vertex   : SV_POSITION;
                fixed4 color : COLOR;
            };

            struct MeshProperties {
                float4 crrs;
                float4 color;
            };

            StructuredBuffer<MeshProperties> _Properties;
            float _Row;
            float _Col;
            float4 _Region;

            v2f vert(appdata_t i, uint instanceID: SV_InstanceID) {
                v2f o;

                float4 p = _Properties[instanceID].crrs;
                float y = 5 * (sin(_Time.y * 2 - p.z * 20));

                float4x4 mat = float4x4(
                    p.w, 0, 0, p.x * (_Region.y - _Region.x) + _Region.x,
                    0, p.w, 0, y,
                    0, 0, p.w, p.y * (_Region.w - _Region.z) + _Region.z,
                    0, 0, 0, 1
                    );

                float4 pos = mul(mat, i.vertex);
                o.vertex = UnityObjectToClipPos(pos);

                o.color = _Properties[instanceID].color + (sin(_Time.y * 2 - p.z * 30)) / 4;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target {
                return i.color;
            }

            ENDCG
        }
    }
}
