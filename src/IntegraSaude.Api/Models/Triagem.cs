namespace IntegraSaude.Api.Models;

public class Triagem
{
    public Guid Id { get; set; }
    public Guid AtendimentoId { get; set; }
    public Atendimento Atendimento { get; set; } = null!;
    public int? PressaoSistolica { get; set; }
    public int? PressaoDiastolica { get; set; }
    public decimal? Temperatura { get; set; }
    public decimal? Glicemia { get; set; }
    public decimal? Saturacao { get; set; }
    public decimal? Peso { get; set; }
    public Manchester Classificacao { get; set; }
    public string? Justificativa { get; set; }
    public string EnfermeiroId { get; set; } = string.Empty;
    public DateTime RealizadaEm { get; set; } = DateTime.UtcNow;
}
