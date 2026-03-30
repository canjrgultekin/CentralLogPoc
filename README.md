# CentralLogPoc — Merkezi Log & Distributed Trace POC

## Mimari

```
Client
  │
  │  POST /api/workflow/start
  │  Header: X-Correlation-Id: <uuid>  ← yoksa API yaratır
  ▼
┌──────────────────────────────┐
│   CentralLogPoc.Api          │  :5000
│                              │
│  CorrelationIdMiddleware     │  ← X-Correlation-Id oku/yarat
│  LogContext.PushProperty     │  ← Serilog scope'a ekle
│  Activity.SetBaggage         │  ← W3C Baggage'a ekle (downstream otomatik taşır)
│                              │
│  WorkflowController          │
│  └─ GatewayClient            │  ← CorrelationIdHandler X-Correlation-Id header ekler
└──────────────┬───────────────┘
               │  HTTP POST /gateway/process
               │  Headers: traceparent + X-Correlation-Id
               ▼
┌──────────────────────────────┐
│   CentralLogPoc.Gateway      │  :5010
│                              │
│  ProcessController           │
│  ├─ OrderServiceClient  ─────┼──→ HTTP POST /api/orders        → OrderService  :5020
│  └─ InventoryServiceClient ──┼──→ HTTP POST /api/inventory/reserve → InventoryService :5030
│       (paralel - WhenAll)    │
└──────────────────────────────┘
```

## Her Log Satırında Ne Görürsün?

```
[12:34:56.789 INF] [CentralLogPoc.Gateway] [CorId:a3f8c1d2e4f5...] [Trace:1a2b3c4d...] [Span:5e6f7a8b] ...
```

| Alan         | Açıklama |
|-------------|---------|
| `ServiceName` | Hangi servis logladı |
| `CorrelationId` | Business-level ID — tüm servisler aynı değeri taşır |
| `TraceId` | OTel W3C TraceId — tüm span'lar aynı root trace'i işaret eder |
| `SpanId` | Her servis için unique — parent/child ilişkisi Jaeger'da görünür |

## Nasıl Çalıştırılır

### Docker Compose (Önerilen)

```bash
# Tüm servisleri + Seq + Jaeger ayağa kaldır
docker-compose up --build

# Arka planda çalıştır
docker-compose up --build -d
```

> **Not:** .NET 10 henüz preview aşamasında. Image bulunamazsa Dockerfile'lardaki
> `10.0` tag'ini `10.0-preview` ile değiştir.

### Local Development (Seq + Jaeger Docker, servisler dotnet run)

```bash
# Sadece altyapıyı başlat
docker-compose up seq jaeger -d

# Her servis için ayrı terminal
dotnet run --project src/CentralLogPoc.InventoryService
dotnet run --project src/CentralLogPoc.OrderService
dotnet run --project src/CentralLogPoc.Gateway
dotnet run --project src/CentralLogPoc.Api
```

## Test Et

### cURL

```bash
# Temel istek (correlationId yok — API yaratır)
curl -X POST http://localhost:5000/api/workflow/start \
  -H "Content-Type: application/json" \
  -d '{"productId": "PROD-42", "quantity": 5, "customerId": "CUST-001"}'

# Kendi correlationId'ini ver
curl -X POST http://localhost:5000/api/workflow/start \
  -H "Content-Type: application/json" \
  -H "X-Correlation-Id: my-test-trace-001" \
  -d '{"productId": "PROD-42", "quantity": 5, "customerId": "CUST-001"}'

# Stok limitini aş (Qty > 100 → InventoryService Warning log atar)
curl -X POST http://localhost:5000/api/workflow/start \
  -H "Content-Type: application/json" \
  -H "X-Correlation-Id: low-stock-test" \
  -d '{"productId": "PROD-99", "quantity": 150, "customerId": "CUST-002"}'
```

### Yanıt Örneği

```json
{
  "orderId": "ORD-3A7F8B2C",
  "productId": "PROD-42",
  "quantity": 5,
  "isInventoryReserved": true,
  "correlationId": "my-test-trace-001"
}
```

## Log & Trace Görüntüleme

| Araç | URL | Ne İçin |
|-----|-----|---------|
| **Seq** | http://localhost:5080 | Structured log arama. `CorrelationId = 'my-test-trace-001'` yaz |
| **Jaeger UI** | http://localhost:16686 | Distributed trace. Service dropdown'dan `CentralLogPoc.Api` seç |

### Seq'de Örnek Sorgu

```
CorrelationId = 'my-test-trace-001'
```

Bu sorgu; Api, Gateway, OrderService ve InventoryService'ten gelen tüm logları
tek ekranda, zamanlamaya göre sıralı gösterir.

### Jaeger'da Ne Görürsün?

```
CentralLogPoc.Api (1 span)
  └─ CentralLogPoc.Gateway (1 span)
       ├─ CentralLogPoc.OrderService (1 span)    ← paralel
       └─ CentralLogPoc.InventoryService (1 span) ← paralel
```

## Packages (Doğrulanmış Versiyonlar)

| Package | Version |
|---------|---------|
| `Serilog.AspNetCore` | 10.0.0 |
| `Serilog.Sinks.Seq` | 9.0.0 |
| `Serilog.Formatting.Compact` | 3.0.0 |
| `Serilog.Enrichers.Environment` | 3.0.0 |
| `Serilog.Enrichers.Thread` | 4.0.0 |
| `OpenTelemetry.Extensions.Hosting` | 1.15.0 |
| `OpenTelemetry.Instrumentation.AspNetCore` | 1.15.1 |
| `OpenTelemetry.Instrumentation.Http` | 1.15.1 |
| `OpenTelemetry.Exporter.Otlp` | 1.15.0 |

## Propagation Detayı

```
Request gelir:
  CorrelationIdMiddleware çalışır
    ├── X-Correlation-Id header'dan oku / yeni GUID üret
    ├── HttpContext.Items["X-Correlation-Id"] = correlationId
    ├── LogContext.PushProperty("CorrelationId", correlationId)   ← Serilog scope
    ├── Activity.SetTag("correlation.id", correlationId)
    └── Activity.SetBaggage("correlation.id", correlationId)      ← W3C Baggage

Outgoing HTTP çağrısı:
  CorrelationIdHandler (DelegatingHandler) çalışır
    └── request.Headers["X-Correlation-Id"] = correlationId      ← header olarak taşı

  OTel HttpClient instrumentation çalışır
    └── request.Headers["traceparent"] = W3C TraceContext         ← trace propagation

Downstream serviste:
  CorrelationIdMiddleware gene çalışır
    └── X-Correlation-Id header'dan okur — aynı değer
  ActivityEnricher çalışır
    └── Activity.Current.TraceId → aynı root TraceId (W3C sayesinde)
    └── Activity.Current.SpanId  → yeni child SpanId
```
