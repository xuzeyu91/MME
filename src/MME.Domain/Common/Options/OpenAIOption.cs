using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MME.Domain.Common.Options
{
    public class OpenAIOption
    {
        /// <summary>
        /// OpenAI 端点
        /// </summary>
        public string EndPoint { get; set; } = "";

        /// <summary>
        /// API 密钥
        /// </summary>
        public string Key { get; set; } = "";

        /// <summary>
        /// 聊天模型
        /// </summary>
        public string ChatModel { get; set; } = "";

        /// <summary>
        /// 嵌入模型
        /// </summary>
        public string EmbeddingModel { get; set; } = "";

        // 保持静态属性用于向后兼容
        public static string StaticEndPoint { get; set; } = "";
        public static string StaticKey { get; set; } = "";
        public static string StaticChatModel { get; set; } = "";
        public static string StaticEmbeddingModel { get; set; } = "";
    }
}
