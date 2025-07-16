using MME.Domain.Model;
using SqlSugar;
using System.Linq.Expressions;

namespace MME.Domain.Repositories.Dataset
{
    /// <summary>
    /// 数据集仓储接口
    /// </summary>
    public interface IDatasetRepository
    {
        #region 数据集操作

        /// <summary>
        /// 创建数据集
        /// </summary>
        Task<Guid> CreateDatasetAsync(Dataset dataset);

        /// <summary>
        /// 更新数据集
        /// </summary>
        Task<bool> UpdateDatasetAsync(Dataset dataset);

        /// <summary>
        /// 删除数据集（软删除）
        /// </summary>
        Task<bool> DeleteDatasetAsync(Guid datasetId);

        /// <summary>
        /// 根据ID获取数据集
        /// </summary>
        Task<Dataset?> GetDatasetByIdAsync(Guid datasetId);

        /// <summary>
        /// 根据名称获取数据集
        /// </summary>
        Task<Dataset?> GetDatasetByNameAsync(string name);

        /// <summary>
        /// 分页查询数据集
        /// </summary>
        Task<PageList<DatasetDto>> GetDatasetsAsync(DatasetQueryParams queryParams);

        /// <summary>
        /// 获取所有活跃的数据集（用于下拉选择）
        /// </summary>
        Task<List<DatasetDto>> GetActiveDatasetListAsync();

        /// <summary>
        /// 检查数据集名称是否已存在
        /// </summary>
        Task<bool> IsDatasetNameExistsAsync(string name, Guid? excludeId = null);

        #endregion

        #region 数据集项操作

        /// <summary>
        /// 添加数据集项
        /// </summary>
        Task<Guid> AddDatasetItemAsync(DatasetItem item);

        /// <summary>
        /// 批量添加数据集项
        /// </summary>
        Task<List<Guid>> AddDatasetItemsAsync(List<DatasetItem> items);

        /// <summary>
        /// 更新数据集项
        /// </summary>
        Task<bool> UpdateDatasetItemAsync(DatasetItem item);

        /// <summary>
        /// 删除数据集项
        /// </summary>
        Task<bool> DeleteDatasetItemAsync(Guid itemId);

        /// <summary>
        /// 批量删除数据集项
        /// </summary>
        Task<bool> DeleteDatasetItemsAsync(List<Guid> itemIds);

        /// <summary>
        /// 根据ID获取数据集项
        /// </summary>
        Task<DatasetItem?> GetDatasetItemByIdAsync(Guid itemId);

        /// <summary>
        /// 分页查询数据集项
        /// </summary>
        Task<PageList<DatasetItemDto>> GetDatasetItemsAsync(DatasetItemQueryParams queryParams);

        /// <summary>
        /// 检查请求日志是否已添加到数据集
        /// </summary>
        Task<bool> IsRequestLogInDatasetAsync(Guid datasetId, string requestLogId);

        /// <summary>
        /// 批量检查请求日志是否已添加到数据集
        /// </summary>
        Task<List<string>> GetExistingRequestLogIdsAsync(Guid datasetId, List<string> requestLogIds);

        #endregion

        #region 数据集统计

        /// <summary>
        /// 获取数据集统计信息
        /// </summary>
        Task<DatasetStatisticsDto> GetDatasetStatisticsAsync(Guid datasetId);

        /// <summary>
        /// 更新数据集项数量
        /// </summary>
        Task<bool> UpdateDatasetItemCountAsync(Guid datasetId);

        #endregion

        #region 从请求日志添加

        /// <summary>
        /// 从请求日志添加到数据集
        /// </summary>
        Task<List<Guid>> AddFromRequestLogsAsync(Guid datasetId, List<string> requestLogIds, 
            List<string>? tags = null, string? remarks = null, int? difficulty = null, int? quality = null);

        #endregion

        #region 导出功能

        /// <summary>
        /// 导出数据集为JSON格式
        /// </summary>
        Task<string> ExportDatasetToJsonAsync(Guid datasetId);

        /// <summary>
        /// 导出数据集为Excel格式
        /// </summary>
        Task<byte[]> ExportDatasetToExcelAsync(Guid datasetId);

        #endregion
    }
} 