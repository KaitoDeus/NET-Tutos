# Security Policy (NET-Tutos)

## Supported Versions

| Version | .NET Target | Architecture | Supported |
| :--- | :--- | :--- | :---: |
| **2.6.x (Current)** | `.NET 10.0` / `C# 14` | 9-Container Microservices + YARP + Redis 7 + PostgreSQL 17 | ✅ |
| **< 2.0** | `.NET 8.0 / 9.0` | Legacy Monolith | ❌ |

## Security Architecture Highlights

1. **Isolated Roslyn C# Sandbox (`CodeRunner.API`)**:
   - Blocks dangerous namespaces and reflection/process APIs (`System.Diagnostics.Process`, `System.IO.File`, `System.IO.Directory`, `System.Net.Sockets`, `System.Reflection.Emit`, `Environment.Exit`).
   - Enforces strict execution timeouts (`CancellationTokenSource`) and YARP API Gateway rate limiting (`60 req/min`).
2. **Persistent DataProtection & Anti-Forgery**:
   - Cryptographic key ring persisted to `/app/.keys` volume with strict `RequestVerificationToken` validation.
3. **Automated Continuous CVE Auditing**:
   - Every push, PR, and weekly schedule runs `.github/workflows/security-audit.yml` (`dotnet list package --vulnerable --include-transitive`) and Dependabot security updates.

## Reporting a Vulnerability

If you discover a security issue, please report it privately via GitHub Security Advisories on `https://github.com/KaitoDeus/NET-Tutos/security/advisories` rather than opening a public issue.
