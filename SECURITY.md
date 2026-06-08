# Security Policy

## Reporting a Vulnerability

If you discover a security issue, please email **pavlo.dorofieiev@gmail.com** rather than opening a public issue.

## Credential hygiene

This repository intentionally ships **no real secrets**. Use:

- `dotnet user-secrets` for local development
- `.env` (from `.env.example`) for Docker Compose

Never commit `.env`, `secrets.json`, or `appsettings.Development.json`.

## History note

Commits before the June 2026 public-release rewrite may have contained **example dev placeholders** in git history. If you cloned an older revision, do not reuse any credentials found there — generate fresh values for every environment.
