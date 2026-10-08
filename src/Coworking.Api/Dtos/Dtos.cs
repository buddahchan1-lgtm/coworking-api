using System.Text.Json.Serialization;

namespace Coworking.Api.Dtos;

// -------------------------------------------------------------------------
// DTOs usam DateTimeOffset e são convertidos para/de UTC nas bordas da API.
// (Npgsql exige DateTime UTC no banco; o cliente envia datas com offset.)
// -------------------------------------------------------------------------

// ------------------------------ Auth ------------------------------
public record RequisicaoRegistro(string Nome, string Email, string Senha);

public record RequisicaoLogin(string Email, string Senha);

public record RespostaAuth(
    string Token,
    string Nome,
    string Email,
    [property: JsonPropertyName("papel")] string Papel);

// ------------------------------ Salas ------------------------------
public record SalaDto(
    int Id,
    string Nome,
    string? Descricao,
    int Capacidade,
    decimal PrecoHora,
    string Recursos);

public record SalaEntrada(
    string Nome,
    string? Descricao,
    int Capacidade,
    decimal PrecoHora,
    string Recursos);

public record ListaSalasDto(
    int Pagina,
    int Tamanho,
    int Total,
    int TotalPaginas,
    IReadOnlyList<SalaDto> Itens);

// ------------------------------ Reservas ------------------------------
public record ReservaEntrada(int SalaId, DateTimeOffset Inicio, DateTimeOffset Fim);

public record ReservaDto(
    int Id,
    int SalaId,
    string SalaNome,
    DateTimeOffset Inicio,
    DateTimeOffset Fim,
    DateTimeOffset CriadoEm);
