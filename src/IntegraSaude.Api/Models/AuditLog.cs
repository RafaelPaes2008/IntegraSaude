namespace IntegraSaude.Api.Models;

public class AuditLog
{
    public Guid Id { get; set; }
    public string UsuarioId { get; set; } = string.Empty;
    public string Acao { get; set; } = string.Empty;
    public string Entidade { get; set; } = string.Empty;
    public string? EntidadeId { get; set; }
    public string? Detalhes { get; set; }
    public DateTime Em { get; set; } = DateTime.UtcNow;
}
