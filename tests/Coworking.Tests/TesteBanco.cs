using Coworking.Api.Data;
using Coworking.Api.Domain;
using Coworking.Api.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Coworking.Tests;

/// <summary>Fábrica de contexto EF Core em memória SQLite para os testes.</summary>
public static class TesteBanco
{
    public static CoworkingDbContext CriarContexto()
    {
        var opcoes = new DbContextOptionsBuilder<CoworkingDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        var db = new CoworkingDbContext(opcoes);
        db.Database.OpenConnection();
        db.Database.EnsureCreated();
        return db;
    }

    public static async Task<Sala> NovaSalaAsync(CoworkingDbContext db, string nome = "Sala Teste")
    {
        var sala = new Sala
        {
            Nome = nome,
            Descricao = "Sala de teste",
            Capacidade = 4,
            PrecoHora = 30m,
            Recursos = "Wi-Fi"
        };
        db.Salas.Add(sala);
        await db.SaveChangesAsync();
        return sala;
    }

    public static async Task<Usuario> NovoUsuarioAsync(CoworkingDbContext db,
        string nome = "Cliente Teste", string papel = "cliente")
    {
        var usuario = new Usuario
        {
            Nome = nome,
            Email = $"{nome.ToLower().Replace(" ", ".")}.{Guid.NewGuid():N}@exemplo.com",
            HashSenha = BCrypt.Net.BCrypt.HashPassword("Senha@123"),
            Papel = papel
        };
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();
        return usuario;
    }
}
