using IntegraSaude.Api.Data;
using IntegraSaude.Api.Dtos;
using IntegraSaude.Api.Models;
using IntegraSaude.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IntegraSaude.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(
    UserManager<ApplicationUser> users,
    TokenService tokens,
    AppDbContext db) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Usuario) || string.IsNullOrWhiteSpace(request.Senha))
            return BadRequest(new { message = "Informe usuário e senha." });

        var user = await users.FindByNameAsync(request.Usuario.Trim());
        if (user is null || !await users.CheckPasswordAsync(user, request.Senha))
            return Unauthorized(new { message = "Usuário ou senha inválidos." });

        var roles = await users.GetRolesAsync(user);
        var (access, refresh, minutes) = await tokens.CreateAsync(user, roles, request.LembrarMe);
        return Ok(new AuthResponse(access, refresh, minutes, user.NomeCompleto, roles.ToArray()));
    }

    [HttpPost("govbr")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> GovBr(GovBrRequest request)
    {
        var digits = new string((request.Cpf ?? "").Where(char.IsDigit).ToArray());
        if (digits.Length != 11)
            return BadRequest(new { message = "Informe um CPF com 11 dígitos (simulação Gov.br)." });

        var user = await users.FindByNameAsync("medico");
        if (user is null)
            return StatusCode(500, new { message = "Usuário de demonstração Gov.br não encontrado." });

        var roles = await users.GetRolesAsync(user);
        var (access, refresh, minutes) = await tokens.CreateAsync(user, roles, true);
        return Ok(new AuthResponse(access, refresh, minutes, user.NomeCompleto, roles.ToArray()));
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Refresh([FromBody] RefreshBody body)
    {
        if (string.IsNullOrWhiteSpace(body.RefreshToken))
            return BadRequest(new { message = "Refresh token ausente." });

        var hash = TokenService.Sha256(body.RefreshToken);
        var stored = await db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash && !t.Revogado);
        if (stored is null || stored.ExpiraEm < DateTime.UtcNow)
            return Unauthorized(new { message = "Sessão expirada. Faça login novamente." });

        var user = await users.FindByIdAsync(stored.UserId);
        if (user is null)
            return Unauthorized(new { message = "Usuário inválido." });

        stored.Revogado = true;
        var roles = await users.GetRolesAsync(user);
        var (access, refresh, minutes) = await tokens.CreateAsync(user, roles, true);
        return Ok(new AuthResponse(access, refresh, minutes, user.NomeCompleto, roles.ToArray()));
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] RefreshBody? body)
    {
        if (!string.IsNullOrWhiteSpace(body?.RefreshToken))
        {
            var hash = TokenService.Sha256(body.RefreshToken);
            var stored = await db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash);
            if (stored is not null)
            {
                stored.Revogado = true;
                await db.SaveChangesAsync();
            }
        }
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<MeDto>> Me()
    {
        var user = await users.GetUserAsync(User);
        if (user is null) return Unauthorized();
        var roles = await users.GetRolesAsync(user);
        return Ok(new MeDto(user.Id, user.UserName ?? "", user.NomeCompleto, roles.ToArray()));
    }

    public record RefreshBody(string? RefreshToken);
}
