using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Coworking.Api.Domain;
using Coworking.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Coworking.Tests;

/// <summary>Cobre domínio (entidades) e a autorização baseada em papéis.</summary>
public class DominioEPermissaoTests
{
    [Fact]
    public void Usuario_novo_tem_papel_cliente_por_padrao()
    {
        var usuario = new Usuario { Nome = "Ana", Email = "ana@exemplo.com" };
        Assert.Equal("cliente", usuario.Papel);
    }

    [Fact]
    public void Sala_1_para_N_em_reservas()
    {
        var sala = new Sala { Nome = "Sala X" };
        var reservaA = new Reserva { Sala = sala, SalaId = sala.Id };
        var reservaB = new Reserva { Sala = sala, SalaId = sala.Id };

        sala.Reservas.Add(reservaA);
        sala.Reservas.Add(reservaB);

        Assert.Equal(2, sala.Reservas.Count);
        Assert.Same(sala, reservaA.Sala);
        Assert.Same(sala, reservaB.Sala);
    }

    [Fact]
    public void Token_contem_os_claims_de_identidade_e_papel()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Chave"] = "ChaveDeTesteComMuitosCaracteresParaAssinarHS256DoProjeto!!"
            })
            .Build();

        var servico = new TokenService(config);
        var usuario = new Usuario
        {
            Id = 42, Nome = "Rafael Bertuloso", Email = "rafael@exemplo.com", Papel = "admin"
        };

        var token = servico.GerarToken(usuario);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Contains(jwt.Claims, c => c.Type == ClaimTypes.NameIdentifier && c.Value == "42");
        Assert.Contains(jwt.Claims, c => c.Type == ClaimTypes.Name && c.Value == "Rafael Bertuloso");
        Assert.Contains(jwt.Claims, c => c.Type == ClaimTypes.Role && c.Value == "admin");
    }

    [Fact]
    public void Token_de_cliente_tem_papel_cliente()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Chave"] = "ChaveDeTesteComMuitosCaracteresParaAssinarHS256DoProjeto!!"
            })
            .Build();

        var token = new TokenService(config).GerarToken(
            new Usuario { Id = 7, Nome = "Bruno", Email = "bruno@exemplo.com", Papel = "cliente" });

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.Contains(jwt.Claims, c => c.Type == ClaimTypes.Role && c.Value == "cliente");
    }

    [Fact]
    public void Hash_de_senha_eh_verificado_com_bcrypt()
    {
        var hash = BCrypt.Net.BCrypt.HashPassword("MinhaSenha@123");

        Assert.NotEqual("MinhaSenha@123", hash);
        Assert.True(BCrypt.Net.BCrypt.Verify("MinhaSenha@123", hash));
        Assert.False(BCrypt.Net.BCrypt.Verify("senhaErrada", hash));
    }

    [Fact]
    public async Task Excluir_sala_em_cascata_remove_reservas()
    {
        var db = TesteBanco.CriarContexto();
        try
        {
            var sala = await TesteBanco.NovaSalaAsync(db);
            var usuario = await TesteBanco.NovoUsuarioAsync(db);

            db.Reservas.Add(new Reserva
            {
                SalaId = sala.Id,
                UsuarioId = usuario.Id,
                InicioUtc = DateTime.UtcNow,
                FimUtc = DateTime.UtcNow.AddHours(1),
                CriadoEmUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync();

            db.Salas.Remove(sala);
            await db.SaveChangesAsync();

            Assert.Equal(0, await db.Reservas.CountAsync());
        }
        finally
        {
            await db.Database.GetDbConnection().CloseAsync();
        }
    }
}
