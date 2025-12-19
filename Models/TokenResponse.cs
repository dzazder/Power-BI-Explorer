namespace PowerBIExplorer.Models;

public class TokenResponse
{
    public bool Success { get; set; }
    public string? AccessToken { get; set; }
    public DateTimeOffset? ExpiresOn { get; set; }
    public string? Error { get; set; }
}

