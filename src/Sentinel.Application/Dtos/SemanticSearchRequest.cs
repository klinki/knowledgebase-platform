namespace Sentinel.Application.Dtos;

public sealed class SemanticSearchRequest
{
    public string Query { get; set; } = string.Empty;
    public int Limit { get; set; } = 10;
}
