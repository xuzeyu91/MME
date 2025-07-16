using MME.Domain.Repositories;
using MME.Domain.Common.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace MME.Domain.Services;

public interface IProxyService
{
    Task<ProxyConfig> CreateConfigAsync(string name, string targetUrl, string apiKey, string? description = null, 
        string? supportedPaths = null, int timeoutSeconds = 300, int maxRetries = 3, bool logRequestBody = true, bool logResponseBody = true);
    Task<ProxyConfig> UpdateConfigAsync(Guid id, string name, string targetUrl, string apiKey, string? description = null,
        string? supportedPaths = null, int timeoutSeconds = 300, int maxRetries = 3, bool logRequestBody = true, bool logResponseBody = true);
    Task<bool> DeleteConfigAsync(Guid id);
    Task<bool> ToggleConfigAsync(Guid id, bool isEnabled);
    Task<ProxyConfig?> GetConfigByIdAsync(Guid id);
    Task<List<ProxyConfig>> GetAllConfigsAsync();
    Task<ProxyConfig?> GetConfigByBearerTokenAsync(string bearerToken);
    Task<string> RefreshBearerTokenAsync(Guid id);
}

[ServiceDescription(typeof(IProxyService), ServiceLifetime.Scoped)]
public class ProxyService : IProxyService
{
    private readonly IProxyConfigRepository _proxyConfigRepository;

    public ProxyService(IProxyConfigRepository proxyConfigRepository)
    {
        _proxyConfigRepository = proxyConfigRepository;
    }

    /// <summary>
    /// 创建代理配置
    /// </summary>
    public async Task<ProxyConfig> CreateConfigAsync(string name, string targetUrl, string apiKey, string? description = null,
        string? supportedPaths = null, int timeoutSeconds = 300, int maxRetries = 3, bool logRequestBody = true, bool logResponseBody = true)
    {
        var bearerToken = await _proxyConfigRepository.GenerateUniqueBearerTokenAsync();
        
        var config = new ProxyConfig
        {
            Name = name,
            TargetUrl = targetUrl.TrimEnd('/'),
            ApiKey = apiKey,
            BearerToken = bearerToken,
            Description = description,
            SupportedPaths = supportedPaths ?? "[\"/v1/chat/completions\", \"/v1/embeddings\", \"/v1/rerank\"]",
            TimeoutSeconds = timeoutSeconds,
            MaxRetries = maxRetries,
            LogRequestBody = logRequestBody,
            LogResponseBody = logResponseBody,
            CreateTime = DateTime.Now,
            UpdateTime = DateTime.Now
        };

        await _proxyConfigRepository.InsertAsync(config);
        return config;
    }

    /// <summary>
    /// 更新代理配置
    /// </summary>
    public async Task<ProxyConfig> UpdateConfigAsync(Guid id, string name, string targetUrl, string apiKey, string? description = null,
        string? supportedPaths = null, int timeoutSeconds = 300, int maxRetries = 3, bool logRequestBody = true, bool logResponseBody = true)
    {
        var config = await _proxyConfigRepository.GetByIdAsync(id);
        if (config == null)
            throw new ArgumentException($"未找到ID为{id}的配置");

        config.Name = name;
        config.TargetUrl = targetUrl.TrimEnd('/');
        config.ApiKey = apiKey;
        config.Description = description;
        config.SupportedPaths = supportedPaths ?? config.SupportedPaths;
        config.TimeoutSeconds = timeoutSeconds;
        config.MaxRetries = maxRetries;
        config.LogRequestBody = logRequestBody;
        config.LogResponseBody = logResponseBody;
        config.UpdateTime = DateTime.Now;

        await _proxyConfigRepository.UpdateAsync(config);
        return config;
    }

    /// <summary>
    /// 删除代理配置
    /// </summary>
    public async Task<bool> DeleteConfigAsync(Guid id)
    {
        return await _proxyConfigRepository.DeleteAsync(id);
    }

    /// <summary>
    /// 启用/禁用代理配置
    /// </summary>
    public async Task<bool> ToggleConfigAsync(Guid id, bool isEnabled)
    {
        var config = await _proxyConfigRepository.GetByIdAsync(id);
        if (config == null)
            return false;

        config.IsEnabled = isEnabled;
        config.UpdateTime = DateTime.Now;

        return await _proxyConfigRepository.UpdateAsync(config);
    }

    /// <summary>
    /// 根据ID获取配置
    /// </summary>
    public async Task<ProxyConfig?> GetConfigByIdAsync(Guid id)
    {
        return await _proxyConfigRepository.GetByIdAsync(id);
    }

    /// <summary>
    /// 获取所有配置
    /// </summary>
    public async Task<List<ProxyConfig>> GetAllConfigsAsync()
    {
        return await _proxyConfigRepository.GetListAsync();
    }

    /// <summary>
    /// 根据Bearer Token获取配置
    /// </summary>
    public async Task<ProxyConfig?> GetConfigByBearerTokenAsync(string bearerToken)
    {
        return await _proxyConfigRepository.GetByBearerTokenAsync(bearerToken);
    }

    /// <summary>
    /// 刷新Bearer Token
    /// </summary>
    public async Task<string> RefreshBearerTokenAsync(Guid id)
    {
        var config = await _proxyConfigRepository.GetByIdAsync(id);
        if (config == null)
            throw new ArgumentException($"未找到ID为{id}的配置");

        var newToken = await _proxyConfigRepository.GenerateUniqueBearerTokenAsync();
        config.BearerToken = newToken;
        config.UpdateTime = DateTime.Now;

        await _proxyConfigRepository.UpdateAsync(config);
        return newToken;
    }
} 