using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using IntegraSaude.Api.Dtos;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using IntegraSaude.Api.Data;

namespace IntegraSaude.Api.Tests;

public class CustomWebAppFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            var remove = services.Where(d =>
                d.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                d.ServiceType.IsGenericType && d.ServiceType.GetGenericTypeDefinition() == typeof(DbContextOptions<>)).ToList();
            foreach (var d in remove)
                services.Remove(d);

            services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase("it-" + Guid.NewGuid()));
        });
    }
}

public class AuthAndFlowTests : IClassFixture<CustomWebAppFactory>
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    public AuthAndFlowTests(CustomWebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_invalido_retorna_401()
    {
        var res = await _client.PostAsJsonAsync("/api/auth/login", new { usuario = "admin", senha = "errada", lembrarMe = false });
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Login_valido_retorna_token()
    {
        var auth = await Login("recepcao", "Recepcao@123");
        Assert.False(string.IsNullOrWhiteSpace(auth.AccessToken));
        Assert.Contains("Recepcionista", auth.Roles);
    }

    [Fact]
    public async Task GovBr_mock_autentica_medico()
    {
        var res = await _client.PostAsJsonAsync("/api/auth/govbr", new { cpf = "529.982.247-25" });
        res.EnsureSuccessStatusCode();
        var auth = await res.Content.ReadFromJsonAsync<AuthResponse>(Json);
        Assert.Contains("Medico", auth!.Roles);
    }

    [Fact]
    public async Task Fluxo_recepcao_triagem_consulta()
    {
        var recep = await Login("recepcao", "Recepcao@123");
        UseToken(recep.AccessToken);

        var cpf = $"9{DateTime.UtcNow.Ticks.ToString()[^10..]}";
        var criar = await _client.PostAsJsonAsync("/api/pacientes", new
        {
            nome = "Paciente Teste",
            cpf,
            cartaoSus = "700111222333444",
            telefone = "11999990000",
            endereco = "Rua Teste, 1",
            dataNascimento = "1990-01-15"
        });
        criar.EnsureSuccessStatusCode();
        var paciente = await criar.Content.ReadFromJsonAsync<PacienteDto>(Json);
        Assert.NotNull(paciente);

        var senhaRes = await _client.PostAsJsonAsync("/api/atendimentos/senha", new { pacienteId = paciente!.Id });
        senhaRes.EnsureSuccessStatusCode();
        var atendimento = await senhaRes.Content.ReadFromJsonAsync<AtendimentoDto>(Json);
        Assert.Equal("Aguardando", atendimento!.Status.ToString());

        var enf = await Login("enfermagem", "Enfermagem@123");
        UseToken(enf.AccessToken);
        var triagem = await _client.PostAsJsonAsync("/api/triagem", new
        {
            atendimentoId = atendimento.Id,
            pressaoSistolica = 120,
            pressaoDiastolica = 80,
            temperatura = 36.5,
            glicemia = 95,
            saturacao = 98,
            peso = 70.5,
            classificacao = 2,
            justificativa = "Dor leve"
        });
        triagem.EnsureSuccessStatusCode();

        var med = await Login("medico", "Medico@123");
        UseToken(med.AccessToken);
        var fila = await _client.GetFromJsonAsync<List<AtendimentoDto>>("/api/medico/fila", Json);
        Assert.Contains(fila!, a => a.Id == atendimento.Id);

        var inicio = await _client.PostAsync($"/api/medico/{atendimento.Id}/iniciar", null);
        inicio.EnsureSuccessStatusCode();

        var fim = await _client.PostAsJsonAsync($"/api/medico/{atendimento.Id}/finalizar", new
        {
            queixaPrincipal = true,
            historiaDoencaAtual = true,
            exameFisico = true,
            orientacoes = true,
            anamnese = "Queixa de cefaleia.",
            diagnostico = "Cefaleia tensional",
            cid = "G44",
            prescricao = "Dipirona 500mg se dor"
        });
        fim.EnsureSuccessStatusCode();
        var consulta = await fim.Content.ReadFromJsonAsync<ConsultaDto>(Json);
        Assert.NotNull(consulta!.FimEm);
    }

    private async Task<AuthResponse> Login(string usuario, string senha)
    {
        var res = await _client.PostAsJsonAsync("/api/auth/login", new { usuario, senha, lembrarMe = false });
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<AuthResponse>(Json))!;
    }

    private void UseToken(string token)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }
}
