using AutoMapper;
using MME.Domain.Model;
using SqlSugar;
using System.Text.Json;
using NPOI.XSSF.UserModel;
using NPOI.SS.UserModel;
using AntSK.Domain.Repositories.Base;
using MME.Domain.Common.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace MME.Domain.Repositories.Dataset
{
    /// <summary>
    /// 数据集仓储实现
    /// </summary>
    [ServiceDescription(typeof(IDatasetRepository), ServiceLifetime.Scoped)]
    public class DatasetRepository : Repository<Dataset>, IDatasetRepository
    {
        private readonly IApiRequestLogRepository _logRepository;

        public DatasetRepository(IApiRequestLogRepository logRepository)
        {
            _logRepository = logRepository;
        }

        #region 数据集操作

        public async Task<Guid> CreateDatasetAsync(Dataset dataset)
        {
            dataset.Id = Guid.NewGuid();
            dataset.CreateTime = DateTime.Now;
            dataset.UpdateTime = DateTime.Now;
            dataset.ItemCount = 0;

            await InsertAsync(dataset);
            return dataset.Id;
        }

        public async Task<bool> UpdateDatasetAsync(Dataset dataset)
        {
            dataset.UpdateTime = DateTime.Now;
            return await UpdateAsync(dataset);
        }

        public async Task<bool> DeleteDatasetAsync(Guid datasetId)
        {
            var dataset = await GetByIdAsync(datasetId);
            if (dataset == null) return false;

            dataset.Status = DatasetStatus.Deleted;
            dataset.UpdateTime = DateTime.Now;

            return await UpdateAsync(dataset);
        }

        public async Task<Dataset?> GetDatasetByIdAsync(Guid datasetId)
        {
            return await GetByIdAsync(datasetId);
        }

        public async Task<Dataset?> GetDatasetByNameAsync(string name)
        {
            return await GetFirstAsync(d => d.Name == name && d.Status != DatasetStatus.Deleted);
        }

        public async Task<PageList<DatasetDto>> GetDatasetsAsync(DatasetQueryParams queryParams)
        {
            // 使用动态表达式构建安全的查询条件
            var exp = SqlSugar.Expressionable.Create<Dataset>()
                .And(d => d.Status != DatasetStatus.Deleted)
                .AndIF(!string.IsNullOrWhiteSpace(queryParams.Name), d => d.Name.Contains(queryParams.Name))
                .AndIF(!string.IsNullOrWhiteSpace(queryParams.Type), d => d.Type == queryParams.Type)
                .AndIF(queryParams.Status.HasValue, d => d.Status == queryParams.Status.Value)
                .AndIF(!string.IsNullOrWhiteSpace(queryParams.Creator), d => d.Creator != null && d.Creator.Contains(queryParams.Creator))
                .AndIF(queryParams.StartDate.HasValue, d => d.CreateTime >= queryParams.StartDate.Value);

            if (queryParams.EndDate.HasValue)
            {
                var endDate = queryParams.EndDate.Value.Date.AddDays(1).AddTicks(-1);
                exp = exp.And(d => d.CreateTime <= endDate);
            }

            var query = GetDB().Queryable<Dataset>();

            query = query.Where(exp.ToExpression());

            // 排序
            switch (queryParams.SortField?.ToLower())
            {
                case "name":
                    query = queryParams.SortDescending ? query.OrderByDescending(d => d.Name) : query.OrderBy(d => d.Name);
                    break;
                case "itemcount":
                    query = queryParams.SortDescending ? query.OrderByDescending(d => d.ItemCount) : query.OrderBy(d => d.ItemCount);
                    break;
                case "updatetime":
                    query = queryParams.SortDescending ? query.OrderByDescending(d => d.UpdateTime) : query.OrderBy(d => d.UpdateTime);
                    break;
                default:
                    query = queryParams.SortDescending ? query.OrderByDescending(d => d.CreateTime) : query.OrderBy(d => d.CreateTime);
                    break;
            }

            var totalCount = await query.CountAsync();
            var list = await query.ToPageListAsync(queryParams.Page, queryParams.PageSize);

            var dtoList = list.Select(d => MapToDto(d)).ToList();

            return new PageList<DatasetDto>
            {
                List = dtoList,
                TotalCount = totalCount
            };
        }

        public async Task<List<DatasetDto>> GetActiveDatasetListAsync()
        {
            var datasets = await GetListAsync(d => d.Status == DatasetStatus.Active);
            return datasets.Select(d => MapToDto(d)).OrderBy(d => d.Name).ToList();
        }

        public async Task<bool> IsDatasetNameExistsAsync(string name, Guid? excludeId = null)
        {
            // 使用动态表达式构建安全的查询条件
            var exp = SqlSugar.Expressionable.Create<Dataset>()
                .And(d => d.Name == name && d.Status != DatasetStatus.Deleted)
                .AndIF(excludeId.HasValue, d => d.Id != excludeId.Value);

            return await GetDB().Queryable<Dataset>()
                .Where(exp.ToExpression())
                .AnyAsync();
        }

        #endregion

        #region 数据集项操作

        public async Task<Guid> AddDatasetItemAsync(DatasetItem item)
        {
            item.Id = Guid.NewGuid();
            item.CreateTime = DateTime.Now;
            item.UpdateTime = DateTime.Now;

            var result = await GetDB().Insertable(item).ExecuteReturnEntityAsync();
            
            // 更新数据集项数量
            await UpdateDatasetItemCountAsync(item.DatasetId);

            return result.Id;
        }

        public async Task<List<Guid>> AddDatasetItemsAsync(List<DatasetItem> items)
        {
            if (!items.Any()) return new List<Guid>();

            var now = DateTime.Now;
            foreach (var item in items)
            {
                item.Id = Guid.NewGuid();
                item.CreateTime = now;
                item.UpdateTime = now;
            }

            await GetDB().Insertable(items).ExecuteCommandAsync();

            // 更新数据集项数量
            var datasetIds = items.Select(i => i.DatasetId).Distinct();
            foreach (var datasetId in datasetIds)
            {
                await UpdateDatasetItemCountAsync(datasetId);
            }

            return items.Select(i => i.Id).ToList();
        }

        public async Task<bool> UpdateDatasetItemAsync(DatasetItem item)
        {
            item.UpdateTime = DateTime.Now;
            return await GetDB().Updateable(item).ExecuteCommandAsync() > 0;
        }

        public async Task<bool> DeleteDatasetItemAsync(Guid itemId)
        {
            var item = await GetDatasetItemByIdAsync(itemId);
            if (item == null) return false;

            var result = await GetDB().Deleteable<DatasetItem>().Where(i => i.Id == itemId).ExecuteCommandAsync() > 0;
            
            if (result)
            {
                await UpdateDatasetItemCountAsync(item.DatasetId);
            }

            return result;
        }

        public async Task<bool> DeleteDatasetItemsAsync(List<Guid> itemIds)
        {
            if (!itemIds.Any()) return true;

            // 获取受影响的数据集ID
            var affectedDatasetIds = await GetDB().Queryable<DatasetItem>()
                .Where(i => itemIds.Contains(i.Id))
                .Select(i => i.DatasetId)
                .ToListAsync();

            var result = await GetDB().Deleteable<DatasetItem>().Where(i => itemIds.Contains(i.Id)).ExecuteCommandAsync() > 0;

            if (result)
            {
                // 更新相关数据集的项数量
                foreach (var datasetId in affectedDatasetIds.Distinct())
                {
                    await UpdateDatasetItemCountAsync(datasetId);
                }
            }

            return result;
        }

        public async Task<DatasetItem?> GetDatasetItemByIdAsync(Guid itemId)
        {
            return await GetDB().Queryable<DatasetItem>().Where(i => i.Id == itemId).FirstAsync();
        }

        public async Task<PageList<DatasetItemDto>> GetDatasetItemsAsync(DatasetItemQueryParams queryParams)
        {
            // 使用动态表达式构建安全的查询条件
            var exp = SqlSugar.Expressionable.Create<DatasetItem>()
                .And(i => i.DatasetId == queryParams.DatasetId)
                .AndIF(!string.IsNullOrWhiteSpace(queryParams.SourceType), i => i.SourceType == queryParams.SourceType)
                .AndIF(!string.IsNullOrWhiteSpace(queryParams.ModelName), i => i.ModelName != null && i.ModelName.Contains(queryParams.ModelName))
                .AndIF(!string.IsNullOrWhiteSpace(queryParams.ProxyName), i => i.ProxyName != null && i.ProxyName.Contains(queryParams.ProxyName))
                .AndIF(queryParams.Difficulty.HasValue, i => i.Difficulty == queryParams.Difficulty.Value)
                .AndIF(queryParams.Quality.HasValue, i => i.Quality == queryParams.Quality.Value)
                .AndIF(!string.IsNullOrWhiteSpace(queryParams.SearchText), i => i.Input.Contains(queryParams.SearchText) || 
                                        (i.ExpectedOutput != null && i.ExpectedOutput.Contains(queryParams.SearchText)))
                .AndIF(queryParams.StartDate.HasValue, i => i.CreateTime >= queryParams.StartDate.Value);

            if (queryParams.EndDate.HasValue)
            {
                var endDate = queryParams.EndDate.Value.Date.AddDays(1).AddTicks(-1);
                exp = exp.And(i => i.CreateTime <= endDate);
            }

            var query = GetDB().Queryable<DatasetItem>().Where(exp.ToExpression());

            // 排序
            switch (queryParams.SortField?.ToLower())
            {
                case "sourcetype":
                    query = queryParams.SortDescending ? query.OrderByDescending(i => i.SourceType) : query.OrderBy(i => i.SourceType);
                    break;
                case "modelname":
                    query = queryParams.SortDescending ? query.OrderByDescending(i => i.ModelName) : query.OrderBy(i => i.ModelName);
                    break;
                case "updatetime":
                    query = queryParams.SortDescending ? query.OrderByDescending(i => i.UpdateTime) : query.OrderBy(i => i.UpdateTime);
                    break;
                default:
                    query = queryParams.SortDescending ? query.OrderByDescending(i => i.CreateTime) : query.OrderBy(i => i.CreateTime);
                    break;
            }

            var totalCount = await query.CountAsync();
            var list = await query.ToPageListAsync(queryParams.Page, queryParams.PageSize);

            var dtoList = list.Select(i => MapItemToDto(i)).ToList();

            return new PageList<DatasetItemDto>
            {
                List = dtoList,
                TotalCount = totalCount
            };
        }

        public async Task<bool> IsRequestLogInDatasetAsync(Guid datasetId, string requestLogId)
        {
            var db = GetDB();
            if (db == null) return false;
            return await db.Queryable<DatasetItem>()
                .Where(i => i.DatasetId == datasetId && i.SourceType == "RequestLog" && i.SourceId == requestLogId)
                .AnyAsync();
        }

        public async Task<List<string>> GetExistingRequestLogIdsAsync(Guid datasetId, List<string> requestLogIds)
        {
            var db = GetDB();
            if (db == null) return new List<string>();
            return await db.Queryable<DatasetItem>()
                .Where(i => i.DatasetId == datasetId && i.SourceType == "RequestLog" && requestLogIds.Contains(i.SourceId))
                .Select(i => i.SourceId)
                .ToListAsync();
        }

        #endregion

        #region 数据集统计

        public async Task<DatasetStatisticsDto> GetDatasetStatisticsAsync(Guid datasetId)
        {
            var db = GetDB();
            if (db == null)
            {
                return new DatasetStatisticsDto
                {
                    DatasetId = datasetId,
                    TotalItems = 0,
                    ItemsWithExpectedOutput = 0,
                    ItemsFromRequestLog = 0,
                    ItemsManualAdded = 0,
                    ItemsImported = 0,
                    LastUpdateTime = null,
                    ModelDistribution = new Dictionary<string, int>(),
                    ProxyDistribution = new Dictionary<string, int>(),
                    DifficultyDistribution = new Dictionary<int, int>(),
                    QualityDistribution = new Dictionary<int, int>()
                };
            }

            var items = await db.Queryable<DatasetItem>()
                .Where(i => i.DatasetId == datasetId)
                .ToListAsync();

            var stats = new DatasetStatisticsDto
            {
                DatasetId = datasetId,
                TotalItems = items.Count,
                ItemsWithExpectedOutput = items.Count(i => !string.IsNullOrEmpty(i.ExpectedOutput)),
                ItemsFromRequestLog = items.Count(i => i.SourceType == "RequestLog"),
                ItemsManualAdded = items.Count(i => i.SourceType == "Manual"),
                ItemsImported = items.Count(i => i.SourceType == "Import"),
                LastUpdateTime = items.Any() ? items.Max(i => i.UpdateTime) : null
            };

            // 模型分布
            stats.ModelDistribution = items
                .Where(i => !string.IsNullOrEmpty(i.ModelName))
                .GroupBy(i => i.ModelName!)
                .ToDictionary(g => g.Key, g => g.Count());

            // 代理分布
            stats.ProxyDistribution = items
                .Where(i => !string.IsNullOrEmpty(i.ProxyName))
                .GroupBy(i => i.ProxyName!)
                .ToDictionary(g => g.Key, g => g.Count());

            // 难度分布
            stats.DifficultyDistribution = items
                .Where(i => i.Difficulty.HasValue)
                .GroupBy(i => i.Difficulty!.Value)
                .ToDictionary(g => g.Key, g => g.Count());

            // 质量分布
            stats.QualityDistribution = items
                .Where(i => i.Quality.HasValue)
                .GroupBy(i => i.Quality!.Value)
                .ToDictionary(g => g.Key, g => g.Count());

            return stats;
        }

        public async Task<bool> UpdateDatasetItemCountAsync(Guid datasetId)
        {
            var count = await GetDB().Queryable<DatasetItem>()
                .Where(i => i.DatasetId == datasetId)
                .CountAsync();

            return await GetDB().Updateable<Dataset>()
                .SetColumns(d => new Dataset { ItemCount = count, UpdateTime = DateTime.Now })
                .Where(d => d.Id == datasetId)
                .ExecuteCommandAsync() > 0;
        }

        #endregion

        #region 从请求日志添加

        public async Task<List<Guid>> AddFromRequestLogsAsync(Guid datasetId, List<string> requestLogIds, 
            List<string>? tags = null, string? remarks = null, int? difficulty = null, int? quality = null)
        {
            // 检查数据集是否存在
            var dataset = await GetDatasetByIdAsync(datasetId);
            if (dataset == null)
            {
                throw new ArgumentException("数据集不存在");
            }

            // 过滤已存在的请求日志
            var existingIds = await GetExistingRequestLogIdsAsync(datasetId, requestLogIds);
            var newRequestLogIds = requestLogIds.Except(existingIds).ToList();

            if (!newRequestLogIds.Any())
            {
                return new List<Guid>();
            }

            // 获取请求日志详情
            var logs = await _logRepository.GetLogsByIdsAsync(newRequestLogIds);

            // 转换为数据集项
            var items = new List<DatasetItem>();
            var now = DateTime.Now;
            var tagsJson = tags?.Any() == true ? JsonSerializer.Serialize(tags) : null;

            foreach (var log in logs)
            {
                var item = new DatasetItem
                {
                    Id = Guid.NewGuid(),
                    DatasetId = datasetId,
                    Input = log.RequestBody ?? "",
                    ExpectedOutput = log.ResponseBody,
                    SourceType = "RequestLog",
                    SourceId = log.RequestId,
                    ModelName = log.ModelName,
                    ProxyName = log.ProxyName,
                    Tags = tagsJson,
                    Remarks = remarks,
                    Difficulty = difficulty,
                    Quality = quality,
                    CreateTime = now,
                    UpdateTime = now
                };

                items.Add(item);
            }

            return await AddDatasetItemsAsync(items);
        }

        #endregion

        #region 导出功能

        public async Task<string> ExportDatasetToJsonAsync(Guid datasetId)
        {
            var dataset = await GetDatasetByIdAsync(datasetId);
            if (dataset == null)
            {
                throw new ArgumentException("数据集不存在");
            }

            var items = await GetDB().Queryable<DatasetItem>()
                .Where(i => i.DatasetId == datasetId)
                .OrderBy(i => i.CreateTime)
                .ToListAsync();

            var exportData = new
            {
                Dataset = new
                {
                    dataset.Id,
                    dataset.Name,
                    dataset.Description,
                    dataset.Type,
                    dataset.CreateTime,
                    ItemCount = items.Count
                },
                Items = items.Select(i => new
                {
                    i.Id,
                    i.Input,
                    i.ExpectedOutput,
                    i.SourceType,
                    i.SourceId,
                    i.ModelName,
                    i.ProxyName,
                    Tags = ParseTags(i.Tags),
                    i.Remarks,
                    i.Difficulty,
                    i.Quality,
                    i.CreateTime
                }).ToList()
            };

            return JsonSerializer.Serialize(exportData, new JsonSerializerOptions 
            { 
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
        }

        public async Task<byte[]> ExportDatasetToExcelAsync(Guid datasetId)
        {
            var dataset = await GetDatasetByIdAsync(datasetId);
            if (dataset == null)
            {
                throw new ArgumentException("数据集不存在");
            }

            var items = await GetDB().Queryable<DatasetItem>()
                .Where(i => i.DatasetId == datasetId)
                .OrderBy(i => i.CreateTime)
                .ToListAsync();

            var workbook = new XSSFWorkbook();
            var sheet = workbook.CreateSheet($"{dataset.Name}_数据集");

            // 创建表头
            var headerRow = sheet.CreateRow(0);
            var headers = new[] { "ID", "输入内容", "期望输出", "来源类型", "模型名称", "代理名称", "标签", "备注", "难度", "质量", "创建时间" };
            
            for (int i = 0; i < headers.Length; i++)
            {
                headerRow.CreateCell(i).SetCellValue(headers[i]);
            }

            // 填充数据
            for (int i = 0; i < items.Count; i++)
            {
                var row = sheet.CreateRow(i + 1);
                var item = items[i];

                row.CreateCell(0).SetCellValue(item.Id.ToString());
                row.CreateCell(1).SetCellValue(item.Input);
                row.CreateCell(2).SetCellValue(item.ExpectedOutput ?? "");
                row.CreateCell(3).SetCellValue(item.SourceType);
                row.CreateCell(4).SetCellValue(item.ModelName ?? "");
                row.CreateCell(5).SetCellValue(item.ProxyName ?? "");
                row.CreateCell(6).SetCellValue(string.Join(", ", ParseTags(item.Tags)));
                row.CreateCell(7).SetCellValue(item.Remarks ?? "");
                row.CreateCell(8).SetCellValue(item.Difficulty?.ToString() ?? "");
                row.CreateCell(9).SetCellValue(item.Quality?.ToString() ?? "");
                row.CreateCell(10).SetCellValue(item.CreateTime.ToString("yyyy-MM-dd HH:mm:ss"));
            }

            // 自动调整列宽
            for (int i = 0; i < headers.Length; i++)
            {
                sheet.AutoSizeColumn(i);
            }

            using var stream = new MemoryStream();
            workbook.Write(stream);
            return stream.ToArray();
        }

        #endregion

        #region 私有方法

        private DatasetDto MapToDto(Dataset dataset)
        {
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

        private DatasetItemDto MapItemToDto(DatasetItem item)
        {
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