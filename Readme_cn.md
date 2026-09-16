# Rampage

![.NET](https://img.shields.io/badge/.NET-8.0%2B-512BD4) ![C#](https://img.shields.io/badge/C%23-12-239120) ![Platform](https://img.shields.io/badge/Platform-Windows%20%7C%20Linux%20%7C%20macOS-6C7A89) ![License](https://img.shields.io/badge/License-MIT-yellow)

**English | [简体中文](Readme.md)**

Rampage is a personally developed, open-source, full-stack general-purpose .NET framework (.NET 8+). Built around the principles of "attribute-driven, convention over configuration", its bootstrapper (`RampageBooter`) combined with Emit dynamic compilation automatically performs service scanning, dependency wiring and database client initialization at startup, letting you build a complete application from the data layer to the web layer with minimal boilerplate.

The framework covers the full chain of enterprise development: **automatic dependency injection, a lightweight ORM (multi-database), declarative HTTP clients, JWT authentication, rate limiting, idempotency, multi-timezone support, Redis distributed components, cron scheduling, resilience (retry/circuit breaker/timeout), an RBAC admin module, and CAPTCHA generation**, plus two development tools: a **code generator** and a **database deployer**.

---

## Key Features

- **Automatic Dependency Injection** — mark members with `[Auto]` / `[Singleton]` and registration/injection just happens; no manual `AddScoped`. Supports `Lazy<T>` for deferred resolution to break dependency cycles, with cycle detection at startup
- **Lightweight ORM Data Layer** — `[TableMapping]` / `[FieldMapping]` entity mapping; three query styles (Lambda expressions, `LambdaWrapper`, `DataFilter` which turns HTTP query parameters straight into SQL conditions); full CRUD, paging, batching and transactions in both sync and async APIs
- **Multi-Database Support** — MySQL, SQL Server, PostgreSQL, SQLite and Oracle; switch via configuration, multiple databases coexist
- **Declarative HTTP Client** — define an interface, annotate with `[SuperClient]` / `[SuperGet]` / `[SuperPost]` etc., and the framework emits an async proxy; interceptors supported
- **Full Web Toolkit** — web bootstrapper, JWT authentication (access/refresh dual tokens), model validation, rate limiting, idempotent replay, CorrelationId tracing, automatic timezone conversion in JSON, multi-theme
- **Redis Distributed Components** — distributed lock (watchdog auto-renewal, token-safe release), distributed idempotency store, distributed rate limiter, scheduler mutual-exclusion lock
- **Cron Scheduling** — built-in minute-level cron scheduler (self-implemented 5-field expression parser), starts/stops with the host, pluggable distributed lock
- **Resilience** — retry (exponential backoff) + circuit breaker + timeout in one; provider-based design, swappable for Polly or any third-party implementation
- **RBAC Admin Module** — administrators, groups, menus, permissions and audit logs as ready-to-use libraries, with annotation-driven permission declaration
- **Developer Toolchain** — code generator (database tables → Model/Dao/Service three-tier code) and database deployer (versioned incremental SQL scripts)

## Solution Layout

| Group | Project | Description |
| --- | --- | --- |
| Core | **Rampage** | DI, data access (ORM), dynamic compilation, HTTP client, caching, configuration, validation, logging, localization, rate limiting, idempotency, utilities |
| Web | **Rampage.Web** | Web bootstrapper, JWT, filters & middleware, timezone, themes, paging components |
| Drawing | **Rampage.Drawing** | Cross-platform CAPTCHA via SkiaSharp (10 styles) and image tools |
| Database | **Rampage.Dapper** | Dapper-based data accessor |
| Database | **Rampage.DatabaseLibrary.MySql / SqlServer / PostgreSQL / Sqlite / Oracle** | Five database providers |
| Distributed | **Rampage.RedisLibrary** | Redis helper, distributed lock, idempotency store, rate limiter, scheduler lock |
| Resilience | **Rampage.Resilience** | Retry + circuit breaker + timeout |
| Scheduling | **Rampage.Scheduling** | Cron job scheduler |
| Management | **Rampage.Management.Core / Web** | RBAC admin module libraries |
| Tools | **Rampage.CodeGenerator** | Three-tier code generator |
| Tools | **Rampage.DatabaseDeployer** | Versioned database deployer |

## Getting Started

### Prerequisites

- .NET 8.0 SDK or later
- For database access: any of MySQL / SQL Server / PostgreSQL / SQLite / Oracle
- For distributed capabilities: Redis

### Referencing

Clone this repository and reference the projects directly, or pack them as NuGet packages.

```bash
git clone https://github.com/XTiger1986/Rampage.git
cd Rampage
dotnet build
```

### Bootstrapping a Web Application

Define an application class inheriting `RampageWebApplication` and start with a single line:

```csharp
// Program.cs
using Rampage;
using Rampage.Web;

RampageBooter.Run<MyApplication>(args);

public class MyApplication : RampageWebApplication
{
    public MyApplication() : base()
    {
        // Fired when the application has been fully initialized
        ApplicationInitialized += (app, provider) =>
        {
            Console.WriteLine("Rampage application started");
        };
    }
}
```

The bootstrapper automatically performs: multi-threaded assembly scanning, service registration, log4net initialization, database client initialization and dependency cycle detection.

### Configuration

Configuration lives in `config/app.json`, with `app.Development.json` / `app.Production.json` loaded per environment:

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
      "SecretKey": "<random key of at least 64 characters>",
      "Issuer": "Demo.Api",
      "Audience": "Demo.Client",
      "ExpiredSeconds": 300,
      "RefreshExpiredSeconds": 86400
    }
  }
}
```

---

## Capability Tour

### 1. Automatic Dependency Injection

Annotate members to declare dependencies; the framework scans all assemblies at startup and wires everything automatically:

```csharp
public class UserService : BaseService<User, UserDao>, IUserService
{
    [Auto]                          // Scoped lifetime (default)
    protected MemoryCacheManager Cache = default!;

    [Singleton]                     // Singleton lifetime
    protected SomeWorker Worker = default!;

    [Auto]                          // Lazy<T> deferred resolution breaks circular dependencies
    protected Lazy<IOtherService> Other = default!;
}
```

- Service classes can be marked `[Service("name")]` for named registration and resolved by name via the service locators
- Two services must not inject each other with eager `[Auto]`; startup cycle detection fails fast with a clear message
- `SingletonServiceLocator` / `ScopeServiceLocator` provide static service location

### 2. Data Access (Built-in ORM)

**Entity mapping**:

```csharp
[TableMapping("user")]
public class User
{
    [FieldMapping(PrimaryKey = true, AutoIncrement = true, ColumnName = "id")]
    public int Id { get; set; }

    [FieldMapping("username")]
    public string? Username { get; set; }

    [FieldMapping(EnumString = true)]       // store enum as string (integer by default)
    public CommonStatus Status { get; set; }

    [FieldMapping(NotColumn = true)]        // not a table column; excluded from SQL generation
    public string? DisplayName { get; set; }
}
```

**Three-tier structure** (Entity → Dao → Service):

```csharp
public interface UserDao : ISuperDao<User> { }

public class UserService : BaseService<User, UserDao>, IUserService { }
```

Dao and Service expose the same full API surface (`SelectOne` / `SelectList` / `SelectPage` / `SelectById(s)` / `Insert` / `Update` / `Delete` / `Count` / batch operations / transactions), each with an async twin.

**Three ways to query**:

```csharp
// ① Lambda expression
List<User> users = dao.SelectList(u => u.Status == CommonStatus.Enabled);

// ② LambdaWrapper for dynamic composition
LambdaWrapper<User> wrapper = new LambdaWrapper<User>();
if (!string.IsNullOrEmpty(keyword))
{
    wrapper.And(u => u.Username!.Contains(keyword));
}
wrapper.And(u => u.Status == CommonStatus.Enabled);
wrapper.OrderByDescending(u => u.Id);
users = dao.SelectList(wrapper);

// ③ DataFilter: map HTTP query parameters straight into SQL conditions
[Filter]
public class UserFilter : DataFilter
{
    public CommonStatus? Status { get; set; }

    [FilterItem(Operator = FilterOperatorType.Like, FieldName = "username",
                OrFields = "true_name,mobile,email")]   // fuzzy-match across multiple fields
    public string? Keywords { get; set; }

    public ValueScope<DateTime?>? CreateTime { get; set; }   // range condition
}
List<User> result = dao.SelectList(new FilterWrapper<User>(filter));
```

**Paging and transactions**:

```csharp
PagerInfo pager = new PagerInfo { PageIndex = 1, PageSize = 20 };
List<User> page = dao.SelectPage(pager, wrapper);
long total = pager.RecordCount!.Value;        // automatically filled after the paged query

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

**Multiple databases**: configure several clients under `DatabaseFactory:Clients` to connect to multiple databases at once (even of different kinds); Dao instances route automatically by entity mapping. Connection strings can be decrypted via a custom `IConnectionStringDecoder`.

### 3. Declarative HTTP Client

Define an interface with request attributes; the framework emits an async proxy via Emit:

```csharp
[SuperClient(Name = "UserApi", Handler = typeof(UserApiHandler))]   // Name maps to a configured client
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

- Parameter binding attributes: `[RequestHeader]` `[RequestParam]` `[RequestPathParam]` `[RequestBody]` `[RequestCookie]` `[RequestFormFile]`
- Implement `ISuperClientInterceptor` to handle cross-cutting concerns such as authentication, signing and logging
- Interface methods must have `Task<T>` async signatures

### 4. Web Development

- **Uniform result**: `R` / `R<T>` result model (Code / Message / Data / ValidationResult)
- **Model validation**: mark parameters or properties with `[Valid]`, combined with 20+ validation attributes (`Required` `EmailFormat` `MobileFormat` `PasswordFormat` `StringLength` `IntegerRange` `RegexFormat`, etc.); failures return structured errors automatically
- **Rate limiting**: register `services.AddRampageRateLimitFilter()` and annotate actions

```csharp
[RateLimit("login", 5, 60)]      // max 5 hits per IP within a 60s window
public R Login(LoginInput input) { ... }
```

- **Idempotency**: write endpoints carrying an `Idempotency-Key` header have their first successful response cached and replayed verbatim on retries
- **JWT authentication**: `JwtTokenManager` issues and validates access/refresh token pairs, with independent configuration per client
- **Automatic timezones**: annotate DTOs with `[AutoTimeZone]` and JSON serialization converts timestamps to the visitor's timezone automatically
- **Tracing**: `CorrelationIdMiddleware` generates/propagates a correlation ID for every request
- **Controller bases**: `SuperController` / `SuperApiController` ship with service location, localization and per-request caching

### 5. Redis Distributed Components

```csharp
RedisHelper redis = new RedisHelper();          // String/Hash/List/Set/ZSet operations with automatic key prefix

// Distributed lock: returns null when not acquired; watchdog auto-renews; Dispose releases safely by token
var locker = RedisDistributedLock.TryAcquire(redis, "order:sync", TimeSpan.FromSeconds(30),
                                            retryCount: 2, retryDelay: TimeSpan.FromMilliseconds(100));
if (locker != null)
{
    try { /* critical section */ }
    finally { locker.Dispose(); }
}
```

- `RedisFactory` supports multiple Redis clients with key-prefix isolation
- `RedisIdempotencyStore` / `RedisRateLimiter` provide distributed implementations for idempotency and rate limiting
- Connection strings can be decrypted via a custom `IRedisDecoder`

### 6. Cron Scheduling

```csharp
// Register (starts and stops with the host)
services.AddRampageScheduler();

// Declare jobs: standard 5-field cron (minute hour day month weekday); overlapping runs of the same job are skipped
scheduler.Register("daily-report", "0 3 * * *", async ct => await BuildReportAsync(ct));
scheduler.Register("cache-warmup", "*/30 * * * *", WarmUpCache, runImmediately: true);

// Inspect job snapshots (name / cron / last run / next trigger / last error)
var jobs = scheduler.GetJobs();
```

For multi-instance deployments, plug in the Redis distributed lock so each job runs on exactly one instance at a time:

```csharp
services.AddRampageScheduler();
services.AddRampageRedisJobLock();     // replaces the default in-process lock
```

### 7. Resilience (Retry / Circuit Breaker / Timeout)

```csharp
services.AddRampageResilience();

var result = await resilience.ExecuteAsync(
    "query-user",                                              // operation key; breakers are isolated by it
    new ResiliencePolicy
    {
        MaxRetries = 3,
        RetryBaseDelay = TimeSpan.FromMilliseconds(100),       // exponential backoff by default
        BreakerFailureThreshold = 20,                          // open the breaker after 20 consecutive failures for 30s
        Timeout = TimeSpan.FromSeconds(3)
    },
    async ct => await QueryUserAsync(ct));
```

Implement `IResilienceProvider` to swap the whole thing for Polly or another library — business code stays untouched.

### 8. Utility Toolbox

| Tool | Capabilities |
| --- | --- |
| `SecurityTool` | MD5 / SHA1 / SHA256 / HMAC-SHA256 / PBKDF2 password hashing & verification / DES / AES (CBC) |
| `SnowflakeTool` | Snowflake IDs (configurable WorkerId, string form supported) |
| `MapperTool` | Same-name property object mapping |
| `CsvTool` / `ZipTool` | CSV read/write, ZIP compress/decompress |
| `SensitiveWordFilter` | Sensitive-word filtering |
| `VerificationCodeManager` | 10 styles of image CAPTCHA (cross-platform via SkiaSharp) |
| `StringTool` / `DateTimeTool` / `MathTool` / `ByteTool` / `FileTool` / `PathTool` / `IdTool` | Common everyday utilities |

Also included: structured logging on log4net (per-environment configuration), JSON-based multi-language localization, in-memory cache management and other foundational components.

### 9. RBAC Admin Module

`Rampage.Management.Core` + `Rampage.Management.Web` provide ready-to-use admin libraries:

- Complete data model for administrators, groups, admin menus, permission points, operation logs and login logs
- Annotation-driven permission declaration (`PermissionAction` / `PermissionModule` / `PermissionResource`); the permission manifest is loaded straight from code
- Passport login (JWT), personal center, admin API and MVC controller bases (`SuperManagementController` / `SuperManagementApiController`)
- Ships with table-creation scripts for each database; initialize in one shot together with the DatabaseDeployer

### 10. Developer Toolchain

**Code Generator (Rampage.CodeGenerator)** — connects to a database and generates three-tier code from customizable templates:

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

**Database Deployer (Rampage.DatabaseDeployer)** — executes version-numbered SQL scripts under `SqlScripts` incrementally in order, with multi-database deployment support:

```json
{ "ScriptSetting": { "Root": "SqlScripts", "VersionPattern": "^(?<Version>\\d+)[^0-9]+?.+?\\.sql" } }
```

---

## Database Support

| Database | Driver |
| --- | --- |
| MySQL / MariaDB | MySqlConnector |
| SQL Server | Microsoft.Data.SqlClient |
| PostgreSQL | Npgsql |
| SQLite | Microsoft.Data.Sqlite |
| Oracle | Oracle.ManagedDataAccess.Core |

## Repository Structure

```
Rampage
├── Rampage/                          Core library
│   ├── Caching/                      In-memory cache
│   ├── Configuration/                Configuration management (SuperConfig)
│   ├── Data/                         Data access layer (ORM)
│   ├── DependencyInjection/          Automatic dependency injection
│   ├── Dynamic/                      Emit builders (Dao/HttpClient)
│   ├── Enums/                        Enums and enum reader
│   ├── Hierarchy/                    Service/Dao base classes
│   ├── Idempotency/                  Idempotency abstractions
│   ├── Localization/                 Multi-language localization
│   ├── Log/                          log4net logging
│   ├── Mapping/                      Object mapping
│   ├── Model/                        Common models (R / PageListData, etc.)
│   ├── Net/                          Declarative HTTP client
│   ├── RateLimit/                    Rate limiting abstractions
│   ├── Reflection/                   Reflection acceleration (EmitTool)
│   ├── Security/                     Login attempt tracking
│   ├── Threading/                    Threading worker
│   ├── Utilities/                    Utilities
│   └── Validation/                   Data validation
├── Rampage.Web/                      Web library (JWT/middleware/timezone/themes)
├── Rampage.Drawing/                  CAPTCHA and drawing
├── DatabaseLibrary/                  Dapper + five database providers
├── Distributed/Rampage.RedisLibrary/ Redis distributed components
├── Resilience/Rampage.Resilience/    Retry/breaker/timeout
├── Scheduling/Rampage.Scheduling/    Cron scheduling
├── Management/                       RBAC admin libraries
└── Tools/                            Code generator, database deployer
```

## License

This project is open sourced under the [MIT License](LICENSE).

## Feedback

- Website: [http://www.1986z.com/](http://www.1986z.com/)
- GitHub: [@XTiger1986](https://github.com/XTiger1986)
- Issues and suggestions are welcome via the [issue tracker](https://github.com/XTiger1986/Rampage/issues)
