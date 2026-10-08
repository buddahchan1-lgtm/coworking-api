# Espaços Bertuloso — API de Coworking 🏢

Projeto individual da disciplina de **Desenvolvimento Back-End**.
**Autor:** Rafael Machado Carôlo Bertuloso · Nicho: **Coworking**

> ⚠️ Projeto escolar — empresa fictícia, não é um serviço real.

## Domínio

- **Sala → Reserva** (1 para N) e **Usuario** com papéis `admin` / `cliente`.
- **Regra de conflito (HTTP 409):** tentar reservar a mesma Sala em um horário
  que se sobrepõe a uma reserva existente devolve `409 Conflict` em ProblemDetails.

## Stack

| Camada | Tecnologia |
|---|---|
| API | .NET 10 (SDK 10.0.x), C# 14, ASP.NET Core Web API com Controllers |
| ORM | EF Core 10 + `Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.3 (PostgreSQL no **Neon**) |
| Auth | JWT Bearer (`Microsoft.AspNetCore.Authentication.JwtBearer` 10.0.12) + `BCrypt.Net-Next` 4.2.1 |
| Docs | Swashbuckle.AspNetCore 10.2.3 (habilitado em produção, com Bearer no Swagger) |
| Health | `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore` 10.0.12 → `GET /health` |
| Testes | xUnit 2.9.3 + EF Core SQLite 10.0.12 (24 testes) |
| Deploy | Docker 2 estágios (porta **10000**) no **Render** · CI no GitHub Actions |
| Front | React + Vite + TypeScript em `/front` (`react-router` 8.4.0, fetch nativo, CSS puro) |

## Estrutura

```
coworking/
├── .github/workflows/ci.yml      # CI: build -warnaserror + dotnet test + build do front
├── Coworking.sln
├── requisicoes.http              # testes de rotas (REST Client / Rider)
├── Dockerfile.run                # comando de build da imagem
├── src/Coworking.Api/
│   ├── Program.cs                # DI, JWT, CORS, Swagger 10, ProblemDetails, / e /api/versao
│   ├── ConflitoReservaExceptionHandler.cs
│   ├── Dockerfile                # 2 estágios, porta 10000
│   ├── appsettings.json
│   ├── Controllers/              # AuthController, SalasController, ReservasController
│   ├── Data/                     # CoworkingDbContext, InicializadorBanco (migrate+seed), Migrations/
│   ├── Domain/Entidades.cs       # Usuario, Sala, Reserva
│   ├── Dtos/Dtos.cs              # DateTimeOffset nas bordas, UtcDateTime no banco
│   └── Services/                 # TokenService, ReservaService (regra 409 em transação)
├── tests/Coworking.Tests/        # 24 testes: domínio, regra 409, autorização, integração
└── front/                        # React + Vite (vitrine, login, cadastro, painel de reservas)
```

## Endpoints principais

| Método | Rota | Acesso |
|---|---|---|
| GET | `/api/salas?busca=&pagina=1&tamanho=10` | público (paginado, filtro por nome) |
| GET | `/api/salas/{id}` | público (200/404 ProblemDetails) |
| POST/PUT/DELETE | `/api/salas[/{id}]` | **admin** (201+Location / 200 / 204) |
| POST | `/api/auth/registro` · `/api/auth/login` | público |
| POST | `/api/reservas` | cliente logado (valida 409) |
| GET | `/api/reservas` | dono das reservas |
| PUT/DELETE | `/api/reservas/{id}` | dono **ou** admin (403 caso contrário) |
| GET | `/health` · `/api/versao` | público |
| GET | `/` | redireciona para `/swagger` |

## Como rodar (local)

```bash
# 1. Back-end — configure a connection string do Neon em appsettings.json
#    (ou via env: ConnectionStrings__Padrao="Host=...;Database=...;Username=...;Password=...")
cd src/Coworking.Api
dotnet ef database update   # ou deixe o Program.cs migrar na subida
dotnet run                  # http://localhost:5000/swagger

# 2. Front-end
cd front
echo 'VITE_API_URL=http://localhost:5000' > .env
npm install && npm run dev  # http://localhost:5173

# 3. Testes
dotnet test
```

**Semente de dados:** admin `admin@coworking.com` / senha do env `Seed__AdminSenha`
(padrão `Admin@123`); clientes `ana@exemplo.com` e `bruno@exemplo.com` com `Cliente@123`.

## Variáveis de ambiente (Render)

| Variável | Exemplo |
|---|---|
| `ConnectionStrings__Padrao` | `Host=ep-xxx.aws.neon.tech;Database=coworking;Username=don0_xxx;Password=***` |
| `Jwt__Chave` | chave secreta com 64+ caracteres (HS256) |
| `Cors__Origens` | `https://seu-front.onrender.com` |
| `Seed__AdminSenha` | senha do admin semeado |

## Deploy no Render (Docker, porta 10000)

1. Faça push do repositório no GitHub (CI fica verde com build + testes).
2. No Render: **New → Web Service → Docker** apontando para o repo
   (o `Dockerfile` usado é o `src/Coworking.Api/Dockerfile`, porta 10000).
3. Configure as variáveis de ambiente da tabela acima.
4. A API aplica migrações e o seed idempotente na subida; `GET /health` é o health check.

## Notas técnicas

- **UTC:** Npgsql exige `DateTime` UTC — entidades gravam `UtcDateTime` e os DTOs
  expõem `DateTimeOffset`.
- **409:** `ReservaService` verifica sobreposição (`inicio < fimExistente && fim > inicioExistente`)
  e grava dentro de `BeginTransactionAsync()`; o `ConflitoReservaExceptionHandler`
  devolve ProblemDetails com mensagem limpa.
- **Swagger:** Swashbuckle 10 (Microsoft.OpenApi 2.x) usa
  `AddSecurityRequirement(documento => ...)` com `OpenApiSecuritySchemeReference`.
