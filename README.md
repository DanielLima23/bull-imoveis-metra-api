# Imoveis API (.NET 8 + PostgreSQL)

API REST para gestao de imoveis, locacoes, contas, pendencias, visitas e manutencoes.

## Stack
- ASP.NET Core Web API (.NET 8)
- EF Core + Npgsql
- PostgreSQL
- JWT (access + refresh token)
- Swagger/OpenAPI
- Migration EF Core

## Estrutura
- `Imoveis.Api`
- `Imoveis.Application`
- `Imoveis.Domain`
- `Imoveis.Infrastructure`

## Pre requisitos
- .NET SDK 8+
- PostgreSQL (ex.: local em 5432)

## Configuracao
Arquivo: `Imoveis.Api/appsettings.Development.json`

Ajuste:
- `ConnectionStrings:Default`
- `Jwt:Issuer`
- `Jwt:Audience`
- `Jwt:Key`
- `Jwt:ExpiryMinutes`
- `Jwt:RefreshExpiryDays`

O backend usa o schema PostgreSQL `imoveis` como `Search Path`, sem depender do schema `public`.

## Rodar

```bash
dotnet run --project Imoveis.Api
```

Swagger:
- `https://localhost:7083/swagger`

Health:
- `https://localhost:7083/health`

## Migrations
Migration criada:
- `Imoveis.Infrastructure/Persistence/Migrations/20260305202428_InitialCreate.cs`

Comandos:

```bash
dotnet ef database update --project Imoveis.Infrastructure --startup-project Imoveis.Api --context Imoveis.Infrastructure.Persistence.AppDbContext
```

## Auth seed
- `super@dw-softwares.com.br` / `123456`
- `admin@imoveis.dev` / `123456`
- `operador@imoveis.dev` / `123456`

## Endpoints principais
- `/api/auth/*`
- `/api/imoveis/*`
- `/api/locatarios/*`
- `/api/locacoes/*`
- `/api/despesas/*`
- `/api/pendencias/*`
- `/api/visitas/*`
- `/api/manutencoes/*`
- `/api/dashboard/imobiliario`
- `/api/relatorios/*`
