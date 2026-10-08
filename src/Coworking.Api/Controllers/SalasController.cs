using Coworking.Api.Data;
using Coworking.Api.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Coworking.Api.Controllers;

[ApiController]
[Route("api/salas")]
public class SalasController(CoworkingDbContext db) : ControllerBase
{
    // GET /api/salas?busca=&pagina=1&tamanho=10  (leitura pública, paginada)
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<ListaSalasDto>> Listar(
        [FromQuery] string? busca,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanho = 10)
    {
        pagina = Math.Max(pagina, 1);
        tamanho = Math.Clamp(tamanho, 1, 50);

        var consulta = db.Salas.AsNoTracking().OrderBy(s => s.Nome).AsQueryable();

        if (!string.IsNullOrWhiteSpace(busca))
            consulta = consulta.Where(s => s.Nome.ToLower().Contains(busca.Trim().ToLower()));

        var total = await consulta.CountAsync();
        var itens = await consulta
            .Skip((pagina - 1) * tamanho)
            .Take(tamanho)
            .Select(s => new SalaDto(s.Id, s.Nome, s.Descricao, s.Capacidade, s.PrecoHora, s.Recursos))
            .ToListAsync();

        var totalPaginas = (int)Math.Ceiling(total / (double)tamanho);

        return Ok(new ListaSalasDto(pagina, tamanho, total, totalPaginas, itens));
    }

    // GET /api/salas/{id}  (200 ou 404 ProblemDetails)
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<SalaDto>> Obter(int id)
    {
        var sala = await db.Salas.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);

        if (sala is null)
            return Problem(statusCode: 404, title: "Sala não encontrada",
                detail: $"Não existe sala com o id {id}.");

        return Ok(new SalaDto(sala.Id, sala.Nome, sala.Descricao, sala.Capacidade,
            sala.PrecoHora, sala.Recursos));
    }

    // POST /api/salas  (escrita só admin)
    [HttpPost]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<SalaDto>> Criar(SalaEntrada entrada)
    {
        var sala = new Domain.Sala
        {
            Nome = entrada.Nome.Trim(),
            Descricao = entrada.Descricao,
            Capacidade = entrada.Capacidade,
            PrecoHora = entrada.PrecoHora,
            Recursos = entrada.Recursos
        };

        db.Salas.Add(sala);
        await db.SaveChangesAsync();

        var dto = new SalaDto(sala.Id, sala.Nome, sala.Descricao, sala.Capacidade,
            sala.PrecoHora, sala.Recursos);

        return CreatedAtAction(nameof(Obter), new { id = sala.Id }, dto); // 201 + Location
    }

    // PUT /api/salas/{id}  (escrita só admin)
    [HttpPut("{id:int}")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<SalaDto>> Atualizar(int id, SalaEntrada entrada)
    {
        var sala = await db.Salas.FirstOrDefaultAsync(s => s.Id == id);

        if (sala is null)
            return Problem(statusCode: 404, title: "Sala não encontrada",
                detail: $"Não existe sala com o id {id}.");

        sala.Nome = entrada.Nome.Trim();
        sala.Descricao = entrada.Descricao;
        sala.Capacidade = entrada.Capacidade;
        sala.PrecoHora = entrada.PrecoHora;
        sala.Recursos = entrada.Recursos;

        await db.SaveChangesAsync();

        return Ok(new SalaDto(sala.Id, sala.Nome, sala.Descricao, sala.Capacidade,
            sala.PrecoHora, sala.Recursos));
    }

    // DELETE /api/salas/{id}  (204 No Content; escrita só admin)
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Excluir(int id)
    {
        var sala = await db.Salas.FirstOrDefaultAsync(s => s.Id == id);

        if (sala is null)
            return Problem(statusCode: 404, title: "Sala não encontrada",
                detail: $"Não existe sala com o id {id}.");

        db.Salas.Remove(sala);
        await db.SaveChangesAsync();

        return NoContent();
    }
}
