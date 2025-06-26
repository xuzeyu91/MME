using MME.Domain;
using MME.Domain.Common.Options;
using SqlSugar;
using System.Reflection;
using Microsoft.Extensions.Options;

namespace MME.Domain.Repositories.Base
{
    public class SqlSugarHelper
    {
        /// <summary>
        /// 创建 SqlSugar 作用域（使用配置选项）
        /// </summary>
        public static SqlSugarScope CreateSqlScope(DBConnectionOption dbOptions)
        {
            var config = new ConnectionConfig()
            {
                ConnectionString = dbOptions.DBConnection,
                InitKeyType = InitKeyType.Attribute,//从特性读取主键和自增列信息
                IsAutoCloseConnection = true,
                ConfigureExternalServices = new ConfigureExternalServices
                {
                    //注意:  这儿AOP设置不能少
                    EntityService = (c, p) =>
                    {
                        /***高版C#写法***/
                        //支持string?和string  
                        if (p.IsPrimarykey == false && new NullabilityInfoContext()
                         .Create(c).WriteState is NullabilityState.Nullable)
                        {
                            p.IsNullable = true;
                        }
                    }
                }
            };
            
            DbType dbType = (DbType)Enum.Parse(typeof(DbType), dbOptions.DbType);
            config.DbType = dbType;
            
            var scope = new SqlSugarScope(config, Db =>
            {
                // 可以在这里添加额外的配置
            });
            
            return scope;
        }

        /// <summary>
        /// sqlserver连接（向后兼容）
        /// </summary>
        [Obsolete("请使用 CreateSqlScope(DBConnectionOption) 方法")]
        public static SqlSugarScope SqlScope()
        {
            string DBType = DBConnectionOption.StaticDbType ?? "Sqlite";
            string ConnectionString = DBConnectionOption.ConnectionStrings ?? "";

            var config = new ConnectionConfig()
            {
                ConnectionString = ConnectionString,
                InitKeyType = InitKeyType.Attribute,//从特性读取主键和自增列信息
                IsAutoCloseConnection = true,
                ConfigureExternalServices = new ConfigureExternalServices
                {
                    //注意:  这儿AOP设置不能少
                    EntityService = (c, p) =>
                    {
                        /***高版C#写法***/
                        //支持string?和string  
                        if (p.IsPrimarykey == false && new NullabilityInfoContext()
                         .Create(c).WriteState is NullabilityState.Nullable)
                        {
                            p.IsNullable = true;
                        }
                    }
                }
            };
            DbType dbType = (DbType)Enum.Parse(typeof(DbType), DBType);
            config.DbType = dbType;
            var scope = new SqlSugarScope(config, Db =>
            {

            });
            return scope;
        }
    }
}
