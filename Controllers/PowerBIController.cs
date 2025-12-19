using Microsoft.AspNetCore.Mvc;
using PowerBIExplorer.Services;

namespace PowerBIExplorer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PowerBIController : ControllerBase
{
    private readonly IPowerBIService _powerBIService;
    private readonly ILogger<PowerBIController> _logger;

    public PowerBIController(IPowerBIService powerBIService, ILogger<PowerBIController> logger)
    {
        _powerBIService = powerBIService;
        _logger = logger;
    }

    /// <summary>
    /// Get access token for Power BI API
    /// </summary>
    [HttpGet("token")]
    public async Task<IActionResult> GetToken()
    {
        var result = await _powerBIService.GetAccessTokenAsync();
        return Ok(result);
    }

    /// <summary>
    /// Get all workspaces (groups)
    /// </summary>
    [HttpGet("workspaces")]
    public async Task<IActionResult> GetWorkspaces()
    {
        var result = await _powerBIService.GetWorkspacesAsync();
        return Ok(result);
    }

    /// <summary>
    /// Get reports in a workspace
    /// </summary>
    [HttpGet("workspaces/{workspaceId}/reports")]
    public async Task<IActionResult> GetReports(Guid workspaceId)
    {
        var result = await _powerBIService.GetReportsAsync(workspaceId);
        return Ok(result);
    }

    /// <summary>
    /// Get reports in My Workspace
    /// </summary>
    [HttpGet("reports")]
    public async Task<IActionResult> GetReportsInMyWorkspace()
    {
        var result = await _powerBIService.GetReportsInMyWorkspaceAsync();
        return Ok(result);
    }

    /// <summary>
    /// Get a specific report
    /// </summary>
    [HttpGet("workspaces/{workspaceId}/reports/{reportId}")]
    public async Task<IActionResult> GetReport(Guid workspaceId, Guid reportId)
    {
        var result = await _powerBIService.GetReportAsync(workspaceId, reportId);
        return Ok(result);
    }

    /// <summary>
    /// Get datasets in a workspace
    /// </summary>
    [HttpGet("workspaces/{workspaceId}/datasets")]
    public async Task<IActionResult> GetDatasets(Guid workspaceId)
    {
        var result = await _powerBIService.GetDatasetsAsync(workspaceId);
        return Ok(result);
    }

    /// <summary>
    /// Get datasets in My Workspace
    /// </summary>
    [HttpGet("datasets")]
    public async Task<IActionResult> GetDatasetsInMyWorkspace()
    {
        var result = await _powerBIService.GetDatasetsInMyWorkspaceAsync();
        return Ok(result);
    }

    /// <summary>
    /// Get dashboards in a workspace
    /// </summary>
    [HttpGet("workspaces/{workspaceId}/dashboards")]
    public async Task<IActionResult> GetDashboards(Guid workspaceId)
    {
        var result = await _powerBIService.GetDashboardsAsync(workspaceId);
        return Ok(result);
    }

    /// <summary>
    /// Get dashboards in My Workspace
    /// </summary>
    [HttpGet("dashboards")]
    public async Task<IActionResult> GetDashboardsInMyWorkspace()
    {
        var result = await _powerBIService.GetDashboardsInMyWorkspaceAsync();
        return Ok(result);
    }

    /// <summary>
    /// Get tiles in a dashboard
    /// </summary>
    [HttpGet("workspaces/{workspaceId}/dashboards/{dashboardId}/tiles")]
    public async Task<IActionResult> GetTiles(Guid workspaceId, Guid dashboardId)
    {
        var result = await _powerBIService.GetTilesAsync(workspaceId, dashboardId);
        return Ok(result);
    }

    /// <summary>
    /// Get refresh history for a dataset
    /// </summary>
    [HttpGet("workspaces/{workspaceId}/datasets/{datasetId}/refreshes")]
    public async Task<IActionResult> GetRefreshHistory(Guid workspaceId, Guid datasetId)
    {
        var result = await _powerBIService.GetRefreshHistoryAsync(workspaceId, datasetId);
        return Ok(result);
    }

    /// <summary>
    /// Trigger dataset refresh
    /// </summary>
    [HttpPost("workspaces/{workspaceId}/datasets/{datasetId}/refresh")]
    public async Task<IActionResult> RefreshDataset(Guid workspaceId, Guid datasetId)
    {
        var result = await _powerBIService.RefreshDatasetAsync(workspaceId, datasetId);
        return Ok(result);
    }

    /// <summary>
    /// Get embed configuration for a report
    /// </summary>
    [HttpGet("workspaces/{workspaceId}/reports/{reportId}/embed")]
    public async Task<IActionResult> GetReportEmbedConfig(Guid workspaceId, Guid reportId)
    {
        var result = await _powerBIService.GetReportEmbedConfigAsync(workspaceId, reportId);
        return Ok(result);
    }

    /// <summary>
    /// Get all capacities
    /// </summary>
    [HttpGet("capacities")]
    public async Task<IActionResult> GetCapacities()
    {
        var result = await _powerBIService.GetCapacitiesAsync();
        return Ok(result);
    }

    /// <summary>
    /// Get all gateways
    /// </summary>
    [HttpGet("gateways")]
    public async Task<IActionResult> GetGateways()
    {
        var result = await _powerBIService.GetGatewaysAsync();
        return Ok(result);
    }

    /// <summary>
    /// Get dataflows in a workspace
    /// </summary>
    [HttpGet("workspaces/{workspaceId}/dataflows")]
    public async Task<IActionResult> GetDataflows(Guid workspaceId)
    {
        var result = await _powerBIService.GetDataflowsAsync(workspaceId);
        return Ok(result);
    }

    /// <summary>
    /// Export report to file
    /// </summary>
    [HttpPost("workspaces/{workspaceId}/reports/{reportId}/export")]
    public async Task<IActionResult> ExportReport(Guid workspaceId, Guid reportId, [FromQuery] string format = "pdf")
    {
        var result = await _powerBIService.ExportReportAsync(workspaceId, reportId, format);
        return Ok(result);
    }
}

