using IntegraSaude.Api.Models;

namespace IntegraSaude.Api.Dtos;

public record LoginRequest(string Usuario, string Senha, bool LembrarMe);
public record GovBrRequest(string Cpf);
public record AuthResponse(string AccessToken, string RefreshToken, int ExpiresInMinutes, string Nome, string[] Roles);

public record PacienteRequest(string Nome, string Cpf, string? CartaoSus, string? Telefone, string? Endereco, DateOnly? DataNascimento);
public record PacienteDto(Guid Id, string Nome, string Cpf, string? CartaoSus, string? Telefone, string? Endereco, DateOnly? DataNascimento);

public record EmitirSenhaRequest(Guid PacienteId);

public record AtendimentoDto(
    Guid Id,
    Guid PacienteId,
    string PacienteNome,
    string Cpf,
    string Senha,
    AtendimentoStatus Status,
    DateTime ChegadaEm,
    Manchester? Classificacao,
    string? ClassificacaoNome);

public record AgendamentoRequest(Guid PacienteId, DateTime DataHora, string? Observacao);
public record AgendamentoDto(Guid Id, Guid PacienteId, string PacienteNome, DateTime DataHora, string? Observacao, string Status);

public record TriagemRequest(
    Guid AtendimentoId,
    int? PressaoSistolica,
    int? PressaoDiastolica,
    decimal? Temperatura,
    decimal? Glicemia,
    decimal? Saturacao,
    decimal? Peso,
    Manchester Classificacao,
    string? Justificativa);

public record ConsultaSalvarRequest(
    bool QueixaPrincipal,
    bool HistoriaDoencaAtual,
    bool ExameFisico,
    bool Orientacoes,
    string? Anamnese,
    string? Diagnostico,
    string? Cid,
    string? Prescricao);

public record ConsultaDto(
    Guid Id,
    Guid AtendimentoId,
    bool QueixaPrincipal,
    bool HistoriaDoencaAtual,
    bool ExameFisico,
    bool Orientacoes,
    string? Anamnese,
    string? Diagnostico,
    string? Cid,
    string? Prescricao,
    DateTime? InicioEm,
    DateTime? FimEm);

public record UsuarioAdminDto(string Id, string Usuario, string NomeCompleto, string[] Roles);
public record UsuarioAdminRequest(string Usuario, string Senha, string NomeCompleto, string Papel);

public record MeDto(string Id, string Usuario, string Nome, string[] Roles);
