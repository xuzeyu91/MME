# MME - 模型管理评测系统

MME（Model Management & Evaluation）是一个基于 Blazor Server Side 构建的现代化模型管理评测平台，提供 OpenAI API 代理、数据集管理、请求日志分析等核心功能。

## ✨ 核心功能

### 🔀 OpenAI API 代理
- **多配置管理**：支持管理多个不同的 OpenAI API 配置
- **自动转发**：智能代理转发请求到目标 API 服务
- **Bearer Token 管理**：为每个配置生成独立的访问令牌
- **流式支持**：完整支持流式响应（SSE）
- **接口覆盖**：支持 `/v1/chat/completions`、`/v1/embeddings`、`/v1/rerank`、`/v1/models` 等接口

### 📊 数据集管理
- **多类型支持**：支持 QA（问答）、Chat（对话）、Completion（文本完成）等数据集类型
- **批量操作**：支持批量导入、导出数据集
- **格式导出**：支持导出为 Excel 和 Log 文件格式
- **标签系统**：灵活的标签分类管理
- **质量评估**：内置难度和质量评分机制

### 📈 请求日志分析
- **实时记录**：自动记录所有 API 请求和响应
- **详细分析**：包含请求体、响应体、耗时、状态码等详细信息
- **数据导出**：支持日志数据导出和分析
- **可视化展示**：提供直观的数据统计图表

### 🔐 用户认证
- **Cookie 认证**：基于 Cookie 的安全认证机制
- **会话管理**：支持 7 天免登录和滑动过期
- **权限控制**：基于角色的访问控制

## 🛠️ 技术栈

- **前端框架**：Blazor Server Side
- **UI 组件库**：Ant Design Blazor
- **后端框架**：ASP.NET Core 8.0
- **数据库**：PostgreSQL
- **ORM 框架**：SqlSugar
- **反向代理**：Yarp
- **容器化**：Docker
- **其他依赖**：
  - Microsoft.SemanticKernel.Core
  - AutoMapper
  - NPOI (Excel处理)
  - Polly (重试策略)

## 🚀 快速开始

### 环境要求

- .NET 8.0 SDK
- PostgreSQL 12+
- Docker（可选）

### 本地运行

1. **克隆项目**
```bash
git clone <repository-url>
cd mme
```

2. **配置数据库**
编辑 `src/MME/appsettings.json` 文件，修改数据库连接字符串：
```json
{
  "DBConnection": {
    "DbType": "PostgreSQL",
    "DBConnection": "Host=localhost;Port=5432;Database=mme;User ID=your_user;Password=your_password;",
    "VectorConnection": "Host=localhost;Port=5432;Database=mme;User ID=your_user;Password=your_password;",
    "VectorSize": 1536
  }
}
```

3. **配置 OpenAI 设置**（可选）
```json
{
  "OpenAI": {
    "Key": "your_openai_api_key",
    "EndPoint": "https://api.openai.com",
    "ChatModel": "gpt-4o",
    "EmbeddingModel": "text-embedding-ada-002"
  }
}
```

4. **配置管理员账户**
```json
{
  "Admin": {
    "Username": "admin",
    "Password": "your_secure_password"
  }
}
```

5. **运行项目**
```bash
cd src/MME
dotnet restore
dotnet run
```

6. **访问应用**
打开浏览器访问：`http://localhost:5000`

### Docker 部署

1. **构建镜像**
```bash
docker build -t mme:latest .
```

2. **运行容器**
```bash
docker run -d \
  --name mme \
  -p 5000:5000 \
  -e DBConnection__DBConnection="Host=your_db_host;Port=5432;Database=mme;User ID=your_user;Password=your_password;" \
  mme:latest
```

## 📖 使用指南

### 1. 用户登录
- 使用配置的管理员账户登录系统
- 默认用户名：`admin`，密码：`123456`（请及时修改）

### 2. 配置 API 代理
1. 进入「代理配置管理」页面
2. 点击「添加配置」
3. 填写配置信息：
   - 配置名称：便于识别的名称
   - 目标地址：如 `https://api.openai.com`
   - API密钥：目标服务的 API Key
   - 支持接口：选择需要代理的接口类型
4. 保存后系统会自动生成 Bearer Token

### 3. 使用代理 API
使用生成的 Bearer Token 访问代理接口：
```bash
curl -X POST http://localhost:5000/v1/chat/completions \
  -H "Authorization: Bearer your_bearer_token" \
  -H "Content-Type: application/json" \
  -d '{
    "model": "gpt-3.5-turbo",
    "messages": [{"role": "user", "content": "Hello!"}]
  }'
```

### 4. 管理数据集
1. 进入「数据集管理」页面
2. 创建新数据集或导入现有数据
3. 支持的数据集类型：
   - **QA**：问答对数据集
   - **Chat**：多轮对话数据集
   - **Completion**：文本完成数据集

### 5. 查看请求日志
- 进入「请求日志」页面查看所有 API 调用记录
- 支持按代理配置、时间范围筛选
- 可导出日志数据进行分析

## 🔧 配置说明

### 主要配置项

| 配置项 | 说明 | 默认值 |
|--------|------|--------|
| `urls` | 应用监听地址 | `http://*:5000` |
| `ProSettings.Title` | 应用标题 | `MME` |
| `DBConnection.DbType` | 数据库类型 | `PostgreSQL` |
| `Admin.Username` | 管理员用户名 | `admin` |
| `Admin.Password` | 管理员密码 | `123456` |

### 代理配置参数

| 参数 | 说明 | 默认值 |
|------|------|--------|
| `TimeoutSeconds` | 请求超时时间（秒） | `3000` |
| `MaxRetries` | 最大重试次数 | `3` |
| `LogRequestBody` | 是否记录请求体 | `true` |
| `LogResponseBody` | 是否记录响应体 | `true` |

## 📁 项目结构

```
mme/
├── src/
│   ├── MME/                          # 主应用程序
│   │   ├── Controllers/              # API 控制器
│   │   │   ├── OpenAIProxyController.cs  # OpenAI 代理控制器
│   │   │   └── ProxyController.cs    # 代理配置控制器
│   │   ├── Pages/                    # Blazor 页面
│   │   │   ├── DatasetManagement.razor  # 数据集管理
│   │   │   ├── ProxyConfig.razor     # 代理配置
│   │   │   └── RequestLogs.razor     # 请求日志
│   │   ├── Components/               # 共享组件
│   │   ├── Layouts/                  # 布局模板
│   │   └── Services/                 # 应用服务
│   └── MME.Domain/                   # 领域层
│       ├── Repositories/             # 数据仓储
│       ├── Services/                 # 领域服务
│       └── Common/                   # 公共组件
├── Dockerfile                        # Docker 配置
└── README.md                         # 项目文档
```

## 🤝 贡献指南

欢迎提交 Issue 和 Pull Request 来改进项目。

### 开发环境搭建

1. 安装 .NET 8.0 SDK
2. 安装 PostgreSQL
3. 克隆项目并还原依赖
4. 运行项目进行开发

### 代码规范

- 遵循 C# 编码规范
- 使用有意义的变量和方法命名
- 添加必要的注释和文档

## 📄 许可证

本项目采用 MIT 许可证，详情请参阅 [LICENSE](LICENSE) 文件。

## 🆘 支持

如果您在使用过程中遇到问题，请：

1. 查看本文档的常见问题部分
2. 提交 Issue 描述问题
3. 联系开发团队

---

**MME** - 让模型管理和评测更简单高效！
