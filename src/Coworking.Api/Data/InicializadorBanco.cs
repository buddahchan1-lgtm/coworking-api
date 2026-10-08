using Coworking.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Coworking.Api.Data;

/// <summary>Aplica migrações e executa o seed idempotente na subida da aplicação.</summary>
public static class InicializadorBanco
{
    public static async Task InicializarBancoAsync(this WebApplication app)
    {
        using var escopo = app.Services.CreateScope();
        var db = escopo.ServiceProvider.GetRequiredService<CoworkingDbContext>();

        // Em produção (Npgsql/Neon) aplicamos as migrações geradas.
        // Nos testes de integração (SQLite) criamos o schema direto.
        if (db.Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
            await db.Database.EnsureCreatedAsync();
        else
            await db.Database.MigrateAsync();

        // ---------------- Seed idempotente ----------------
        // 1 Admin (senha do env Seed__AdminSenha), 2 Clientes e 8 Salas realistas.
        if (!await db.Usuarios.AnyAsync())
        {
            var senhaAdmin = app.Configuration["Seed:AdminSenha"] ?? "Admin@123";

            db.Usuarios.AddRange(
                new Usuario
                {
                    Nome = "Administrador",
                    Email = "admin@coworking.com",
                    HashSenha = BCrypt.Net.BCrypt.HashPassword(senhaAdmin),
                    Papel = "admin"
                },
                new Usuario
                {
                    Nome = "Ana Souza",
                    Email = "ana@exemplo.com",
                    HashSenha = BCrypt.Net.BCrypt.HashPassword("Cliente@123"),
                    Papel = "cliente"
                },
                new Usuario
                {
                    Nome = "Bruno Lima",
                    Email = "bruno@exemplo.com",
                    HashSenha = BCrypt.Net.BCrypt.HashPassword("Cliente@123"),
                    Papel = "cliente"
                });
        }

        if (!await db.Salas.AnyAsync())
        {
            db.Salas.AddRange(
                new Sala
                {
                    Nome = "Sala Aurora",
                    Descricao = "Sala clara para reuniões de equipe até 6 pessoas.",
                    Capacidade = 6,
                    PrecoHora = 45.00m,
                    Recursos = "TV 55, Quadro branco, Wi-Fi 6, Café"
                },
                new Sala
                {
                    Nome = "Sala Horizonte",
                    Descricao = "Espaço amplo para sprints e workshops.",
                    Capacidade = 12,
                    PrecoHora = 80.00m,
                    Recursos = "Projetor 4K, Soundbar, Mesa 12 lugares"
                },
                new Sala
                {
                    Nome = "Sala Órion",
                    Descricao = "Sala executiva com isolamento acústico.",
                    Capacidade = 4,
                    PrecoHora = 60.00m,
                    Recursos = "Videoconferência, Poltronas, Ar-condicionado"
                },
                new Sala
                {
                    Nome = "Sala Borealis",
                    Descricao = "Sala criativa com paredes lousa e puffs.",
                    Capacidade = 8,
                    PrecoHora = 55.00m,
                    Recursos = "Lousa, TV 65, Material de brainstorm"
                },
                new Sala
                {
                    Nome = "Auditório Pilar",
                    Descricao = "Auditório para palestras e eventos internos.",
                    Capacidade = 40,
                    PrecoHora = 180.00m,
                    Recursos = "Palco, Projeção dupla, Microfones"
                },
                new Sala
                {
                    Nome = "Estúdio Vértice",
                    Descricao = "Estúdio para gravações e transmissões.",
                    Capacidade = 5,
                    PrecoHora = 95.00m,
                    Recursos = "Iluminação, Fundo verde, Captura 4K"
                },
                new Sala
                {
                    Nome = "Sala Semente",
                    Descricao = "Espaço compacto ideal para entrevistas e 1:1.",
                    Capacidade = 2,
                    PrecoHora = 25.00m,
                    Recursos = "Mesa redonda, Wi-Fi 6, Silêncio garantido"
                },
                new Sala
                {
                    Nome = "Sala Atlas",
                    Descricao = "Sala de treinamento com computadores.",
                    Capacidade = 15,
                    PrecoHora = 120.00m,
                    Recursos = "15 PCs, Projetor, Quadro interativo"
                });
        }

        await db.SaveChangesAsync();
    }
}
