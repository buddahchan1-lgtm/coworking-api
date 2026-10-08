using Coworking.Api.Data;
using Coworking.Api.Dtos;
using Coworking.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Coworking.Api.Controllers;

[ApiController]
[Route("api/reservas")]
[Authorize] // exige login para tudo
public class ReservasController(CoworkingDbContext db, ReservaService reservaService)
    : ControllerBase
{
    // POST /api/reservas — cria reserva para o usuário logado, com validação 409
    [HttpPost]
    public async Task<IActionResult> Criar(ReservaEntrada entrada)
    {
        var usuarioId = IdDoUsuario();

        try
        {
            var reserva = await reservaService.CriarReservaAsync(
                entrada.SalaId, usuarioId, entrada.Inicio, entrada.Fim);

            var sala = await db.Salas.AsNoTracking().FirstAsync(s => s.Id == reserva.SalaId);

            var dto = new ReservaDto(reserva.Id, reserva.SalaId, sala.Nome,
                new DateTimeOffset(reserva.InicioUtc), new DateTimeOffset(reserva.FimUtc),
                new DateTimeOffset(reserva.CriadoEmUtc));

            return Created($"/api/reservas/{dto.Id}", dto);
        }
        catch (ConflitoReservaException)
        {
            throw; // tratado pelo handler -> 409 ProblemDetails
        }
        catch (KeyNotFoundException)
        {
            throw; // tratado pelo handler -> 404 ProblemDetails
        }
        catch (InvalidOperationException erro)
        {
            return Problem(statusCode: 400, title: "Reserva inválida", detail: erro.Message);
        }
    }

    // GET /api/reservas — lista apenas as reservas do usuário logado
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ReservaDto>>> Listar()
    {
        var usuarioId = IdDoUsuario();

        var reservas = await db.Reservas
            .AsNoTracking()
            .Where(r => r.UsuarioId == usuarioId)
            .OrderBy(r => r.InicioUtc)
            .Select(r => new ReservaDto(
                r.Id,
                r.SalaId,
                r.Sala != null ? r.Sala.Nome : string.Empty,
                new DateTimeOffset(r.InicioUtc),
                new DateTimeOffset(r.FimUtc),
                new DateTimeOffset(r.CriadoEmUtc)))
            .ToListAsync();

        return Ok(reservas);
    }

    // PUT /api/reservas/{id} — só dono da reserva ou admin (403 caso contrário)
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Atualizar(int id, ReservaEntrada entrada)
    {
        var reserva = await db.Reservas.FirstOrDefaultAsync(r => r.Id == id);

        if (reserva is null)
            return Problem(statusCode: 404, title: "Reserva não encontrada",
                detail: $"Não existe reserva com o id {id}.");

        if (reserva.UsuarioId != IdDoUsuario() && !User.IsInRole("admin"))
            return Problem(statusCode: 403, title: "Sem permissão",
                detail: "Você só pode alterar as suas próprias reservas.");

        if (entrada.Fim <= entrada.Inicio)
            return Problem(statusCode: 400, title: "Reserva inválida",
                detail: "A hora final deve ser posterior à hora inicial.");

        await using var transacao = await db.Database.BeginTransactionAsync();

        var inicioUtc = entrada.Inicio.UtcDateTime;
        var fimUtc = entrada.Fim.UtcDateTime;

        // Regra 409: mesma sala + sobreposição de horário (ignorando a própria reserva).
        var conflito = await db.Reservas.AnyAsync(r =>
            r.Id != id &&
            r.SalaId == entrada.SalaId &&
            r.InicioUtc < fimUtc &&
            r.FimUtc > inicioUtc);

        if (conflito)
            throw new ConflitoReservaException(
                "A sala já está reservada nesse horário.");

        reserva.SalaId = entrada.SalaId;
        reserva.InicioUtc = inicioUtc;
        reserva.FimUtc = fimUtc;
        await db.SaveChangesAsync();
        await transacao.CommitAsync();

        var sala = await db.Salas.AsNoTracking().FirstAsync(s => s.Id == reserva.SalaId);

        return Ok(new ReservaDto(reserva.Id, reserva.SalaId, sala.Nome,
            new DateTimeOffset(reserva.InicioUtc), new DateTimeOffset(reserva.FimUtc),
            new DateTimeOffset(reserva.CriadoEmUtc)));
    }

    // DELETE /api/reservas/{id} — só dono da reserva ou admin (403 caso contrário)
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Excluir(int id)
    {
        var reserva = await db.Reservas.FirstOrDefaultAsync(r => r.Id == id);

        if (reserva is null)
            return Problem(statusCode: 404, title: "Reserva não encontrada",
                detail: $"Não existe reserva com o id {id}.");

        if (reserva.UsuarioId != IdDoUsuario() && !User.IsInRole("admin"))
            return Problem(statusCode: 403, title: "Sem permissão",
                detail: "Você só pode excluir as suas próprias reservas.");

        db.Reservas.Remove(reserva);
        await db.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>Id do usuário logado, lido do claim NameIdentifier do JWT.</summary>
    private int IdDoUsuario() =>
        int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? throw new InvalidOperationException("Token sem o claim de identidade."));
}
