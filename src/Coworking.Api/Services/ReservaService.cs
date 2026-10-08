using Coworking.Api.Data;
using Coworking.Api.Domain;
using Coworking.Api.Dtos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;

namespace Coworking.Api.Services;

/// <summary>
/// Exceção de negócio: a sala já possui reserva no mesmo horário (HTTP 409).
/// </summary>
public class ConflitoReservaException(string mensagem) : Exception(mensagem);

/// <summary>
/// Regras do negócio de reservas. A verificação de conflito e a gravação
/// acontecem dentro de uma MESMA transação de banco, evitando reservas
/// duplicadas em condições de corrida (o índice de banco é a última barreira).
/// </summary>
public class ReservaService(CoworkingDbContext db)
{
    /// <summary>Verifica sobreposição: inicio &lt; existente.Fim &amp;&amp; fim &gt; existente.Inicio.</summary>
    public static bool HaConflitoHorario(DateTime inicioUtc, DateTime fimUtc,
        DateTime existenteInicioUtc, DateTime existenteFimUtc)
        => inicioUtc < existenteFimUtc && fimUtc > existenteInicioUtc;

    /// <summary>Cria uma reserva para o usuário, validando conflito de horário (409).</summary>
    public async Task<Reserva> CriarReservaAsync(int salaId, int usuarioId,
        DateTimeOffset inicio, DateTimeOffset fim)
    {
        if (fim <= inicio)
            throw new InvalidOperationException("A hora final deve ser posterior à hora inicial.");

        await using var transacao = await db.Database.BeginTransactionAsync();

        var sala = await db.Salas.FindAsync(salaId)
            ?? throw new KeyNotFoundException($"Sala {salaId} não encontrada.");

        var inicioUtc = inicio.UtcDateTime;
        var fimUtc = fim.UtcDateTime;

        var ocupada = await db.Reservas
            .AnyAsync(r => r.SalaId == salaId &&
                           r.InicioUtc < fimUtc &&
                           r.FimUtc > inicioUtc);

        if (ocupada)
            throw new ConflitoReservaException(
                $"A sala \"{sala.Nome}\" já está reservada nesse horário.");

        var reserva = new Reserva
        {
            SalaId = salaId,
            UsuarioId = usuarioId,
            InicioUtc = inicioUtc,
            FimUtc = fimUtc,
            CriadoEmUtc = DateTime.UtcNow
        };

        db.Reservas.Add(reserva);
        await db.SaveChangesAsync();
        await transacao.CommitAsync();

        return reserva;
    }
}
