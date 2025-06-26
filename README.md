MME – Model Monitoring & Evaluation
使用 .NET 8 + Ant Design Blazor 打造的 LLM 日志追踪 / 回放 / 评测一体化平台

背景 / Motivation
在真实业务中我们经常需要

快速切换不同 LLM（OpenAI、Azure、Bedrock、私有 Llama…）
对同一批请求做 A/B 或回归测试，量化模型效果
采集线上日志，事后定位 prompt / 参数 / token / latency
给输出结果加标签，沉淀高质量数据集
LangChain 等侧重“编排”，W&B 更关注“训练”，缺少一个专门做“监控 + 评测 + 标注”的轻量平台。
MME 目标：像加日志框架一样简单地给任何 .NET 应用插上 LLM 可观测性与评测能力。

