using MME.Domain.Model;
using MME.Domain.Repositories.Dataset;
using Microsoft.Extensions.DependencyInjection;
using MME.Domain.Common.Extensions;
using System.Text.Json;

namespace MME.Domain.Services
{
    /// <summary>
    /// 数据集服务实现
    /// </summary>
    [ServiceDescription(typeof(IDatasetService), ServiceLifetime.Scoped)]
    public class DatasetService : IDatasetService
    {
        private readonly IDatasetRepository _datasetRepository;

        public DatasetService(IDatasetRepository datasetRepository)
        {
            _datasetRepository = datasetRepository;
        }

        #region 数据集操作

        public async Task<(bool Success, string Message, Guid? DatasetId)> CreateDatasetAsync(CreateDatasetRequest request)
        {
            try
            {
                // 验证输入
                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    return (false, "数据集名称不能为空", null);
                }

                // 检查名称是否已存在
                if (await _datasetRepository.IsDatasetNameExistsAsync(request.Name))
                {
                    return (false, "数据集名称已存在", null);
                }

                // 创建数据集
                var dataset = new Dataset
                {
                    Name = request.Name.Trim(),
                    Description = request.Description?.Trim(),
                    Type = request.Type,
                    Creator = request.Creator,
                    Tags = request.Tags?.Any() == true ? JsonSerializer.Serialize(request.Tags) : null
                };

                var datasetId = await _datasetRepository.CreateDatasetAsync(dataset);
                return (true, "数据集创建成功", datasetId);
            }
            catch (Exception ex)
            {
                return (false, $"创建数据集失败：{ex.Message}", null);
            }
        }

        public async Task<(bool Success, string Message)> UpdateDatasetAsync(Guid datasetId, CreateDatasetRequest request)
        {
            try
            {
                // 验证输入
                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    return (false, "数据集名称不能为空");
                }

                // 获取数据集
                var dataset = await _datasetRepository.GetDatasetByIdAsync(datasetId);
                if (dataset == null)
                {
                    return (false, "数据集不存在");
                }

                // 检查名称是否已被其他数据集使用
                if (await _datasetRepository.IsDatasetNameExistsAsync(request.Name, datasetId))
                {
                    return (false, "数据集名称已存在");
                }

                // 更新数据集
                dataset.Name = request.Name.Trim();
                dataset.Description = request.Description?.Trim();
                dataset.Type = request.Type;
                dataset.Creator = request.Creator;
                dataset.Tags = request.Tags?.Any() == true ? JsonSerializer.Serialize(request.Tags) : null;

                await _datasetRepository.UpdateDatasetAsync(dataset);
                return (true, "数据集更新成功");
            }
            catch (Exception ex)
            {
                return (false, $"更新数据集失败：{ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> DeleteDatasetAsync(Guid datasetId)
        {
            try
            {
                var dataset = await _datasetRepository.GetDatasetByIdAsync(datasetId);
                if (dataset == null)
                {
                    return (false, "数据集不存在");
                }

                await _datasetRepository.DeleteDatasetAsync(datasetId);
                return (true, "数据集删除成功");
            }
            catch (Exception ex)
            {
                return (false, $"删除数据集失败：{ex.Message}");
            }
        }

        public async Task<DatasetDto?> GetDatasetAsync(Guid datasetId)
        {
            var dataset = await _datasetRepository.GetDatasetByIdAsync(datasetId);
            if (dataset == null) return null;

            return new DatasetDto
            {
                Id = dataset.Id,
                Name = dataset.Name,
                Description = dataset.Description,
                CreateTime = dataset.CreateTime,
                UpdateTime = dataset.UpdateTime,
                Type = dataset.Type,
                Status = dataset.Status,
                Tags = ParseTags(dataset.Tags),
                Creator = dataset.Creator,
                ItemCount = dataset.ItemCount
            };
        }

        public async Task<DatasetDto?> GetDatasetByIdAsync(Guid datasetId)
        {
            return await GetDatasetAsync(datasetId);
        }

        public async Task<PageList<DatasetDto>> GetDatasetsAsync(DatasetQueryParams queryParams)
        {
            return await _datasetRepository.GetDatasetsAsync(queryParams);
        }

        public async Task<List<DatasetDto>> GetActiveDatasetListAsync()
        {
            return await _datasetRepository.GetActiveDatasetListAsync();
        }

        #endregion

        #region 数据集项操作

        public async Task<(bool Success, string Message)> AddDatasetItemAsync(Guid datasetId, string input, string? expectedOutput, List<string>? tags = null, string? remarks = null, int? difficulty = null, int? quality = null)
        {
            try
            {
                // 验证输入
                if (string.IsNullOrWhiteSpace(input))
                {
                    return (false, "输入内容不能为空");
                }

                // 验证数据集存在
                var dataset = await _datasetRepository.GetDatasetByIdAsync(datasetId);
                if (dataset == null)
                {
                    return (false, "数据集不存在");
                }

                // 创建数据集项
                var item = new DatasetItem
                {
                    DatasetId = datasetId,
                    Input = input.Trim(),
                    ExpectedOutput = expectedOutput?.Trim(),
                    SourceType = "Manual",
                    Tags = tags?.Any() == true ? JsonSerializer.Serialize(tags) : null,
                    Remarks = remarks?.Trim(),
                    Difficulty = difficulty,
                    Quality = quality
                };

                await _datasetRepository.AddDatasetItemAsync(item);
                return (true, "数据项添加成功");
            }
            catch (Exception ex)
            {
                return (false, $"添加数据项失败：{ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> AddDatasetItemAsync(CreateDatasetItemRequest request)
        {
            return await AddDatasetItemAsync(
                request.DatasetId,
                request.Input,
                request.ExpectedOutput,
                request.Tags,
                request.Remarks,
                request.Difficulty,
                request.Quality
            );
        }

        public async Task<(bool Success, string Message)> UpdateDatasetItemAsync(Guid itemId, string input, string? expectedOutput, List<string>? tags = null, string? remarks = null, int? difficulty = null, int? quality = null)
        {
            try
            {
                // 验证输入
                if (string.IsNullOrWhiteSpace(input))
                {
                    return (false, "输入内容不能为空");
                }

                // 获取数据集项
                var item = await _datasetRepository.GetDatasetItemByIdAsync(itemId);
                if (item == null)
                {
                    return (false, "数据项不存在");
                }

                // 更新数据集项
                item.Input = input.Trim();
                item.ExpectedOutput = expectedOutput?.Trim();
                item.Tags = tags?.Any() == true ? JsonSerializer.Serialize(tags) : null;
                item.Remarks = remarks?.Trim();
                item.Difficulty = difficulty;
                item.Quality = quality;

                await _datasetRepository.UpdateDatasetItemAsync(item);
                return (true, "数据项更新成功");
            }
            catch (Exception ex)
            {
                return (false, $"更新数据项失败：{ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> UpdateDatasetItemAsync(UpdateDatasetItemRequest request)
        {
            return await UpdateDatasetItemAsync(
                request.Id,
                request.Input,
                request.ExpectedOutput,
                request.Tags,
                request.Remarks,
                request.Difficulty,
                request.Quality
            );
        }

        public async Task<(bool Success, string Message)> DeleteDatasetItemAsync(Guid itemId)
        {
            try
            {
                var item = await _datasetRepository.GetDatasetItemByIdAsync(itemId);
                if (item == null)
                {
                    return (false, "数据项不存在");
                }

                await _datasetRepository.DeleteDatasetItemAsync(itemId);
                return (true, "数据项删除成功");
            }
            catch (Exception ex)
            {
                return (false, $"删除数据项失败：{ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> DeleteDatasetItemsAsync(List<Guid> itemIds)
        {
            try
            {
                if (!itemIds.Any())
                {
                    return (false, "请选择要删除的数据项");
                }

                await _datasetRepository.DeleteDatasetItemsAsync(itemIds);
                return (true, $"成功删除 {itemIds.Count} 个数据项");
            }
            catch (Exception ex)
            {
                return (false, $"批量删除数据项失败：{ex.Message}");
            }
        }

        public async Task<DatasetItemDto?> GetDatasetItemAsync(Guid itemId)
        {
            var item = await _datasetRepository.GetDatasetItemByIdAsync(itemId);
            if (item == null) return null;

            return new DatasetItemDto
            {
                Id = item.Id,
                DatasetId = item.DatasetId,
                Input = item.Input,
                ExpectedOutput = item.ExpectedOutput,
                ActualOutput = item.ActualOutput,
                SourceType = item.SourceType,
                SourceId = item.SourceId,
                CreateTime = item.CreateTime,
                UpdateTime = item.UpdateTime,
                Tags = ParseTags(item.Tags),
                Remarks = item.Remarks,
                Difficulty = item.Difficulty,
                Quality = item.Quality,
                ModelName = item.ModelName,
                ProxyName = item.ProxyName
            };
        }

        public async Task<PageList<DatasetItemDto>> GetDatasetItemsAsync(DatasetItemQueryParams queryParams)
        {
            return await _datasetRepository.GetDatasetItemsAsync(queryParams);
        }

        #endregion

        #region 从请求日志添加

        public async Task<(bool Success, string Message, int AddedCount)> AddFromRequestLogsAsync(AddFromRequestLogRequest request)
        {
            try
            {
                // 验证输入
                if (!request.RequestLogIds.Any())
                {
                    return (false, "请选择要添加的请求日志", 0);
                }

                // 验证数据集存在
                var dataset = await _datasetRepository.GetDatasetByIdAsync(request.DatasetId);
                if (dataset == null)
                {
                    return (false, "数据集不存在", 0);
                }

                // 添加到数据集
                var addedIds = await _datasetRepository.AddFromRequestLogsAsync(
                    request.DatasetId,
                    request.RequestLogIds,
                    request.Tags,
                    request.Remarks,
                    request.Difficulty,
                    request.Quality);

                var addedCount = addedIds.Count;
                var skippedCount = request.RequestLogIds.Count - addedCount;

                var message = $"成功添加 {addedCount} 个数据项";
                if (skippedCount > 0)
                {
                    message += $"，跳过 {skippedCount} 个已存在的项";
                }

                return (true, message, addedCount);
            }
            catch (Exception ex)
            {
                return (false, $"从请求日志添加失败：{ex.Message}", 0);
            }
        }

        public async Task<List<string>> CheckRequestLogsInDatasetAsync(Guid datasetId, List<string> requestLogIds)
        {
            return await _datasetRepository.GetExistingRequestLogIdsAsync(datasetId, requestLogIds);
        }

        #endregion

        #region 统计和导出

        public async Task<DatasetStatisticsDto> GetDatasetStatisticsAsync(Guid datasetId)
        {
            return await _datasetRepository.GetDatasetStatisticsAsync(datasetId);
        }

        public async Task<(bool Success, string Message, string? JsonData)> ExportDatasetToJsonAsync(Guid datasetId)
        {
            try
            {
                var dataset = await _datasetRepository.GetDatasetByIdAsync(datasetId);
                if (dataset == null)
                {
                    return (false, "数据集不存在", null);
                }

                var jsonData = await _datasetRepository.ExportDatasetToJsonAsync(datasetId);
                return (true, "导出成功", jsonData);
            }
            catch (Exception ex)
            {
                return (false, $"导出失败：{ex.Message}", null);
            }
        }

        public async Task<(bool Success, string Message, byte[]? ExcelData)> ExportDatasetToExcelAsync(Guid datasetId)
        {
            try
            {
                var dataset = await _datasetRepository.GetDatasetByIdAsync(datasetId);
                if (dataset == null)
                {
                    return (false, "数据集不存在", null);
                }

                var excelData = await _datasetRepository.ExportDatasetToExcelAsync(datasetId);
                return (true, "导出成功", excelData);
            }
            catch (Exception ex)
            {
                return (false, $"导出失败：{ex.Message}", null);
            }
        }

        public async Task<ExportDatasetResponse> ExportDatasetAsync(ExportDatasetRequest request)
        {
            try
            {
                if (request.Format.Equals("Excel", StringComparison.OrdinalIgnoreCase))
                {
                    var result = await ExportDatasetToExcelAsync(request.DatasetId);
                    return new ExportDatasetResponse
                    {
                        Success = result.Success,
                        Message = result.Message,
                        Data = result.ExcelData
                    };
                }
                else
                {
                    return new ExportDatasetResponse
                    {
                        Success = false,
                        Message = "不支持的导出格式"
                    };
                }
            }
            catch (Exception ex)
            {
                return new ExportDatasetResponse
                {
                    Success = false,
                    Message = $"导出失败：{ex.Message}"
                };
            }
        }

        #endregion

        #region 私有方法

        private List<string> ParseTags(string? tagsJson)
        {
            if (string.IsNullOrEmpty(tagsJson))
                return new List<string>();

            try
            {
                return JsonSerializer.Deserialize<List<string>>(tagsJson) ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }

        #endregion
    }
} 