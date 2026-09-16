# Rampage

![.NET](https://img.shields.io/badge/.NET-8.0%2B-512BD4) ![C#](https://img.shields.io/badge/C%23-12-239120) ![平台](https://img.shields.io/badge/%E5%B9%B3%E5%8F%B0-Windows%20%7C%20Linux%20%7C%20macOS-6C7A89) ![License](https://img.shields.io/badge/License-MIT-yellow)

**[English](Readme.md) | 简体中文**

Rampage 是一套个人开发、开源的全栈式 .NET 通用开发框架（.NET 8+）。它以「特性驱动、约定优于配置」为设计核心，通过启动器（`RampageBooter`）+ Emit 动态编译，在应用启动时自动完成服务扫描、依赖装配与数据库客户端初始化，让你以最少的样板代码搭建从数据层到 Web 层的完整应用。

框架覆盖企业级开发的完整链路：**自动依赖注入、轻量 ORM（多数据库）、声明式 HTTP 客户端、JWT 认证、限流、幂等、多时区、Redis 分布式组件、Cron 定时任务、弹性（重试/熔断/超时）、RBAC 管理后台、验证码**，并配套**代码生成器**与**数据库部署器**两款开发工具。

---

## 核心特性

- **自动依赖注入** — 在成员上标注 `[Auto]` / `[Singleton]` 即完成注册与注入，无需手写 `AddScoped`；支持 `Lazy<T>` 延迟解析打破循环依赖，启动期自动环检测
- **轻量 ORM 数据层** — `[TableMapping]` / `[FieldMapping]` 实体映射，Lambda 表达式、`LambdaWrapper`、`DataFilter`（HTTP 查询参数直转 SQL 条件）三种查询方式，覆盖增删改查、分页、批量、事务，同步/异步双套 API
- **多数据库支持** — MySQL、SQL Server、PostgreSQL、SQLite、Oracle 五大数据库，配置文件一键切换，多库共存
- **声明式 HTTP 客户端** — 定义接口 + 标注 `[SuperClient]` / `[SuperGet]` / `[SuperPost]` 等特性，框架动态生成异步代理，支持拦截器
- **Web 开发全家桶** — Web 启动器、JWT 认证（Access/Refresh 双令牌）、模型验证、接口限流、幂等回放、CorrelationId 链路追踪、JSON 自动时区转换、多主题
- **Redis 分布式组件** — 分布式锁（看门狗自动续期、Token 安全释放）、分布式幂等存储、分布式限流、调度任务互斥锁
- **Cron 定时任务** — 内置分钟级 Cron 调度器（自研 5 字段表达式解析），随宿主自动启停，可插拔分布式锁
- **弹性能力** — 重试（指数退避）+ 熔断 + 超时三合一，Provider 接口化设计，可无缝替换为 Polly 等第三方实现
- **RBAC 管理后台模块** — 管理员/角色组/菜单/权限/操作日志开箱即用的类库，注解式权限声明
- **开发工具链** — 代码生成器（数据库表 → Model/Dao/Service 三层代码）、数据库部署器（版本化 SQL 脚本增量部署）

## 框架组成

| 分组 | 项目 | 说明 |
| --- | --- | --- |
| 核心 | **Rampage** | 依赖注入、数据访问（ORM）、动态编译、HTTP 客户端、缓存、配置、验证、日志、本地化、限流、幂等、工具集 |
| Web | **Rampage.Web** | Web 启动器、JWT、过滤器与中间件、时区、主题、分页组件 |
| 绘图 | **Rampage.Drawing** | 基于 SkiaSharp 的跨平台验证码（10 种样式）、图片工具 |
| 数据库 | **Rampage.Dapper** | 基于 Dapper 的数据访问器 |
| 数据库 | **Rampage.DatabaseLibrary.MySql / SqlServer / PostgreSQL / Sqlite / Oracle** | 五大数据库 Provider |
| 分布式 | **Rampage.RedisLibrary** | Redis 助手、分布式锁、幂等存储、分布式限流、调度互斥 |
| 弹性 | **Rampage.Resilience** | 重试 + 熔断 + 超时 |
| 调度 | **Rampage.Scheduling** | Cron 定时任务调度器 |
| 管理 | **Rampage.Management.Core / Web** | RBAC 管理后台类库 |
| 工具 | **Rampage.CodeGenerator** | 三层代码生成器 |
| 工具 | **Rampage.DatabaseDeployer** | 数据库版本化部署器 |

## 快速开始

### 环境要求

- .NET 8.0 SDK 及以上
- 需要数据库时：MySQL / SQL Server / PostgreSQL / SQLite / Oracle 任一
- 需要分布式能力时：Redis

### 引用方式

克隆本仓库后，在解决方案中直接引用对应项目；或将各项目编译为 NuGet 包后引用。

```bash
git clone https://github.com/XTiger1986/Rampage.git
cd Rampage
dotnet build
```

### 启动一个 Web 应用

定义应用类并继承 `RampageWebApplication`，一行代码启动：

```csharp
// Program.cs
using Rampage;
using Rampage.Web;

RampageBooter.Run<MyApplication>(args);

public class MyApplication : RampageWebApplication
{
    public MyApplication() : base()
    {
        // 应用初始化完成事件，可在此追加自定义配置
        ApplicationInitialized += (app, provider) =>
        {
            Console.WriteLine("Rampage 应用已启动");
        };
    }
}
```

启动器将自动完成：程序集多线程扫描、服务注册、log4net 日志初始化、数据库客户端初始化、依赖装配环检测。

### 配置文件

配置位于 `config/app.json`，按环境自动加载 `app.Development.json` / `app.Production.json`：

```json
{
  "DatabaseFactory": {
    "Clients": [
      {
        "Name": "Main",
        "Type": "PostgreSQL",
        "ConnectionString": "Host=127.0.0.1;Port=5432;Database=demo;Username=rampage;Password=rampage;"
      }
    ]
  },
  "RedisFactory": {
    "Clients": [
      { "Name": "Main", "ConnectionString": "127.0.0.1:6379", "PrefixKey": "Demo" }
    ]
  },
  "SuperHttpClients": {
    "Clients": [
      { "Name": "UserApi", "Root": "http://localhost:5204/" }
    ]
  },
  "JwtAuthenticationConfig": {
    "Main": {
      "SecretKey": "<至少 64 字符的随机密钥>",
      "Issuer": "Demo.Api",
      "Audience": "Demo.Client",
      "ExpiredSeconds": 300,
      "RefreshExpiredSeconds": 86400
    }
  }
}
```

---

## 核心能力详解

### 1. 自动依赖注入

在类成员上标注特性即可声明依赖，框架启动时扫描全部程序集自动完成注册与注入：

```csharp
public class UserService : BaseService<User, UserDao>, IUserService
{
    [Auto]                          // Scoped 生命周期（默认）
    protected MemoryCacheManager Cache = default!;

    [Singleton]                     // Singleton 生命周期
    protected SomeWorker Worker = default!;

    [Auto]                          // Lazy<T> 延迟解析，打破服务间循环依赖
    protected Lazy<IOtherService> Other = default!;
}
```

- 服务类可标注 `[Service("服务名")]` 实现命名注册，通过服务定位器按名解析
- 两个服务禁止以立即 `[Auto]` 互相注入，启动期环检测会直接失败并给出提示
- `SingletonServiceLocator` / `ScopeServiceLocator` 提供静态服务定位

### 2. 数据访问（内置 ORM）

**实体映射**：

```csharp
[TableMapping("user")]
public class User
{
    [FieldMapping(PrimaryKey = true, AutoIncrement = true, ColumnName = "id")]
    public int Id { get; set; }

    [FieldMapping("username")]
    public string? Username { get; set; }

    [FieldMapping(EnumString = true)]       // 枚举以字符串存库（默认存整型）
    public CommonStatus Status { get; set; }

    [FieldMapping(NotColumn = true)]        // 非表字段，不参与 SQL 生成
    public string? DisplayName { get; set; }
}
```

**三层结构**（Entity → Dao → Service）：

```csharp
public interface UserDao : ISuperDao<User> { }

public class UserService : BaseService<User, UserDao>, IUserService { }
```

Dao 与 Service 均提供全量同构 API（`SelectOne` / `SelectList` / `SelectPage` / `SelectById(s)` / `Insert` / `Update` / `Delete` / `Count` / 批量 / 事务），并各有一套异步版本。

**查询的三种姿势**：

```csharp
// ① Lambda 表达式
List<User> users = dao.SelectList(u => u.Status == CommonStatus.Enabled);

// ② LambdaWrapper 动态拼接
LambdaWrapper<User> wrapper = new LambdaWrapper<User>();
if (!string.IsNullOrEmpty(keyword))
{
    wrapper.And(u => u.Username!.Contains(keyword));
}
wrapper.And(u => u.Status == CommonStatus.Enabled);
wrapper.OrderByDescending(u => u.Id);
users = dao.SelectList(wrapper);

// ③ DataFilter：HTTP 查询参数直接映射为 SQL 条件
[Filter]
public class UserFilter : DataFilter
{
    public CommonStatus? Status { get; set; }

    [FilterItem(Operator = FilterOperatorType.Like, FieldName = "username",
                OrFields = "true_name,mobile,email")]   // 关键字模糊匹配多个字段
    public string? Keywords { get; set; }

    public ValueScope<DateTime?>? CreateTime { get; set; }   // 区间条件
}
List<User> result = dao.SelectList(new FilterWrapper<User>(filter));
```

**分页与事务**：

```csharp
PagerInfo pager = new PagerInfo { PageIndex = 1, PageSize = 20 };
List<User> page = dao.SelectPage(pager, wrapper);
long total = pager.RecordCount!.Value;        // 分页查询后自动回填总数

dao.BeginTransaction();
try
{
    dao.Insert(order);
    dao.UpdateById(stock);
    dao.CommitTransaction();
}
catch
{
    dao.RollbackTransaction();
}
```

**多数据库**：在 `DatabaseFactory:Clients` 中配置多个客户端即可同时连接多个数据库（可跨数据库类型），Dao 按实体映射自动路由；连接字符串支持通过 `IConnectionStringDecoder` 自定义解密。

### 3. 声明式 HTTP 客户端

定义接口并标注请求特性，框架通过 Emit 动态生成异步代理：

```csharp
[SuperClient(Name = "UserApi", Handler = typeof(UserApiHandler))]   // Name 对应配置中的客户端
public interface IUserApi : ISuperHttpClient
{
    [SuperGet(Resource = "api/user/{id}")]
    Task<R<User>> GetUser([RequestPathParam] int id);

    [SuperGet(Resource = "api/user")]
    Task<R<List<User>>> QueryUsers([RequestParam] string keyword);

    [SuperPost(Resource = "api/user")]
    Task<R> CreateUser([RequestBody] CreateUserInput input);

    [SuperDelete(Resource = "api/user/{id}")]
    Task<R> DeleteUser([RequestHeader] int storeId, [RequestPathParam] int id);
}
```

- 参数绑定特性：`[RequestHeader]` `[RequestParam]` `[RequestPathParam]` `[RequestBody]` `[RequestCookie]` `[RequestFormFile]`
- 实现拦截器 `ISuperClientInterceptor` 统一处理鉴权、签名、日志等横切逻辑
- 接口方法须为 `Task<T>` 异步签名

### 4. Web 开发

- **统一返回**：`R` / `R<T>` 结果模型（Code / Message / Data / ValidationResult）
- **模型验证**：`[Valid]` 标记参数或属性，配合 20 余种验证特性（`Required` `EmailFormat` `MobileFormat` `PasswordFormat` `StringLength` `IntegerRange` `RegexFormat` 等），验证失败自动返回结构化错误
- **接口限流**：注册 `services.AddRampageRateLimitFilter()` 后按特性标记

```csharp
[RateLimit("login", 5, 60)]      // 60 秒窗口内每 IP 最多 5 次
public R Login(LoginInput input) { ... }
```

- **接口幂等**：携带 `Idempotency-Key` 请求头的写接口，首次成功响应被缓存，重复请求原样回放
- **JWT 认证**：`JwtTokenManager` 签发与校验 Access / Refresh 双令牌，支持多客户端独立配置
- **自动时区**：`[AutoTimeZone]` 标记 DTO，JSON 序列化时按访问者时区自动转换时间展示
- **链路追踪**：`CorrelationIdMiddleware` 为每个请求生成/透传追踪 ID
- **控制器基类**：`SuperController` / `SuperApiController` 内置服务定位、本地化、请求级缓存

### 5. Redis 分布式组件

```csharp
RedisHelper redis = new RedisHelper();          // String/Hash/List/Set/ZSet 常规操作，自动 Key 前缀

// 分布式锁：获取失败返回 null；看门狗自动续期，Dispose 按 Token 安全释放
var locker = RedisDistributedLock.TryAcquire(redis, "order:sync", TimeSpan.FromSeconds(30),
                                            retryCount: 2, retryDelay: TimeSpan.FromMilliseconds(100));
if (locker != null)
{
    try { /* 临界区逻辑 */ }
    finally { locker.Dispose(); }
}
```

- `RedisFactory` 支持多 Redis 客户端与 Key 前缀隔离
- `RedisIdempotencyStore` / `RedisRateLimiter` 为幂等与限流提供分布式实现
- 连接字符串支持 `IRedisDecoder` 自定义解密

### 6. Cron 定时任务

```csharp
// 注册（随宿主自动启停）
services.AddRampageScheduler();

// 声明任务：标准 5 字段 Cron（分 时 日 月 周），同一任务上一次未跑完时自动跳过本次触发
scheduler.Register("daily-report", "0 3 * * *", async ct => await BuildReportAsync(ct));
scheduler.Register("cache-warmup", "*/30 * * * *", WarmUpCache, runImmediately: true);

// 查看任务运行快照（名称 / Cron / 上次执行 / 下次触发 / 最近错误）
var jobs = scheduler.GetJobs();
```

多实例部署时接入 Redis 分布式锁，保证同一任务同一时刻仅在一个实例执行：

```csharp
services.AddRampageScheduler();
services.AddRampageRedisJobLock();     // 替换默认进程内锁
```

### 7. 弹性（重试 / 熔断 / 超时）

```csharp
services.AddRampageResilience();

var result = await resilience.ExecuteAsync(
    "query-user",                                              // 操作键，熔断器按此隔离
    new ResiliencePolicy
    {
        MaxRetries = 3,
        RetryBaseDelay = TimeSpan.FromMilliseconds(100),       // 默认指数退避
        BreakerFailureThreshold = 20,                          // 连续失败 20 次熔断 30 秒
        Timeout = TimeSpan.FromSeconds(3)
    },
    async ct => await QueryUserAsync(ct));
```

实现 `IResilienceProvider` 即可整体替换为 Polly 等第三方实现，业务代码零改动。

### 8. 常用工具集

| 工具 | 能力 |
| --- | --- |
| `SecurityTool` | MD5 / SHA1 / SHA256 / HMAC-SHA256 / PBKDF2 口令哈希与校验 / DES / AES（CBC） |
| `SnowflakeTool` | 雪花 ID（可配置 WorkerId，支持字符串形式） |
| `MapperTool` | 同名属性对象映射 |
| `CsvTool` / `ZipTool` | CSV 读写、ZIP 压缩解压 |
| `SensitiveWordFilter` | 敏感词过滤 |
| `VerificationCodeManager` | 10 种样式图形验证码（SkiaSharp 跨平台） |
| `StringTool` / `DateTimeTool` / `MathTool` / `ByteTool` / `FileTool` / `PathTool` / `IdTool` | 常用基础工具 |

另有基于 log4net 的结构化日志（多环境配置）、JSON 多语言本地化模块、内存缓存管理等基础组件。

### 9. RBAC 管理后台模块

`Rampage.Management.Core` + `Rampage.Management.Web` 提供开箱即用的后台管理类库：

- 管理员、角色组、管理菜单、权限点、操作日志、登录日志完整数据模型
- 注解式权限声明（`PermissionAction` / `PermissionModule` / `PermissionResource`），从代码自动加载权限清单
- Passport 登录（JWT）、个人中心、后台 API 与 MVC 控制器基类（`SuperManagementController` / `SuperManagementApiController`）
- 自带各数据库建表脚本，配合 DatabaseDeployer 一键初始化

### 10. 开发工具链

**代码生成器（Rampage.CodeGenerator）** — 连接数据库后按模板批量生成三层代码，模板可自定义：

```json
{
  "ConnectionSetting": [
    {
      "Name": "Demo",
      "DatabaseLibrary": "Rampage.DatabaseLibrary.PostgreSQL",
      "Provider": "PgSqlClient",
      "ConnectionString": "Host=127.0.0.1;Port=5432;Database=demo;Username=rampage;Password=rampage;",
      "GenerateTables": "*"
    }
  ],
  "GenerateSetting": { "TemplateRoot": "Templates", "OutputRoot": "Outputs" }
}
```

**数据库部署器（Rampage.DatabaseDeployer）** — 将 `SqlScripts` 目录下带版本号的 SQL 脚本按顺序增量执行，支持多库部署：

```json
{ "ScriptSetting": { "Root": "SqlScripts", "VersionPattern": "^(?<Version>\\d+)[^0-9]+?.+?\\.sql" } }
```

---

## 数据库支持

| 数据库 | 驱动 |
| --- | --- |
| MySQL / MariaDB | MySqlConnector |
| SQL Server | Microsoft.Data.SqlClient |
| PostgreSQL | Npgsql |
| SQLite | Microsoft.Data.Sqlite |
| Oracle | Oracle.ManagedDataAccess.Core |

## 解决方案结构

```
Rampage
├── Rampage/                          核心库
│   ├── Caching/                      内存缓存
│   ├── Configuration/                配置管理（SuperConfig）
│   ├── Data/                         数据访问层（ORM）
│   ├── DependencyInjection/          自动依赖注入
│   ├── Dynamic/                      Emit 动态构建（Dao/HttpClient）
│   ├── Enums/                        枚举与枚举读取器
│   ├── Hierarchy/                    Service/Dao 基类
│   ├── Idempotency/                  幂等抽象
│   ├── Localization/                 多语言本地化
│   ├── Log/                          log4net 日志
│   ├── Mapping/                      对象映射
│   ├── Model/                        R / PageListData 等通用模型
│   ├── Net/                          声明式 HTTP 客户端
│   ├── RateLimit/                    限流抽象
│   ├── Reflection/                   反射加速（EmitTool）
│   ├── Security/                     登录尝试追踪
│   ├── Threading/                    多线程工作器
│   ├── Utilities/                    工具集
│   └── Validation/                   数据验证
├── Rampage.Web/                      Web 库（JWT/中间件/时区/主题）
├── Rampage.Drawing/                  验证码与绘图
├── DatabaseLibrary/                  Dapper + 五大数据库 Provider
├── Distributed/Rampage.RedisLibrary/ Redis 分布式组件
├── Resilience/Rampage.Resilience/    重试/熔断/超时
├── Scheduling/Rampage.Scheduling/    Cron 调度
├── Management/                       RBAC 管理后台类库
└── Tools/                            代码生成器、数据库部署器
```

## 许可协议

本项目基于 [MIT License](LICENSE) 开源。

## 交流反馈

- 个人网站：[http://www.1986z.com/](http://www.1986z.com/)
- GitHub：[@XTiger1986](https://github.com/XTiger1986)
- 欢迎通过 [Issue](https://github.com/XTiger1986/Rampage/issues) 提交问题与建议
