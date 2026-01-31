namespace Sentinel.Domain.Normalization;

public static class TagNormalizer
{
    public static string Normalize(string tag)
    {
        return tag.Trim().ToLowerInvariant();
    }
}
