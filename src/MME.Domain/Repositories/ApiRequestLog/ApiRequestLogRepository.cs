using MME.Domain.Repositories.Base;
using AntSK.Domain.Repositories.Base;
using MME.Domain.Model;
using SqlSugar;
using MME.Domain.Common.Extensions;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace MME.Domain.Repositories;

public interface IApiRequestLogRepository : IRepository<ApiRequestLog>
{
    Task<PageList<ApiRequestLog>> GetLogsByProxyConfigIdAsync(Guid proxyConfigId, PageModel page);
    Task<List<ApiRequestLog>> GetRecentLogsAsync(int count = 100);
    Task<ApiRequestLog?> GetByRequestIdAsync(string requestId);
    Task<PageList<ApiRequestLogDto>> GetLogsWithProxyInfoAsync(
        System.Linq.Expressions.Expression<Func<ApiRequestLog, bool>> whereExpression,
        PageModel page,
        OrderByType orderByType = OrderByType.Desc);
    Task<PageList<ApiRequestLogDto>> GetLogsWithProxyInfoAsync(
        System.Linq.Expressions.Expression<Func<ApiRequestLog, bool>> whereExpression,
        PageModel page,
        string? proxyName = null,
        string? modelName = null,
        OrderByType orderByType = OrderByType.Desc);
    Task<List<string>> GetDistinctProxyNamesAsync();
    Task<List<string>> GetDistinctModelNamesAsync();
    Task<List<ApiRequestLogDto>> GetLogsByIdsAsync(List<string> requestLogIds);
}

[ServiceDescription(typeof(IApiRequestLogRepository), ServiceLifetime.Scoped)]
public class ApiRequestLogRepository : Repository<ApiRequestLog>, IApiRequestLogRepository
{
    public ApiRequestLogRepository(ISqlSugarClient db) : base(db)
    {
    }

    /// <summary>
    /// 根据代理配置ID获取日志分页数据
    /// </summary>
    public async Task<PageList<ApiRequestLog>> GetLogsByProxyConfigIdAsync(Guid proxyConfigId, PageModel page)
    {
        return await GetPageListAsync(x => x.ProxyConfigId == proxyConfigId, page, x => x.RequestTime, OrderByType.Desc);
    }

    /// <summary>
    /// 获取最近的日志记录
    /// </summary>
    public async Task<List<ApiRequestLog>> GetRecentLogsAsync(int count = 100)
    {
        return await Context.Queryable<ApiRequestLog>()
            .OrderByDescending(x => x.RequestTime)
            .Take(count)
            .ToListAsync();
    }

    /// <summary>
    /// 根据请求ID获取日志
    /// </summary>
    public async Task<ApiRequestLog?> GetByRequestIdAsync(string requestId)
    {
        return await Context.Queryable<ApiRequestLog>()
            .Where(x => x.RequestId == requestId)
            .FirstAsync();
    }

    /// <summary>
    /// 获取包含代理信息的日志分页数据
    /// </summary>
    public async Task<PageList<ApiRequestLogDto>> GetLogsWithProxyInfoAsync(
        System.Linq.Expressions.Expression<Func<ApiRequestLog, bool>> whereExpression,
        PageModel page,
        OrderByType orderByType = OrderByType.Desc)
    {
        var queryable = Context.Queryable<ApiRequestLog, ProxyConfig>((log, proxy) => new JoinQueryInfos(
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
    /// 获取包含代理信息的日志分页数据（支持代理名称和模型名称筛选）
    /// </summary>
    public async Task<PageList<ApiRequestLogDto>> GetLogsWithProxyInfoAsync(
        System.Linq.Expressions.Expression<Func<ApiRequestLog, bool>> whereExpression,
        PageModel page,
        string? proxyName = null,
        string? modelName = null,
        OrderByType orderByType = OrderByType.Desc)
    {
        var queryable = Context.Queryable<ApiRequestLog, ProxyConfig>((log, proxy) => new JoinQueryInfos(
                JoinType.Left, log.ProxyConfigId == proxy.Id))
            .Where(whereExpression);

        // 如果指定了代理名称筛选
        if (!string.IsNullOrWhiteSpace(proxyName))
        {
            queryable = queryable.Where((log, proxy) => proxy.Name == proxyName);
        }

        var resultQueryable = queryable
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

        // 获取总数（模型名称筛选需要在内存中处理）
        int totalCount;
        List<ApiRequestLogDto> list;

        if (string.IsNullOrWhiteSpace(modelName))
        {
            // 没有模型名称筛选，使用简化的查询避免SQLite JSON错误
            try
            {
                // 分别计算总数和获取数据，避免复杂的JOIN查询
                var baseQuery = Context.Queryable<ApiRequestLog>()
                    .Where(whereExpression);
                
                totalCount = await baseQuery.CountAsync();
                
                var logs = await baseQuery
                    .OrderBy(log => log.RequestTime, orderByType)
                    .Skip((page.PageIndex - 1) * page.PageSize)
                    .Take(page.PageSize)
                    .ToListAsync();
                
                // 获取相关的代理信息
                var proxyIds = logs.Select(l => l.ProxyConfigId).Distinct().ToList();
                var proxies = new Dictionary<Guid, string>();
                
                if (proxyIds.Any())
                {
                    var proxyConfigs = await Context.Queryable<ProxyConfig>()
                        .Where(p => proxyIds.Contains(p.Id))
                        .ToListAsync();
                    
                    proxies = proxyConfigs.ToDictionary(p => p.Id, p => p.Name ?? "");
                }
                
                // 转换为DTO
                list = logs.Select(log => new ApiRequestLogDto
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
                    ProxyName = proxies.TryGetValue(log.ProxyConfigId, out var proxyName) ? proxyName : "未知",
                    ModelName = "" // 将在后续处理中从RequestBody解析
                }).ToList();
            }
            catch (Exception ex)
            {
                // 如果还是出错，使用最简单的查询
                Console.WriteLine($"SQL查询错误: {ex.Message}");
                
                var fallbackQuery = Context.Queryable<ApiRequestLog>()
                    .OrderByDescending(log => log.RequestTime);
                    
                totalCount = await fallbackQuery.CountAsync();
                
                var fallbackList = await fallbackQuery
                    .Skip((page.PageIndex - 1) * page.PageSize)
                    .Take(page.PageSize)
                    .ToListAsync();
                    
                list = fallbackList.Select(log => new ApiRequestLogDto
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
                    ProxyName = "未知",
                    ModelName = ""
                }).ToList();
            }
        }
        else
        {
            // 有模型名称筛选，需要获取所有数据后在内存中筛选
            var allData = await resultQueryable.ToListAsync();
            
            // 解析模型名称并进行筛选
            var filteredData = new List<ApiRequestLogDto>();
            foreach (var log in allData)
            {
                log.ModelName = ExtractModelName(log.RequestBody);
                if (log.ModelName == modelName)
                {
                    filteredData.Add(log);
                }
            }

            totalCount = filteredData.Count;
            list = filteredData.Skip((page.PageIndex - 1) * page.PageSize).Take(page.PageSize).ToList();
        }

        // 对于没有模型名称筛选的数据，解析模型名称
        if (string.IsNullOrWhiteSpace(modelName))
        {
            foreach (var log in list)
            {
                log.ModelName = ExtractModelName(log.RequestBody);
            }
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

    /// <summary>
    /// 获取所有不同的代理名称
    /// </summary>
    public async Task<List<string>> GetDistinctProxyNamesAsync()
    {
        return await Context.Queryable<ApiRequestLog, ProxyConfig>((log, proxy) => new JoinQueryInfos(
                JoinType.Left, log.ProxyConfigId == proxy.Id))
            .Where((log, proxy) => proxy.Name != null && proxy.Name != "")
            .Select((log, proxy) => proxy.Name)
            .Distinct()
            .ToListAsync();
    }

    /// <summary>
    /// 获取所有不同的模型名称
    /// </summary>
    public async Task<List<string>> GetDistinctModelNamesAsync()
    {
        // 获取所有非空的请求体
        var requestBodies = await Context.Queryable<ApiRequestLog>()
            .Where(log => log.RequestBody != null && log.RequestBody != "")
            .Select(log => log.RequestBody)
            .ToListAsync();

        var modelNames = new HashSet<string>();
        foreach (var requestBody in requestBodies)
        {
            var modelName = ExtractModelName(requestBody);
            if (!string.IsNullOrEmpty(modelName))
            {
                modelNames.Add(modelName);
            }
        }

        return modelNames.OrderBy(x => x).ToList();
    }

    /// <summary>
    /// 根据请求日志ID列表获取日志详情
    /// </summary>
    public async Task<List<ApiRequestLogDto>> GetLogsByIdsAsync(List<string> requestLogIds)
    {
        if (!requestLogIds.Any())
            return new List<ApiRequestLogDto>();

        var logs = await Context.Queryable<ApiRequestLog, ProxyConfig>((log, proxy) => new JoinQueryInfos(
                JoinType.Left, log.ProxyConfigId == proxy.Id))
            .Where((log, proxy) => requestLogIds.Contains(log.RequestId))
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
            })
            .ToListAsync();

        // 解析模型名称
        foreach (var log in logs)
        {
            log.ModelName = ExtractModelName(log.RequestBody);
        }

        return logs;
    }
} 