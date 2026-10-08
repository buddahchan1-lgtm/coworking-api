using System.Security.Claims;
using System.Text;
using Coworking.Api;
using Coworking.Api.Data;
using Coworking.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using OpenApiInfo = Microsoft.OpenApi.OpenApiInfo;
using OpenApiSecurityScheme = Microsoft.OpenApi.OpenApiSecurityScheme;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------
// Configuração base
// ---------------------------------------------------------------------
var connectionString = builder.Configuration.GetConnectionString("Padrao")
    ?? throw new InvalidOperationException("Connection string 'Padrao' não configurada no appsettings.");

var chaveJwt = builder.Configuration["Jwt:Chave"]
    ?? throw new InvalidOperationException("Configuração 'Jwt:Chave' não configurada.");

// ---------------------------------------------------------------------
// EF Core: PostgreSQL (Npgsql/Neon) em produção; SQLite nos testes de
// integração (basta a connection string começar com "Data Source=").
// ---------------------------------------------------------------------
builder.Services.AddDbContext<CoworkingDbContext>(opcoes =>
{
    if (connectionString.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase))
        opcoes.UseSqlite(connectionString);
    else
        opcoes.UseNpgsql(connectionString);
});

// ---------------------------------------------------------------------
// Health checks (GET /health valida o acesso ao banco)
// ---------------------------------------------------------------------
builder.Services.AddHealthChecks()
    .AddDbContextCheck<CoworkingDbContext>("Banco de dados");

// ---------------------------------------------------------------------
// Autenticação JWT (identidade lida dos Claims)
// ---------------------------------------------------------------------
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opcoes =>
    {
        opcoes.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            NameClaimType = ClaimTypes.NameIdentifier,
            RoleClaimType = ClaimTypes.Role,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(chaveJwt))
        };
    });

builder.Services.AddAuthorization();

// ---------------------------------------------------------------------
// Controllers + Serviços
// ---------------------------------------------------------------------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<ReservaService>();

// ---------------------------------------------------------------------
// CORS: lê Cors__Origens (variável de ambiente) ou usa o padrão local do Vite
// ---------------------------------------------------------------------
var origensCors = (builder.Configuration["Cors:Origens"] ?? "http://localhost:5173")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

builder.Services.AddCors(opcoes => opcoes.AddPolicy("Front", politica =>
    politica.WithOrigins(origensCors).AllowAnyHeader().AllowAnyMethod()));

// ---------------------------------------------------------------------
// Tratamento de erros: ProblemDetails (RFC 7807) em toda a API
// ---------------------------------------------------------------------
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ConflitoReservaExceptionHandler>();

// ---------------------------------------------------------------------
// Swagger / Swashbuckle 10 (habilitado também em produção)
// ---------------------------------------------------------------------
builder.Services.AddSwaggerGen(config =>
{
    config.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Coworking API",
        Version = "v1",
        Description = "API do projeto escolar de Desenvolvimento Back-End — nicho Coworking. Autor: Rafael Machado Carôlo Bertuloso."
    });

    config.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Informe apenas o token JWT obtido em POST /api/auth/login."
    });

    // Padrão Swashbuckle 10 (Microsoft.OpenApi 2.x): lambda sobre o documento,
    // usando OpenApiSecuritySchemeReference.
    config.AddSecurityRequirement(documento =>
        new Microsoft.OpenApi.OpenApiSecurityRequirement
        {
            [new Microsoft.OpenApi.OpenApiSecuritySchemeReference("Bearer", documento)] =
                new List<string>()
        });
});

var app = builder.Build();

// ---------------------------------------------------------------------
// Pipeline
// ---------------------------------------------------------------------
app.UseExceptionHandler();
app.UseCors("Front");
app.UseAuthentication();
app.UseAuthorization();

// Swagger habilitado em produção também (fora do if IsDevelopment).
app.UseSwagger();
app.UseSwaggerUI(opcoes =>
{
    opcoes.SwaggerEndpoint("/swagger/v1/swagger.json", "Coworking API v1");
    opcoes.DocumentTitle = "Coworking API";
});

// Redireciona GET / para /swagger
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

// ---------------------------------------------------------------------
// Rotas especiais
// ---------------------------------------------------------------------
app.MapHealthChecks("/health").ExcludeFromDescription();

app.MapGet("/api/versao", () => Results.Ok(new
{
    commit = Environment.GetEnvironmentVariable("RENDER_GIT_COMMIT") ?? "local"
})).ExcludeFromDescription();

app.MapControllers();

// ---------------------------------------------------------------------
// Migrações + Seed idempotente na subida
// ---------------------------------------------------------------------
await app.InicializarBancoAsync();

app.Run();

// Necessário para o projeto de testes referenciar o entry point.
public partial class Program { }
