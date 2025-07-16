using System.ComponentModel.DataAnnotations;

namespace MME.Domain.Repositories.Dataset
{
    /// <summary>
    /// 数据集DTO
    /// </summary>
    public class DatasetDto
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "数据集名称不能为空")]
        [StringLength(200, ErrorMessage = "数据集名称不能超过200个字符")]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "描述不能超过1000个字符")]
        public string? Description { get; set; }

        public DateTime CreateTime { get; set; }

        public DateTime UpdateTime { get; set; }

        public string Type { get; set; } = "QA";

        public DatasetStatus Status { get; set; }

        public List<string> Tags { get; set; } = new();

        public string? Creator { get; set; }

        public int ItemCount { get; set; }

        /// <summary>
        /// 状态显示文本
        /// </summary>
        public string StatusText => Status switch
        {
            DatasetStatus.Active => "活跃",
            DatasetStatus.Archived => "归档",
            DatasetStatus.Deleted => "已删除",
            _ => "未知"
        };

        /// <summary>
        /// 类型显示文本
        /// </summary>
        public string TypeText => Type switch
        {
            "QA" => "问答",
            "Chat" => "对话",
            "Completion" => "文本完成",
            _ => Type
        };
    }

    /// <summary>
    /// 数据集项DTO
    /// </summary>
    public class DatasetItemDto
    {
        public Guid Id { get; set; }

        public Guid DatasetId { get; set; }

        [Required(ErrorMessage = "输入内容不能为空")]
        public string Input { get; set; } = string.Empty;

        public string? ExpectedOutput { get; set; }

        public string? ActualOutput { get; set; }

        public string SourceType { get; set; } = "Manual";

        public string? SourceId { get; set; }

        public DateTime CreateTime { get; set; }

        public DateTime UpdateTime { get; set; }

        public List<string> Tags { get; set; } = new();

        public string? Remarks { get; set; }

        public int? Difficulty { get; set; }

        public int? Quality { get; set; }

        public string? ModelName { get; set; }

        public string? ProxyName { get; set; }

        /// <summary>
        /// 来源类型显示文本
        /// </summary>
        public string SourceTypeText => SourceType switch
        {
            "RequestLog" => "请求日志",
            "Manual" => "手动添加",
            "Import" => "导入",
            _ => SourceType
        };

        /// <summary>
        /// 输入内容预览（截取前100个字符）
        /// </summary>
        public string InputPreview => Input.Length > 100 ? Input.Substring(0, 100) + "..." : Input;

        /// <summary>
        /// 期望输出预览（截取前100个字符）
        /// </summary>
        public string ExpectedOutputPreview => 
            string.IsNullOrEmpty(ExpectedOutput) ? "" : 
            ExpectedOutput.Length > 100 ? ExpectedOutput.Substring(0, 100) + "..." : ExpectedOutput;
    }

    /// <summary>
    /// 数据集统计DTO
    /// </summary>
    public class DatasetStatisticsDto
    {
        public Guid DatasetId { get; set; }
        public int TotalItems { get; set; }
        public int ItemsWithExpectedOutput { get; set; }
        public int ItemsFromRequestLog { get; set; }
        public int ItemsManualAdded { get; set; }
        public int ItemsImported { get; set; }
        public Dictionary<string, int> ModelDistribution { get; set; } = new();
        public Dictionary<string, int> ProxyDistribution { get; set; } = new();
        public Dictionary<int, int> DifficultyDistribution { get; set; } = new();
        public Dictionary<int, int> QualityDistribution { get; set; } = new();
        public DateTime? LastUpdateTime { get; set; }
    }

    /// <summary>
    /// 创建数据集请求DTO
    /// </summary>
    public class CreateDatasetRequest
    {
        [Required(ErrorMessage = "数据集名称不能为空")]
        [StringLength(200, ErrorMessage = "数据集名称不能超过200个字符")]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "描述不能超过1000个字符")]
        public string? Description { get; set; }

        public string Type { get; set; } = "QA";

        public List<string> Tags { get; set; } = new();

        public string? Creator { get; set; }
    }

    /// <summary>
    /// 从请求日志添加到数据集的请求DTO
    /// </summary>
    public class AddFromRequestLogRequest
    {
        [Required]
        public Guid DatasetId { get; set; }

        [Required]
        public List<string> RequestLogIds { get; set; } = new();

        public List<string> Tags { get; set; } = new();

        public string? Remarks { get; set; }

        public int? Difficulty { get; set; }

        public int? Quality { get; set; }
    }

    /// <summary>
    /// 数据集查询参数DTO
    /// </summary>
    public class DatasetQueryParams
    {
        public string? Name { get; set; }
        public string? Type { get; set; }
        public DatasetStatus? Status { get; set; }
        public string? Creator { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SortField { get; set; } = "CreateTime";
        public bool SortDescending { get; set; } = true;
    }

    /// <summary>
    /// 数据集项查询参数DTO
    /// </summary>
    public class DatasetItemQueryParams
    {
        public Guid DatasetId { get; set; }
        public string? SourceType { get; set; }
        public string? ModelName { get; set; }
        public string? ProxyName { get; set; }
        public int? Difficulty { get; set; }
        public int? Quality { get; set; }
        public string? SearchText { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SortField { get; set; } = "CreateTime";
        public bool SortDescending { get; set; } = true;
    }

    /// <summary>
    /// 创建数据集项请求
    /// </summary>
    public class CreateDatasetItemRequest
    {
        [Required]
        public Guid DatasetId { get; set; }

        [Required(ErrorMessage = "输入内容不能为空")]
        public string Input { get; set; } = "";

        public string? ExpectedOutput { get; set; }

        public List<string> Tags { get; set; } = new();

        public string? Remarks { get; set; }

        public int? Difficulty { get; set; }

        public int? Quality { get; set; }
    }

    /// <summary>
    /// 更新数据集项请求
    /// </summary>
    public class UpdateDatasetItemRequest
    {
        [Required]
        public Guid Id { get; set; }

        [Required(ErrorMessage = "输入内容不能为空")]
        public string Input { get; set; } = "";

        public string? ExpectedOutput { get; set; }

        public List<string> Tags { get; set; } = new();

        public string? Remarks { get; set; }

        public int? Difficulty { get; set; }

        public int? Quality { get; set; }
    }

    /// <summary>
    /// 导出数据集请求
    /// </summary>
    public class ExportDatasetRequest
    {
        [Required]
        public Guid DatasetId { get; set; }

        public string Format { get; set; } = "Excel";
    }

    /// <summary>
    /// 导出数据集响应
    /// </summary>
    public class ExportDatasetResponse
    {
        public bool Success { get; set; }
        
        public string? Message { get; set; }
        
        public byte[]? Data { get; set; }
    }
} 