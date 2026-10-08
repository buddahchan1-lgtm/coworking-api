using Coworking.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Coworking.Api.Data;

public class CoworkingDbContext(DbContextOptions<CoworkingDbContext> opcoes)
    : DbContext(opcoes)
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Sala> Salas => Set<Sala>();
    public DbSet<Reserva> Reservas => Set<Reserva>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Usuario>(entidade =>
        {
            entidade.HasIndex(u => u.Email).IsUnique();
            entidade.Property(u => u.Nome).HasMaxLength(120).IsRequired();
            entidade.Property(u => u.Email).HasMaxLength(180).IsRequired();
            entidade.Property(u => u.HashSenha).HasMaxLength(200).IsRequired();
            entidade.Property(u => u.Papel).HasMaxLength(20).IsRequired();
        });

        modelBuilder.Entity<Sala>(entidade =>
        {
            entidade.Property(s => s.Nome).HasMaxLength(120).IsRequired();
            entidade.Property(s => s.Descricao).HasMaxLength(600);
            entidade.Property(s => s.Recursos).HasMaxLength(300);
            entidade.Property(s => s.PrecoHora).HasPrecision(10, 2);
            entidade.HasIndex(s => s.Nome);
        });

        modelBuilder.Entity<Reserva>(entidade =>
        {
            entidade.Property(r => r.InicioUtc).IsRequired();
            entidade.Property(r => r.FimUtc).IsRequired();
            entidade.Property(r => r.CriadoEmUtc).IsRequired();

            // Relação 1 para N: uma Sala -> muitas Reservas.
            entidade.HasOne(r => r.Sala)
                .WithMany(s => s.Reservas)
                .HasForeignKey(r => r.SalaId)
                .OnDelete(DeleteBehavior.Cascade);

            entidade.HasOne(r => r.Usuario)
                .WithMany(u => u.Reservas)
                .HasForeignKey(r => r.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);

            // Índice que sustenta a regra de conflito (mesma sala / mesmo horário).
            entidade.HasIndex(r => new { r.SalaId, r.InicioUtc, r.FimUtc });
        });
    }
}
