using MME.Domain.Models;
using MME.Domain.Repositories.ProxyConfig;
using MME.Domain.Common.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace MME.Domain.Services;

public interface IProxyService
{
    Task<Models.ProxyConfig> CreateConfigAsync(string name, string targetUrl, string apiKey, string? description = null);
    Task<Models.ProxyConfig> UpdateConfigAsync(long id, string name, string targetUrl, string apiKey, string? description = null);
    Task<bool> DeleteConfigAsync(long id);
    Task<bool> ToggleConfigAsync(long id, bool isEnabled);
    Task<Models.ProxyConfig?> GetConfigByIdAsync(long id);
    Task<List<Models.ProxyConfig>> GetAllConfigsAsync();
    Task<Models.ProxyConfig?> GetConfigByBearerTokenAsync(string bearerToken);
    Task<string> RefreshBearerTokenAsync(long id);
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
    public async Task<Models.ProxyConfig> CreateConfigAsync(string name, string targetUrl, string apiKey, string? description = null)
    {
        var bearerToken = await _proxyConfigRepository.GenerateUniqueBearerTokenAsync();
        
        var config = new Models.ProxyConfig
        {
            Name = name,
            TargetUrl = targetUrl.TrimEnd('/'),
            ApiKey = apiKey,
            BearerToken = bearerToken,
            Description = description,
            CreateTime = DateTime.Now,
            UpdateTime = DateTime.Now
        };

        await _proxyConfigRepository.InsertAsync(config);
        return config;
    }

    /// <summary>
    /// 更新代理配置
    /// </summary>
    public async Task<Models.ProxyConfig> UpdateConfigAsync(long id, string name, string targetUrl, string apiKey, string? description = null)
    {
        var config = await _proxyConfigRepository.GetByIdAsync(id);
        if (config == null)
            throw new ArgumentException($"未找到ID为{id}的配置");

        config.Name = name;
        config.TargetUrl = targetUrl.TrimEnd('/');
        config.ApiKey = apiKey;
        config.Description = description;
        config.UpdateTime = DateTime.Now;

        await _proxyConfigRepository.UpdateAsync(config);
        return config;
    }

    /// <summary>
    /// 删除代理配置
    /// </summary>
    public async Task<bool> DeleteConfigAsync(long id)
    {
        return await _proxyConfigRepository.DeleteAsync(id);
    }

    /// <summary>
    /// 启用/禁用代理配置
    /// </summary>
    public async Task<bool> ToggleConfigAsync(long id, bool isEnabled)
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
    public async Task<Models.ProxyConfig?> GetConfigByIdAsync(long id)
    {
        return await _proxyConfigRepository.GetByIdAsync(id);
    }

    /// <summary>
    /// 获取所有配置
    /// </summary>
    public async Task<List<Models.ProxyConfig>> GetAllConfigsAsync()
    {
        return await _proxyConfigRepository.GetListAsync();
    }

    /// <summary>
    /// 根据Bearer Token获取配置
    /// </summary>
    public async Task<Models.ProxyConfig?> GetConfigByBearerTokenAsync(string bearerToken)
    {
        return await _proxyConfigRepository.GetByBearerTokenAsync(bearerToken);
    }

    /// <summary>
    /// 刷新Bearer Token
    /// </summary>
    public async Task<string> RefreshBearerTokenAsync(long id)
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