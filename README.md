# MME - 模型管理评测系统

MME（Model Management & Evaluation）是一个基于 Blazor Server Side 构建的现代化 LLM 日志追踪、回放、评测一体化平台，为企业级 AI 应用提供全方位的模型管理和效果评估解决方案。

## ✨ 核心功能特性

### 🔀 智能代理切换
- **多配置管理**：支持管理多个不同 LLM 服务商（OpenAI、Azure、Bedrock、私有模型）
- **智能路由**：自动代理转发请求到目标 API 服务，统一接口管理
- **Bearer Token 管理**：为每个配置自动生成独立的访问令牌
- **流式支持**：完整支持 SSE 流式响应，实时记录和转发
- **接口覆盖**：支持 `/v1/chat/completions`、`/v1/embeddings`、`/v1/rerank`、`/v1/models` 等接口
- **高可用性**：支持请求重试、超时配置、自动故障转移

### 📊 A/B 测试评估
- **对比测试**：对同一批请求进行不同模型间的 A/B 测试
- **回归测试**：验证模型更新后的效果变化
- **量化分析**：科学评估不同模型的效果表现
- **数据驱动**：基于真实请求数据进行模型选择决策

### 🗂️ 智能数据集管理
- **多类型支持**：支持 QA（问答）、Chat（对话）、Completion（文本完成）等数据集类型
- **来源多样化**：支持从请求日志、手动添加、批量导入等多种方式创建数据集
- **批量操作**：支持 Excel、CSV、JSON 格式的批量导入导出
- **智能标签**：灵活的标签分类和质量评分机制（难度1-5级，质量1-5星）
- **统计分析**：提供详细的数据集统计信息和分布图表
- **版本管理**：支持数据集的状态管理（活跃、归档、删除）

### 📈 全链路日志追踪
- **实时记录**：自动记录所有 API 请求和响应的完整信息
- **流式处理**：专门优化的流式响应记录和合并机制
- **详细分析**：包含请求体、响应体、耗时、状态码、Token 使用等信息
- **智能筛选**：支持按时间范围、状态码、代理配置、模型名称等多维度筛选
- **数据导出**：支持导出为 Excel 和 Log 文件格式，便于后续分析
- **性能监控**：监控 API 调用性能和成本使用情况

### 🔄 日志回放功能
- **一键回放**：将历史请求日志快速转换为测试数据集
- **智能去重**：自动检测和跳过已存在的重复数据
- **批量添加**：支持批量选择日志条目添加到数据集
- **元数据保留**：保留原始请求的模型名称、代理信息等元数据
- **质量标注**：支持为回放数据添加难度和质量评分

### 🔐 企业级认证
- **Cookie 认证**：基于安全 Cookie 的认证机制
- **会话管理**：支持 7 天免登录和滑动过期
- **权限控制**：页面级访问控制，保护敏感数据
- **会话保护**：防止会话劫持和数据泄露

## 🛠️ 技术架构

### 前端技术栈
- **Blazor Server Side**：.NET 8.0 原生 Web 框架
- **Ant Design Blazor**：企业级 UI 组件库
- **响应式设计**：适配多种设备和屏幕尺寸

### 后端技术栈
- **ASP.NET Core 8.0**：高性能 Web API 框架
- **Yarp 反向代理**：微软官方反向代理解决方案
- **中间件架构**：自定义代理日志中间件

### 数据存储
- **主数据库**：PostgreSQL / SQLite 双模式支持
- **ORM 框架**：SqlSugar，支持 Code First
- **向量存储**：预留向量数据库支持接口

### 第三方集成
- **Microsoft.SemanticKernel.Core**：AI 应用开发框架
- **AutoMapper**：对象映射框架
- **NPOI**：Excel 文件处理
- **Polly**：重试和熔断策略

## 🚀 快速开始

### 环境要求

- **.NET 8.0 SDK** 或更高版本
- **PostgreSQL 12+** 或 **SQLite**（开发环境）
- **Docker**（可选，用于容器部署）

### 本地开发部署

1. **克隆项目**
```bash
git clone https://github.com/xuzeyu91/MME
cd MME
```

2. **配置数据库连接**

编辑 `src/MME/appsettings.json` 文件：

**PostgreSQL 配置（生产推荐）：**
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

**SQLite 配置（开发环境）：**
```json
{
  "DBConnection": {
    "DbType": "Sqlite",
    "DBConnection": "Data Source=mme.db",
    "VectorConnection": "mmevector.db",
    "VectorSize": 1536
  }
}
```

3. **配置管理员账户**
```json
{
  "Admin": {
    "Username": "admin",
    "Password": "your_secure_password"
  }
}
```

4. **配置 OpenAI 设置**（可选）
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

5. **启动应用**
```bash
cd src/MME
dotnet restore
dotnet run
```

6. **访问系统**
- 打开浏览器访问：`http://localhost:5000`
- 使用配置的管理员账户登录系统

### Docker 容器部署

1. **构建镜像**
```bash
docker build -t mme:latest .
```

2. **运行容器**
```bash
docker run -d \
  --name mme \
  -p 5000:5000 \
  -e DBConnection__DbType="PostgreSQL" \
  -e DBConnection__DBConnection="Host=your_db_host;Port=5432;Database=mme;User ID=your_user;Password=your_password;" \
  -e Admin__Username="admin" \
  -e Admin__Password="your_secure_password" \
  mme:latest
```

## 📖 使用指南

### 1. 系统登录
- 使用配置的管理员账户登录系统
- 默认账户：用户名 `admin`，密码 `123456`
- **重要：首次登录后请立即修改默认密码**

![登录界面](images/登录.png)

### 2. 配置 API 代理
1. 导航到「代理配置管理」页面
2. 点击「添加配置」按钮
3. 填写代理配置信息：
   - **配置名称**：便于识别的名称（如：OpenAI-GPT4）
   - **目标地址**：API 服务商地址（如：`https://api.openai.com`）
   - **API 密钥**：目标服务的 API Key
   - **超时设置**：请求超时时间（默认300秒）
   - **重试次数**：失败重试次数（默认3次）
   - **支持接口**：选择需要代理的接口类型
4. 保存后系统自动生成专用的 Bearer Token

![代理配置界面](images/添加代理.png)

### 3. 使用代理 API
配置完成后，使用生成的 Bearer Token 调用代理接口：

```bash
# Chat Completions 示例
curl -X POST http://localhost:5000/v1/chat/completions \
  -H "Authorization: Bearer your_generated_bearer_token" \
  -H "Content-Type: application/json" \
  -d '{
    "model": "gpt-4",
    "messages": [
      {"role": "user", "content": "你好，请介绍一下MME系统"}
    ],
    "stream": true
  }'

# Embeddings 示例
curl -X POST http://localhost:5000/v1/embeddings \
  -H "Authorization: Bearer your_generated_bearer_token" \
  -H "Content-Type: application/json" \
  -d '{
    "model": "text-embedding-ada-002",
    "input": "MME是一个LLM管理平台"
  }'
```

### 4. 数据集管理操作

#### 创建数据集
1. 进入「数据集管理」页面
2. 点击「创建数据集」
3. 填写数据集信息：
   - **数据集名称**：唯一标识名称
   - **描述信息**：数据集用途和说明
   - **数据类型**：QA（问答）、Chat（对话）、Completion（文本完成）
   - **标签分类**：便于后续筛选和管理

#### 添加数据项
支持多种方式添加数据：

- **手动添加**：逐条添加高质量数据
- **批量导入**：支持 Excel、CSV、JSON 格式
- **日志回放**：从历史请求日志中筛选和转换

#### 数据集导出
- **Excel 格式**：包含完整数据项信息，便于离线查看编辑
- **Log 格式**：适合程序处理和模型训练的格式

![数据集管理界面](images/数据集管理.png)

### 5. 请求日志分析
1. 进入「请求日志」页面查看所有 API 调用记录
2. 使用筛选功能：
   - **时间范围**：查看特定时间段的请求
   - **状态码**：筛选成功/失败的请求
   - **代理配置**：按不同配置分组查看
   - **模型名称**：按使用的模型分类
3. 查看详细信息：
   - 点击日志条目查看完整的请求响应详情
   - 流式响应自动合并显示最终内容
   - Token 使用统计和成本分析
4. 导出分析：
   - 支持导出筛选结果为 Excel 或 Log 文件
   - 便于进行离线分析和报告生成

![请求日志界面](images/请求日志.png)

### 6. A/B 测试和效果评估
1. 从历史日志中选择测试数据集
2. 配置多个模型的代理
3. 使用相同输入测试不同模型
4. 对比分析响应质量和性能指标
5. 基于结果数据做出模型选择决策

## 🔧 系统配置

### 主要配置参数

| 配置项 | 说明 | 默认值 | 备注 |
|--------|------|--------|------|
| `urls` | 应用监听地址 | `http://*:5000` | 生产环境建议使用 HTTPS |
| `ProSettings.Title` | 应用标题 | `MME` | 可自定义品牌名称 |
| `DBConnection.DbType` | 数据库类型 | `PostgreSQL` | 支持 PostgreSQL/SQLite |
| `Admin.Username` | 管理员用户名 | `admin` | 建议使用强用户名 |
| `Admin.Password` | 管理员密码 | `123456` | **必须修改默认密码** |

### 代理配置参数

| 参数 | 说明 | 默认值 | 调优建议 |
|------|------|--------|----------|
| `TimeoutSeconds` | 请求超时时间（秒） | `3000` | 根据模型响应时间调整 |
| `MaxRetries` | 最大重试次数 | `3` | 平衡可用性和性能 |
| `LogRequestBody` | 是否记录请求体 | `true` | 开发环境建议开启 |
| `LogResponseBody` | 是否记录响应体 | `true` | 注意存储空间占用 |

### 性能优化建议

1. **数据库优化**：
   - 生产环境使用 PostgreSQL
   - 定期清理过期日志数据
   - 适当建立索引提升查询性能

2. **内存管理**：
   - 大文件导出时注意内存使用
   - 流式响应缓存适当限制大小
   - 定期重启应用释放内存

3. **网络配置**：
   - 配置适当的超时时间
   - 启用 HTTP/2 支持
   - 使用 CDN 加速静态资源

## 📁 项目架构

```
MME/
├── src/
│   ├── MME/                          # 主应用程序
│   │   ├── Controllers/              # API 控制器
│   │   │   ├── OpenAIProxyController.cs  # OpenAI 代理控制器
│   │   │   └── ProxyController.cs    # 代理配置 API
│   │   ├── Pages/                    # Blazor 页面组件
│   │   │   ├── Index.razor           # 系统首页
│   │   │   ├── DatasetManagement.razor  # 数据集管理
│   │   │   ├── DatasetDetail.razor   # 数据集详情
│   │   │   ├── ProxyConfig.razor     # 代理配置管理
│   │   │   ├── RequestLogs.razor     # 请求日志查看
│   │   │   ├── UserGuide.razor       # 使用手册
│   │   │   └── User/                 # 用户认证页面
│   │   ├── Components/               # 可复用组件
│   │   │   ├── GlobalHeader/         # 全局头部组件
│   │   │   └── RequestLogDetailModal.razor  # 日志详情弹窗
│   │   ├── Layouts/                  # 页面布局模板
│   │   ├── Services/Auth/            # 认证服务
│   │   └── wwwroot/                  # 静态资源
│   └── MME.Domain/                   # 领域层
│       ├── Repositories/             # 数据仓储层
│       │   ├── ApiRequestLog/        # API 日志仓储
│       │   ├── Dataset/              # 数据集仓储
│       │   ├── ProxyConfig/          # 代理配置仓储
│       │   └── Base/                 # 基础仓储
│       ├── Services/                 # 领域服务层
│       ├── Middlewares/              # 中间件
│       │   └── ProxyLoggingMiddleware.cs  # 代理日志中间件
│       ├── Common/                   # 公共组件
│       │   ├── Extensions/           # 扩展方法
│       │   ├── Options/              # 配置选项
│       │   └── Excel/                # Excel 处理
│       └── Utils/                    # 工具类
├── Dockerfile                        # Docker 容器配置
└── README.md                         # 项目文档
```

## 🎯 应用场景

### 企业 AI 应用开发
- 为企业内部 AI 应用提供统一的模型接入和管理平台
- 支持多团队、多项目的模型资源共享
- 提供完整的 API 调用链路追踪和监控

### 模型效果评估
- 对不同 LLM 模型进行科学的 A/B 测试
- 基于真实业务数据评估模型效果
- 支持模型更新后的回归测试验证

### 成本优化分析
- 实时监控 Token 使用情况和 API 调用成本
- 分析不同模型的性价比
- 优化模型选择策略降低运营成本

### 数据集构建和管理
- 从生产环境的真实请求中构建高质量测试数据集
- 支持数据集版本管理和质量评估
- 便于进行模型fine-tuning和效果验证

## 🔮 后续规划

### 近期功能
- [ ] 支持更多 LLM 服务商 API（Claude、文心一言等）
- [ ] 增加模型性能监控和报警功能
- [ ] 支持自定义评估指标和规则
- [ ] 增加数据集导入格式验证

### 中期目标
- [ ] 集成向量数据库支持语义搜索
- [ ] 实现模型输出质量自动评估
- [ ] 支持多租户和团队协作功能
- [ ] 增加可视化的数据分析图表

### 长期愿景
- [ ] AI 模型效果自动评测平台
- [ ] 智能化的模型推荐和切换
- [ ] 与 MLOps 平台深度集成
- [ ] 支持自定义模型接入和管理

## 🤝 贡献指南

欢迎提交 Issue 和 Pull Request 来改进项目！

### 开发环境搭建
1. 安装 .NET 8.0 SDK
2. 安装 PostgreSQL 数据库
3. 克隆项目并还原依赖包
4. 配置开发环境数据库连接
5. 运行项目进行开发和调试

### 代码贡献规范
- 遵循 C# 和 Blazor 最佳实践
- 使用有意义的变量和方法命名
- 添加必要的代码注释和文档
- 提交前确保代码通过编译和基本测试

### Issue 提交指南
- 使用清晰的标题描述问题
- 提供详细的问题复现步骤
- 包含相关的错误日志和截图
- 标明使用的操作系统和 .NET 版本

## 📄 开源协议

本项目采用 MIT 开源协议，详情请参阅 [LICENSE](LICENSE) 文件。

## 📚 系统使用手册

系统内置了详细的使用手册，帮助用户快速上手：

![使用手册界面](images/使用手册.png)

## 🆘 技术支持

如果您在使用过程中遇到问题：

1. 📖 查看本文档的详细说明
2. 🔍 搜索已有的 Issue 和解决方案  
3. 💬 提交新的 Issue 描述问题
4. 📧 联系开发团队获取技术支持

---

**MME** - 让 LLM 模型管理和评测更智能、更高效！

🌟 **Star** 项目以支持我们的工作  
🔄 **Fork** 项目开始您的贡献  
📢 **Share** 将 MME 推荐给更多开发者
