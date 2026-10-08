namespace Coworking.Api.Domain;

/// <summary>Usuário do sistema, com papel "admin" ou "cliente".</summary>
public class Usuario
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string HashSenha { get; set; } = string.Empty;
    public string Papel { get; set; } = "cliente"; // "admin" | "cliente"
    public ICollection<Reserva> Reservas { get; set; } = new List<Reserva>();
}

/// <summary>Sala de coworking disponível para reservas.</summary>
public class Sala
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public int Capacidade { get; set; }
    public decimal PrecoHora { get; set; }
    public string Recursos { get; set; } = string.Empty; // ex.: "Projetor, Quadro branco, Wi-Fi 6"
    public ICollection<Reserva> Reservas { get; set; } = new List<Reserva>();
}

/// <summary>Reserva de uma sala feita por um usuário (Sala 1 -> N Reserva).</summary>
public class Reserva
{
    public int Id { get; set; }
    public int SalaId { get; set; }
    public Sala? Sala { get; set; }
    public int UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }

    /// <summary>Início da reserva, sempre em UTC (exigência do Npgsql).</summary>
    public DateTime InicioUtc { get; set; }

    /// <summary>Fim da reserva, sempre em UTC (exigência do Npgsql).</summary>
    public DateTime FimUtc { get; set; }

    /// <summary>Data/hora de criação do registro, em UTC.</summary>
    public DateTime CriadoEmUtc { get; set; }
}
