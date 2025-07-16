using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MME.Domain.Repositories;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MME.Domain.Common.Extensions
{
    public static class InitExtensions
    {

        /// <summary>
        /// 根据程序集中的实体类创建数据库表
        /// </summary>
        /// <param name="services"></param>
        public static void CodeFirst(this WebApplication app)
        {
            using (var scope = app.Services.CreateScope())
            {
                var logger = scope.ServiceProvider.GetService<ILogger<IServiceProvider>>();
                
                try
                {
                    // 获取仓储服务
                    var _repository = scope.ServiceProvider.GetRequiredService<IApiRequestLogRepository>();
                    
                    // 创建数据库（如果不存在）
                    try
                    {
                        _repository.GetDB().DbMaintenance.CreateDatabase();
                        logger?.LogInformation("数据库创建成功或已存在");
                    }
                    catch (Exception dbEx)
                    {
                        logger?.LogWarning($"数据库创建失败或已存在: {dbEx.Message}");
                    }
                    
                    // 获取当前程序集以及引用的程序集
                    var assemblies = new List<Assembly>
                    {
                        Assembly.GetExecutingAssembly(),
                        typeof(InitExtensions).Assembly // MME.Domain程序集
                    };
                    
                    var entityTypes = new List<Type>();
                    
                    // 在所有相关程序集中查找具有[SugarTable]特性的类
                    foreach (var assembly in assemblies)
                    {
                        var types = assembly.GetTypes()
                            .Where(type => TypeIsEntity(type))
                            .ToList();
                        entityTypes.AddRange(types);
                        
                        logger?.LogInformation($"在程序集 {assembly.FullName} 中找到 {types.Count} 个实体类");
                    }
                    
                    logger?.LogInformation($"总共找到 {entityTypes.Count} 个实体类");
                    
                    // 为每个找到的类型初始化数据库表
                    foreach (var type in entityTypes)
                    {
                        try
                        {
                            logger?.LogInformation($"正在创建表: {type.Name}");
                            _repository.GetDB().CodeFirst.InitTables(type);
                            logger?.LogInformation($"表 {type.Name} 创建成功");
                        }
                        catch (Exception ex)
                        {
                            logger?.LogError($"创建表 {type.Name} 失败: {ex.Message}");
                            // 继续处理其他表，不要因为一个表失败而终止整个过程
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger?.LogError($"CodeFirst 初始化失败: {ex.Message}");
                    throw; // 重新抛出异常以便上层处理
                }
            }
        }

        static bool TypeIsEntity(Type type)
        {
            // 检查类型是否具有SugarTable特性
            return type.GetCustomAttributes(typeof(SugarTable), inherit: false).Length > 0;
        }
    }
}
