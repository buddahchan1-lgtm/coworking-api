using System.Security.Claims;
using Coworking.Api.Data;
using Coworking.Api.Domain;
using Coworking.Api.Dtos;
using Coworking.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Coworking.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(CoworkingDbContext db, TokenService tokenService)
    : ControllerBase
{
    // POST /api/auth/registro
    [HttpPost("registro")]
    [AllowAnonymous]
    public async Task<ActionResult<RespostaAuth>> Registrar(RequisicaoRegistro entrada)
    {
        var email = entrada.Email.Trim().ToLowerInvariant();

        var existe = await db.Usuarios.AnyAsync(u => u.Email == email);
        if (existe)
            return Conflict(new { title = "E-mail já cadastrado", detail = $"O e-mail {email} já possui conta." });

        var usuario = new Usuario
        {
            Nome = entrada.Nome.Trim(),
            Email = email,
            HashSenha = BCrypt.Net.BCrypt.HashPassword(entrada.Senha),
            Papel = "cliente" // auto-cadastro é sempre cliente; admin é criado via seed
        };

        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();

        return Ok(new RespostaAuth(tokenService.GerarToken(usuario), usuario.Nome, usuario.Email, usuario.Papel));
    }

    // POST /api/auth/login
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<RespostaAuth>> Login(RequisicaoLogin entrada)
    {
        var email = entrada.Email.Trim().ToLowerInvariant();
        var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Email == email);

        if (usuario is null || !BCrypt.Net.BCrypt.Verify(entrada.Senha, usuario.HashSenha))
            return Problem(statusCode: 401, title: "Credenciais inválidas", detail: "E-mail ou senha incorretos.");

        return Ok(new RespostaAuth(tokenService.GerarToken(usuario), usuario.Nome, usuario.Email, usuario.Papel));
    }

    /// <summary>Id do usuário logado, lido do claim NameIdentifier do JWT.</summary>
    public static int IdDoUsuario(ClaimsPrincipal usuario) =>
        int.Parse(usuario.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Token sem o claim de identidade."));
}
