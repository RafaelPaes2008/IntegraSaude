using IntegraSaude.Api.Data;
using IntegraSaude.Api.Dtos;
using IntegraSaude.Api.Models;
using IntegraSaude.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IntegraSaude.Api.Controllers;

[ApiController]
[Route("api/pacientes")]
[Authorize(Roles = $"{AppRoles.Recepcionista},{AppRoles.Admin},{AppRoles.Enfermagem},{AppRoles.Medico}")]
public class PacientesController(AppDbContext db, AuditService audit) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PacienteDto>>> Listar([FromQuery] string? q)
    {
        var query = db.Pacientes.AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var digits = Digits(q);
            query = query.Where(p => p.Nome.Contains(q) || p.Cpf.Contains(digits) || (p.CartaoSus != null && p.CartaoSus.Contains(q)));
        }

        var list = await query.OrderBy(p => p.Nome).Take(100)
            .Select(p => new PacienteDto(p.Id, p.Nome, p.Cpf, p.CartaoSus, p.Telefone, p.Endereco, p.DataNascimento))
            .ToListAsync();
        return Ok(list);
    }

    [HttpPost]
    [Authorize(Roles = $"{AppRoles.Recepcionista},{AppRoles.Admin}")]
    public async Task<ActionResult<PacienteDto>> Criar(PacienteRequest request)
    {
        var cpf = Digits(request.Cpf);
        if (string.IsNullOrWhiteSpace(request.Nome) || cpf.Length != 11)
            return BadRequest(new { message = "Nome e CPF (11 dígitos) são obrigatórios." });

        if (await db.Pacientes.AnyAsync(p => p.Cpf == cpf))
            return Conflict(new { message = "Já existe paciente com este CPF." });

        var p = new Paciente
        {
            Id = Guid.NewGuid(),
            Nome = request.Nome.Trim(),
            Cpf = cpf,
            CartaoSus = request.CartaoSus?.Trim(),
            Telefone = request.Telefone?.Trim(),
            Endereco = request.Endereco?.Trim(),
            DataNascimento = request.DataNascimento
        };
        db.Pacientes.Add(p);
        await db.SaveChangesAsync();
        await audit.LogAsync(User.Identity?.Name, "criar", "Paciente", p.Id.ToString(), p.Nome);
        return CreatedAtAction(nameof(Obter), new { id = p.Id }, ToDto(p));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PacienteDto>> Obter(Guid id)
    {
        var p = await db.Pacientes.FindAsync(id);
        return p is null ? NotFound() : Ok(ToDto(p));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = $"{AppRoles.Recepcionista},{AppRoles.Admin}")]
    public async Task<ActionResult<PacienteDto>> Atualizar(Guid id, PacienteRequest request)
    {
        var p = await db.Pacientes.FindAsync(id);
        if (p is null) return NotFound();
        var cpf = Digits(request.Cpf);
        if (cpf.Length != 11) return BadRequest(new { message = "CPF inválido." });
        if (await db.Pacientes.AnyAsync(x => x.Cpf == cpf && x.Id != id))
            return Conflict(new { message = "CPF já utilizado." });

        p.Nome = request.Nome.Trim();
        p.Cpf = cpf;
        p.CartaoSus = request.CartaoSus?.Trim();
        p.Telefone = request.Telefone?.Trim();
        p.Endereco = request.Endereco?.Trim();
        p.DataNascimento = request.DataNascimento;
        await db.SaveChangesAsync();
        await audit.LogAsync(User.Identity?.Name, "atualizar", "Paciente", p.Id.ToString(), p.Nome);
        return Ok(ToDto(p));
    }

    private static PacienteDto ToDto(Paciente p) =>
        new(p.Id, p.Nome, p.Cpf, p.CartaoSus, p.Telefone, p.Endereco, p.DataNascimento);

    private static string Digits(string? value) => new((value ?? "").Where(char.IsDigit).ToArray());
}
