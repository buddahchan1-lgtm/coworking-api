using Coworking.Api.Data;
using Coworking.Api.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Coworking.Tests;

/// <summary>Cobre a regra central do projeto: conflito de horário => HTTP 409.</summary>
public class RegraConflitoTests : IDisposable
{
    private readonly CoworkingDbContext _db = TesteBanco.CriarContexto();

    public void Dispose() => _db.Database.CloseConnection();

    private static readonly DateTimeOffset Base = new(2026, 3, 10, 14, 0, 0, TimeSpan.Zero); // 14:00 UTC

    [Fact]
    public void Intervalos_que_se_sobrepoem_sao_conflito()
    {
        // Reserva existente: 14:00 -> 15:00
        Assert.True(ReservaService.HaConflitoHorario(
            Base.UtcDateTime, Base.AddHours(1).UtcDateTime,
            Base.UtcDateTime, Base.AddHours(1).UtcDateTime));
    }

    [Fact]
    public void Intervalos_momentos_diferentes_nao_sao_conflito()
    {
        // Nova: 15:00 -> 16:00, existente termina às 15:00 — não há sobreposição.
        Assert.False(ReservaService.HaConflitoHorario(
            Base.AddHours(1).UtcDateTime, Base.AddHours(2).UtcDateTime,
            Base.UtcDateTime, Base.AddHours(1).UtcDateTime));
    }

    [Fact]
    public void Intervalo_envolto_por_reserva_existente_e_conflito()
    {
        // Nova: 14:15 -> 14:45 dentro de 14:00 -> 15:00
        Assert.True(ReservaService.HaConflitoHorario(
            Base.AddMinutes(15).UtcDateTime, Base.AddMinutes(45).UtcDateTime,
            Base.UtcDateTime, Base.AddHours(1).UtcDateTime));
    }

    [Fact]
    public async Task CriarReservaAsync_lanca_ConflitoReservaException_quando_sala_ja_ocupada()
    {
        var sala = await TesteBanco.NovaSalaAsync(_db);
        var usuario = await TesteBanco.NovoUsuarioAsync(_db);
        var servico = new ReservaService(_db);

        // Primeira reserva OK: 14:00 -> 15:00
        await servico.CriarReservaAsync(sala.Id, usuario.Id, Base, Base.AddHours(1));

        // Segunda reserva no MESMO horário deve conflitar (409).
        await Assert.ThrowsAsync<ConflitoReservaException>(() =>
            servico.CriarReservaAsync(sala.Id, usuario.Id, Base.AddMinutes(30), Base.AddMinutes(90)));
    }

    [Fact]
    public async Task CriarReservaAsync_permite_horarios_livres_na_mesma_sala()
    {
        var sala = await TesteBanco.NovaSalaAsync(_db);
        var usuario = await TesteBanco.NovoUsuarioAsync(_db);
        var servico = new ReservaService(_db);

        await servico.CriarReservaAsync(sala.Id, usuario.Id, Base, Base.AddHours(1));

        var reserva = await servico.CriarReservaAsync(
            sala.Id, usuario.Id, Base.AddHours(2), Base.AddHours(3));

        Assert.Equal(2, await _db.Reservas.CountAsync());
        Assert.True(reserva.Id > 0);
    }

    [Fact]
    public async Task CriarReservaAsync_aceita_mesmo_horario_em_salas_diferentes()
    {
        var sala1 = await TesteBanco.NovaSalaAsync(_db, "Sala A");
        var sala2 = await TesteBanco.NovaSalaAsync(_db, "Sala B");
        var usuario = await TesteBanco.NovoUsuarioAsync(_db);
        var servico = new ReservaService(_db);

        await servico.CriarReservaAsync(sala1.Id, usuario.Id, Base, Base.AddHours(1));

        // Mesma sala? Não. Mesmo horário? Sim. Não é conflito: salas são independentes.
        var reserva = await servico.CriarReservaAsync(sala2.Id, usuario.Id, Base, Base.AddHours(1));
        Assert.Equal(sala2.Id, reserva.SalaId);
    }

    [Fact]
    public async Task CriarReservaAsync_rejeita_fim_antes_do_inicio()
    {
        var sala = await TesteBanco.NovaSalaAsync(_db);
        var usuario = await TesteBanco.NovoUsuarioAsync(_db);
        var servico = new ReservaService(_db);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            servico.CriarReservaAsync(sala.Id, usuario.Id, Base.AddHours(2), Base));
    }

    [Fact]
    public async Task CriarReservaAsync_para_sala_inexistente_lanca_KeyNotFound()
    {
        var usuario = await TesteBanco.NovoUsuarioAsync(_db);
        var servico = new ReservaService(_db);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            servico.CriarReservaAsync(9999, usuario.Id, Base, Base.AddHours(1)));
    }
}
