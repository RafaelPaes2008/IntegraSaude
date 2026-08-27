namespace IntegraSaude.Api.Models;

public class Consulta
{
    public Guid Id { get; set; }
    public Guid AtendimentoId { get; set; }
    public Atendimento Atendimento { get; set; } = null!;
    public bool QueixaPrincipal { get; set; }
    public bool HistoriaDoencaAtual { get; set; }
    public bool ExameFisico { get; set; }
    public bool Orientacoes { get; set; }
    public string? Anamnese { get; set; }
    public string? Diagnostico { get; set; }
    public string? Cid { get; set; }
    public string? Prescricao { get; set; }
    public DateTime? InicioEm { get; set; }
    public DateTime? FimEm { get; set; }
    public string MedicoId { get; set; } = string.Empty;
}
