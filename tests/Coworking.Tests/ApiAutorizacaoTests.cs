using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Coworking.Api;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Coworking.Tests;

/// <summary>
/// Testes de integração de rotas/autorização: sobem a API real com banco
/// SQLite em arquivo temporário (basta a connection string apontar para
/// "Data Source=..."), exercitando o pipeline completo (JWT, 403, 404, 409).
/// </summary>
public class ApiAutorizacaoTests : IClassFixture<ApiAutorizacaoTests.FabricaApi>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary>Fábrica compartilhada: sobe a API uma única vez para a classe de testes.</summary>
    public class FabricaApi : WebApplicationFactory<Program>
    {
        public FabricaApi()
        {
            CaminhoBanco = Path.Combine(Path.GetTempPath(), $"coworking_test_{Guid.NewGuid():N}.db");
        }

        public string CaminhoBanco { get; }

        protected override void ConfigureWebHost(IWebHostBuilder construtor)
        {
            // Connection string em formato SQLite => Program.cs usa UseSqlite
            // e o InicializadorBanco cria o schema com EnsureCreated + seed.
            construtor.UseSetting("ConnectionStrings:Padrao", $"Data Source={CaminhoBanco}");
            construtor.UseSetting("Seed:AdminSenha", "Admin@123");
        }

        public override async ValueTask DisposeAsync()
        {
            await base.DisposeAsync();
            if (File.Exists(CaminhoBanco)) File.Delete(CaminhoBanco);
        }
    }

    private readonly FabricaApi _fabrica;

    public ApiAutorizacaoTests(FabricaApi fabrica) => _fabrica = fabrica;

    private HttpClient NovoCliente() => _fabrica.CreateClient();

    private async Task<string> LoginAsync(HttpClient cliente, string email, string senha)
    {
        var resposta = await cliente.PostAsJsonAsync("/api/auth/login", new { email, senha });
        resposta.EnsureSuccessStatusCode();
        var corpo = await resposta.Content.ReadFromJsonAsync<RespostaAuth>(Json);
        return corpo!.Token;
    }

    private async Task<string> RegistrarAsync(HttpClient cliente, string nome, string email, string senha)
    {
        var resposta = await cliente.PostAsJsonAsync("/api/auth/registro", new { nome, email, senha });
        resposta.EnsureSuccessStatusCode();
        var corpo = await resposta.Content.ReadFromJsonAsync<RespostaAuth>(Json);
        return corpo!.Token;
    }

    private static void UsarToken(HttpClient cliente, string token) =>
        cliente.DefaultRequestHeaders.Authorization = new("Bearer", token);

    [Fact]
    public async Task GET_salas_e_publico_e_retorna_200_com_paginacao()
    {
        var cliente = NovoCliente();

        var resposta = await cliente.GetAsync("/api/salas?pagina=1&tamanho=5");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var corpo = await resposta.Content.ReadFromJsonAsync<ListaSalasDto>(Json);
        Assert.True(corpo!.Total >= 8);        // o seed cria 8 salas
        Assert.True(corpo.TotalPaginas >= 2);  // 8 salas / tamanho 5
        Assert.True(corpo.Itens.Count <= 5);
    }

    [Fact]
    public async Task GET_salas_com_busca_filtra_por_nome()
    {
        var cliente = NovoCliente();

        var resposta = await cliente.GetAsync("/api/salas?busca=aurora");

        resposta.EnsureSuccessStatusCode();
        var corpo = await resposta.Content.ReadFromJsonAsync<ListaSalasDto>(Json);
        Assert.True(corpo!.Itens.Count > 0);
        Assert.True(corpo.Itens.All(s => s.Nome.Contains("Aurora", StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public async Task POST_salas_sem_token_retorna_401()
    {
        var cliente = NovoCliente();

        var resposta = await cliente.PostAsJsonAsync("/api/salas",
            new { nome = "Sala Hacker", capacidade = 5, precoHora = 10m, recursos = "" });

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task POST_salas_com_cliente_retorna_403()
    {
        var cliente = NovoCliente();
        var token = await RegistrarAsync(cliente, "Cliente Comum", "cliente.comum@teste.com", "Cliente@123");
        UsarToken(cliente, token);

        var resposta = await cliente.PostAsJsonAsync("/api/salas",
            new { nome = "Sala Proibida", capacidade = 5, precoHora = 10m, recursos = "" });

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode); // [Authorize(Roles="admin")]
    }

    [Fact]
    public async Task POST_salas_com_admin_retorna_201_com_location()
    {
        var cliente = NovoCliente();
        UsarToken(cliente, await LoginAsync(cliente, "admin@coworking.com", "Admin@123"));

        var resposta = await cliente.PostAsJsonAsync("/api/salas",
            new { nome = "Sala do Admin", capacidade = 10, precoHora = 99.5m, recursos = "Projetor" });

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        Assert.NotNull(resposta.Headers.Location); // header Location do 201
    }

    [Fact]
    public async Task GET_sala_inexistente_retorna_404_problem_details()
    {
        var cliente = NovoCliente();

        var resposta = await cliente.GetAsync("/api/salas/99999");

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
        var corpo = await resposta.Content.ReadAsStringAsync();
        Assert.Contains("não encontrada", corpo); // ProblemDetails
    }

    [Fact]
    public async Task GET_reservas_sem_token_retorna_401()
    {
        var cliente = NovoCliente();

        var resposta = await cliente.GetAsync("/api/reservas");

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task Criar_e_listar_reserva_do_usuario_logado_funciona()
    {
        var cliente = NovoCliente();
        UsarToken(cliente, await RegistrarAsync(cliente, "Cliente Reserva", "cliente.reserva@teste.com", "Cliente@123"));

        // Localiza uma sala do seed.
        var salas = await cliente.GetFromJsonAsync<ListaSalasDto>("/api/salas?tamanho=1", Json);
        var salaId = salas!.Itens[0].Id;

        var inicio = new DateTimeOffset(2026, 6, 1, 13, 0, 0, TimeSpan.Zero);
        var fim = new DateTimeOffset(2026, 6, 1, 14, 0, 0, TimeSpan.Zero);

        var criacao = await cliente.PostAsJsonAsync("/api/reservas",
            new { salaId, inicio, fim });

        Assert.Equal(HttpStatusCode.Created, criacao.StatusCode);

        var lista = await cliente.GetFromJsonAsync<List<ReservaDto>>("/api/reservas", Json);
        Assert.Contains(lista!, r => r.SalaId == salaId && r.Inicio == inicio);
    }

    [Fact]
    public async Task Reserva_em_horario_ja_ocupado_retorna_409()
    {
        var cliente = NovoCliente();
        UsarToken(cliente, await RegistrarAsync(cliente, "Cliente 409", "cliente.409@teste.com", "Cliente@123"));

        var salas = await cliente.GetFromJsonAsync<ListaSalasDto>("/api/salas?tamanho=1", Json);
        var salaId = salas!.Itens[0].Id;

        var inicio = new DateTimeOffset(2026, 6, 2, 9, 0, 0, TimeSpan.Zero);
        var fim = new DateTimeOffset(2026, 6, 2, 10, 0, 0, TimeSpan.Zero);

        await cliente.PostAsJsonAsync("/api/reservas", new { salaId, inicio, fim });

        var conflito = await cliente.PostAsJsonAsync("/api/reservas",
            new { salaId, inicio = inicio.AddMinutes(30), fim = fim.AddMinutes(30) });

        Assert.Equal(HttpStatusCode.Conflict, conflito.StatusCode); // 409

        var corpo = await conflito.Content.ReadAsStringAsync();
        Assert.Contains("reservada", corpo); // mensagem limpa em ProblemDetails
    }

    [Fact]
    public async Task Rotas_especiais_health_e_versao_funcionam()
    {
        var cliente = NovoCliente();

        var health = await cliente.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, health.StatusCode);

        var versao = await cliente.GetAsync("/api/versao");
        Assert.Equal(HttpStatusCode.OK, versao.StatusCode);
        var corpo = await versao.Content.ReadFromJsonAsync<JsonElement>(Json);
        Assert.Equal("local", corpo.GetProperty("commit").GetString());
    }

    private record RespostaAuth(string Token, string Nome, string Email, string Papel);
    private record ListaSalasDto(int Pagina, int Tamanho, int Total, int TotalPaginas, List<SalaDto> Itens);
    private record SalaDto(int Id, string Nome);
    private record ReservaDto(int Id, int SalaId, string SalaNome, DateTimeOffset Inicio, DateTimeOffset Fim, DateTimeOffset CriadoEm);
}
