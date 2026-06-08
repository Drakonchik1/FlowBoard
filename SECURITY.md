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