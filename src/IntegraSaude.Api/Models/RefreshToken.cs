namespace IntegraSaude.Api.Models;

public class RefreshToken
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiraEm { get; set; }
    public bool Revogado { get; set; }
}
