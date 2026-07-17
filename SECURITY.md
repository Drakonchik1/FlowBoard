# Security Policy

## Supported Versions

Only the latest commit on `master` is supported.

## Reporting a Vulnerability

Email: pavlo.dorofieiev@gmail.com

Please include steps to reproduce and impact assessment. Expect a response within 7 days.

## Credential Hygiene

- Never commit `.env`, `secrets.json`, or `appsettings.Development.json`
- Use `dotnet user-secrets` locally and environment variables in Docker/production
- Rotate JWT secret and database passwords if you suspect exposure

## Production hardening (Sprint 8)

- **Write rate limits:** authenticated POST/PUT/PATCH/DELETE — 60 requests/minute per user (IP fallback)
- **Auth rate limits:** `/api/auth/*` — 5 requests/minute per IP; configure `ForwardedHeaders:KnownProxies` behind reverse proxies so `RemoteIpAddress` reflects real clients
- **Registration:** `Auth:AllowRegistration` defaults to `false` in Production (`appsettings.Production.json`); set `Auth__AllowRegistration=true` only when open signup is intended
- **SignalR scale-out:** Redis backplane is supported, but board group eviction is **process-local** — deploy a **single API replica** until distributed eviction is implemented
- **SMTP:** assignment emails require `Smtp__Host` and `Smtp__FromEmail`; misconfiguration throws so Hangfire retries instead of silently dropping mail
- **Migrations:** Production startup fails fast when pending EF migrations exist; apply `dotnet ef database update` before traffic
- **SQL `TrustServerCertificate=True`:** acceptable on isolated Docker networks; use CA-trusted certs (`TrustServerCertificate=False`) on managed cloud SQL