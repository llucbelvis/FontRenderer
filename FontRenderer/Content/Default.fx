#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

float4x4 World;
float4x4 View;
float4x4 Projection;

float4 Color;

texture Texture;

int VertexColorEnabled;
int TextureColorEnabled;

sampler Sampler0 = sampler_state
{
    Texture = <Texture>;
    MinFilter = LINEAR;
    MagFilter = LINEAR;
    MipFilter = LINEAR;
    AddressU = Wrap;
    AddressV = Wrap;
};

struct VertexShaderInput
{
    float4 Position : POSITION;
    float2 Coords : TEXCOORD0;
};

struct PixelShaderInput
{
    float4 Position : SV_POSITION;
    float2 Coords : TEXCOORD0;
};

PixelShaderInput VS(VertexShaderInput input)
{
    PixelShaderInput output;
    
    float4 worldPos = mul(input.Position, World);
    float4 viewPos = mul(worldPos, View);
    output.Position = mul(viewPos, Projection);
    
    output.Coords = input.Coords;
    return output;
}

float4 PS(PixelShaderInput input) : SV_TARGET
{
    float4 color = lerp(float4(1, 0, 1, 1), Color, VertexColorEnabled);
    color = lerp(color, tex2D(Sampler0, input.Coords), TextureColorEnabled);
    
    return color;
}

technique FillColor
{
    pass P0
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 PS();
    }
}