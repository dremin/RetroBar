sampler2D input : register(s0);

float4 main(float2 uv : TEXCOORD) : COLOR
{
    float4 color = tex2D(input, uv);
    float alpha = color.a;

    if (!(color.r == color.g && color.g == color.b))
    {
        return color;
    }

    float3 notAlpha = color.rgb * (1.0 / max(0.0001, alpha));
    notAlpha = 1 - notAlpha;
    color.rgb = notAlpha * alpha;

    return color;
}