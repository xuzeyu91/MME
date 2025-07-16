using MME.Domain.Model;
using MME.Domain.Repositories.Dataset;

namespace MME.Domain.Services
{
    /// <summary>
    /// 数据集服务接口
    /// </summary>
    public interface IDatasetService
    {
        #region 数据集操作

        /// <summary>
        /// 创建数据集
        /// </summary>
        Task<(bool Success, string Message, Guid? DatasetId)> CreateDatasetAsync(CreateDatasetRequest request);

        /// <summary>
        /// 更新数据集
        /// </summary>
        Task<(bool Success, string Message)> UpdateDatasetAsync(Guid datasetId, CreateDatasetRequest request);

        /// <summary>
        /// 删除数据集
        /// </summary>
        Task<(bool Success, string Message)> DeleteDatasetAsync(Guid datasetId);

        /// <summary>
        /// 获取数据集详情
        /// </summary>
        Task<DatasetDto?> GetDatasetAsync(Guid datasetId);

        /// <summary>
        /// 根据ID获取数据集详情（别名方法）
        /// </summary>
        Task<DatasetDto?> GetDatasetByIdAsync(Guid datasetId);

        /// <summary>
        /// 分页查询数据集
        /// </summary>
        Task<PageList<DatasetDto>> GetDatasetsAsync(DatasetQueryParams queryParams);

        /// <summary>
        /// 获取活跃的数据集列表
        /// </summary>
        Task<List<DatasetDto>> GetActiveDatasetListAsync();

        #endregion

        #region 数据集项操作

        /// <summary>
        /// 手动添加数据集项
        /// </summary>
        Task<(bool Success, string Message)> AddDatasetItemAsync(Guid datasetId, string input, string? expectedOutput, List<string>? tags = null, string? remarks = null, int? difficulty = null, int? quality = null);

        /// <summary>
        /// 添加数据集项（使用请求对象）
        /// </summary>
        Task<(bool Success, string Message)> AddDatasetItemAsync(CreateDatasetItemRequest request);

        /// <summary>
        /// 更新数据集项
        /// </summary>
        Task<(bool Success, string Message)> UpdateDatasetItemAsync(Guid itemId, string input, string? expectedOutput, List<string>? tags = null, string? remarks = null, int? difficulty = null, int? quality = null);

        /// <summary>
        /// 更新数据集项（使用请求对象）
        /// </summary>
        Task<(bool Success, string Message)> UpdateDatasetItemAsync(UpdateDatasetItemRequest request);

        /// <summary>
        /// 删除数据集项
        /// </summary>
        Task<(bool Success, string Message)> DeleteDatasetItemAsync(Guid itemId);

        /// <summary>
        /// 批量删除数据集项
        /// </summary>
        Task<(bool Success, string Message)> DeleteDatasetItemsAsync(List<Guid> itemIds);

        /// <summary>
        /// 获取数据集项详情
        /// </summary>
        Task<DatasetItemDto?> GetDatasetItemAsync(Guid itemId);

        /// <summary>
        /// 分页查询数据集项
        /// </summary>
        Task<PageList<DatasetItemDto>> GetDatasetItemsAsync(DatasetItemQueryParams queryParams);

        #endregion

        #region 从请求日志添加

        /// <summary>
        /// 从请求日志添加到数据集
        /// </summary>
        Task<(bool Success, string Message, int AddedCount)> AddFromRequestLogsAsync(AddFromRequestLogRequest request);

        /// <summary>
        /// 检查请求日志是否已在数据集中
        /// </summary>
        Task<List<string>> CheckRequestLogsInDatasetAsync(Guid datasetId, List<string> requestLogIds);

        #endregion

        #region 统计和导出

        /// <summary>
        /// 获取数据集统计信息
        /// </summary>
        Task<DatasetStatisticsDto> GetDatasetStatisticsAsync(Guid datasetId);

        /// <summary>
        /// 导出数据集为JSON
        /// </summary>
        Task<(bool Success, string Message, string? JsonData)> ExportDatasetToJsonAsync(Guid datasetId);

        /// <summary>
        /// 导出数据集为Excel
        /// </summary>
        Task<(bool Success, string Message, byte[]? ExcelData)> ExportDatasetToExcelAsync(Guid datasetId);

        /// <summary>
        /// 导出数据集（通用方法）
        /// </summary>
        Task<ExportDatasetResponse> ExportDatasetAsync(ExportDatasetRequest request);

        #endregion
    }
} 