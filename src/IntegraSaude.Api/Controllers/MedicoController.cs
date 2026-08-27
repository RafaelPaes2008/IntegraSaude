using IntegraSaude.Api.Data;
using IntegraSaude.Api.Dtos;
using IntegraSaude.Api.Models;
using IntegraSaude.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IntegraSaude.Api.Controllers;

[ApiController]
[Route("api/medico")]
[Authorize(Roles = $"{AppRoles.Medico},{AppRoles.Admin}")]
public class MedicoController(AppDbContext db, AuditService audit) : ControllerBase
{
    [HttpGet("fila")]
    public async Task<ActionResult<IEnumerable<AtendimentoDto>>> Fila()
    {
        var list = await db.Atendimentos
            .Include(a => a.Paciente)
            .Include(a => a.Triagem)
            .Where(a => a.Status == AtendimentoStatus.Triado || a.Status == AtendimentoStatus.EmConsulta)
            .ToListAsync();

        var ordered = list
            .OrderBy(a => a.Triagem?.Classificacao ?? Manchester.Azul)
            .ThenBy(a => a.ChegadaEm)
            .Select(a => AtendimentosController.Map(a, a.Paciente, a.Triagem));
        return Ok(ordered);
    }

    [HttpPost("{id:guid}/iniciar")]
    public async Task<ActionResult<ConsultaDto>> Iniciar(Guid id)
    {
        var at = await db.Atendimentos.Include(a => a.Consulta).FirstOrDefaultAsync(a => a.Id == id);
        if (at is null) return NotFound();
        if (at.Status == AtendimentoStatus.Finalizado)
            return Conflict(new { message = "Consulta já finalizada." });
        if (at.Status != AtendimentoStatus.Triado && at.Status != AtendimentoStatus.EmConsulta)
            return Conflict(new { message = "Paciente ainda não foi triado." });

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";
        at.Status = AtendimentoStatus.EmConsulta;
        if (at.Consulta is null)
        {
            at.Consulta = new Consulta
            {
                Id = Guid.NewGuid(),
                AtendimentoId = at.Id,
                MedicoId = userId,
                InicioEm = DateTime.UtcNow
            };
            db.Consultas.Add(at.Consulta);
        }

        await db.SaveChangesAsync();
        await audit.LogAsync(User.Identity?.Name, "iniciar_consulta", "Consulta", at.Consulta.Id.ToString());
        return Ok(Map(at.Consulta));
    }

    [HttpGet("{id:guid}/consulta")]
    public async Task<ActionResult<ConsultaDto>> Obter(Guid id)
    {
        var at = await db.Atendimentos.Include(a => a.Consulta).FirstOrDefaultAsync(a => a.Id == id);
        if (at?.Consulta is null) return NotFound();
        return Ok(Map(at.Consulta));
    }

    [HttpPut("{id:guid}/consulta")]
    public async Task<ActionResult<ConsultaDto>> Salvar(Guid id, ConsultaSalvarRequest request)
    {
        var at = await db.Atendimentos.Include(a => a.Consulta).FirstOrDefaultAsync(a => a.Id == id);
        if (at?.Consulta is null) return NotFound(new { message = "Inicie a consulta primeiro." });
        if (at.Status == AtendimentoStatus.Finalizado)
            return Conflict(new { message = "Consulta já finalizada." });

        Apply(at.Consulta, request);
        await db.SaveChangesAsync();
        return Ok(Map(at.Consulta));
    }

    [HttpPost("{id:guid}/finalizar")]
    public async Task<ActionResult<ConsultaDto>> Finalizar(Guid id, ConsultaSalvarRequest request)
    {
        var at = await db.Atendimentos.Include(a => a.Consulta).FirstOrDefaultAsync(a => a.Id == id);
        if (at is null) return NotFound();
        if (at.Consulta is null)
            return Conflict(new { message = "Inicie a consulta antes de finalizar." });
        if (string.IsNullOrWhiteSpace(request.Diagnostico))
            return BadRequest(new { message = "Informe o diagnóstico para encerrar." });

        Apply(at.Consulta, request);
        at.Consulta.FimEm = DateTime.UtcNow;
        at.Status = AtendimentoStatus.Finalizado;
        await db.SaveChangesAsync();
        await audit.LogAsync(User.Identity?.Name, "finalizar_consulta", "Consulta", at.Consulta.Id.ToString(), request.Diagnostico);
        return Ok(Map(at.Consulta));
    }

    private static void Apply(Consulta c, ConsultaSalvarRequest r)
    {
        c.QueixaPrincipal = r.QueixaPrincipal;
        c.HistoriaDoencaAtual = r.HistoriaDoencaAtual;
        c.ExameFisico = r.ExameFisico;
        c.Orientacoes = r.Orientacoes;
        c.Anamnese = r.Anamnese;
        c.Diagnostico = r.Diagnostico;
        c.Cid = r.Cid;
        c.Prescricao = r.Prescricao;
    }

    private static ConsultaDto Map(Consulta c) => new(
        c.Id, c.AtendimentoId, c.QueixaPrincipal, c.HistoriaDoencaAtual, c.ExameFisico, c.Orientacoes,
        c.Anamnese, c.Diagnostico, c.Cid, c.Prescricao, c.InicioEm, c.FimEm);
}
