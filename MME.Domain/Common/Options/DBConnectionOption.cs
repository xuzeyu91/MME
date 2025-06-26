namespace MME.Domain.Common.Options
{
    public class DBConnectionOption
    {
        /// <summary>
        /// 数据库类型
        /// </summary>
        public string DbType { get; set; } = "Sqlite";
        
        /// <summary>
        /// 数据库连接字符串
        /// </summary>
        public string DBConnection { get; set; } = "";
        
        /// <summary>
        /// 向量数据库连接字符串
        /// </summary>
        public string VectorConnection { get; set; } = "";
        
        /// <summary>
        /// 向量大小
        /// </summary>
        public int VectorSize { get; set; } = 1536;
        
        // 保持静态属性用于向后兼容
        public static string StaticDbType { get; set; } = "Sqlite";
        public static string ConnectionStrings { get; set; } = "";
    }
}
