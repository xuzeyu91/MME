using Microsoft.AspNetCore.Mvc;
using MME.Domain.Services;
using System.Text;
using System.Text.Json;

namespace MME.Controllers;

[ApiController]
[Route("v1")]
public class OpenAIProxyController : ControllerBase
{
    private readonly IProxyService _proxyService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<OpenAIProxyController> _logger;

    public OpenAIProxyController(
        IProxyService proxyService, 
        IHttpClientFactory httpClientFactory,
        ILogger<OpenAIProxyController> logger)
    {
        _proxyService = proxyService;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// 通用代理方法 - 处理所有 v1/ 路径下的请求
    /// </summary>
    [HttpGet("{**path}")]
    [HttpPost("{**path}")]
    [HttpPut("{**path}")]
    [HttpDelete("{**path}")]
    [HttpPatch("{**path}")]
    [HttpHead("{**path}")]
    [HttpOptions("{**path}")]
    public async Task<IActionResult> ProxyAll(string path)
    {
        var targetPath = $"/v1/{path}";
        return await ProxyRequest(targetPath);
    }

    /// <summary>
    /// 通用代理处理方法
    /// </summary>
    private async Task<IActionResult> ProxyRequest(string targetPath)
    {
        try
        {
            // 获取Authorization头
            var authHeader = Request.Headers.Authorization.FirstOrDefault();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                return Unauthorized(new { error = "Missing or invalid authorization header" });
            }

            var bearerToken = authHeader.Substring("Bearer ".Length);
            
            // 根据Bearer Token获取代理配置
            var proxyConfig = await _proxyService.GetConfigByBearerTokenAsync(bearerToken);
            if (proxyConfig == null)
            {
                return Unauthorized(new { error = "Invalid bearer token" });
            }

            if (!proxyConfig.IsEnabled)
            {
                return BadRequest(new { error = "Proxy configuration is disabled" });
            }

            // 创建支持自动解压缩的HTTP客户端
            using var handler = new HttpClientHandler()
            {
                AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate | System.Net.DecompressionMethods.Brotli
            };
            using var httpClient = new HttpClient(handler);
            httpClient.Timeout = TimeSpan.FromSeconds(proxyConfig.TimeoutSeconds);

            // 构建目标URL
            var targetUrl = $"{proxyConfig.TargetUrl.TrimEnd('/')}{targetPath}";
            if (!string.IsNullOrEmpty(Request.QueryString.Value))
            {
                targetUrl += Request.QueryString.Value;
            }

            // 读取请求体
            string requestBody = "";
            if (Request.ContentLength > 0)
            {
                using var reader = new StreamReader(Request.Body);
                requestBody = await reader.ReadToEndAsync();
            }

            // 检查是否为流式请求
            bool isStreamRequest = IsStreamRequest(requestBody);

            // 创建代理请求
            var request = new HttpRequestMessage(new HttpMethod(Request.Method), targetUrl);

            // 添加请求体
            if (!string.IsNullOrEmpty(requestBody))
            {
                request.Content = new StringContent(requestBody, Encoding.UTF8, Request.ContentType ?? "application/json");
            }

            // 复制请求头（排除某些头）
            foreach (var header in Request.Headers)
            {
                if (!ShouldSkipHeader(header.Key))
                {
                    if (header.Key == "Authorization")
                    {
                        // 使用配置中的API Key
                        request.Headers.Add("Authorization", $"Bearer {proxyConfig.ApiKey}");
                    }
                    else
                    {
                        request.Headers.TryAddWithoutValidation(header.Key, header.Value.AsEnumerable());
                    }
                }
            }

            // 发送请求
            var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            // 复制响应头
            foreach (var header in response.Headers)
            {
                if (!ShouldSkipResponseHeader(header.Key))
                {
                    Response.Headers.TryAdd(header.Key, header.Value.ToArray());
                }
            }

            foreach (var header in response.Content.Headers)
            {
                if (!ShouldSkipResponseHeader(header.Key))
                {
                    Response.Headers.TryAdd(header.Key, header.Value.ToArray());
                }
            }

            // 设置状态码
            Response.StatusCode = (int)response.StatusCode;

            // 如果是流式请求，则实时转发数据流
            if (isStreamRequest)
            {
                Response.ContentType = response.Content.Headers.ContentType?.ToString() ?? "text/event-stream";
                
                // 实时转发流式数据
                using var responseStream = await response.Content.ReadAsStreamAsync();
                await responseStream.CopyToAsync(Response.Body);
                await Response.Body.FlushAsync();
                
                return new EmptyResult();
            }
            else
            {
                // 非流式请求，读取完整响应内容
                var responseContent = await response.Content.ReadAsStringAsync();
                
                return new ContentResult
                {
                    Content = responseContent,
                    ContentType = response.Content.Headers.ContentType?.ToString() ?? "application/json",
                    StatusCode = (int)response.StatusCode
                };
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed for path: {Path}", targetPath);
            return StatusCode(502, new { error = "Bad gateway", message = ex.Message });
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError(ex, "Request timeout for path: {Path}", targetPath);
            return StatusCode(504, new { error = "Gateway timeout" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error for path: {Path}", targetPath);
            return StatusCode(500, new { error = "Internal server error", message = ex.Message });
        }
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

    private static bool ShouldSkipHeader(string headerName)
    {
        var skipHeaders = new[]
        {
            "host", "content-length", "connection", "upgrade", "transfer-encoding"
        };
        return skipHeaders.Contains(headerName.ToLowerInvariant());
    }

    private static bool ShouldSkipResponseHeader(string headerName)
    {
        var skipHeaders = new[]
        {
            "transfer-encoding", "connection", "upgrade", "content-encoding"
        };
        return skipHeaders.Contains(headerName.ToLowerInvariant());
    }
} 