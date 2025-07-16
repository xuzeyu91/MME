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
            // 流式请求：使用包装流来记录响应信息
            var originalResponseBody = context.Response.Body;
            var streamInfo = new StreamResponseInfo();
            var loggingStream = new LoggingStream(originalResponseBody, streamInfo);
            context.Response.Body = loggingStream;

            try
            {
                await _next(context);
                
                stopwatch.Stop();

                // 等待流式响应完全处理完毕
                await loggingStream.FlushAsync();

                // 创建日志条目
                using var scope = _serviceProvider.CreateScope();
                var logRepository = scope.ServiceProvider.GetRequiredService<IApiRequestLogRepository>();
                var proxyService = scope.ServiceProvider.GetRequiredService<IProxyService>();
                
                var proxyConfig = await GetProxyConfigFromAuthAsync(context, proxyService);
                var proxyConfigId = proxyConfig?.Id ?? Guid.Empty;

                // 记录响应头
                var responseHeaders = SerializeHeaders(context.Response.Headers);

                logEntry = new ApiRequestLog
                {
                    RequestId = requestId,
                    ProxyConfigId = proxyConfigId,
                    RequestPath = context.Request.Path + context.Request.QueryString,
                    Method = context.Request.Method,
                    RequestHeaders = requestHeaders,
                    RequestBody = requestBody,
                    ResponseStatusCode = context.Response.StatusCode,
                    ResponseHeaders = responseHeaders,  // 记录实际的响应头
                    ResponseBody = streamInfo.FullResponseContent, // 记录完整的流式响应内容
                    TargetUrl = proxyConfig?.TargetUrl ?? "",
                    RequestTime = DateTime.Now.AddMilliseconds(-stopwatch.ElapsedMilliseconds),
                    ResponseTime = DateTime.Now,
                    Duration = stopwatch.ElapsedMilliseconds,
                    ClientIp = clientIp,
                    UserAgent = context.Request.Headers.UserAgent.ToString(),
                    TokenUsage = ExtractTokenUsageFromStreamInfo(streamInfo) // 尝试从流式数据中提取token信息
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
                var proxyConfigId = proxyConfig?.Id ?? Guid.Empty;

                logEntry = new ApiRequestLog
                {
                    RequestId = requestId,
                    ProxyConfigId = proxyConfigId,
                    RequestPath = context.Request.Path + context.Request.QueryString,
                    Method = context.Request.Method,
                    RequestHeaders = requestHeaders,
                    RequestBody = requestBody,
                    ResponseStatusCode = context.Response.StatusCode,
                    ResponseHeaders = SerializeHeaders(context.Response.Headers),
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
            finally
            {
                context.Response.Body = originalResponseBody;
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
                var proxyConfigId = proxyConfig?.Id ?? Guid.Empty;

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
                    var proxyConfigId = proxyConfig?.Id ?? Guid.Empty;

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

    /// <summary>
    /// 从流式响应信息中提取Token使用情况
    /// </summary>
    private static string? ExtractTokenUsageFromStreamInfo(StreamResponseInfo streamInfo)
    {
        try
        {
            // 尝试从最后一个数据块中提取usage信息
            if (!string.IsNullOrEmpty(streamInfo.LastDataChunk))
            {
                var lines = streamInfo.LastDataChunk.Split('\n');
                foreach (var line in lines)
                {
                    if (line.StartsWith("data: ") && !line.Contains("[DONE]"))
                    {
                        var jsonData = line.Substring(6).Trim();
                        if (!string.IsNullOrEmpty(jsonData))
                        {
                            using var doc = JsonDocument.Parse(jsonData);
                            if (doc.RootElement.TryGetProperty("usage", out var usage))
                            {
                                return usage.GetRawText();
                            }
                        }
                    }
                }
            }
        }
        catch
        {
            // 忽略解析错误
        }

        return null;
    }
}

/// <summary>
/// 流式响应信息收集器
/// </summary>
public class StreamResponseInfo
{
    public int ChunkCount { get; set; } = 0;
    public long TotalBytes { get; set; } = 0;
    public string LastDataChunk { get; set; } = string.Empty;
    public string FullResponseContent { get; set; } = string.Empty;
    public DateTime FirstChunkTime { get; set; }
    public DateTime LastChunkTime { get; set; }
}

/// <summary>
/// 用于记录流式响应信息的包装流
/// </summary>
public class LoggingStream : Stream
{
    private readonly Stream _innerStream;
    private readonly StreamResponseInfo _streamInfo;
    private readonly StringBuilder _lastDataBuffer = new StringBuilder();
    private readonly StringBuilder _fullResponseBuffer = new StringBuilder();

    public LoggingStream(Stream innerStream, StreamResponseInfo streamInfo)
    {
        _innerStream = innerStream;
        _streamInfo = streamInfo;
    }

    public override bool CanRead => _innerStream.CanRead;
    public override bool CanSeek => _innerStream.CanSeek;
    public override bool CanWrite => _innerStream.CanWrite;
    public override long Length => _innerStream.Length;
    public override long Position { get => _innerStream.Position; set => _innerStream.Position = value; }

    public override void Flush() => _innerStream.Flush();

    public override async Task FlushAsync(CancellationToken cancellationToken) => 
        await _innerStream.FlushAsync(cancellationToken);

    public override int Read(byte[] buffer, int offset, int count) => 
        _innerStream.Read(buffer, offset, count);

    public override long Seek(long offset, SeekOrigin origin) => 
        _innerStream.Seek(offset, origin);

    public override void SetLength(long value) => _innerStream.SetLength(value);

    public override void Write(byte[] buffer, int offset, int count)
    {
        // 记录写入信息
        RecordChunk(buffer, offset, count);
        _innerStream.Write(buffer, offset, count);
    }

    public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        // 记录写入信息
        RecordChunk(buffer, offset, count);
        await _innerStream.WriteAsync(buffer, offset, count, cancellationToken);
    }

    private void RecordChunk(byte[] buffer, int offset, int count)
    {
        if (_streamInfo.ChunkCount == 0)
        {
            _streamInfo.FirstChunkTime = DateTime.Now;
        }

        _streamInfo.ChunkCount++;
        _streamInfo.TotalBytes += count;
        _streamInfo.LastChunkTime = DateTime.Now;

        // 记录完整的响应数据
        var chunkData = Encoding.UTF8.GetString(buffer, offset, count);
        
        // 收集完整的响应内容，但限制大小避免内存问题
        if (_fullResponseBuffer.Length < 1048576) // 限制为1MB
        {
            _fullResponseBuffer.Append(chunkData);
            _streamInfo.FullResponseContent = _fullResponseBuffer.ToString();
        }
        else if (_streamInfo.FullResponseContent.Length < 1048576)
        {
            // 如果已经达到限制，添加截断标记
            _streamInfo.FullResponseContent += "\n... [RESPONSE_TRUNCATED_DUE_TO_SIZE_LIMIT] ...";
        }

        // 记录最后的数据块，用于提取token使用信息
        _lastDataBuffer.Append(chunkData);

        // 只保留最后的几KB数据，避免内存占用过大
        if (_lastDataBuffer.Length > 4096)
        {
            _lastDataBuffer.Remove(0, _lastDataBuffer.Length - 2048);
        }

        _streamInfo.LastDataChunk = _lastDataBuffer.ToString();
    }
}