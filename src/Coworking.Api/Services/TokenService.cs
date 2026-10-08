using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Coworking.Api.Domain;
using Microsoft.IdentityModel.Tokens;

namespace Coworking.Api.Services;

/// <summary>Gera tokens JWT cujos claims carregam a identidade (NameIdentifier + Role).</summary>
public class TokenService(IConfiguration configuracao)
{
    public string GerarToken(Usuario usuario)
    {
        var chave = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(configuracao["Jwt:Chave"]!));

        var credenciais = new SigningCredentials(chave, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new(ClaimTypes.Name, usuario.Nome),
            new(ClaimTypes.Role, usuario.Papel)
        };

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddHours(12),
            signingCredentials: credenciais);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
