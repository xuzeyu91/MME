using Microsoft.AspNetCore.Mvc;
using MME.Domain.Services;
using MME.Domain.Repositories;
using MME.Domain.Model;
using SqlSugar;

namespace MME.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProxyController : ControllerBase
{
    private readonly IProxyService _proxyService;
    private readonly IApiRequestLogRepository _logRepository;

    public ProxyController(IProxyService proxyService, IApiRequestLogRepository logRepository)
    {
        _proxyService = proxyService;
        _logRepository = logRepository;
    }

    /// <summary>
    /// 获取所有代理配置
    /// </summary>
    [HttpGet("configs")]
    public async Task<IActionResult> GetConfigs()
    {
        var configs = await _proxyService.GetAllConfigsAsync();
        return Ok(configs);
    }

    /// <summary>
    /// 根据ID获取代理配置
    /// </summary>
    [HttpGet("configs/{id}")]
    public async Task<IActionResult> GetConfig(long id)
    {
        var config = await _proxyService.GetConfigByIdAsync(id);
        if (config == null)
            return NotFound();
        
        return Ok(config);
    }

    /// <summary>
    /// 创建代理配置
    /// </summary>
    [HttpPost("configs")]
    public async Task<IActionResult> CreateConfig([FromBody] CreateProxyConfigRequest request)
    {
        try
        {
            var config = await _proxyService.CreateConfigAsync(
                request.Name, 
                request.TargetUrl, 
                request.ApiKey, 
                request.Description);
            
            return CreatedAtAction(nameof(GetConfig), new { id = config.Id }, config);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// 更新代理配置
    /// </summary>
    [HttpPut("configs/{id}")]
    public async Task<IActionResult> UpdateConfig(long id, [FromBody] UpdateProxyConfigRequest request)
    {
        try
        {
            var config = await _proxyService.UpdateConfigAsync(
                id, 
                request.Name, 
                request.TargetUrl, 
                request.ApiKey, 
                request.Description);
            
            return Ok(config);
        }
        catch (ArgumentException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// 删除代理配置
    /// </summary>
    [HttpDelete("configs/{id}")]
    public async Task<IActionResult> DeleteConfig(long id)
    {
        var result = await _proxyService.DeleteConfigAsync(id);
        if (!result)
            return NotFound();
        
        return NoContent();
    }

    /// <summary>
    /// 启用/禁用代理配置
    /// </summary>
    [HttpPatch("configs/{id}/toggle")]
    public async Task<IActionResult> ToggleConfig(long id, [FromBody] ToggleConfigRequest request)
    {
        var result = await _proxyService.ToggleConfigAsync(id, request.IsEnabled);
        if (!result)
            return NotFound();
        
        return Ok();
    }

    /// <summary>
    /// 刷新Bearer Token
    /// </summary>
    [HttpPost("configs/{id}/refresh-token")]
    public async Task<IActionResult> RefreshBearerToken(long id)
    {
        try
        {
            var newToken = await _proxyService.RefreshBearerTokenAsync(id);
            return Ok(new { BearerToken = newToken });
        }
        catch (ArgumentException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// 获取API请求日志
    /// </summary>
    [HttpGet("logs")]
    public async Task<IActionResult> GetLogs([FromQuery] long? proxyConfigId, [FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 20)
    {
        var page = new PageModel { PageIndex = pageIndex, PageSize = pageSize };
        
        if (proxyConfigId.HasValue)
        {
            var logs = await _logRepository.GetLogsByProxyConfigIdAsync(proxyConfigId.Value, page);
            return Ok(logs);
        }
        else
        {
            var logs = await _logRepository.GetPageListAsync(x => true, page, x => x.RequestTime, SqlSugar.OrderByType.Desc);
            return Ok(logs);
        }
    }

    /// <summary>
    /// 获取最近的API请求日志
    /// </summary>
    [HttpGet("logs/recent")]
    public async Task<IActionResult> GetRecentLogs([FromQuery] int count = 100)
    {
        var logs = await _logRepository.GetRecentLogsAsync(count);
        return Ok(logs);
    }
}

/// <summary>
/// 创建代理配置请求
/// </summary>
public class CreateProxyConfigRequest
{
    public string Name { get; set; } = string.Empty;
    public string TargetUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string? Description { get; set; }
}

/// <summary>
/// 更新代理配置请求
/// </summary>
public class UpdateProxyConfigRequest
{
    public string Name { get; set; } = string.Empty;
    public string TargetUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string? Description { get; set; }
}

/// <summary>
/// 启用/禁用配置请求
/// </summary>
public class ToggleConfigRequest
{
    public bool IsEnabled { get; set; }
} 