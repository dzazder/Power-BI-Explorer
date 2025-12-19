using Microsoft.Identity.Client;
using Microsoft.PowerBI.Api;
using Microsoft.PowerBI.Api.Models;
using Microsoft.Rest;
using PowerBIExplorer.Models;

namespace PowerBIExplorer.Services;

public interface IPowerBIService
{
    Task<TokenResponse> GetAccessTokenAsync();
    Task<ApiResult<List<PbiWorkspaceInfo>>> GetWorkspacesAsync();
    Task<ApiResult<List<PbiReportInfo>>> GetReportsAsync(Guid workspaceId);
    Task<ApiResult<List<PbiReportInfo>>> GetReportsInMyWorkspaceAsync();
    Task<ApiResult<PbiReportInfo>> GetReportAsync(Guid workspaceId, Guid reportId);
    Task<ApiResult<List<PbiDatasetInfo>>> GetDatasetsAsync(Guid workspaceId);
    Task<ApiResult<List<PbiDatasetInfo>>> GetDatasetsInMyWorkspaceAsync();
    Task<ApiResult<List<PbiDashboardInfo>>> GetDashboardsAsync(Guid workspaceId);
    Task<ApiResult<List<PbiDashboardInfo>>> GetDashboardsInMyWorkspaceAsync();
    Task<ApiResult<List<PbiTileInfo>>> GetTilesAsync(Guid workspaceId, Guid dashboardId);
    Task<ApiResult<List<PbiRefreshHistoryInfo>>> GetRefreshHistoryAsync(Guid workspaceId, Guid datasetId);
    Task<ApiResult<bool>> RefreshDatasetAsync(Guid workspaceId, Guid datasetId);
    Task<ApiResult<EmbedConfig>> GetReportEmbedConfigAsync(Guid workspaceId, Guid reportId);
    Task<ApiResult<List<PbiCapacityInfo>>> GetCapacitiesAsync();
    Task<ApiResult<List<PbiGatewayInfo>>> GetGatewaysAsync();
    Task<ApiResult<List<PbiDataflowInfo>>> GetDataflowsAsync(Guid workspaceId);
    Task<ApiResult<string>> ExportReportAsync(Guid workspaceId, Guid reportId, string format);
}

public class PowerBIService : IPowerBIService
{
    private readonly PowerBIConfig _config;
    private readonly ILogger<PowerBIService> _logger;
    private string? _cachedToken;
    private DateTimeOffset _tokenExpiry = DateTimeOffset.MinValue;

    public PowerBIService(IConfiguration configuration, ILogger<PowerBIService> logger)
    {
        _config = new PowerBIConfig();
        configuration.GetSection("PowerBI").Bind(_config);
        _logger = logger;
    }

    public async Task<TokenResponse> GetAccessTokenAsync()
    {
        try
        {
            if (!string.IsNullOrEmpty(_cachedToken) && DateTimeOffset.UtcNow < _tokenExpiry.AddMinutes(-5))
            {
                return new TokenResponse
                {
                    Success = true,
                    AccessToken = _cachedToken,
                    ExpiresOn = _tokenExpiry
                };
            }

            if (string.IsNullOrEmpty(_config.ApplicationId) || 
                string.IsNullOrEmpty(_config.ApplicationSecret) || 
                string.IsNullOrEmpty(_config.TenantId))
            {
                return new TokenResponse
                {
                    Success = false,
                    Error = "Power BI configuration is incomplete. Please set ApplicationId, ApplicationSecret, and TenantId in appsettings.json"
                };
            }

            var authority = $"{_config.AuthorityUri}{_config.TenantId}";
            
            var app = ConfidentialClientApplicationBuilder
                .Create(_config.ApplicationId)
                .WithClientSecret(_config.ApplicationSecret)
                .WithAuthority(new Uri(authority))
                .Build();

            var scopes = new[] { _config.Scope };
            var result = await app.AcquireTokenForClient(scopes).ExecuteAsync();

            _cachedToken = result.AccessToken;
            _tokenExpiry = result.ExpiresOn;

            return new TokenResponse
            {
                Success = true,
                AccessToken = result.AccessToken,
                ExpiresOn = result.ExpiresOn
            };
        }
        catch (MsalException ex)
        {
            _logger.LogError(ex, "MSAL authentication error");
            return new TokenResponse
            {
                Success = false,
                Error = $"Authentication failed: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting access token");
            return new TokenResponse
            {
                Success = false,
                Error = $"Error: {ex.Message}"
            };
        }
    }

    private async Task<PowerBIClient?> GetPowerBIClientAsync()
    {
        var tokenResponse = await GetAccessTokenAsync();
        if (!tokenResponse.Success || string.IsNullOrEmpty(tokenResponse.AccessToken))
        {
            return null;
        }

        var tokenCredentials = new TokenCredentials(tokenResponse.AccessToken, "Bearer");
        return new PowerBIClient(new Uri(_config.ApiUrl), tokenCredentials);
    }

    public async Task<ApiResult<List<PbiWorkspaceInfo>>> GetWorkspacesAsync()
    {
        try
        {
            using var client = await GetPowerBIClientAsync();
            if (client == null)
            {
                return new ApiResult<List<PbiWorkspaceInfo>> { Success = false, Error = "Failed to authenticate" };
            }

            var groups = await client.Groups.GetGroupsAsync();
            var workspaces = groups.Value.Select(g => new PbiWorkspaceInfo
            {
                Id = g.Id,
                Name = g.Name ?? "",
                IsReadOnly = g.IsReadOnly ?? false,
                IsOnDedicatedCapacity = g.IsOnDedicatedCapacity ?? false,
                Type = "Workspace"
            }).ToList();

            return new ApiResult<List<PbiWorkspaceInfo>> 
            { 
                Success = true, 
                Data = workspaces,
                Count = workspaces.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting workspaces");
            return new ApiResult<List<PbiWorkspaceInfo>> { Success = false, Error = ex.Message };
        }
    }

    public async Task<ApiResult<List<PbiReportInfo>>> GetReportsAsync(Guid workspaceId)
    {
        try
        {
            using var client = await GetPowerBIClientAsync();
            if (client == null)
            {
                return new ApiResult<List<PbiReportInfo>> { Success = false, Error = "Failed to authenticate" };
            }

            var reports = await client.Reports.GetReportsInGroupAsync(workspaceId);
            var reportList = reports.Value.Select(r => new PbiReportInfo
            {
                Id = r.Id,
                Name = r.Name ?? "",
                WebUrl = r.WebUrl,
                EmbedUrl = r.EmbedUrl,
                DatasetId = !string.IsNullOrEmpty(r.DatasetId) ? Guid.Parse(r.DatasetId) : null,
                ReportType = r.ReportType?.ToString()
            }).ToList();

            return new ApiResult<List<PbiReportInfo>> 
            { 
                Success = true, 
                Data = reportList,
                Count = reportList.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reports for workspace {WorkspaceId}", workspaceId);
            return new ApiResult<List<PbiReportInfo>> { Success = false, Error = ex.Message };
        }
    }

    public async Task<ApiResult<List<PbiReportInfo>>> GetReportsInMyWorkspaceAsync()
    {
        try
        {
            using var client = await GetPowerBIClientAsync();
            if (client == null)
            {
                return new ApiResult<List<PbiReportInfo>> { Success = false, Error = "Failed to authenticate" };
            }

            var reports = await client.Reports.GetReportsAsync();
            var reportList = reports.Value.Select(r => new PbiReportInfo
            {
                Id = r.Id,
                Name = r.Name ?? "",
                WebUrl = r.WebUrl,
                EmbedUrl = r.EmbedUrl,
                DatasetId = !string.IsNullOrEmpty(r.DatasetId) ? Guid.Parse(r.DatasetId) : null,
                ReportType = r.ReportType?.ToString()
            }).ToList();

            return new ApiResult<List<PbiReportInfo>> 
            { 
                Success = true, 
                Data = reportList,
                Count = reportList.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reports in My Workspace");
            return new ApiResult<List<PbiReportInfo>> { Success = false, Error = ex.Message };
        }
    }

    public async Task<ApiResult<PbiReportInfo>> GetReportAsync(Guid workspaceId, Guid reportId)
    {
        try
        {
            using var client = await GetPowerBIClientAsync();
            if (client == null)
            {
                return new ApiResult<PbiReportInfo> { Success = false, Error = "Failed to authenticate" };
            }

            var report = await client.Reports.GetReportInGroupAsync(workspaceId, reportId);
            var reportInfo = new PbiReportInfo
            {
                Id = report.Id,
                Name = report.Name ?? "",
                WebUrl = report.WebUrl,
                EmbedUrl = report.EmbedUrl,
                DatasetId = !string.IsNullOrEmpty(report.DatasetId) ? Guid.Parse(report.DatasetId) : null,
                ReportType = report.ReportType?.ToString()
            };

            return new ApiResult<PbiReportInfo> { Success = true, Data = reportInfo };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting report {ReportId}", reportId);
            return new ApiResult<PbiReportInfo> { Success = false, Error = ex.Message };
        }
    }

    public async Task<ApiResult<List<PbiDatasetInfo>>> GetDatasetsAsync(Guid workspaceId)
    {
        try
        {
            using var client = await GetPowerBIClientAsync();
            if (client == null)
            {
                return new ApiResult<List<PbiDatasetInfo>> { Success = false, Error = "Failed to authenticate" };
            }

            var datasets = await client.Datasets.GetDatasetsInGroupAsync(workspaceId);
            var datasetList = datasets.Value.Select(d => new PbiDatasetInfo
            {
                Id = Guid.Parse(d.Id),
                Name = d.Name ?? "",
                WebUrl = d.WebUrl,
                IsRefreshable = d.IsRefreshable,
                IsOnPremGatewayRequired = d.IsOnPremGatewayRequired,
                ConfiguredBy = d.ConfiguredBy
            }).ToList();

            return new ApiResult<List<PbiDatasetInfo>> 
            { 
                Success = true, 
                Data = datasetList,
                Count = datasetList.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting datasets for workspace {WorkspaceId}", workspaceId);
            return new ApiResult<List<PbiDatasetInfo>> { Success = false, Error = ex.Message };
        }
    }

    public async Task<ApiResult<List<PbiDatasetInfo>>> GetDatasetsInMyWorkspaceAsync()
    {
        try
        {
            using var client = await GetPowerBIClientAsync();
            if (client == null)
            {
                return new ApiResult<List<PbiDatasetInfo>> { Success = false, Error = "Failed to authenticate" };
            }

            var datasets = await client.Datasets.GetDatasetsAsync();
            var datasetList = datasets.Value.Select(d => new PbiDatasetInfo
            {
                Id = Guid.Parse(d.Id),
                Name = d.Name ?? "",
                WebUrl = d.WebUrl,
                IsRefreshable = d.IsRefreshable,
                IsOnPremGatewayRequired = d.IsOnPremGatewayRequired,
                ConfiguredBy = d.ConfiguredBy
            }).ToList();

            return new ApiResult<List<PbiDatasetInfo>> 
            { 
                Success = true, 
                Data = datasetList,
                Count = datasetList.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting datasets in My Workspace");
            return new ApiResult<List<PbiDatasetInfo>> { Success = false, Error = ex.Message };
        }
    }

    public async Task<ApiResult<List<PbiDashboardInfo>>> GetDashboardsAsync(Guid workspaceId)
    {
        try
        {
            using var client = await GetPowerBIClientAsync();
            if (client == null)
            {
                return new ApiResult<List<PbiDashboardInfo>> { Success = false, Error = "Failed to authenticate" };
            }

            var dashboards = await client.Dashboards.GetDashboardsInGroupAsync(workspaceId);
            var dashboardList = dashboards.Value.Select(d => new PbiDashboardInfo
            {
                Id = d.Id,
                DisplayName = d.DisplayName ?? "",
                WebUrl = d.WebUrl,
                EmbedUrl = d.EmbedUrl,
                IsReadOnly = d.IsReadOnly ?? false
            }).ToList();

            return new ApiResult<List<PbiDashboardInfo>> 
            { 
                Success = true, 
                Data = dashboardList,
                Count = dashboardList.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboards for workspace {WorkspaceId}", workspaceId);
            return new ApiResult<List<PbiDashboardInfo>> { Success = false, Error = ex.Message };
        }
    }

    public async Task<ApiResult<List<PbiDashboardInfo>>> GetDashboardsInMyWorkspaceAsync()
    {
        try
        {
            using var client = await GetPowerBIClientAsync();
            if (client == null)
            {
                return new ApiResult<List<PbiDashboardInfo>> { Success = false, Error = "Failed to authenticate" };
            }

            var dashboards = await client.Dashboards.GetDashboardsAsync();
            var dashboardList = dashboards.Value.Select(d => new PbiDashboardInfo
            {
                Id = d.Id,
                DisplayName = d.DisplayName ?? "",
                WebUrl = d.WebUrl,
                EmbedUrl = d.EmbedUrl,
                IsReadOnly = d.IsReadOnly ?? false
            }).ToList();

            return new ApiResult<List<PbiDashboardInfo>> 
            { 
                Success = true, 
                Data = dashboardList,
                Count = dashboardList.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboards in My Workspace");
            return new ApiResult<List<PbiDashboardInfo>> { Success = false, Error = ex.Message };
        }
    }

    public async Task<ApiResult<List<PbiTileInfo>>> GetTilesAsync(Guid workspaceId, Guid dashboardId)
    {
        try
        {
            using var client = await GetPowerBIClientAsync();
            if (client == null)
            {
                return new ApiResult<List<PbiTileInfo>> { Success = false, Error = "Failed to authenticate" };
            }

            var tiles = await client.Dashboards.GetTilesInGroupAsync(workspaceId, dashboardId);
            var tileList = tiles.Value.Select(t => new PbiTileInfo
            {
                Id = t.Id,
                Title = t.Title ?? "",
                EmbedUrl = t.EmbedUrl,
                ReportId = t.ReportId,
                DatasetId = t.DatasetId != null ? Guid.Parse(t.DatasetId) : null
            }).ToList();

            return new ApiResult<List<PbiTileInfo>> 
            { 
                Success = true, 
                Data = tileList,
                Count = tileList.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tiles for dashboard {DashboardId}", dashboardId);
            return new ApiResult<List<PbiTileInfo>> { Success = false, Error = ex.Message };
        }
    }

    public async Task<ApiResult<List<PbiRefreshHistoryInfo>>> GetRefreshHistoryAsync(Guid workspaceId, Guid datasetId)
    {
        try
        {
            using var client = await GetPowerBIClientAsync();
            if (client == null)
            {
                return new ApiResult<List<PbiRefreshHistoryInfo>> { Success = false, Error = "Failed to authenticate" };
            }

            var refreshes = await client.Datasets.GetRefreshHistoryInGroupAsync(workspaceId, datasetId.ToString());
            var refreshList = refreshes.Value.Select(r => new PbiRefreshHistoryInfo
            {
                RequestId = r.RequestId,
                StartTime = r.StartTime,
                EndTime = r.EndTime,
                Status = r.Status,
                RefreshType = r.RefreshType,
                ServiceExceptionJson = r.ServiceExceptionJson
            }).ToList();

            return new ApiResult<List<PbiRefreshHistoryInfo>> 
            { 
                Success = true, 
                Data = refreshList,
                Count = refreshList.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting refresh history for dataset {DatasetId}", datasetId);
            return new ApiResult<List<PbiRefreshHistoryInfo>> { Success = false, Error = ex.Message };
        }
    }

    public async Task<ApiResult<bool>> RefreshDatasetAsync(Guid workspaceId, Guid datasetId)
    {
        try
        {
            using var client = await GetPowerBIClientAsync();
            if (client == null)
            {
                return new ApiResult<bool> { Success = false, Error = "Failed to authenticate" };
            }

            await client.Datasets.RefreshDatasetInGroupAsync(workspaceId, datasetId.ToString());
            return new ApiResult<bool> { Success = true, Data = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing dataset {DatasetId}", datasetId);
            return new ApiResult<bool> { Success = false, Error = ex.Message };
        }
    }

    public async Task<ApiResult<EmbedConfig>> GetReportEmbedConfigAsync(Guid workspaceId, Guid reportId)
    {
        try
        {
            using var client = await GetPowerBIClientAsync();
            if (client == null)
            {
                return new ApiResult<EmbedConfig> { Success = false, Error = "Failed to authenticate" };
            }

            var report = await client.Reports.GetReportInGroupAsync(workspaceId, reportId);
            
            var generateTokenRequest = new GenerateTokenRequest(
                accessLevel: TokenAccessLevel.View,
                allowSaveAs: false
            );

            var embedToken = await client.Reports.GenerateTokenInGroupAsync(workspaceId, reportId, generateTokenRequest);

            var embedConfig = new EmbedConfig
            {
                ReportId = report.Id.ToString(),
                ReportName = report.Name,
                EmbedUrl = report.EmbedUrl,
                EmbedToken = embedToken.Token,
                TokenExpiry = embedToken.Expiration
            };

            return new ApiResult<EmbedConfig> { Success = true, Data = embedConfig };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting embed config for report {ReportId}", reportId);
            return new ApiResult<EmbedConfig> { Success = false, Error = ex.Message };
        }
    }

    public async Task<ApiResult<List<PbiCapacityInfo>>> GetCapacitiesAsync()
    {
        try
        {
            using var client = await GetPowerBIClientAsync();
            if (client == null)
            {
                return new ApiResult<List<PbiCapacityInfo>> { Success = false, Error = "Failed to authenticate" };
            }

            var capacities = await client.Capacities.GetCapacitiesAsync();
            var capacityList = capacities.Value.Select(c => new PbiCapacityInfo
            {
                Id = c.Id,
                DisplayName = c.DisplayName ?? "",
                Sku = c.Sku,
                State = c.State.ToString(),
                Region = c.Region
            }).ToList();

            return new ApiResult<List<PbiCapacityInfo>> 
            { 
                Success = true, 
                Data = capacityList,
                Count = capacityList.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting capacities");
            return new ApiResult<List<PbiCapacityInfo>> { Success = false, Error = ex.Message };
        }
    }

    public async Task<ApiResult<List<PbiGatewayInfo>>> GetGatewaysAsync()
    {
        try
        {
            using var client = await GetPowerBIClientAsync();
            if (client == null)
            {
                return new ApiResult<List<PbiGatewayInfo>> { Success = false, Error = "Failed to authenticate" };
            }

            var gateways = await client.Gateways.GetGatewaysAsync();
            var gatewayList = gateways.Value.Select(g => new PbiGatewayInfo
            {
                Id = g.Id,
                Name = g.Name ?? "",
                Type = g.Type,
                PublicKey = g.PublicKey?.Exponent
            }).ToList();

            return new ApiResult<List<PbiGatewayInfo>> 
            { 
                Success = true, 
                Data = gatewayList,
                Count = gatewayList.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting gateways");
            return new ApiResult<List<PbiGatewayInfo>> { Success = false, Error = ex.Message };
        }
    }

    public async Task<ApiResult<List<PbiDataflowInfo>>> GetDataflowsAsync(Guid workspaceId)
    {
        try
        {
            using var client = await GetPowerBIClientAsync();
            if (client == null)
            {
                return new ApiResult<List<PbiDataflowInfo>> { Success = false, Error = "Failed to authenticate" };
            }

            var dataflows = await client.Dataflows.GetDataflowsAsync(workspaceId);
            var dataflowList = dataflows.Value.Select(d => new PbiDataflowInfo
            {
                ObjectId = d.ObjectId,
                Name = d.Name ?? "",
                Description = d.Description,
                ConfiguredBy = d.ConfiguredBy
            }).ToList();

            return new ApiResult<List<PbiDataflowInfo>> 
            { 
                Success = true, 
                Data = dataflowList,
                Count = dataflowList.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dataflows for workspace {WorkspaceId}", workspaceId);
            return new ApiResult<List<PbiDataflowInfo>> { Success = false, Error = ex.Message };
        }
    }

    public async Task<ApiResult<string>> ExportReportAsync(Guid workspaceId, Guid reportId, string format)
    {
        try
        {
            using var client = await GetPowerBIClientAsync();
            if (client == null)
            {
                return new ApiResult<string> { Success = false, Error = "Failed to authenticate" };
            }

            FileFormat fileFormat = format.ToLower() switch
            {
                "pdf" => FileFormat.PDF,
                "pptx" => FileFormat.PPTX,
                "png" => FileFormat.PNG,
                _ => FileFormat.PDF
            };

            var exportRequest = new ExportReportRequest
            {
                Format = fileFormat
            };

            var export = await client.Reports.ExportToFileInGroupAsync(workspaceId, reportId, exportRequest);
            
            return new ApiResult<string> 
            { 
                Success = true, 
                Data = $"Export initiated. Export ID: {export.Id}, Status: {export.Status}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting report {ReportId}", reportId);
            return new ApiResult<string> { Success = false, Error = ex.Message };
        }
    }
}
