namespace Core;

public sealed class EndpointInfo
{
    public required string HttpMethod { get; init; }
    public required string Route { get; init; }
    public string? Controller { get; init; }
    public string? SourceFile { get; init; }
    public int? LineNumber { get; init; }
    public string? Summary { get; set; }
}