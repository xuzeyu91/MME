using MME.Domain.Models;
using MME.Domain.Repositories.Base;
using AntSK.Domain.Repositories.Base;
using MME.Domain.Model;
using SqlSugar;
using MME.Domain.Common.Extensions;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace MME.Domain.Repositories.ApiRequestLog;

public interface IApiRequestLogRepository : IRepository<Models.ApiRequestLog>
{
    Task<PageList<Models.ApiRequestLog>> GetLogsByProxyConfigIdAsync(long proxyConfigId, PageModel page);
    Task<List<Models.ApiRequestLog>> GetRecentLogsAsync(int count = 100);
    Task<Models.ApiRequestLog?> GetByRequestIdAsync(string requestId);
    Task<PageList<ApiRequestLogDto>> GetLogsWithProxyInfoAsync(
        System.Linq.Expressions.Expression<Func<Models.ApiRequestLog, bool>> whereExpression,
        PageModel page,
        OrderByType orderByType = OrderByType.Desc);
}

[ServiceDescription(typeof(IApiRequestLogRepository), ServiceLifetime.Scoped)]
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

    /// <summary>
    /// 获取包含代理信息的日志分页数据
    /// </summary>
    public async Task<PageList<ApiRequestLogDto>> GetLogsWithProxyInfoAsync(
        System.Linq.Expressions.Expression<Func<Models.ApiRequestLog, bool>> whereExpression,
        PageModel page,
        OrderByType orderByType = OrderByType.Desc)
    {
        var queryable = Context.Queryable<Models.ApiRequestLog, Models.ProxyConfig>((log, proxy) => new JoinQueryInfos(
                JoinType.Left, log.ProxyConfigId == proxy.Id))
            .Where(whereExpression)
            .OrderBy((log, proxy) => log.RequestTime, orderByType)
            .Select((log, proxy) => new ApiRequestLogDto
            {
                Id = log.Id,
                RequestId = log.RequestId,
                ProxyConfigId = log.ProxyConfigId,
                RequestPath = log.RequestPath,
                Method = log.Method,
                RequestHeaders = log.RequestHeaders,
                RequestBody = log.RequestBody,
                ResponseStatusCode = log.ResponseStatusCode,
                ResponseHeaders = log.ResponseHeaders,
                ResponseBody = log.ResponseBody,
                TargetUrl = log.TargetUrl,
                RequestTime = log.RequestTime,
                ResponseTime = log.ResponseTime,
                Duration = log.Duration,
                ErrorMessage = log.ErrorMessage,
                ClientIp = log.ClientIp,
                UserAgent = log.UserAgent,
                TokenUsage = log.TokenUsage,
                ProxyName = proxy.Name ?? "",
                ModelName = "" // 将在后续处理中从RequestBody解析
            });

        var totalCount = await queryable.CountAsync();
        var list = await queryable.ToPageListAsync(page.PageIndex, page.PageSize);

        // 解析模型名称
        foreach (var log in list)
        {
            log.ModelName = ExtractModelName(log.RequestBody);
        }

        return new PageList<ApiRequestLogDto>
        {
            List = list,
            TotalCount = totalCount,
            PageIndex = page.PageIndex,
            PageSize = page.PageSize
        };
    }

    /// <summary>
    /// 从请求体中提取模型名称
    /// </summary>
    private static string ExtractModelName(string requestBody)
    {
        if (string.IsNullOrEmpty(requestBody))
            return "";

        try
        {
            using var doc = JsonDocument.Parse(requestBody);
            var root = doc.RootElement;
            
            if (root.TryGetProperty("model", out var modelElement))
            {
                return modelElement.GetString() ?? "";
            }
        }
        catch
        {
            // 忽略解析错误
        }

        return "";
    }
} 