Shader "_MyShader/Raindrop_HDRP"
{
    Properties
    {
        // ここは最小限（値は基本C#からSetXXXで入れる想定）
        _Range("Range", Float) = 1
        _RangeR("RangeR", Float) = 1
        _MoveTotal("MoveTotal", Float) = 0
        _Move("Move", Float) = 0
        _TargetPosition("TargetPosition", Vector) = (0,0,0,0)
        // _PrevInvMatrix は Properties で直接いじる用途が薄いので、CBUFFER側で受ける
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="HDRenderPipeline"
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
        }

        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off

        Pass
        {
            HLSLPROGRAM

            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                float4 color      : COLOR;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color      : COLOR;
            };

            CBUFFER_START(UnityPerMaterial)
                float4x4 _PrevInvMatrix;
                float3   _TargetPosition;
                float    _Range;
                float    _RangeR;
                float    _MoveTotal;
                float    _Move;
            CBUFFER_END

            Varyings Vert(Attributes v)
            {
                // object空間で操作（元コードの意図を維持）
                float3 posOS = v.positionOS;
                posOS.y += _MoveTotal;

                float3 target = _TargetPosition; // C#でワールド渡しなら、OSに変換するか target をWSで合わせるのじゃ！
                float3 trip = floor(((target - posOS) * _RangeR + 1) * 0.5);
                trip *= (_Range * 2.0);
                posOS += trip;

                // head
                float4 headOS = float4(posOS, 1.0) * v.uv.x;
                float4 tv0CS  = TransformObjectToHClip(headOS.xyz);

                // trail（前フレームView相当）
                posOS.y -= _Move;
                float4 trailOS = float4(posOS, 1.0) * v.uv.y;

                // 現フレーム Object->View
                float3 posWS = TransformObjectToWorld(trailOS.xyz);
                float4 tv1VS = mul(GetWorldToViewMatrix(), float4(posWS, 1.0));

                // 前フレーム系への変換（元の流れ：MV -> PrevInv -> P）
                float4 tv1VS_prev = mul(_PrevInvMatrix, tv1VS);

                // View -> Clip（HDRPのプロジェクション行列）
                float4 tv1CS = mul(GetViewToHClipMatrix(), tv1VS_prev);

                Varyings o;
                o.positionCS = tv0CS + tv1CS;

                float depth = o.positionCS.z * 0.02;
                float normalized_depth = (1.0 - depth);

                o.color = v.color;
                o.color.a *= normalized_depth;
                return o;
            }

            float4 Frag(Varyings i) : SV_Target
            {
                return i.color;
            }

            ENDHLSL
        }
    }
}
