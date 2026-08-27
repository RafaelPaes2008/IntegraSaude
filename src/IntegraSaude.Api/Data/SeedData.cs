using IntegraSaude.Api.Models;
using Microsoft.AspNetCore.Identity;

namespace IntegraSaude.Api.Data;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        await db.Database.EnsureCreatedAsync();

        foreach (var role in AppRoles.All)
        {
            if (!await roles.RoleExistsAsync(role))
                await roles.CreateAsync(new IdentityRole(role));
        }

        await EnsureUser(users, "admin", "Admin@123", "Administrador UBS", AppRoles.Admin);
        await EnsureUser(users, "recepcao", "Recepcao@123", "Maria Recepção", AppRoles.Recepcionista);
        await EnsureUser(users, "enfermagem", "Enfermagem@123", "Ana Enfermagem", AppRoles.Enfermagem);
        await EnsureUser(users, "medico", "Medico@123", "Dr. Carlos Silva", AppRoles.Medico);

        if (!db.Pacientes.Any())
        {
            var p1 = new Paciente
            {
                Id = Guid.NewGuid(),
                Nome = "João da Silva",
                Cpf = "12345678901",
                CartaoSus = "700000000000000",
                Telefone = "11988880001",
                Endereco = "Rua das Flores, 10",
                DataNascimento = new DateOnly(1984, 3, 12)
            };
            var p2 = new Paciente
            {
                Id = Guid.NewGuid(),
                Nome = "Maria Oliveira",
                Cpf = "98765432100",
                CartaoSus = "700000000000001",
                Telefone = "11988880002",
                Endereco = "Av. Central, 200",
                DataNascimento = new DateOnly(1991, 7, 21)
            };
            var p3 = new Paciente
            {
                Id = Guid.NewGuid(),
                Nome = "Pedro Santos",
                Cpf = "11122233344",
                CartaoSus = "700000000000002",
                Telefone = "11988880003",
                Endereco = "Travessa da Paz, 5",
                DataNascimento = new DateOnly(1975, 11, 2)
            };
            db.Pacientes.AddRange(p1, p2, p3);

            var recep = await users.FindByNameAsync("recepcao");
            db.Atendimentos.Add(new Atendimento
            {
                Id = Guid.NewGuid(),
                PacienteId = p1.Id,
                Senha = "A001",
                Status = AtendimentoStatus.Aguardando,
                ChegadaEm = DateTime.UtcNow.AddMinutes(-25),
                RecepcionistaId = recep?.Id
            });
            db.Atendimentos.Add(new Atendimento
            {
                Id = Guid.NewGuid(),
                PacienteId = p2.Id,
                Senha = "A002",
                Status = AtendimentoStatus.Aguardando,
                ChegadaEm = DateTime.UtcNow.AddMinutes(-12),
                RecepcionistaId = recep?.Id
            });

            db.Agendamentos.Add(new Agendamento
            {
                Id = Guid.NewGuid(),
                PacienteId = p3.Id,
                DataHora = DateTime.SpecifyKind(DateTime.Today.AddHours(15), DateTimeKind.Utc),
                Observacao = "Retorno hipertensão",
                Status = "agendado",
                CriadoPorId = recep?.Id
            });

            await db.SaveChangesAsync();
        }
    }

    private static async Task EnsureUser(
        UserManager<ApplicationUser> users,
        string username,
        string password,
        string nome,
        string role)
    {
        var user = await users.FindByNameAsync(username);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = username,
                Email = $"{username}@integrasaude.local",
                NomeCompleto = nome,
                EmailConfirmed = true
            };
            var result = await users.CreateAsync(user, password);
            if (!result.Succeeded)
                throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
        }

        if (!await users.IsInRoleAsync(user, role))
            await users.AddToRoleAsync(user, role);
    }
}
