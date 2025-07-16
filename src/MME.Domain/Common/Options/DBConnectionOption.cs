namespace MME.Domain.Common.Options
{
    public class DBConnectionOption
    {
        /// <summary>
        /// 数据库类型
        /// </summary>
        public static string DbType { get; set; } = "Sqlite";
        
        /// <summary>
        /// 数据库连接字符串
        /// </summary>
        public static string DBConnection { get; set; } = "";
        
        /// <summary>
        /// 向量数据库连接字符串
        /// </summary>
        public static string VectorConnection { get; set; } = "";
        
        /// <summary>
        /// 向量大小
        /// </summary>
        public static int VectorSize { get; set; } = 1536;
        
    }
}
