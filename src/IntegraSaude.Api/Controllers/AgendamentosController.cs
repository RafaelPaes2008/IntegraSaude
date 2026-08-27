using IntegraSaude.Api.Data;
using IntegraSaude.Api.Dtos;
using IntegraSaude.Api.Models;
using IntegraSaude.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IntegraSaude.Api.Controllers;

[ApiController]
[Route("api/agendamentos")]
[Authorize(Roles = $"{AppRoles.Recepcionista},{AppRoles.Admin}")]
public class AgendamentosController(AppDbContext db, AuditService audit) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AgendamentoDto>>> DoDia([FromQuery] DateTime? data)
    {
        var dia = (data ?? DateTime.UtcNow).Date;
        var fim = dia.AddDays(1);
        var list = await db.Agendamentos.Include(a => a.Paciente)
            .Where(a => a.DataHora >= dia && a.DataHora < fim)
            .OrderBy(a => a.DataHora)
            .ToListAsync();
        return Ok(list.Select(a => new AgendamentoDto(a.Id, a.PacienteId, a.Paciente.Nome, a.DataHora, a.Observacao, a.Status)));
    }

    [HttpPost]
    public async Task<ActionResult<AgendamentoDto>> Criar(AgendamentoRequest request)
    {
        var paciente = await db.Pacientes.FindAsync(request.PacienteId);
        if (paciente is null) return NotFound(new { message = "Paciente não encontrado." });

        var ag = new Agendamento
        {
            Id = Guid.NewGuid(),
            PacienteId = request.PacienteId,
            DataHora = DateTime.SpecifyKind(request.DataHora, DateTimeKind.Utc),
            Observacao = request.Observacao,
            CriadoPorId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
        };
        db.Agendamentos.Add(ag);
        await db.SaveChangesAsync();
        await audit.LogAsync(User.Identity?.Name, "agendar", "Agendamento", ag.Id.ToString());
        return Ok(new AgendamentoDto(ag.Id, paciente.Id, paciente.Nome, ag.DataHora, ag.Observacao, ag.Status));
    }

    [HttpPost("{id:guid}/compareceu")]
    public async Task<IActionResult> Compareceu(Guid id)
    {
        var ag = await db.Agendamentos.FindAsync(id);
        if (ag is null) return NotFound();
        ag.Status = "compareceu";
        await db.SaveChangesAsync();
        return NoContent();
    }
}
