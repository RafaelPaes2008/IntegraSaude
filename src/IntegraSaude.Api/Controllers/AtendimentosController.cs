using IntegraSaude.Api.Data;
using IntegraSaude.Api.Dtos;
using IntegraSaude.Api.Models;
using IntegraSaude.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IntegraSaude.Api.Controllers;

[ApiController]
[Route("api/atendimentos")]
[Authorize]
public class AtendimentosController(AppDbContext db, AuditService audit) : ControllerBase
{
    [HttpPost("senha")]
    [Authorize(Roles = $"{AppRoles.Recepcionista},{AppRoles.Admin}")]
    public async Task<ActionResult<AtendimentoDto>> EmitirSenha(EmitirSenhaRequest request)
    {
        var paciente = await db.Pacientes.FindAsync(request.PacienteId);
        if (paciente is null) return NotFound(new { message = "Paciente não encontrado." });

        var aberto = await db.Atendimentos.AnyAsync(a =>
            a.PacienteId == request.PacienteId && a.Status != AtendimentoStatus.Finalizado);
        if (aberto)
            return Conflict(new { message = "Este paciente já possui atendimento em aberto hoje." });

        var hoje = DateTime.UtcNow.Date;
        var count = await db.Atendimentos.CountAsync(a => a.ChegadaEm >= hoje);
        var senha = $"A{(count + 1):000}";

        var at = new Atendimento
        {
            Id = Guid.NewGuid(),
            PacienteId = paciente.Id,
            Senha = senha,
            Status = AtendimentoStatus.Aguardando,
            RecepcionistaId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
        };
        db.Atendimentos.Add(at);
        await db.SaveChangesAsync();
        await audit.LogAsync(User.Identity?.Name, "emitir_senha", "Atendimento", at.Id.ToString(), senha);
        return Ok(Map(at, paciente, null));
    }

    [HttpGet("fila-espera")]
    [Authorize(Roles = $"{AppRoles.Recepcionista},{AppRoles.Admin},{AppRoles.Enfermagem}")]
    public async Task<ActionResult<IEnumerable<AtendimentoDto>>> FilaEspera()
    {
        var list = await db.Atendimentos
            .Include(a => a.Paciente)
            .Include(a => a.Triagem)
            .Where(a => a.Status == AtendimentoStatus.Aguardando || a.Status == AtendimentoStatus.EmTriagem)
            .OrderBy(a => a.ChegadaEm)
            .ToListAsync();
        return Ok(list.Select(a => Map(a, a.Paciente, a.Triagem)));
    }

    [HttpGet]
    [Authorize(Roles = $"{AppRoles.Recepcionista},{AppRoles.Admin}")]
    public async Task<ActionResult<IEnumerable<AtendimentoDto>>> DoDia()
    {
        var inicio = DateTime.UtcNow.Date;
        var list = await db.Atendimentos
            .Include(a => a.Paciente)
            .Include(a => a.Triagem)
            .Where(a => a.ChegadaEm >= inicio)
            .OrderBy(a => a.ChegadaEm)
            .ToListAsync();
        return Ok(list.Select(a => Map(a, a.Paciente, a.Triagem)));
    }

    internal static AtendimentoDto Map(Atendimento a, Paciente p, Triagem? t) =>
        new(a.Id, p.Id, p.Nome, p.Cpf, a.Senha, a.Status, a.ChegadaEm, t?.Classificacao,
            t is null ? null : t.Classificacao.ToString());
}
