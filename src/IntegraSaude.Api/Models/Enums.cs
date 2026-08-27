namespace IntegraSaude.Api.Models;

public enum AtendimentoStatus
{
    Aguardando = 0,
    EmTriagem = 1,
    Triado = 2,
    EmConsulta = 3,
    Finalizado = 4
}

public enum Manchester
{
    Vermelho = 0,
    Laranja = 1,
    Amarelo = 2,
    Verde = 3,
    Azul = 4
}

public static class AppRoles
{
    public const string Admin = "Admin";
    public const string Recepcionista = "Recepcionista";
    public const string Enfermagem = "Enfermagem";
    public const string Medico = "Medico";

    public static readonly string[] All = [Admin, Recepcionista, Enfermagem, Medico];
}
