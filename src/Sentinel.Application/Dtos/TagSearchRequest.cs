namespace Sentinel.Application.Dtos;

public sealed class TagSearchRequest
{
    public IReadOnlyCollection<string> Tags { get; set; } = Array.Empty<string>();
    public bool MatchAll { get; set; } = true;
    public int Limit { get; set; } = 10;
}
