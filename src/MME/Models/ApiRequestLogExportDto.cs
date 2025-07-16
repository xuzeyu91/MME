using MME.Domain.Common.Excel;
using NPOI.SS.UserModel;

namespace MME.Models;

/// <summary>
/// API请求日志Excel导出模型（仅请求体和响应体）
/// </summary>
public class ApiRequestLogExportDto
{
    [ExeclProperty("请求ID", 1)]
    public string RequestId { get; set; } = string.Empty;

    [ExeclProperty("请求时间", 2)]
    public string RequestTimeFormatted { get; set; } = string.Empty;

    [ExeclProperty("代理名称", 3)]
    public string ProxyName { get; set; } = string.Empty;

    [ExeclProperty("请求路径", 4)]
    public string RequestPath { get; set; } = string.Empty;

    [ExeclProperty("状态码", 5, CellType.Numeric)]
    public int ResponseStatusCode { get; set; }

    [ExeclProperty("请求体", 6)]
    public string RequestBody { get; set; } = string.Empty;

    [ExeclProperty("响应体", 7)]
    public string ResponseBody { get; set; } = string.Empty;
} 