using LiarUtil.Core.PopCap;

namespace LiarUtil.Core.Particles;

internal static class ParticleImageHelper
{
    public static string? ResolveName(ParticleImage? image) =>
        ImageNameResolver.ResolveName(image?.Name, image?.Id);
}
