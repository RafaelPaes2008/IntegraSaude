using IntegraSaude.Api.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace IntegraSaude.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Paciente> Pacientes => Set<Paciente>();
    public DbSet<Atendimento> Atendimentos => Set<Atendimento>();
    public DbSet<Agendamento> Agendamentos => Set<Agendamento>();
    public DbSet<Triagem> Triagens => Set<Triagem>();
    public DbSet<Consulta> Consultas => Set<Consulta>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Paciente>(e =>
        {
            e.HasIndex(p => p.Cpf).IsUnique();
            e.Property(p => p.Nome).HasMaxLength(160).IsRequired();
            e.Property(p => p.Cpf).HasMaxLength(11).IsRequired();
            e.Property(p => p.CartaoSus).HasMaxLength(20);
            e.Property(p => p.Telefone).HasMaxLength(20);
            e.Property(p => p.Endereco).HasMaxLength(240);
        });

        builder.Entity<Atendimento>(e =>
        {
            e.HasIndex(a => a.Senha);
            e.HasIndex(a => a.Status);
            e.HasOne(a => a.Triagem).WithOne(t => t.Atendimento).HasForeignKey<Triagem>(t => t.AtendimentoId);
            e.HasOne(a => a.Consulta).WithOne(c => c.Atendimento).HasForeignKey<Consulta>(c => c.AtendimentoId);
        });

        builder.Entity<Triagem>(e =>
        {
            e.Property(t => t.Temperatura).HasPrecision(4, 1);
            e.Property(t => t.Glicemia).HasPrecision(6, 1);
            e.Property(t => t.Saturacao).HasPrecision(5, 1);
            e.Property(t => t.Peso).HasPrecision(6, 2);
        });
    }
}
