# ActDim.Observability

A lightweight, OpenTelemetry-centric observability library for .NET applications built on top of `Microsoft.Extensions.Logging` and `System.Diagnostics.Activity`.

## Features

- **Zero-Ceremony Developer API:** Developers write standard `ILogger` calls and `logger.BeginScope()` without needing custom logger interfaces.
- **DI Decorator (`EventObservabilityLoggerFactory`):** Transparently decorates `ILoggerFactory` via DI container to inject `EventObservabilityBridge` for enriching logs and traces.
- **Activity & OpenTelemetry Enrichment:** Automatically transforms scope objects, DTOs, and structured log parameters into flattened, dotted OpenTelemetry attributes (`user.id`, `order.price`).
- **Auto Activity Creation on Scope:** Automatically starts an `Activity` span on `logger.BeginScope()` when no ambient span exists (`Activity.Current == null`), resolved via `observability.PushActivitySourceName(...)` or `EventObservabilityOptions.DefaultActivitySourceName`.
- **Ambient Context Separation:** `IAmbientContext` serves as a neutral ambient variable store. Only properties explicitly pushed via `IObservabilityContext` are exported to `Activity` tags.
- **Operation Status & Progress Tracking:** First-class support for setting unified operation status, icons, progress percentage, and step indices (`observability.SetStatus("Downloading Dataset", progress: 45.5, icon: "🚀", step: 1, totalSteps: 3)`), readable via `observability.Status`.
- **Selective Provider & Scope Suppression:** Dynamically suppress console loggers, specific logger providers, or external scope export per async flow (`observability.SuppressConsole()`, `observability.SuppressProviders("File")`, `observability.SuppressExternalScopes()`).
- **Provider Alias Resolution:** Automatically resolves provider aliases via official .NET `[ProviderAlias]` attributes or custom provider mappings.
## Architectural Rationale: Dedicated Observability Engines vs Relational Databases

Storing high-throughput logs and distributed traces in traditional relational databases (like PostgreSQL or MySQL) creates significant operational and performance bottlenecks. Dedicated observability engines solve these problems through specialized architectures.

### Key Bottlenecks of Relational Databases for Telemetry

* **I/O & WAL Overhead:** Transactional databases prioritize strict ACID guarantees. Every log entry generates Write-Ahead Log (WAL) traffic and buffer churn, causing massive disk I/O overhead that degrades core application performance.
* **Storage Inefficiency & Bloat:** Row-oriented architectures compress telemetry poorly. Rotating old data via deletes or TTL triggers heavy background cleanup processes (like `VACUUM`), causing table bloat and CPU spikes.
* **JSON Indexing Trade-Off:** Querying dynamic JSON attributes requires heavy indexing (such as GIN indexes), which cripples write speeds and increases index size beyond the data itself. Without indexes, queries result in slow full-table scans.
* **Lack of Observability Tooling:** Relational databases lack native primitives for live tailing, distributed trace waterfalls (spans/DAGs), and log-centric aggregate pipelines.

---

### Core Advantages of Dedicated Observability Engines

* **Columnar & Append-Only Storage:** Uses efficient storage engines (e.g., Apache Parquet, LSM-trees) tailored for time-series and log data, achieving 10-15x higher compression ratios.
* **Telemetry-First Query Languages:** Purpose-built query languages (like LogsQL or telemetry-aware SQL dialects) parse, extract, and filter arbitrary JSON fields on the fly without heavy index maintenance.
* **Built-in APM Visualizations:** Native support for end-to-end trace waterfalls, span trees, and real-time log streaming right out of the box.

---

### Recommended Lightweight Open-Source Solutions

* **.NET Aspire Dashboard:** Microsoft's official, open-source telemetry visualization dashboard (`mcr.microsoft.com/dotnet/aspire-dashboard`). It natively ingests logs, traces, and metrics directly via standard OpenTelemetry OTLP (`http://localhost:4317` / `http://localhost:4318`) with **zero third-party logger providers or custom adapters required**. Aspire offers real-time trace waterfalls, structured log exploration, resource graphs, and metric charts out of the box.
* **VictoriaLogs:** A high-performance, resource-efficient log engine requiring minimal CPU and RAM (~50-100 MB). It eliminates high-cardinality bottlenecks, indexes all fields automatically, and features the expressive `LogsQL` language for structured JSON analysis.
* **OpenObserve:** A single Rust binary that covers logs, traces, and metrics out of the box. It uses Apache Parquet for storage, natively accepts OpenTelemetry (OTLP) data, and provides a full-featured web UI with trace waterfalls, log exploration, and dashboards without requiring Docker, Java, or external databases.
* **Seq (Datalust):** A developer-friendly, .NET-native observability server designed specifically for structured logs, distributed traces, and metrics. Seq offers a free single-user license for local development (`docker run -d -p 5341:80 -e ACCEPT_EULA=Y datalust/seq`), zero-setup OTLP ingestion out of the box (`http://localhost:5341/ingest/otlp`), and features an intuitive real-time Web UI with instant signals, log tailing, and trace waterfall views.
* **ClickHouse:** An industry-standard, ultra-high-performance columnar analytical database engine for high-volume logs, metrics, and trace telemetry. While we do not maintain a dedicated integration test suite for ClickHouse in this repository, modern distributions and telemetry stacks bundle or integrate with **HyperDX** (an open-source APM & log exploration Web UI), providing a comprehensive out-of-the-box user experience for analyzing traces and logs.

---

## .NET Observability & Logging Architecture: Best Practices Guide

Understanding how modern telemetry works in .NET is essential for building resilient, high-performance applications. Modern observability rests on **Three Pillars**: Traces, Metrics, and Logs.

```mermaid
flowchart TD
    subgraph App[".NET Application (ActDim.Observability)"]
        Logs["Logs (ILogger / EventObservabilityBridge)"]
        Traces["Traces (System.Diagnostics.Activity)"]
        Metrics["Metrics (System.Diagnostics.Metrics)"]
    end

    subgraph Instrumentations["Built-in & BCL Instrumentations"]
        ASPNET["ASP.NET Core (HTTP Requests)"]
        Kestrel["Kestrel & Hosting (Server)"]
        HTTP["HttpClient & System.Net (DNS / HTTP)"]
        EF["EF Core & SqlClient (DB Queries)"]
        Runtime["System.Runtime (GC / ThreadPool / CPU)"]
    end

    subgraph Collection["Export Architecture & Pipeline"]
        Direct["Direct OTLP Exporter (App -> Sink)"]
        Collector["OpenTelemetry Collector (Tail Sampling & Multi-Sink Routing)"]
    end

    subgraph Sinks["Observability Backends & Visualizers"]
        Aspire[".NET Aspire Dashboard (Native OTLP Logs + Traces + Metrics UI)"]
        VL["VictoriaLogs (High-Perf Log Engine / LogsQL)"]
        OO["OpenObserve (All-in-One: Logs + Traces + Metrics UI)"]
        Seq["Seq (Datalust: .NET-Native Logs + Traces + Metrics UI)"]
        Grafana["Grafana Dashboards (Loki/VL Logs, Tempo Traces, Prom Metrics)"]
    end

    Instrumentations --> Traces
    Instrumentations --> Metrics

    Logs --> Direct
    Traces --> Direct
    Metrics --> Direct

    Logs --> Collector
    Traces --> Collector
    Metrics --> Collector

    Direct --> Aspire
    Direct --> VL
    Direct --> OO
    Direct --> Seq
    Collector --> Aspire
    Collector --> VL
    Collector --> OO
    Collector --> Seq
    Collector --> Grafana
```

---

### 1. Complete .NET Setup Code (Logging, Tracing & Metrics)

Below is the standard, production-ready configuration using `Microsoft.Extensions.DependencyInjection` and `OpenTelemetry`:

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using ActDim.Observability;

var builder = WebApplication.CreateBuilder(args);

// 1. Define Resource attributes (Service Name, Version, Environment)
var resourceBuilder = ResourceBuilder.CreateDefault()
    .AddService(
        serviceName: builder.Configuration["Telemetry:ServiceName"] ?? "PractixService",
        serviceVersion: "1.0.0",
        serviceInstanceId: Environment.MachineName);

// 2. Configure Dynamic Trace Sampling from appsettings.json
var samplingRatio = builder.Configuration.GetValue<double>("Telemetry:TraceSamplingRatio", 0.1); // Default 10%
var enableSampling = builder.Configuration.GetValue<bool>("Telemetry:EnableSampling", true);

// During active incidents, set Telemetry:EnableSampling = false in appsettings.json to capture 100% of traces!
Sampler sampler = enableSampling
    ? new ParentBasedSampler(new TraceIdRatioBasedSampler(samplingRatio))
    : new AlwaysOnSampler();

// 3. Register OpenTelemetry Tracing & Metrics
builder.Services.AddOpenTelemetry()
    .WithResource(resourceBuilder)
    .WithTracing(tracing =>
    {
        tracing
            .SetSampler(sampler)
            .AddSource(EventObservabilityOptions.DefaultActivitySourceName)
            .AddAspNetCoreInstrumentation(opts =>
            {
                opts.RecordException = true;
            })
            .AddHttpClientInstrumentation(opts =>
            {
                opts.RecordException = true;
            })
            .AddEntityFrameworkCoreInstrumentation(opts =>
            {
                opts.SetDbStatementForText = true;
            })
            .AddNpgsql() // Npgsql.OpenTelemetry for PostgreSQL diagnostics & connection pooling
            .AddOtlpExporter(opts =>
            {
                opts.Endpoint = new Uri(builder.Configuration["Telemetry:OtlpEndpoint"] ?? "http://localhost:4317");
                opts.Protocol = OtlpExportProtocol.Grpc;
            });
    })
    .WithMetrics(metrics =>
    {
        metrics
            // Enable Exemplars (attaches active TraceId/SpanId to Metric Histograms)
            .SetExemplarFilter(ExemplarFilterType.TraceBased)
            // System & Framework Meters
            .AddMeter("Microsoft.AspNetCore.Hosting")
            .AddMeter("Microsoft.AspNetCore.Server.Kestrel")
            .AddMeter("System.Net.Http")
            .AddMeter("System.Net.NameResolution") // DNS resolution timing & metrics
            .AddMeter("System.Runtime")             // GC, ThreadPool, Locks, CPU
            .AddRuntimeInstrumentation()
            .AddOtlpExporter(opts =>
            {
                opts.Endpoint = new Uri(builder.Configuration["Telemetry:OtlpEndpoint"] ?? "http://localhost:4317");
                opts.Protocol = OtlpExportProtocol.Grpc;
            });
    });

// 4. Register ActDim.Observability & EventObservabilityBridge
builder.Services.AddEventObservability(logging =>
{
    logging.AddConsole();
    logging.AddOtlpExporter(opts =>
    {
        opts.Endpoint = new Uri(builder.Configuration["Telemetry:OtlpEndpoint"] ?? "http://localhost:4318/v1/logs");
        opts.Protocol = OtlpExportProtocol.HttpProtobuf;
    });
}, options =>
{
    options.IncludeExternalScopes = true;
});

var app = builder.Build();
app.Run();
```

---

### 2. Built-in Framework & System Instrumentations

OpenTelemetry .NET leverages BCL `ActivitySource` and `Meter` events built into the .NET runtime and ASP.NET Core:

| Component | Package / API | Emitted Telemetry & Metrics |
| :--- | :--- | :--- |
| **ASP.NET Core** | `OpenTelemetry.Instrumentation.AspNetCore` | Server HTTP spans (`http.request.method`, `http.response.status_code`, `url.path`, route templates). |
| **Kestrel Server** | `AddMeter("Microsoft.AspNetCore.Server.Kestrel")` | Active connections, connection duration, TLS handshakes, request queue length. |
| **Hosting** | `AddMeter("Microsoft.AspNetCore.Hosting")` | Request rate, active requests, unhandled exception counters. |
| **HttpClient & System.Net** | `AddMeter("System.Net.Http")`, `AddMeter("System.Net.NameResolution")` | Client HTTP spans, DNS lookup duration, socket connection timing, connection pool saturation. |
| **Entity Framework Core / SQL** | `OpenTelemetry.Instrumentation.EntityFrameworkCore` | DB command spans (`db.system`, `db.statement`), connection open/close duration. |
| **System.Runtime** | `AddRuntimeInstrumentation()`, `AddMeter("System.Runtime")` | GC heap allocation rate, GC pauses, ThreadPool queue length, thread count, CPU % and memory working set. |

---

### 3. Recommended Database & Infrastructure Instrumentations (`Npgsql.OpenTelemetry`)

In addition to standard BCL and ASP.NET Core instrumentations, production .NET applications interacting with databases, caches, and message brokers **MUST** configure driver-specific OpenTelemetry packages for full diagnostic visibility.

#### 3.1 Npgsql OpenTelemetry (`Npgsql.OpenTelemetry`): PostgreSQL Diagnostics

[`Npgsql.OpenTelemetry`](https://www.nuget.org/packages/Npgsql.OpenTelemetry) is the official OpenTelemetry instrumentation package for Npgsql (the .NET data provider for PostgreSQL).

* **Why it is recommended:** Standard EF Core instrumentation only captures high-level LINQ-to-SQL command executions. `Npgsql.OpenTelemetry` hooks directly into the low-level Npgsql driver, emitting fine-grained database spans, connection pool telemetry, and batch query diagnostics.
* **Emitted Telemetry & Diagnostics:**
  * **SQL Commands & Batching:** Full tracing of PostgreSQL statements (`db.system=postgresql`, `db.statement`), parameter types, and batch execution pipelines (`NpgsqlBatch`).
  * **Connection Pool Metrics:** Live counters for active connections (`npgsql.connection.pool.size`), idle connections, and pending/queued requests (`npgsql.connection.pool.pending_requests`).
  * **Multiplexing & Multi-Host Failover:** Diagnostics for socket connection timing, SSL handshakes, and failover latency across multi-node PostgreSQL clusters.

```csharp
using Npgsql.OpenTelemetry;

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddEntityFrameworkCoreInstrumentation()
            .AddNpgsql(); // Enables Npgsql PostgreSQL command & connection tracing!
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddMeter("Npgsql"); // Enables Npgsql Connection Pool & socket metrics
    });
```

#### 3.2 Recommended Driver & Middleware Instrumentation Matrix

| Component / Driver | Package | Registration API | Diagnostic Benefits |
| :--- | :--- | :--- | :--- |
| **PostgreSQL (Npgsql)** | [`Npgsql.OpenTelemetry`](https://www.nuget.org/packages/Npgsql.OpenTelemetry) | `.AddNpgsql()` | Low-level SQL command spans, connection pool saturation, batch query pipeline tracing. |
| **Entity Framework Core** | `OpenTelemetry.Instrumentation.EntityFrameworkCore` | `.AddEntityFrameworkCoreInstrumentation()` | LINQ query translation, DB context initialization, and unit-of-work transaction spans. |
| **Microsoft SQL Server / SqlClient** | `OpenTelemetry.Instrumentation.SqlClient` | `.AddSqlClientInstrumentation()` | T-SQL statement tracing, procedure calls, and connection timeout diagnostics. |
| **StackExchange.Redis** | `OpenTelemetry.Instrumentation.StackExchangeRedis` | `.AddRedisInstrumentation()` | Redis command latency (GET, SET, MGET), key space operations, and multiplexer connection state. |
| **gRPC Client** | `OpenTelemetry.Instrumentation.GrpcNetClient` | `.AddGrpcClientInstrumentation()` | gRPC RPC method spans (`rpc.service`, `rpc.method`), status codes, and frame payload metrics. |

---

### 4. Dynamic Ratio-Based Sampling via `appsettings.json`

High-throughput production services emitting 100% of traces create massive storage costs and network overhead. **Head Sampling** evaluates trace sampling at span creation.

#### `appsettings.json` Configuration

```json
{
  "Telemetry": {
    "ServiceName": "OrderProcessingService",
    "OtlpEndpoint": "http://otel-collector:4317",
    "TraceSamplingRatio": 0.05,
    "EnableSampling": true
  }
}
```

#### Incident Override Mode (Zero-Loss Telemetry Toggle)

During production incidents or debugging sessions, operators can update `Telemetry:EnableSampling` to `false` via environment variables or configuration reload without deploying code:

```bash
# Override env var during incident to capture 100% of traces:
Telemetry__EnableSampling=false
```

```csharp
// Code implementation dynamically evaluates the configuration value:
Sampler sampler = configuration.GetValue<bool>("Telemetry:EnableSampling", true)
    ? new ParentBasedSampler(new TraceIdRatioBasedSampler(configuration.GetValue<double>("Telemetry:TraceSamplingRatio", 0.1)))
    : new AlwaysOnSampler();
```

---

### 5. Metrics & Exemplars

An **Exemplar** links a metric measurement (such as a 99th percentile request duration histogram bucket) directly to the exact `trace_id` and `span_id` of the HTTP request that produced it.

```
Grafana Metric Chart: kestrel.request.duration [Histogram Bucket: > 500ms]
                     │
                     └── Exemplar Attached: [trace_id = 4bf92f3577b34da6a3ce929d0e0e4736]
                                 │
                                 └── (Click) -> Opens Trace Waterfall in OpenObserve / Tempo / Jaeger!
```

#### Enabling Exemplars in .NET
To enable Exemplars, use `.SetExemplarFilter(ExemplarFilterType.TraceBased)`. This automatically attaches trace context from `Activity.Current` whenever a metric measurement is recorded while a trace is sampled.

---

### 5. OpenTelemetry Collector Architecture & Pipelines

The **OpenTelemetry Collector** is a high-performance proxy component deployed alongside your application stack.

```mermaid
flowchart LR
    App[".NET App (Logs, Traces, Metrics)"] -->|OTLP / gRPC or HTTP| Receiver["Receiver (otlp)"]
    subgraph OtelCollector["OpenTelemetry Collector Pipeline"]
        Receiver --> Processors["Processors (batch, memory_limiter, tail_sampling)"]
        Processors --> Exporters["Exporters (otlp, prometheus, victorialogs)"]
    end
    Exporters -->|Logs + Traces + Metrics| OO["OpenObserve (All-in-One APM UI)"]
    Exporters -->|Logs| VL["VictoriaLogs (LogsQL)"]
    Exporters -->|Traces| Tempo["Grafana Tempo / Jaeger"]
    Exporters -->|Metrics| Prom["Prometheus"]
```

#### Why Use an Otel Collector?
1. **Process Offloading:** Offloads heavy batching, compression (GZip/Zstd), retries, and network TLS overhead out of the .NET application process.
2. **Security & Credential Isolation:** API tokens, basic auth headers, and production credentials live in the Collector configuration rather than microservice environment variables.
3. **Multi-Backend Routing:** Simultaneously forwards logs to **VictoriaLogs**, traces to **Tempo** or **OpenObserve**, and metrics to **Prometheus**.
4. **Tail Sampling:** Evaluates sampling rules *after* the entire distributed trace finishes.

#### OpenTelemetry Collector Tail Sampling Configuration (`otel-collector-config.yaml`)

Unlike Head Sampling (which drops traces randomly at the start), **Tail Sampling** buffers completed traces in the Collector memory and applies intelligent rules:

```yaml
receivers:
  otlp:
    protocols:
      grpc:
        endpoint: 0.0.0.0:4317
      http:
        endpoint: 0.0.0.0:4318

processors:
  memory_limiter:
    check_interval: 1s
    limit_percentage: 75
    spike_limit_percentage: 15

  batch:
    send_batch_size: 8192
    timeout: 1s

  tail_sampling:
    decision_wait: 10s
    num_traces: 10000
    expected_new_traces_per_sec: 2000
    policies:
      # Policy 1: Always drop health checks & metric scrapes
      - name: drop-health-checks
        type: string_attribute
        string_attribute:
          key: http.target
          values: [ "/healthz", "/metrics", "/ready" ]
          enabled_regex_matching: false
          invert_match: true

      # Policy 2: Always keep 100% of traces containing HTTP 5xx errors or exceptions
      - name: keep-all-errors
        type: status_code
        status_code:
          status_codes: [ ERROR ]

      # Policy 3: Always keep 100% of slow requests (> 500ms duration)
      - name: keep-slow-requests
        type: latency
        latency:
          threshold_ms: 500

      # Policy 4: Sample 5% of normal successful requests (HTTP 200 OK)
      - name: sample-normal-traffic
        type: probabilistic
        probabilistic:
          sampling_percentage: 5.0

exporters:
  otlp/openobserve:
    endpoint: "http://openobserve:5080/api/default"
    headers:
      Authorization: "Basic cm9vdEBleGFtcGxlLmNvbTpDb21wbGV4cGFzcyMxMjM="
  
  otlp/victorialogs:
    endpoint: "http://victoria-logs:9428/insert/opentelemetry/v1/logs"
    tls:
      insecure: true

  otlp/seq:
    endpoint: "http://seq:5341/ingest/otlp"
    tls:
      insecure: true

service:
  pipelines:
    traces:
      receivers: [otlp]
      processors: [memory_limiter, tail_sampling, batch]
      exporters: [otlp/openobserve, otlp/seq]
    logs:
      receivers: [otlp]
      processors: [memory_limiter, batch]
      exporters: [otlp/victorialogs, otlp/openobserve, otlp/seq]
    metrics:
      receivers: [otlp]
      processors: [memory_limiter, batch]
      exporters: [otlp/openobserve, otlp/seq]
```

---

### 6. Structured Logging & Context Scopes (`logger.BeginScope`)

#### Structured Logging vs. String Interpolation

```csharp
// ❌ INCORRECT: String Interpolation (Destroys structure, produces unindexed raw text)
logger.LogInformation($"Processed order {orderId} for user {userId}");

// ✅ CORRECT: Named Template Parameters (Extracts structured key-value pairs)
logger.LogInformation("Processed order {OrderId} for user {UserId}", orderId, userId);
```

- **Why it matters:** Columnar log engines (**VictoriaLogs**, **OpenObserve**, **ClickHouse**) automatically parse template parameters into typed JSON attributes (`OrderId: "1234"`). This enables instant filtering with `LogsQL` (`OrderId:="1234"`) or SQL without slow regex searches.

#### Why Logging Scopes are Vital (`logger.BeginScope`)

Logging scopes push ambient contextual key-value pairs onto the current async execution flow:

```csharp
using (logger.BeginScope(new Dictionary<string, object> { ["tenant.id"] = "acme", ["user.id"] = "42" }))
{
    // Every log statement executed in this block (or child async methods) automatically inherits tenant.id and user.id!
    logger.LogInformation("Processing payment");
    await ExecutePaymentStepAsync();
    logger.LogInformation("Payment completed");
}
```

#### How `ActDim.Observability` Enhances Scopes

- **Scope Flattening:** `EventObservabilityBridge` automatically flattens dictionary scopes, anonymous objects, and DTOs into dotted OTel tags (`tenant.id`, `user.id`, `order.price`).
- **Auto Activity Creation:** Calling `logger.BeginScope()` or `logger.BeginMethodScope()` starts a new OpenTelemetry `Activity` span if no active span exists (`Activity.Current == null`).
- **Ambient Context Binding:** Integrates directly with `IAmbientContext` and `IObservabilityContext`, capturing ambient state without parameter drilling.

#### Zero-Ceremony & Framework Independence (Serilog vs. Native .NET Logging)

- **Zero Third-Party Logging Dependencies:** `ActDim.Observability` eliminates the need for heavy external logging frameworks like Serilog or NLog. Standard `Microsoft.Extensions.Logging` combined with OpenTelemetry OTLP Exporters delivers zero-allocation, high-performance structured logging natively out of the box.
- **Seamless Serilog Interop:** If an existing application already relies on **Serilog**, `ActDim.Observability` integrates transparently. Developers can retain `builder.Host.UseSerilog()` or Serilog sinks-`EventObservabilityBridge` decorates `ILoggerFactory` via standard BCL interfaces, capturing scopes and ambient state without conflicts.

#### Why Logger Categories Use Full Type Names (`type.FullName`) & Namespace Hierarchy

When obtaining a logger via `ILogger<T>` or `LoggerFactory.CreateLogger(typeof(T))`, .NET assigns the category name using the **full type name with namespace** (e.g. `ActDim.Practix.Common.Context.AmbientContext`) rather than just the short class name (`AmbientContext`).

```mermaid
flowchart TD
    Root["ActDim (Global Company/Solution Prefix)"] --> Module["ActDim.Practix.Common (Subsystem Prefix)"]
    Module --> Class1["ActDim.Practix.Common.Context.AmbientContext (Specific Type)"]
    Module --> Class2["ActDim.Practix.Common.Compression.CompressionManager (Specific Type)"]

    style Root fill:#1f2937,stroke:#374151,color:#fff
    style Module fill:#1e3a8a,stroke:#3b82f6,color:#fff
    style Class1 fill:#065f46,stroke:#10b981,color:#fff
    style Class2 fill:#065f46,stroke:#10b981,color:#fff
```

##### 1. Hierarchical Prefix Matching & Implicit Wildcards in `appsettings.json`

In `Microsoft.Extensions.Logging`, category filtering uses **implicit prefix matching** (`StartsWith`). Specifying a root or child namespace prefix acts as an **implicit wildcard** (`Namespace.*`). 

Setting a rule for `"Microsoft"` automatically applies to all child namespaces under `Microsoft.*` (`Microsoft.AspNetCore`, `Microsoft.EntityFrameworkCore`, `Microsoft.Extensions`), while specific sub-namespace rules act as targeted overrides:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information",
      "Microsoft.AspNetCore.Server.Kestrel": "Warning",
      "Microsoft.AspNetCore.Routing": "Warning",
      "Microsoft.AspNetCore.HttpLogging.HttpLoggingMiddleware": "Information",
      "Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware": "Error",
      "Microsoft.AspNetCore.Authentication": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning",
      "Microsoft.EntityFrameworkCore.Database.Command": "Warning",
      "System.Net.Http.HttpClient": "Warning",
      "ActDim": "Debug",
      "ActDim.Practix.Common.Context.AmbientContext": "Error"
    }
  }
}
```

##### Production Recommended Log Levels for Core .NET & ASP.NET Core Frameworks

| Namespace / Category | Recommended Level | Rationale & Why |
| :--- | :--- | :--- |
| `"Default"` | `Information` | Baseline verbosity for application business logic. |
| `"Microsoft"` | `Warning` | **Global Framework Wildcard (`Microsoft.*`).** Mutes framework internal noise (routine parameter binding, DI resolution, pipeline dispatch). |
| `"Microsoft.Hosting.Lifetime"` | `Information` | **Host Lifecycle.** Essential for infrastructure & K8s monitoring-logs application startup, listening URLs/ports, environment name, and graceful shutdown. |
| `"Microsoft.AspNetCore.Server.Kestrel"` | `Warning` | **Web Server Socket Layer.** Mutes routine TCP connection/disconnection logs per request while preserving TLS handshake and socket errors (`Warning`/`Error`). |
| `"Microsoft.AspNetCore.Routing"` | `Warning` | **Route Matching Engine.** Mutes `DfaMatcher` diagnostic evaluations that execute on every single incoming HTTP request. |
| `"Microsoft.AspNetCore.HttpLogging.HttpLoggingMiddleware"` | `Information` | **HTTP Audit.** When HTTP Logging Middleware is enabled, setting `Information` captures incoming request/response headers and body payloads. |
| `"Microsoft.AspNetCore.Diagnostics"` | `Error` | **Unhandled Exception Handler.** Ensures unhandled web API exceptions and stack traces are logged at `Error` level. |
| `"Microsoft.AspNetCore.Authentication"` | `Warning` | Mutes routine token validation/claims processing chatter while keeping security warnings (`Warning`). |
| `"Microsoft.EntityFrameworkCore"` | `Warning` | **ORM Pipeline Wildcard (`Microsoft.EntityFrameworkCore.*`).** Suppresses internal state-tracker, model building, and change-tracker events. |
| `"Microsoft.EntityFrameworkCore.Database.Command"` | `Warning` (Prod) / `Information` (Dev) | **SQL Execution.** In Production, `Warning` prevents logging SQL text and parameters (protecting PII and storage quota). In Development, `Information` exposes executed SQL statements. |
| `"System.Net.Http.HttpClient"` | `Warning` | **Outbound HTTP Calls.** Mutes `HttpClient` request lifecycle and header dumps for outgoing microservice calls. |
| `"ActDim"` | `Debug` | **Company Domain Wildcard (`ActDim.*`).** Enables detailed diagnostic logs across all company modules (`ActDim.Emitron`, `ActDim.Reflectron`, `ActDim.Observability`, `ActDim.Practix`). |

##### 2. Explicit Wildcards in Log Aggregators (VictoriaLogs, OpenObserve, Grafana Loki)

While .NET configuration evaluates prefixes implicitly (`StartsWith`), downstream columnar log engines support **explicit wildcards** and regular expressions for querying accumulated type categories:

- **VictoriaLogs (`LogsQL`):** `_stream:{category=~"Microsoft.AspNetCore.*"}` or `category:="ActDim.Practix.*"`
- **OpenObserve / SQL:** `SELECT * FROM logs WHERE category LIKE 'ActDim.Practix.%'`
- **Grafana Loki (`LogQL`):** `{category=~"Microsoft.AspNetCore.*"}`

##### 3. Category Collision Avoidance
In enterprise microservices and multi-project solutions, common class names such as `Service`, `Repository`, `Worker`, `Configuration`, or `Pipeline` appear across dozens of namespaces. Using `type.FullName` prevents category collisions in log aggregators (**VictoriaLogs**, **OpenObserve**, **ClickHouse**), ensuring each log entry is unambiguously mapped to its exact source code origin.

---

## Installation

Install via the .NET CLI:

```bash
dotnet add package ActDim.Observability
```

Or via Package Manager Console:

```powershell
Install-Package ActDim.Observability
```

## Registration

Register observability in your `IServiceCollection`:

```csharp
services.AddEventObservability(logging =>
{
    logging.AddConsole();
}, options =>
{
    options.IncludeExternalScopes = false; // Default: false
});
```

## Usage

### 1. Operation Status & Progress Reporting

```csharp
var observability = serviceProvider.GetRequiredService<IObservabilityContext>();

// Set a unified operation status (name, progress %, icon, step)
using (observability.SetStatus("Downloading Dataset", progress: 45.5, icon: "🚀", step: 1, totalSteps: 3))
using (observability.Push("priority", "high"))
{
    // Read active status anywhere in the execution flow:
    ObservabilityStatus? currentStatus = observability.Status;
    logger.LogInformation("Importing rows into database for status {StatusName}", currentStatus?.Name);
}

// Or pass an ObservabilityStatus instance:
using (observability.SetStatus(new ObservabilityStatus("Processing Rows", progress: 75.0, icon: "⚡", step: 2, totalSteps: 3)))
{
    logger.LogInformation("Processing batch rows");
}
```

### 2. Method Scopes with OpenTelemetry Semantic Conventions

Use `logger.BeginMethodScope()` to automatically capture the executing method name, source file, and line number without manual string formatting. Scope properties strictly adhere to the [OpenTelemetry Source Code Semantic Conventions](https://opentelemetry.io/docs/specs/semconv/attributes-registry/code/):

```csharp
public class OrderService
{
    private readonly ILogger<OrderService> _logger;

    public OrderService(ILogger<OrderService> logger)
    {
        _logger = logger;
    }

    public async Task ProcessOrderAsync(string orderId)
    {
        // Automatically captures code.function="ProcessOrderAsync", code.filename="OrderService.cs", code.lineno=...
        using (_logger.BeginMethodScope())
        {
            _logger.LogInformation("Processing order {OrderId}", orderId);
        }

        // Merge custom state with caller code context
        using (_logger.BeginMethodScope(new Dictionary<string, object?> { ["OrderId"] = orderId }))
        {
            _logger.LogInformation("Order completed");
        }
    }
}
```

| Scope Key | Constant (`ObservabilityTagNames.Code`) | Description |
| :--- | :--- | :--- |
| `code.function` | `ObservabilityTagNames.Code.Function` | Caller method or member name |
| `code.filename` | `ObservabilityTagNames.Code.FileName` | File name (e.g. `OrderService.cs`) |
| `code.filepath` | `ObservabilityTagNames.Code.FilePath` | Full source file path |
| `code.lineno` | `ObservabilityTagNames.Code.LineNumber` | Source code line number |

*Why OpenTelemetry Semantic Conventions?* Standard attribute names (`code.function`, `code.filepath`, `code.lineno`) enable APM tools, log aggregators, and distributed trace visualizers (Jaeger, Grafana Tempo, Datadog, Dynatrace, and .NET Aspire) to natively index, filter, and navigate directly to source code locations.

### 3. Selective Provider Suppression

```csharp
// Suppress Console logger output while preserving Activity traces and other logger sinks
using (observability.SuppressConsole())
{
    logger.LogInformation("Log without console output");
}

// Suppress specific providers by alias or name (e.g., "File", "Console")
using (observability.SuppressProviders("File", "Console"))
{
    logger.LogInformation("Log without File and Console outputs");
}
```

### 4. Integration Testing & Tooling (VictoriaLogs & OpenObserve)

`ActDim.Observability.Tests` includes integration test suites and developer scripts for validating telemetry ingestion and log search:

- **VictoriaLogs Integration (`VictoriaLogsIntegrationTests`):**
  - Validates NDJSON ingestion (`/insert/jsonline`), `_msg` field format, `AmbientContext` properties, `BeginMethodScope()` OTel caller metadata (`code.function`, `code.filename`, `code.filepath`, `code.lineno`), and **LogsQL** queries.
  - Launcher & Download Scripts: `Tools/victoria-logs/run-victoria-logs.cmd` (auto-opens VMUI Web GUI at [http://localhost:9428/select/vmui](http://localhost:9428/select/vmui)) and `download-victoria-logs.cmd`.

- **OpenObserve Integration (`OpenObserveIntegrationTests`):**
  - Validates JSON log ingestion (`/api/{org}/{stream}/_json`), `AmbientContext` enrichment, and **SQL Search API** (`POST /api/{org}/_search`).
  - Launcher & Download Scripts: `Tools/openobserve/run-openobserve.cmd` (auto-opens Web GUI at [http://localhost:5080](http://localhost:5080) with default admin credentials `root@example.com` / `Complexpass#123`) and `download-openobserve.cmd`.

- **Seq (Datalust) Developer Setup:**
  - Highly recommended .NET-native observability server providing structured log tailing, signal filtering, metric charts, and distributed trace waterfalls in a single clean UI.
  - Free single-user license for local development (`docker run -d -p 5341:80 -e ACCEPT_EULA=Y datalust/seq`). Web UI auto-opens at [http://localhost:5341](http://localhost:5341) with native OTLP ingestion on `http://localhost:5341/ingest/otlp`.

- **.NET Aspire Dashboard Setup:**
  - Microsoft's official, open-source telemetry visualization dashboard for local development. Natively ingests OTLP logs, traces, and metrics directly with **zero custom logger providers or third-party adapters**:
    ```bash
    docker run --name aspire-dashboard -d --restart unless-stopped -p 18888:18888 -p 4317:18889 -p 4318:18890 mcr.microsoft.com/dotnet/aspire-dashboard:latest
    ```
  - Web UI auto-opens at [http://localhost:18888](http://localhost:18888) with OTLP gRPC endpoint on `http://localhost:4317` and HTTP endpoint on `http://localhost:4318`.

- **Process Auto-Launch:** Both integration tests automatically detect running local instances or auto-launch local binaries from `Tools/` into isolated temporary storage paths.

## Testing & Quality

- **Test Suite:** `ActDim.Observability.Tests`
- **Total Tests:** 30 passed (100% success rate, 0 failed, 0 skipped)
- **Target Framework:** .NET 10.0

```bash
dotnet test Tests/Observability.Tests/ActDim.Observability.Tests.csproj
```

## License

This project is licensed under the [MIT License](../LICENSE).
