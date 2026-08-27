using IntegraSaude.Api.Data;
using IntegraSaude.Api.Dtos;
using IntegraSaude.Api.Models;
using IntegraSaude.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IntegraSaude.Api.Controllers;

[ApiController]
[Route("api/triagem")]
[Authorize(Roles = $"{AppRoles.Enfermagem},{AppRoles.Admin}")]
public class TriagemController(AppDbContext db, AuditService audit) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<AtendimentoDto>> Registrar(TriagemRequest request)
    {
        var at = await db.Atendimentos.Include(a => a.Paciente).Include(a => a.Triagem)
            .FirstOrDefaultAsync(a => a.Id == request.AtendimentoId);
        if (at is null) return NotFound(new { message = "Atendimento não encontrado." });
        if (at.Status is AtendimentoStatus.Triado or AtendimentoStatus.EmConsulta or AtendimentoStatus.Finalizado)
            return Conflict(new { message = "Este atendimento já foi triado." });

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";
        if (at.Triagem is null)
        {
            at.Triagem = new Triagem { Id = Guid.NewGuid(), AtendimentoId = at.Id };
            db.Triagens.Add(at.Triagem);
        }

        at.Triagem.PressaoSistolica = request.PressaoSistolica;
        at.Triagem.PressaoDiastolica = request.PressaoDiastolica;
        at.Triagem.Temperatura = request.Temperatura;
        at.Triagem.Glicemia = request.Glicemia;
        at.Triagem.Saturacao = request.Saturacao;
        at.Triagem.Peso = request.Peso;
        at.Triagem.Classificacao = request.Classificacao;
        at.Triagem.Justificativa = request.Justificativa;
        at.Triagem.EnfermeiroId = userId;
        at.Triagem.RealizadaEm = DateTime.UtcNow;
        at.Status = AtendimentoStatus.Triado;

        await db.SaveChangesAsync();
        await audit.LogAsync(User.Identity?.Name, "triagem", "Atendimento", at.Id.ToString(), request.Classificacao.ToString());
        return Ok(AtendimentosController.Map(at, at.Paciente, at.Triagem));
    }
}
