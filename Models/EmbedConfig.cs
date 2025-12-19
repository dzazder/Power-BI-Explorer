namespace PowerBIExplorer.Models;

public class EmbedConfig
{
    public string? ReportId { get; set; }
    public string? ReportName { get; set; }
    public string? EmbedUrl { get; set; }
    public string? EmbedToken { get; set; }
    public DateTimeOffset? TokenExpiry { get; set; }
}

