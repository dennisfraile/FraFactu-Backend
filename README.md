# FraFactu — Backend

Backend único en **ASP.NET Core .NET 8** que unifica **Facturación Electrónica** (base Smartix, El Salvador / Ministerio de Hacienda) e **Inventario** en una sola aplicación, con identidad propia (sin Hub) y una sola base de datos PostgreSQL.

> Proyecto nuevo e independiente. La lógica fiscal de Smartix se conserva en comportamiento; los cambios se concentran en cortar el Hub, absorber el inventario y rehacer el frontend. Ver el plan por fases en el repositorio de planificación.

## Stack

- ASP.NET Core .NET 8, Clean Architecture (`Domain` / `Application` / `Infrastructure` / `API`).
- EF Core 8 + PostgreSQL (Npgsql). Migraciones automáticas al arranque (`MigrateAsync`).
- JWT propio, BCrypt, AutoMapper, FluentValidation, QuestPDF, QRCoder.

## Arranque rápido con Docker (recomendado)

Todo el entorno (backend + frontend + PostgreSQL) se levanta con un solo comando. **Requiere que los dos repos estén clonados como carpetas hermanas:**

```
.../FraFactu-Backend    ← este repo (contiene el docker-compose)
.../FraFactu-Frontend
```

Clonado (cuenta `dennisfraile`, vía SSH con alias `github-dennis`):

```bash
git clone git@github-dennis:dennisfraile/FraFactu-Backend.git
git clone git@github-dennis:dennisfraile/FraFactu-Frontend.git
cd FraFactu-Backend
docker compose up --build
```

Servicios:

| Servicio  | URL                          | Notas                          |
|-----------|------------------------------|--------------------------------|
| Backend   | http://localhost:8080        | Swagger en `/swagger`          |
| Frontend  | http://localhost:5173        | Vite dev server (HMR)          |
| Postgres  | localhost:5432               | usuario/clave/BD: `frafactu`   |

Para detener: `docker compose down` (agrega `-v` para borrar también la base de datos).

### Variables de entorno

Los valores por defecto del `docker-compose.yml` son **solo para desarrollo local**. Para sobreescribirlos, copia `.env.example` a `.env` (Compose lo lee automáticamente). En producción se inyectan por variables de entorno / Key Vault.

Claves principales: `ConnectionStrings__DefaultConnection`, `JwtSettings__SecretKey`, `Encryption__MasterKey` (AES-256 en Base64, 32 bytes), `GoogleAuth__*`, `DefaultEmail__*` (SMTP), `MinisterioHacienda__BaseUrl`, `Cors__AllowedOrigins`.

## Desarrollo nativo (opcional, sin Docker)

Requiere el **SDK de .NET 8** instalado. Configura los secretos por User Secrets o variables de entorno y:

```bash
dotnet restore FraFactu.sln
dotnet build FraFactu.sln
dotnet test FraFactu.sln
dotnet run --project src/FraFactu.API
```

## Estructura

```
src/
├── FraFactu.Domain/          # Entidades, enums, excepciones
├── FraFactu.Application/      # Lógica de negocio, DTOs, interfaces, validators
├── FraFactu.Infrastructure/   # EF Core, repos, firma DTE, HTTP MH, SMTP, jobs
└── FraFactu.API/              # Controllers, middleware, Program.cs
tests/
└── FraFactu.Tests/            # xUnit
```

> Estado actual: **Fase 0 (fundaciones)**. El desacople del Hub (F1), el auth propio (F2) y la fusión del inventario (F3) son fases posteriores.
