using SqlSugar;

namespace MME.Domain.Repositories;

/// <summary>
/// API请求日志实体
/// </summary>
[SugarTable("ApiRequestLog")]
[SugarIndex("IX_ApiRequestLog_ProxyConfigId", nameof(ApiRequestLog.ProxyConfigId), OrderByType.Asc)] // 外键索引：代理配置ID
[SugarIndex("IX_ApiRequestLog_RequestTime", nameof(ApiRequestLog.RequestTime), OrderByType.Desc)] // 时间索引：请求时间（降序，最新的在前）
[SugarIndex("IX_ApiRequestLog_RequestId", nameof(ApiRequestLog.RequestId), OrderByType.Asc)] // 请求ID索引：用于关联请求和响应
[SugarIndex("IX_ApiRequestLog_ClientIp", nameof(ApiRequestLog.ClientIp), OrderByType.Asc)] // 客户端IP索引
[SugarIndex("IX_ApiRequestLog_ResponseStatusCode", nameof(ApiRequestLog.ResponseStatusCode), OrderByType.Asc)] // 响应状态码索引
public class ApiRequestLog
{
    /// <summary>
    /// 主键ID
    /// </summary>
    [SugarColumn(IsPrimaryKey = true)]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// 请求ID（用于关联请求和响应）
    /// </summary>
    public string RequestId { get; set; } = string.Empty;

    /// <summary>
    /// 代理配置ID
    /// </summary>
    public Guid ProxyConfigId { get; set; }

    /// <summary>
    /// 请求路径
    /// </summary>
    public string RequestPath { get; set; } = string.Empty;

    /// <summary>
    /// 请求方法
    /// </summary>
    public string Method { get; set; } = string.Empty;

    /// <summary>
    /// 请求头
    /// </summary>
    [SugarColumn(ColumnDataType = "text")]
    public string RequestHeaders { get; set; } = string.Empty;

    /// <summary>
    /// 请求体
    /// </summary>
    [SugarColumn(ColumnDataType = "text")]
    public string RequestBody { get; set; } = string.Empty;

    /// <summary>
    /// 响应状态码
    /// </summary>
    public int ResponseStatusCode { get; set; }

    /// <summary>
    /// 响应头
    /// </summary>
    [SugarColumn(ColumnDataType = "text")]
    public string ResponseHeaders { get; set; } = string.Empty;

    /// <summary>
    /// 响应体
    /// </summary>
    [SugarColumn(ColumnDataType = "text")]
    public string ResponseBody { get; set; } = string.Empty;

    /// <summary>
    /// 目标URL
    /// </summary>
    public string TargetUrl { get; set; } = string.Empty;

    /// <summary>
    /// 请求时间
    /// </summary>
    public DateTime RequestTime { get; set; } = DateTime.Now;

    /// <summary>
    /// 响应时间
    /// </summary>
    public DateTime? ResponseTime { get; set; }

    /// <summary>
    /// 耗时（毫秒）
    /// </summary>
    public long Duration { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 客户端IP
    /// </summary>
    public string ClientIp { get; set; } = string.Empty;

    /// <summary>
    /// User-Agent
    /// </summary>
    public string UserAgent { get; set; } = string.Empty;

    /// <summary>
    /// Token使用量信息
    /// </summary>
    [SugarColumn(ColumnDataType = "text")]
    public string? TokenUsage { get; set; }
}

/// <summary>
/// API请求日志扩展信息（用于显示列表）
/// </summary>
public class ApiRequestLogDto : ApiRequestLog
{
    /// <summary>
    /// 代理配置名称
    /// </summary>
    public string ProxyName { get; set; } = string.Empty;

    /// <summary>
    /// 模型名称（从请求体中解析）
    /// </summary>
    public string ModelName { get; set; } = string.Empty;
} 