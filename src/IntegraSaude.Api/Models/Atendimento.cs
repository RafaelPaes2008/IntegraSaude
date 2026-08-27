namespace IntegraSaude.Api.Models;

public class Atendimento
{
    public Guid Id { get; set; }
    public Guid PacienteId { get; set; }
    public Paciente Paciente { get; set; } = null!;
    public string Senha { get; set; } = string.Empty;
    public AtendimentoStatus Status { get; set; } = AtendimentoStatus.Aguardando;
    public DateTime ChegadaEm { get; set; } = DateTime.UtcNow;
    public string? RecepcionistaId { get; set; }

    public Triagem? Triagem { get; set; }
    public Consulta? Consulta { get; set; }
}
