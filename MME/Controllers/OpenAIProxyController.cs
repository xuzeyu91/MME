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
    /// 代理聊天完成请求
    /// </summary>
    [HttpPost("chat/completions")]
    public async Task<IActionResult> ChatCompletions()
    {
        return await ProxyRequest("/v1/chat/completions");
    }

    /// <summary>
    /// 代理嵌入请求
    /// </summary>
    [HttpPost("embeddings")]
    public async Task<IActionResult> Embeddings()
    {
        return await ProxyRequest("/v1/embeddings");
    }

    /// <summary>
    /// 代理重排序请求
    /// </summary>
    [HttpPost("rerank")]
    public async Task<IActionResult> Rerank()
    {
        return await ProxyRequest("/v1/rerank");
    }

    /// <summary>
    /// 代理模型列表请求
    /// </summary>
    [HttpGet("models")]
    public async Task<IActionResult> Models()
    {
        return await ProxyRequest("/v1/models");
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

            // 创建HTTP客户端
            var httpClient = _httpClientFactory.CreateClient();
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
            var response = await httpClient.SendAsync(request);

            // 读取响应内容
            var responseContent = await response.Content.ReadAsStringAsync();

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

            // 返回响应
            return new ContentResult
            {
                Content = responseContent,
                ContentType = response.Content.Headers.ContentType?.ToString() ?? "application/json",
                StatusCode = (int)response.StatusCode
            };
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
            "transfer-encoding", "connection", "upgrade"
        };
        return skipHeaders.Contains(headerName.ToLowerInvariant());
    }
} 