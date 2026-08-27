using Microsoft.AspNetCore.Identity;

namespace IntegraSaude.Api.Models;

public class ApplicationUser : IdentityUser
{
    public string NomeCompleto { get; set; } = string.Empty;
}
