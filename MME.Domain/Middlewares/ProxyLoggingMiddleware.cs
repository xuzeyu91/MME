using MME.Domain.Services;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using MME.Domain.Repositories;

namespace MME.Domain.Middlewares;

public class ProxyLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ProxyLoggingMiddleware> _logger;
    private readonly IServiceProvider _serviceProvider;

    public ProxyLoggingMiddleware(RequestDelegate next, ILogger<ProxyLoggingMiddleware> logger, IServiceProvider serviceProvider)
    {
        _next = next;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // 只处理代理相关的请求
        if (!IsProxyRequest(context.Request.Path))
        {
            await _next(context);
            return;
        }

        var requestId = Guid.NewGuid().ToString();
        var stopwatch = Stopwatch.StartNew();
        
        // 记录请求信息
        var requestBody = await ReadRequestBodyAsync(context.Request);
        var requestHeaders = SerializeHeaders(context.Request.Headers);

        // 获取客户端IP
        var clientIp = GetClientIpAddress(context);

        // 检查是否为流式请求
        bool isStreamRequest = IsStreamRequest(requestBody);

        ApiRequestLog? logEntry = null;
        
        if (isStreamRequest)
        {
            // 流式请求：不缓存响应体，直接转发
            try
            {
                await _next(context);
                
                stopwatch.Stop();

                // 创建日志条目（流式请求不记录响应体）
                using var scope = _serviceProvider.CreateScope();
                var logRepository = scope.ServiceProvider.GetRequiredService<IApiRequestLogRepository>();
                var proxyService = scope.ServiceProvider.GetRequiredService<IProxyService>();
                
                var proxyConfig = await GetProxyConfigFromAuthAsync(context, proxyService);
                var proxyConfigId = proxyConfig?.Id ?? 0;

                logEntry = new ApiRequestLog
                {
                    RequestId = requestId,
                    ProxyConfigId = proxyConfigId,
                    RequestPath = context.Request.Path + context.Request.QueryString,
                    Method = context.Request.Method,
                    RequestHeaders = requestHeaders,
                    RequestBody = requestBody,
                    ResponseStatusCode = context.Response.StatusCode,
                    ResponseHeaders = "{}",  // 流式请求不记录响应头
                    ResponseBody = "[STREAMING_RESPONSE]", // 标记为流式响应
                    TargetUrl = proxyConfig?.TargetUrl ?? "",
                    RequestTime = DateTime.Now.AddMilliseconds(-stopwatch.ElapsedMilliseconds),
                    ResponseTime = DateTime.Now,
                    Duration = stopwatch.ElapsedMilliseconds,
                    ClientIp = clientIp,
                    UserAgent = context.Request.Headers.UserAgent.ToString(),
                    TokenUsage = null // 流式请求无法统计token使用
                };

                // 异步保存日志
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await logRepository.InsertAsync(logEntry);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "保存API请求日志失败: {RequestId}", requestId);
                    }
                });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                
                // 记录错误日志
                using var scope = _serviceProvider.CreateScope();
                var logRepository = scope.ServiceProvider.GetRequiredService<IApiRequestLogRepository>();
                var proxyService = scope.ServiceProvider.GetRequiredService<IProxyService>();
                
                var proxyConfig = await GetProxyConfigFromAuthAsync(context, proxyService);
                var proxyConfigId = proxyConfig?.Id ?? 0;

                logEntry = new ApiRequestLog
                {
                    RequestId = requestId,
                    ProxyConfigId = proxyConfigId,
                    RequestPath = context.Request.Path + context.Request.QueryString,
                    Method = context.Request.Method,
                    RequestHeaders = requestHeaders,
                    RequestBody = requestBody,
                    ResponseStatusCode = context.Response.StatusCode,
                    TargetUrl = proxyConfig?.TargetUrl ?? "",
                    RequestTime = DateTime.Now.AddMilliseconds(-stopwatch.ElapsedMilliseconds),
                    Duration = stopwatch.ElapsedMilliseconds,
                    ErrorMessage = ex.Message,
                    ClientIp = clientIp,
                    UserAgent = context.Request.Headers.UserAgent.ToString()
                };

                _ = Task.Run(async () =>
                {
                    try
                    {
                        await logRepository.InsertAsync(logEntry);
                    }
                    catch (Exception logEx)
                    {
                        _logger.LogError(logEx, "保存错误日志失败: {RequestId}", requestId);
                    }
                });

                throw;
            }
        }
        else
        {
            // 非流式请求：缓存响应体
            var originalResponseBody = context.Response.Body;
            using var responseBodyStream = new MemoryStream();
            context.Response.Body = responseBodyStream;

            try
            {
                // 执行下一个中间件
                await _next(context);
                
                stopwatch.Stop();

                // 读取响应内容
                responseBodyStream.Seek(0, SeekOrigin.Begin);
                var responseBody = await new StreamReader(responseBodyStream).ReadToEndAsync();
                var responseHeaders = SerializeHeaders(context.Response.Headers);

                // 创建日志条目
                using var scope = _serviceProvider.CreateScope();
                var logRepository = scope.ServiceProvider.GetRequiredService<IApiRequestLogRepository>();
                var proxyService = scope.ServiceProvider.GetRequiredService<IProxyService>();
                
                // 尝试根据Authorization头获取代理配置
                var proxyConfig = await GetProxyConfigFromAuthAsync(context, proxyService);
                var proxyConfigId = proxyConfig?.Id ?? 0;

                logEntry = new ApiRequestLog
                {
                    RequestId = requestId,
                    ProxyConfigId = proxyConfigId,
                    RequestPath = context.Request.Path + context.Request.QueryString,
                    Method = context.Request.Method,
                    RequestHeaders = requestHeaders,
                    RequestBody = requestBody,
                    ResponseStatusCode = context.Response.StatusCode,
                    ResponseHeaders = responseHeaders,
                    ResponseBody = responseBody,
                    TargetUrl = proxyConfig?.TargetUrl ?? "",
                    RequestTime = DateTime.Now.AddMilliseconds(-stopwatch.ElapsedMilliseconds),
                    ResponseTime = DateTime.Now,
                    Duration = stopwatch.ElapsedMilliseconds,
                    ClientIp = clientIp,
                    UserAgent = context.Request.Headers.UserAgent.ToString(),
                    TokenUsage = ExtractTokenUsage(responseBody)
                };

                // 异步保存日志
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await logRepository.InsertAsync(logEntry);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "保存API请求日志失败: {RequestId}", requestId);
                    }
                });

                // 复制响应内容到原始流
                responseBodyStream.Seek(0, SeekOrigin.Begin);
                await responseBodyStream.CopyToAsync(originalResponseBody);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                
                // 记录错误日志
                if (logEntry == null)
                {
                    using var scope = _serviceProvider.CreateScope();
                    var logRepository = scope.ServiceProvider.GetRequiredService<IApiRequestLogRepository>();
                    var proxyService = scope.ServiceProvider.GetRequiredService<IProxyService>();
                    
                    var proxyConfig = await GetProxyConfigFromAuthAsync(context, proxyService);
                    var proxyConfigId = proxyConfig?.Id ?? 0;

                    logEntry = new ApiRequestLog
                    {
                        RequestId = requestId,
                        ProxyConfigId = proxyConfigId,
                        RequestPath = context.Request.Path + context.Request.QueryString,
                        Method = context.Request.Method,
                        RequestHeaders = requestHeaders,
                        RequestBody = requestBody,
                        ResponseStatusCode = context.Response.StatusCode,
                        TargetUrl = proxyConfig?.TargetUrl ?? "",
                        RequestTime = DateTime.Now.AddMilliseconds(-stopwatch.ElapsedMilliseconds),
                        Duration = stopwatch.ElapsedMilliseconds,
                        ErrorMessage = ex.Message,
                        ClientIp = clientIp,
                        UserAgent = context.Request.Headers.UserAgent.ToString()
                    };

                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await logRepository.InsertAsync(logEntry);
                        }
                        catch (Exception logEx)
                        {
                            _logger.LogError(logEx, "保存错误日志失败: {RequestId}", requestId);
                        }
                    });
                }

                // 重新抛出异常
                throw;
            }
            finally
            {
                context.Response.Body = originalResponseBody;
            }
        }
    }

    private static bool IsProxyRequest(PathString path)
    {
        // 检查是否是OpenAI API相关的请求
        var pathValue = path.Value?.ToLower() ?? "";
        return pathValue.StartsWith("/v1/") || 
               pathValue.Contains("chat/completions") || 
               pathValue.Contains("embeddings") || 
               pathValue.Contains("rerank");
    }

    private static async Task<string> ReadRequestBodyAsync(HttpRequest request)
    {
        if (!request.Body.CanSeek)
        {
            request.EnableBuffering();
        }

        request.Body.Position = 0;
        using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        request.Body.Position = 0;
        
        return body;
    }

    private static string SerializeHeaders(IHeaderDictionary headers)
    {
        var headerDict = headers.ToDictionary(h => h.Key, h => h.Value.ToString());
        // 移除敏感信息
        headerDict.Remove("Authorization");
        return JsonSerializer.Serialize(headerDict);
    }

    private static string GetClientIpAddress(HttpContext context)
    {
        var xForwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(xForwardedFor))
        {
            return xForwardedFor.Split(',')[0].Trim();
        }

        var xRealIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(xRealIp))
        {
            return xRealIp;
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "未知";
    }

    private static async Task<ProxyConfig?> GetProxyConfigFromAuthAsync(HttpContext context, IProxyService proxyService)
    {
        var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            return null;
        }

        var token = authHeader.Substring("Bearer ".Length);
        return await proxyService.GetConfigByBearerTokenAsync(token);
    }

    private static string? ExtractTokenUsage(string responseBody)
    {
        try
        {
            if (string.IsNullOrEmpty(responseBody))
                return null;

            using var doc = JsonDocument.Parse(responseBody);
            if (doc.RootElement.TryGetProperty("usage", out var usage))
            {
                return usage.GetRawText();
            }
        }
        catch
        {
            // 忽略解析错误
        }

        return null;
    }

    /// <summary>
    /// 检查请求是否为流式请求
    /// </summary>
    private static bool IsStreamRequest(string requestBody)
    {
        if (string.IsNullOrEmpty(requestBody))
            return false;

        try
        {
            using var document = JsonDocument.Parse(requestBody);
            if (document.RootElement.TryGetProperty("stream", out var streamProperty))
            {
                return streamProperty.GetBoolean();
            }
        }
        catch
        {
            // 解析失败，默认为非流式
        }

        return false;
    }
} 