using MME.Domain.Common.Extensions;
using MME.Domain.Common.Options;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;
using System.Data;

namespace MME.Domain.Repositories
{
    [ServiceDescription(typeof(IEntityService), ServiceLifetime.Scoped)]
    public class EntityService : IEntityService
    {
        public SqlSugarClient db = new SqlSugarClient(new ConnectionConfig()
        {
            ConnectionString = DBConnectionOption.DBConnection,
            DbType = (SqlSugar.DbType)Enum.Parse(typeof(SqlSugar.DbType), DBConnectionOption.DbType),
            InitKeyType = InitKeyType.Attribute,//从特性读取主键和自增列信息
            IsAutoCloseConnection = true,//开启自动释放模式和EF原理一样我就不多解释了
        });
        /// <summary>
        /// 生成实体类
        /// </summary>
        /// <param name="entityName"></param>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public bool CreateEntity(string entityName, string filePath)
        {
            try
            {
                db.DbFirst.IsCreateAttribute().Where(entityName).SettingClassTemplate(old =>
                {
                    return old.Replace("{Namespace}", "MME.Domain.Repositories");//修改Namespace命名空间
                }).CreateClassFile(filePath);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
