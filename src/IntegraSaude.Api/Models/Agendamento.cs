namespace IntegraSaude.Api.Models;

public class Agendamento
{
    public Guid Id { get; set; }
    public Guid PacienteId { get; set; }
    public Paciente Paciente { get; set; } = null!;
    public DateTime DataHora { get; set; }
    public string? Observacao { get; set; }
    public string Status { get; set; } = "agendado";
    public string? CriadoPorId { get; set; }
}
