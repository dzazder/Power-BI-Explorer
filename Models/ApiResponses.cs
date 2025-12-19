namespace PowerBIExplorer.Models;

public class PbiWorkspaceInfo
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsReadOnly { get; set; }
    public bool IsOnDedicatedCapacity { get; set; }
    public string? Type { get; set; }
}

public class PbiReportInfo
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? WebUrl { get; set; }
    public string? EmbedUrl { get; set; }
    public Guid? DatasetId { get; set; }
    public string? ReportType { get; set; }
}

public class PbiDatasetInfo
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? WebUrl { get; set; }
    public bool? IsRefreshable { get; set; }
    public bool? IsOnPremGatewayRequired { get; set; }
    public string? ConfiguredBy { get; set; }
}

public class PbiDashboardInfo
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? WebUrl { get; set; }
    public string? EmbedUrl { get; set; }
    public bool IsReadOnly { get; set; }
}

public class PbiTileInfo
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? EmbedUrl { get; set; }
    public Guid? ReportId { get; set; }
    public Guid? DatasetId { get; set; }
}

public class PbiRefreshHistoryInfo
{
    public string? RequestId { get; set; }
    public DateTimeOffset? StartTime { get; set; }
    public DateTimeOffset? EndTime { get; set; }
    public string? Status { get; set; }
    public string? RefreshType { get; set; }
    public string? ServiceExceptionJson { get; set; }
}

public class PbiCapacityInfo
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? Sku { get; set; }
    public string? State { get; set; }
    public string? Region { get; set; }
}

public class PbiGatewayInfo
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Type { get; set; }
    public string? PublicKey { get; set; }
}

public class PbiDataflowInfo
{
    public Guid ObjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ConfiguredBy { get; set; }
}

public class ApiResult<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Error { get; set; }
    public int? Count { get; set; }
}
