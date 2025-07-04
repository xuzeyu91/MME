using MME.Domain.Repositories.Base;
using AntSK.Domain.Repositories.Base;
using SqlSugar;
using System.Text.Json;
using MME.Domain.Common.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace MME.Domain.Repositories;

public interface IProxyConfigRepository : IRepository<ProxyConfig>
{
    Task<ProxyConfig?> GetByBearerTokenAsync(string bearerToken);
    Task<List<ProxyConfig>> GetEnabledConfigsAsync();
    Task<string> GenerateUniqueBearerTokenAsync();
}

[ServiceDescription(typeof(IProxyConfigRepository), ServiceLifetime.Scoped)]
public class ProxyConfigRepository : Repository<ProxyConfig>, IProxyConfigRepository
{
    public ProxyConfigRepository(ISqlSugarClient db) : base(db)
    {
    }

    /// <summary>
    /// 根据Bearer Token获取配置
    /// </summary>
    public async Task<ProxyConfig?> GetByBearerTokenAsync(string bearerToken)
    {
        return await Context.Queryable<ProxyConfig>()
            .Where(x => x.BearerToken == bearerToken && x.IsEnabled)
            .FirstAsync();
    }

    /// <summary>
    /// 获取所有启用的配置
    /// </summary>
    public async Task<List<ProxyConfig>> GetEnabledConfigsAsync()
    {
        return await Context.Queryable<ProxyConfig>()
            .Where(x => x.IsEnabled)
            .ToListAsync();
    }

    /// <summary>
    /// 生成唯一的Bearer Token
    /// </summary>
    public async Task<string> GenerateUniqueBearerTokenAsync()
    {
        string token;
        bool exists;
        
        do
        {
            token = GenerateRandomToken();
            exists = await Context.Queryable<ProxyConfig>()
                .Where(x => x.BearerToken == token)
                .AnyAsync();
        } while (exists);

        return token;
    }

    private static string GenerateRandomToken()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var random = new Random();
        var result = new char[32];
        
        for (int i = 0; i < result.Length; i++)
        {
            result[i] = chars[random.Next(chars.Length)];
        }
        
        return "pk-" + new string(result);
    }
} 