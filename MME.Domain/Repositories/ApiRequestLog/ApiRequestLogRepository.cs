using MME.Domain.Models;
using MME.Domain.Repositories.Base;
using AntSK.Domain.Repositories.Base;
using MME.Domain.Model;
using SqlSugar;

namespace MME.Domain.Repositories.ApiRequestLog;

public interface IApiRequestLogRepository : IRepository<Models.ApiRequestLog>
{
    Task<PageList<Models.ApiRequestLog>> GetLogsByProxyConfigIdAsync(long proxyConfigId, PageModel page);
    Task<List<Models.ApiRequestLog>> GetRecentLogsAsync(int count = 100);
    Task<Models.ApiRequestLog?> GetByRequestIdAsync(string requestId);
}

public class ApiRequestLogRepository : Repository<Models.ApiRequestLog>, IApiRequestLogRepository
{
    public ApiRequestLogRepository(ISqlSugarClient db) : base(db)
    {
    }

    /// <summary>
    /// 根据代理配置ID获取日志分页数据
    /// </summary>
    public async Task<PageList<Models.ApiRequestLog>> GetLogsByProxyConfigIdAsync(long proxyConfigId, PageModel page)
    {
        return await GetPageListAsync(x => x.ProxyConfigId == proxyConfigId, page, x => x.RequestTime, OrderByType.Desc);
    }

    /// <summary>
    /// 获取最近的日志记录
    /// </summary>
    public async Task<List<Models.ApiRequestLog>> GetRecentLogsAsync(int count = 100)
    {
        return await Context.Queryable<Models.ApiRequestLog>()
            .OrderByDescending(x => x.RequestTime)
            .Take(count)
            .ToListAsync();
    }

    /// <summary>
    /// 根据请求ID获取日志
    /// </summary>
    public async Task<Models.ApiRequestLog?> GetByRequestIdAsync(string requestId)
    {
        return await Context.Queryable<Models.ApiRequestLog>()
            .Where(x => x.RequestId == requestId)
            .FirstAsync();
    }
} 