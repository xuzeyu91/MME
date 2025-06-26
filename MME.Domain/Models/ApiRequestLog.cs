using SqlSugar;

namespace MME.Domain.Models;

/// <summary>
/// API请求日志实体
/// </summary>
[SugarTable("ApiRequestLog")]
public class ApiRequestLog
{
    /// <summary>
    /// 主键ID
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true, ColumnDataType = "INTEGER")]
    public long Id { get; set; }

    /// <summary>
    /// 请求ID（用于关联请求和响应）
    /// </summary>
    public string RequestId { get; set; } = string.Empty;

    /// <summary>
    /// 代理配置ID
    /// </summary>
    public long ProxyConfigId { get; set; }

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
    [SugarColumn(ColumnDataType = "TEXT")]
    public string RequestHeaders { get; set; } = string.Empty;

    /// <summary>
    /// 请求体
    /// </summary>
    [SugarColumn(ColumnDataType = "TEXT")]
    public string RequestBody { get; set; } = string.Empty;

    /// <summary>
    /// 响应状态码
    /// </summary>
    public int ResponseStatusCode { get; set; }

    /// <summary>
    /// 响应头
    /// </summary>
    [SugarColumn(ColumnDataType = "TEXT")]
    public string ResponseHeaders { get; set; } = string.Empty;

    /// <summary>
    /// 响应体
    /// </summary>
    [SugarColumn(ColumnDataType = "TEXT")]
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
    [SugarColumn(ColumnDataType = "TEXT")]
    public string? TokenUsage { get; set; }
} 