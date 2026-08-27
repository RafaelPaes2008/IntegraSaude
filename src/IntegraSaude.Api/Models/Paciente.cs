namespace IntegraSaude.Api.Models;

public class Paciente
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Cpf { get; set; } = string.Empty;
    public string? CartaoSus { get; set; }
    public string? Telefone { get; set; }
    public string? Endereco { get; set; }
    public DateOnly? DataNascimento { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    public ICollection<Atendimento> Atendimentos { get; set; } = [];
    public ICollection<Agendamento> Agendamentos { get; set; } = [];
}
