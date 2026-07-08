# 🔗 SnipLink — URL Shortener with Click Analytics

A small but production-shaped URL shortener built with **.NET 8**. Paste a long URL,
get a short code, share it, and watch per-link analytics update. It demonstrates a
high-read **redirect hot path**, **Redis caching** with graceful fallback, a
**background worker** that rolls raw clicks into daily aggregates, **rate limiting**,
**RFC 7807** error handling, and a minimal **Razor Pages** UI alongside a documented API.

> Built as a portfolio project. Anonymous links are managed with an owner token
> (a lightweight stand-in for real accounts — see [Future work](#future-work)).

---

## Tech stack

| Concern | Choice |
|--------|--------|
| Runtime | .NET 8 (LTS) |
| Web | ASP.NET Core — Web API + Razor Pages |
| Data | Entity Framework Core + PostgreSQL (Npgsql) |
| Cache | Redis (StackExchange.Redis) |
| Background work | `BackgroundService` (hosted) |
| Validation | FluentValidation |
| Logging | Serilog (structured, to console) |
| Tests | xUnit, Moq, FluentAssertions, `WebApplicationFactory` |
| Container | Docker + docker-compose (web, worker, db, redis) |

---

## Architecture

A layered solution with a one-way dependency flow (`Web`/`Worker` → `Application` → `Domain`,
and `Infrastructure` → `Application` → `Domain`). `Domain` depends on nothing.

```
SnipLink.sln
├── src/
│   ├── SnipLink.Domain          # Entities + base62 code generator. No dependencies.
│   ├── SnipLink.Application      # DTOs, service interfaces, LinkService, validators
│   ├── SnipLink.Infrastructure   # EF Core, repositories, Redis cache, click pipeline, aggregator
│   ├── SnipLink.Worker           # BackgroundService that aggregates clicks → daily stats
│   └── SnipLink.Web              # API controllers + Razor Pages + the redirect endpoint
└── tests/
    ├── SnipLink.UnitTests        # code gen, validation, LinkService, aggregation
    └── SnipLink.IntegrationTests # full pipeline via WebApplicationFactory
```

### How the moving parts fit together

**The redirect hot path (`GET /{code}`)** is the most frequently hit route, so it's
optimised for reads:

1. Look up the code in **Redis** first.
2. On a cache miss, read from **PostgreSQL** and populate the cache (only for followable links).
3. **Enqueue the click** into an in-memory channel and **redirect immediately** — click
   recording never blocks the redirect.
4. Return `404` for unknown codes and `410 Gone` for expired/deactivated links.

**Click recording** is fire-and-forget: the redirect drops a `ClickInfo` into a bounded
in-memory channel (`ChannelClickRecorder`). A hosted `ClickFlushService` drains the channel
and writes batches to the database. Under a sustained spike the buffer drops writes rather
than slowing redirects — analytics is best-effort, redirect latency is not.

**The background worker** (`SnipLink.Worker`) periodically recomputes per-(link, day) click
counts from the raw `ClickEvent` rows and upserts them into `DailyStat`. It's **idempotent**:
running it twice over the same data yields identical rows.

**Graceful Redis fallback:** every cache call is wrapped so a Redis outage degrades
*performance* (reads fall back to the DB, writes become no-ops), not *correctness*. If no
Redis connection string is configured, a `NullLinkCache` is used and every redirect resolves
straight from the database.

---

## Domain model

- **ShortLink** — `Code` (unique, indexed), `LongUrl`, `OwnerToken`, `ExpiresAt?`, `IsActive`, `CreatedAt`.
- **ClickEvent** — raw visit: `ClickedAt`, `Referrer?`, `UserAgent?`, `IpHash?`, `Country?`. The source of truth.
- **DailyStat** — derived rollup, unique per `(ShortLinkId, Date)`.

Codes are **base62**, default length 6, collision-checked on creation. Custom aliases must be
3–20 alphanumeric characters and not a reserved word (e.g. `api`, `swagger`, `links`).

---

## Running it

### With Docker (recommended)

```bash
docker compose up --build
```

Then open:

- **UI:** <http://localhost:8080>
- **API docs (Swagger):** <http://localhost:8080/swagger>
- **Health:** <http://localhost:8080/health>

The compose stack runs `web`, `worker`, `db` (postgres:16), and `redis` (redis:7) with health
checks; `web` waits for the database and Redis to be healthy before starting. Demo seed data is
enabled in compose (`SnipLink__Seed=true`) so the stats page looks alive immediately.

### Locally (without Docker)

You need PostgreSQL and (optionally) Redis running. With the connection strings in
`src/SnipLink.Web/appsettings.json` pointing at them:

```bash
dotnet build

# Apply migrations and run the web app (migrations also run automatically on startup)
dotnet run --project src/SnipLink.Web

# In another terminal, run the aggregation worker
dotnet run --project src/SnipLink.Worker
```

Copy `src/SnipLink.Web/appsettings.Example.json` to `appsettings.json` and adjust as needed.
**No secrets are committed**; override the IP-hash salt and DB password via configuration or
environment variables (`ConnectionStrings__Postgres`, `SnipLink__IpHashSalt`, …).

---

## API overview (`/api/v1`)

| Method | Route | Description |
|--------|-------|-------------|
| `POST` | `/api/v1/links` | Create a short link (rate limited). Returns `code`, `shortUrl`, and `ownerToken`. |
| `GET` | `/api/v1/links/{code}` | Get link metadata (requires `X-Owner-Token`). |
| `GET` | `/api/v1/links/{code}/stats` | Analytics: total clicks, daily series, top referrers. |
| `PATCH` | `/api/v1/links/{code}` | Update expiry / active flag (owner token). |
| `DELETE` | `/api/v1/links/{code}` | Deactivate a link (owner token). |
| `GET` | `/{code}` | **Redirect** — `302` to the destination, `404` unknown, `410` gone. |

**Owner auth:** management endpoints compare an `X-Owner-Token` header against the token stored
at creation. This is intentionally simple — replace with real authentication for production.

### Example

```bash
# Create
curl -s -X POST http://localhost:8080/api/v1/links \
  -H 'Content-Type: application/json' \
  -d '{"longUrl":"https://learn.microsoft.com/dotnet/"}'
# => {"code":"aB3xY","shortUrl":"http://localhost:8080/aB3xY","ownerToken":"...","expiresAt":null}

# Follow it (records a click, then redirects)
curl -i http://localhost:8080/aB3xY

# Stats
curl -s http://localhost:8080/api/v1/links/aB3xY/stats -H 'X-Owner-Token: <ownerToken>'
```

---

## Cross-cutting concerns

- **Validation** — FluentValidation on create/update; failures return `400` with field errors.
- **Errors** — a global exception handler emits RFC 7807 `ProblemDetails` (`404` not found,
  `403` bad owner token, `409` alias taken, `400` validation, `410` expired on redirect).
- **Rate limiting** — the built-in ASP.NET Core fixed-window limiter caps `POST /api/v1/links`
  at **10 requests/minute per IP**.
- **Privacy** — visitor IPs are **never stored or logged raw**. They're hashed with SHA-256 over
  a configured salt (`SnipLink__IpHashSalt`) before storage. Behind the Docker/reverse proxy the
  real client IP is read from `X-Forwarded-For`.
- **Async everywhere** — all DB, cache, and request handling is `async`.

---

## Testing

```bash
dotnet test
```

- **Unit tests** — base62 generation + collision retry, alias/URL/expiry validation,
  `LinkService` (cache hit/miss, expiry, owner-token checks), and idempotent click aggregation.
- **Integration tests** — full pipeline via `WebApplicationFactory` (in-memory EF provider,
  Redis disabled): create → redirect → click recorded → stats reflect it; `404` unknown;
  `410` expired; `400` invalid; `403` wrong owner token; deactivate → `410`.

---

## Future work

- Real user accounts + authentication (replace owner tokens).
- Incremental, watermark-based click aggregation (current aggregator recomputes over history —
  fine for demo scale; see `ClickAggregator`).
- QR codes per link, geo-IP enrichment, custom domains, expiry by click count.
- Deploy to a free tier (Render / Fly.io / Azure) and link a live demo.

---

## License

[MIT](LICENSE)
