using SqlSugar;
using System.ComponentModel.DataAnnotations;

namespace MME.Domain.Repositories.Dataset
{
    /// <summary>
    /// 数据集实体
    /// </summary>
    [SugarTable("dataset")]
    public class Dataset
    {
        /// <summary>
        /// 数据集ID
        /// </summary>
        [SugarColumn(IsPrimaryKey = true)]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// 数据集名称
        /// </summary>
        [SugarColumn(Length = 200, IsNullable = false)]
        [Required(ErrorMessage = "数据集名称不能为空")]
        [StringLength(200, ErrorMessage = "数据集名称不能超过200个字符")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 数据集描述
        /// </summary>
        [SugarColumn(Length = 1000, IsNullable = true)]
        [StringLength(1000, ErrorMessage = "描述不能超过1000个字符")]
        public string? Description { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        [SugarColumn(IsNullable = false)]
        public DateTime CreateTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 更新时间
        /// </summary>
        [SugarColumn(IsNullable = false)]
        public DateTime UpdateTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 数据集类型
        /// </summary>
        [SugarColumn(Length = 50, IsNullable = false)]
        public string Type { get; set; } = "QA"; // QA问答, Chat对话, Completion完成等

        /// <summary>
        /// 数据集状态
        /// </summary>
        [SugarColumn(IsNullable = false)]
        public DatasetStatus Status { get; set; } = DatasetStatus.Active;

        /// <summary>
        /// 标签（JSON格式存储）
        /// </summary>
        [SugarColumn(ColumnDataType = "text", IsNullable = true)]
        public string? Tags { get; set; }

        /// <summary>
        /// 创建者
        /// </summary>
        [SugarColumn(Length = 100, IsNullable = true)]
        public string? Creator { get; set; }

        /// <summary>
        /// 数据项数量（冗余字段，便于查询）
        /// </summary>
        [SugarColumn(IsNullable = false)]
        public int ItemCount { get; set; } = 0;
    }

    /// <summary>
    /// 数据集状态枚举
    /// </summary>
    public enum DatasetStatus
    {
        /// <summary>
        /// 活跃
        /// </summary>
        Active = 1,
        
        /// <summary>
        /// 归档
        /// </summary>
        Archived = 2,
        
        /// <summary>
        /// 已删除
        /// </summary>
        Deleted = 3
    }

    /// <summary>
    /// 数据集项实体
    /// </summary>
    [SugarTable("dataset_item")]
    public class DatasetItem
    {
        /// <summary>
        /// 数据项ID
        /// </summary>
        [SugarColumn(IsPrimaryKey = true)]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// 数据集ID
        /// </summary>
        [SugarColumn(IsNullable = false)]
        public Guid DatasetId { get; set; }

        /// <summary>
        /// 输入内容（请求体）
        /// </summary>
        [SugarColumn(ColumnDataType = "text", IsNullable = false)]
        public string Input { get; set; } = string.Empty;

        /// <summary>
        /// 期望输出（响应体）
        /// </summary>
        [SugarColumn(ColumnDataType = "text", IsNullable = true)]
        public string? ExpectedOutput { get; set; }

        /// <summary>
        /// 实际输出（用于评测时存储）
        /// </summary>
        [SugarColumn(ColumnDataType = "text", IsNullable = true)]
        public string? ActualOutput { get; set; }

        /// <summary>
        /// 来源类型
        /// </summary>
        [SugarColumn(Length = 50, IsNullable = false)]
        public string SourceType { get; set; } = "RequestLog"; // RequestLog, Manual, Import

        /// <summary>
        /// 来源ID（如果来自请求日志，则为请求日志ID）
        /// </summary>
        [SugarColumn(Length = 100, IsNullable = true)]
        public string? SourceId { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        [SugarColumn(IsNullable = false)]
        public DateTime CreateTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 更新时间
        /// </summary>
        [SugarColumn(IsNullable = false)]
        public DateTime UpdateTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 标签（JSON格式存储）
        /// </summary>
        [SugarColumn(ColumnDataType = "text", IsNullable = true)]
        public string? Tags { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        [SugarColumn(Length = 500, IsNullable = true)]
        public string? Remarks { get; set; }

        /// <summary>
        /// 难度等级（1-5）
        /// </summary>
        [SugarColumn(IsNullable = true)]
        public int? Difficulty { get; set; }

        /// <summary>
        /// 质量评分（1-5）
        /// </summary>
        [SugarColumn(IsNullable = true)]
        public int? Quality { get; set; }

        /// <summary>
        /// 模型名称（来源于请求日志）
        /// </summary>
        [SugarColumn(Length = 100, IsNullable = true)]
        public string? ModelName { get; set; }

        /// <summary>
        /// 代理名称（来源于请求日志）
        /// </summary>
        [SugarColumn(Length = 100, IsNullable = true)]
        public string? ProxyName { get; set; }
    }
} 