using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MME.Domain.Common.Options
{
    public class OpenAIOption
    {
        public static string EndPoint { get; set; }

        public static string Key { get; set; }

        public static string ChatModel { get; set; }

        public static string EmbeddingModel { get; set; }
    }
}
