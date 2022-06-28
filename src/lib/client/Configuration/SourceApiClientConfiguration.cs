namespace SunshineExpress.Client.Configuration;

public record SourceApiClientConfiguration
{
    public string BaseUri { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}