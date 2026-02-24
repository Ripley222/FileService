namespace FileService.Contracts.HttpCommunication;

public record FileServiceOptions
{
    public string Url { get; init; } = string.Empty;
    
    public int TimeoutInSeconds { get; init; }
}