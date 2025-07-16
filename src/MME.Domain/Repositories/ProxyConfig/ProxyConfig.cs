using SqlSugar;

namespace MME.Domain.Repositories;

/// <summary>
/// 代理配置实体
/// </summary>
[SugarTable("ProxyConfig")]
[SugarIndex("IX_ProxyConfig_Name", nameof(ProxyConfig.Name), OrderByType.Asc)] // 唯一索引：配置名称
[SugarIndex("IX_ProxyConfig_BearerToken", nameof(ProxyConfig.BearerToken), OrderByType.Asc)] // 唯一索引：Bearer Token
[SugarIndex("IX_ProxyConfig_IsEnabled", nameof(ProxyConfig.IsEnabled), OrderByType.Asc)] // 普通索引：启用状态
[SugarIndex("IX_ProxyConfig_CreateTime", nameof(ProxyConfig.CreateTime), OrderByType.Desc)] // 普通索引：创建时间
public class ProxyConfig
{
    /// <summary>
    /// 主键ID
    /// </summary>
    [SugarColumn(IsPrimaryKey = true)]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// 配置名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 目标服务器URL（如：https://api.openai.com）
    /// </summary>
    public string TargetUrl { get; set; } = string.Empty;

    /// <summary>
    /// API密钥
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// 生成的Bearer Token（用于客户端访问）
    /// </summary>
    public string BearerToken { get; set; } = string.Empty;

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 支持的路径列表（JSON格式，如：["/v1/chat/completions", "/v1/embeddings"]）
    /// </summary>
    [SugarColumn(ColumnDataType = "text")]
    public string SupportedPaths { get; set; } = "[\"/v1/chat/completions\", \"/v1/embeddings\", \"/v1/rerank\"]";

    /// <summary>
    /// 请求超时时间（秒）
    /// </summary>
    public int TimeoutSeconds { get; set; } = 3000;

    /// <summary>
    /// 最大重试次数
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// 是否记录请求体
    /// </summary>
    public bool LogRequestBody { get; set; } = true;

    /// <summary>
    /// 是否记录响应体
    /// </summary>
    public bool LogResponseBody { get; set; } = true;

    /// <summary>
    /// 描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreateTime { get; set; } = DateTime.Now;

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdateTime { get; set; } = DateTime.Now;
} 