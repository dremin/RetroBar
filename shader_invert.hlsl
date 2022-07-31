sampler2D input : register(s0);

float4 main(float2 uv : TEXCOORD) : COLOR
{
    float4 color = tex2D(input, uv);
    float alpha = color.a;

    if (!(color.r == color.g && color.g == color.b))
    {
        return color;
    }

    color.rgb /= alpha;
    color.rgb = 1 - color.rgb;
    color.rgb *= alpha;

    return color;
}