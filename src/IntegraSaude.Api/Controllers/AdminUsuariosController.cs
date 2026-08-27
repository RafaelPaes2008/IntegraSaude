using IntegraSaude.Api.Dtos;
using IntegraSaude.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace IntegraSaude.Api.Controllers;

[ApiController]
[Route("api/admin/usuarios")]
[Authorize(Roles = AppRoles.Admin)]
public class AdminUsuariosController(UserManager<ApplicationUser> users) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UsuarioAdminDto>>> Listar()
    {
        var list = new List<UsuarioAdminDto>();
        foreach (var user in users.Users.OrderBy(u => u.UserName).ToList())
        {
            var roles = await users.GetRolesAsync(user);
            list.Add(new UsuarioAdminDto(user.Id, user.UserName ?? "", user.NomeCompleto, roles.ToArray()));
        }
        return Ok(list);
    }

    [HttpPost]
    public async Task<ActionResult<UsuarioAdminDto>> Criar(UsuarioAdminRequest request)
    {
        if (!AppRoles.All.Contains(request.Papel))
            return BadRequest(new { message = "Papel inválido." });

        var user = new ApplicationUser
        {
            UserName = request.Usuario.Trim(),
            Email = $"{request.Usuario.Trim()}@integrasaude.local",
            NomeCompleto = request.NomeCompleto.Trim(),
            EmailConfirmed = true
        };
        var created = await users.CreateAsync(user, request.Senha);
        if (!created.Succeeded)
            return BadRequest(new { message = string.Join(" ", created.Errors.Select(e => e.Description)) });

        await users.AddToRoleAsync(user, request.Papel);
        return Ok(new UsuarioAdminDto(user.Id, user.UserName!, user.NomeCompleto, [request.Papel]));
    }
}
