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
        public static SqlSugarScope CreateSqlScope()
        {
            var config = new ConnectionConfig()
            {
                ConnectionString = DBConnectionOption.DBConnection,
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
                        
                        // PostgreSQL 特定处理
                        if (DBConnectionOption.DbType == "PostgreSQL")
                        {
                            // 明确处理自增列类型映射
                            if (p.IsIdentity)
                            {
                                if (p.PropertyInfo.PropertyType == typeof(long) || p.PropertyInfo.PropertyType == typeof(long?))
                                {
                                    p.DataType = "bigserial";
                                }
                                else if (p.PropertyInfo.PropertyType == typeof(int) || p.PropertyInfo.PropertyType == typeof(int?))
                                {
                                    p.DataType = "serial";
                                }
                            }
                            
                            // 处理其他常见的数据类型映射
                            if (p.PropertyInfo.PropertyType == typeof(DateTime) || p.PropertyInfo.PropertyType == typeof(DateTime?))
                            {
                                p.DataType = "timestamp";
                            }
                            else if (p.PropertyInfo.PropertyType == typeof(bool) || p.PropertyInfo.PropertyType == typeof(bool?))
                            {
                                p.DataType = "boolean";
                            }
                            else if (p.PropertyInfo.PropertyType == typeof(string) && p.Length > 255)
                            {
                                p.DataType = "text";
                            }
                        }
                    }
                }
            };
            
            DbType dbType = (DbType)Enum.Parse(typeof(DbType), DBConnectionOption.DbType);
            config.DbType = dbType;
            
            var scope = new SqlSugarScope(config, Db =>
            {
                // PostgreSQL 特定配置
                if (DBConnectionOption.DbType == "PostgreSQL")
                {
                    // 设置 PostgreSQL 特定的配置
                    Db.CurrentConnectionConfig.MoreSettings = new ConnMoreSettings()
                    {
                        IsAutoRemoveDataCache = true,
                        SqlServerCodeFirstNvarchar = true
                    };
                }
            });
            
            return scope;
        }

        /// <summary>
        /// sqlserver连接（向后兼容）
        /// </summary>
        [Obsolete("请使用 CreateSqlScope(DBConnectionOption) 方法")]
        public static SqlSugarScope SqlScope()
        {
            string DBType = DBConnectionOption.DbType ?? "Sqlite";
            string ConnectionString = DBConnectionOption.DBConnection ?? "";

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
                        
                        // PostgreSQL 特定处理
                        if (DBType == "PostgreSQL")
                        {
                            // 明确处理自增列类型映射
                            if (p.IsIdentity)
                            {
                                if (p.PropertyInfo.PropertyType == typeof(long) || p.PropertyInfo.PropertyType == typeof(long?))
                                {
                                    p.DataType = "bigserial";
                                }
                                else if (p.PropertyInfo.PropertyType == typeof(int) || p.PropertyInfo.PropertyType == typeof(int?))
                                {
                                    p.DataType = "serial";
                                }
                            }
                        }
                    }
                }
            };
            DbType dbType = (DbType)Enum.Parse(typeof(DbType), DBType);
            config.DbType = dbType;
            var scope = new SqlSugarScope(config, Db =>
            {
                // PostgreSQL 特定配置
                if (DBType == "PostgreSQL")
                {
                    Db.CurrentConnectionConfig.MoreSettings = new ConnMoreSettings()
                    {
                        IsAutoRemoveDataCache = true,
                        SqlServerCodeFirstNvarchar = true
                    };
                }
            });
            return scope;
        }
    }
}
