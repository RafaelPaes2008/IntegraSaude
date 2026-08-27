using IntegraSaude.Api.Data;
using IntegraSaude.Api.Models;

namespace IntegraSaude.Api.Services;

public class AuditService(AppDbContext db)
{
    public async Task LogAsync(string? userId, string acao, string entidade, string? entidadeId, string? detalhes = null)
    {
        db.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            UsuarioId = userId ?? "anonimo",
            Acao = acao,
            Entidade = entidade,
            EntidadeId = entidadeId,
            Detalhes = detalhes
        });
        await db.SaveChangesAsync();
    }
}
