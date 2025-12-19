namespace PowerBIExplorer.Models;

public class PowerBIConfig
{
    public string ApplicationId { get; set; } = string.Empty;
    public string ApplicationSecret { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string AuthorityUri { get; set; } = "https://login.microsoftonline.com/";
    public string ResourceUrl { get; set; } = "https://analysis.windows.net/powerbi/api";
    public string ApiUrl { get; set; } = "https://api.powerbi.com/";
    public string Scope { get; set; } = "https://analysis.windows.net/powerbi/api/.default";
}

