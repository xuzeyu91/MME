# OpenAI API 代理系统使用说明

## 功能概述

这个系统提供了一个OpenAI API的代理服务，可以：

1. **代理转发**：将客户端的OpenAI API请求转发到指定的目标服务器
2. **身份验证**：使用自定义的Bearer Token进行身份验证，自动替换为目标服务器的API Key
3. **请求日志**：记录所有API请求和响应的详细信息到数据库
4. **配置管理**：通过Web界面管理代理配置

## 支持的API端点

- `/v1/chat/completions` - 聊天完成
- `/v1/embeddings` - 文本嵌入
- `/v1/rerank` - 重排序
- `/v1/models` - 模型列表

## 使用步骤

### 1. 创建代理配置

1. 访问系统的"代理配置"页面
2. 点击"添加配置"按钮
3. 填写以下信息：
   - **配置名称**：给配置起一个名字，如"OpenAI生产环境"
   - **目标地址**：目标OpenAI API服务器地址，如 `https://api.openai.com`
   - **API密钥**：目标服务器的真实API Key
   - **描述**：可选的描述信息

4. 点击"确定"保存配置
5. 系统会自动生成一个Bearer Token供客户端使用

### 2. 使用代理服务

客户端可以像使用普通的OpenAI API一样使用代理服务：

```bash
# 原本的请求
curl https://api.openai.com/v1/chat/completions \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer sk-your-real-api-key" \
  -d '{
    "model": "gpt-3.5-turbo",
    "messages": [{"role": "user", "content": "Hello!"}]
  }'

# 通过代理的请求
curl http://your-proxy-server/v1/chat/completions \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer pk-your-generated-token" \
  -d '{
    "model": "gpt-3.5-turbo",
    "messages": [{"role": "user", "content": "Hello!"}]
  }'
```

### 3. 查看请求日志

1. 访问系统的"请求日志"页面
2. 可以看到所有API请求的详细信息：
   - 请求时间
   - HTTP方法和路径
   - 状态码
   - 响应时间
   - 客户端IP
   - Token使用量

3. 点击"详情"按钮可以查看完整的请求和响应内容

## 配置管理

### 编辑配置
1. 在代理配置页面找到要编辑的配置
2. 点击"编辑"按钮
3. 修改信息后保存

### 启用/禁用配置
- 使用配置行中的开关可以快速启用或禁用配置
- 禁用的配置不会处理任何请求

### 刷新Token
- 如果需要更换Bearer Token，可以点击"刷新"按钮
- 系统会生成一个新的Token，旧Token将失效

### 删除配置
- 点击"删除"按钮可以删除配置
- 删除后相关的Bearer Token将失效

## 安全注意事项

1. **保护Bearer Token**：生成的Bearer Token相当于API密钥，请妥善保管
2. **定期轮换**：建议定期刷新Bearer Token
3. **访问控制**：确保代理服务器的管理界面有适当的访问控制
4. **日志管理**：请求日志包含敏感信息，注意数据安全

## 技术架构

- **后端**：ASP.NET Core + Blazor Server
- **数据库**：SqlSugar ORM + SQLite/SQL Server
- **UI框架**：Ant Design Blazor
- **代理实现**：自定义HTTP代理控制器

## 配置文件

系统使用 `appsettings.json` 进行配置：

```json
{
  "DBConnection": {
    // 数据库连接配置
  },
  "Logging": {
    // 日志配置
  }
}
``` 