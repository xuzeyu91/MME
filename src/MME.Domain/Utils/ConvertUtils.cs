using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;

namespace MME.Domain
{
    /// <summary>
    /// 转换工具类
    /// </summary>
    public static class ConvertUtils
    {
        /// <summary>
        /// 判断是否为空，为空返回true
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static bool IsNull(this object data)
        {
            //如果为null
            if (data == null)
            {
                return true;
            }

            //如果为""
            if (data.GetType() == typeof(String))
            {
                if (string.IsNullOrEmpty(data.ToString().Trim()))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 判断是否为空，为空返回true
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static bool IsNotNull(this object data)
        {
            //如果为null
            if (data == null)
            {
                return false;
            }

            //如果为""
            if (data.GetType() == typeof(String))
            {
                if (string.IsNullOrEmpty(data.ToString().Trim()))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 判断是否为空，为空返回true
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static bool IsNull(string data)
        {
            //如果为null
            if (data == null)
            {
                return true;
            }

            //如果为""
            if (data.GetType() == typeof(String))
            {
                if (string.IsNullOrEmpty(data.ToString().Trim()))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 将obj类型转换为string
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string ConvertToString(this object s)
        {
            if (s == null)
            {
                return "";
            }
            else
            {
                return Convert.ToString(s);
            }
        }

        /// <summary>
        /// object 转int32
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static Int32 ConvertToInt32(this object s)
        {
            int i = 0;
            if (s == null)
            {
                return 0;
            }
            else
            {
                int.TryParse(s.ToString(), out i);
            }
            return i;
        }

        /// <summary>
        /// object 转int32
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static Int64 ConvertToInt64(this object s)
        {
            long i = 0;
            if (s == null)
            {
                return 0;
            }
            else
            {
                long.TryParse(s.ToString(), out i);
            }
            return i;
        }

        /// <summary>
        /// 将字符串转double
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static double ConvertToDouble(this object s)
        {
            double i = 0;
            if (s == null)
            {
                return 0;
            }
            else
            {
                double.TryParse(s.ToString(), out i);
            }
            return i;
        }

        /// <summary>
        /// 转换为datetime类型
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static DateTime ConvertToDateTime(this string s)
        {
            DateTime dt = new DateTime();
            if (s == null || s == "")
            {
                return DateTime.Now;
            }
            DateTime.TryParse(s, out dt);
            return dt;
        }

        /// <summary>
        /// 转换为datetime类型的格式字符串
        /// </summary>
        /// <param name="s">要转换的对象</param>
        /// <param name="y">格式化字符串</param>
        /// <returns></returns>
        public static string ConvertToDateTime(this string s, string y)
        {
            DateTime dt = new DateTime();
            DateTime.TryParse(s, out dt);
            return dt.ToString(y);
        }


        /// <summary>
        /// 将字符串转换成decimal
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static decimal ConvertToDecimal(this object s)
        {
            decimal d = 0;
            if (s == null || s == "")
            {
                return 0;
            }

            Decimal.TryParse(s.ToString(), out d);

            return d;

        }
        /// <summary>
        /// decimal保留2位小数
        /// </summary>
        public static decimal DecimalFraction(this decimal num)
        {
            return Convert.ToDecimal(num.ToString("f2"));
        }


        /// <summary>
        /// 替换html种的特殊字符
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string ReplaceHtml(this string s)
        {
            return s.Replace("&lt;", "<").Replace("&gt;", ">").Replace("&amp;", "&").Replace("&quot;", "\"");
        }

        /// <summary>
        /// 流转byte
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static byte[] StreamToByte(this Stream stream)
        {
            byte[] bytes = new byte[stream.Length];
            stream.Read(bytes, 0, bytes.Length);
            // 设置当前流的位置为流的开始
            stream.Seek(0, SeekOrigin.Begin);
            return bytes;
        }

        /// <summary>
        /// \uxxxx转中文,保留换行符号
        /// </summary>
        /// <param name="unicodeString"></param>
        /// <returns></returns>
        public static string Unescape(this string value)
        {
            if (value.IsNull())
            {
                return "";
            }

            try
            {
                Formatting formatting = Formatting.None;

                object jsonObj = JsonConvert.DeserializeObject(value);
                string unescapeValue = JsonConvert.SerializeObject(jsonObj, formatting);
                return unescapeValue;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return "";
            }
        }

        /// <summary>
        /// 解析流式响应并合并内容
        /// </summary>
        /// <param name="responseBody">流式响应体</param>
        /// <returns>合并后的JSON格式响应</returns>
        public static string ParseAndMergeStreamResponse(string? responseBody)
        {
            if (string.IsNullOrEmpty(responseBody))
                return "";

            try
            {
                var lines = responseBody.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                var contentBuilder = new StringBuilder();
                var reasoningContentBuilder = new StringBuilder();
                var reasoningBuilder = new StringBuilder();
                var toolCallsList = new List<object>();
                string? chatId = null;
                string? modelName = null;
                int lineIndex = 0;
                
                foreach (var line in lines)
                {
                    var trimmedLine = line.Trim();
                    if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("data: [DONE]"))
                        continue;
                        
                    lineIndex++;
                    
                    // 移除 "data: " 前缀
                    if (trimmedLine.StartsWith("data: "))
                    {
                        trimmedLine = trimmedLine.Substring(6);
                    }
                    
                    try
                    {
                        var json = JObject.Parse(trimmedLine);
                        
                        // 从第二条消息开始解析id和model（第一条通常为空）
                        if (lineIndex >= 2)
                        {
                            if (chatId == null && json.TryGetValue("id", out var idElement) && idElement.Type == JTokenType.String)
                            {
                                chatId = idElement.Value<string>();
                            }
                            
                            if (modelName == null && json.TryGetValue("model", out var modelElement) && modelElement.Type == JTokenType.String)
                            {
                                modelName = modelElement.Value<string>();
                            }
                        }
                        
                        // 检查是否有choices数组
                        if (json.TryGetValue("choices", out var choicesToken) && choicesToken.Type == JTokenType.Array)
                        {
                            var choices = choicesToken as JArray;
                            if (choices != null)
                            {
                                foreach (var choice in choices)
                                {
                                    // 流式响应只提取 delta 中的内容，避免重复
                                    if (choice is JObject choiceObj && choiceObj.TryGetValue("delta", out var deltaToken) && deltaToken is JObject delta)
                                    {
                                        // 提取 content
                                        if (delta.TryGetValue("content", out var contentToken) && contentToken.Type == JTokenType.String)
                                        {
                                            contentBuilder.Append(contentToken.Value<string>());
                                        }
                                        
                                        // 提取 reasoning_content（兼容形式）
                                        if (delta.TryGetValue("reasoning_content", out var reasoningContentToken) && reasoningContentToken.Type == JTokenType.String)
                                        {
                                            reasoningContentBuilder.Append(reasoningContentToken.Value<string>());
                                        }
                                        
                                        // 提取 reasoning
                                        if (delta.TryGetValue("reasoning", out var reasoningToken) && reasoningToken.Type == JTokenType.String)
                                        {
                                            reasoningBuilder.Append(reasoningToken.Value<string>());
                                        }
                                        
                                        // 提取 tool_calls
                                        if (delta.TryGetValue("tool_calls", out var toolCallsToken) && toolCallsToken.Type == JTokenType.Array)
                                        {
                                            var toolCalls = toolCallsToken as JArray;
                                            if (toolCalls != null)
                                            {
                                                foreach (var toolCall in toolCalls)
                                                {
                                                    if (toolCall is JObject)
                                                    {
                                                        toolCallsList.Add(toolCall);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (JsonException)
                    {
                        // 忽略无法解析的行
                        continue;
                    }
                }
                
                // 构建标准的ChatCompletion响应格式，包含合并后的内容
                var messageContent = new Dictionary<string, object>
                {
                    ["role"] = "assistant",
                    ["content"] = contentBuilder.ToString()
                };
                
                // 添加reasoning_content（如果有）
                if (reasoningContentBuilder.Length > 0)
                {
                    messageContent["reasoning_content"] = reasoningContentBuilder.ToString();
                }
                
                // 添加reasoning（如果有）
                if (reasoningBuilder.Length > 0)
                {
                    messageContent["reasoning"] = reasoningBuilder.ToString();
                }
                
                // 添加tool_calls（如果有）
                if (toolCallsList.Any())
                {
                    messageContent["tool_calls"] = toolCallsList.ToArray();
                }
                
                var mergedResponse = new
                {
                    id = chatId ?? "merged-response",
                    model = modelName ?? "merged-stream",
                    @object = "chat.completion",
                    choices = new[]
                    {
                        new
                        {
                            index = 0,
                            message = messageContent,
                            finish_reason = "stop"
                        }
                    },
                    usage = new
                    {
                        content_length = contentBuilder.Length,
                        reasoning_content_length = reasoningContentBuilder.Length,
                        reasoning_length = reasoningBuilder.Length,
                        tool_calls_count = toolCallsList.Count,
                        total_chunks_merged = lineIndex
                    },
                    created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    merged_at = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };
                
                return JsonConvert.SerializeObject(mergedResponse, Formatting.Indented);
            }
            catch (Exception ex)
            {
                return $"解析流式响应失败: {ex.Message}";
            }
        }

    }
}
