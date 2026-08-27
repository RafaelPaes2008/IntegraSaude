# IntegraSaúde

Sistema acadêmico para Unidades Básicas de Saúde: recepção, triagem (Manchester) e consulta médica.

Stack: **ASP.NET Core 10 (C#)** + **HTML/CSS/JS**. Banco **PostgreSQL** (Docker) ou **InMemory** no Development.

## Como executar

```bash
cd IntegraSaude
dotnet run --project src/IntegraSaude.Api
```

Abra http://localhost:5014

O ambiente Development usa banco em memória para subir sem Docker. A API também serve o frontend em `src/IntegraSaude.Web`.

### PostgreSQL (TAP)

```bash
docker compose up -d
```

Em `src/IntegraSaude.Api/appsettings.Development.json` defina `"UseInMemory": false`. Connection string padrão: usuário/senha/banco `integrasaude` na porta 5432.

## Usuários demo

| Usuário | Senha | Papel |
|---|---|---|
| admin | Admin@123 | Admin |
| recepcao | Recepcao@123 | Recepcionista |
| enfermagem | Enfermagem@123 | Enfermagem |
| medico | Medico@123 | Médico |

Gov.br é **simulado**: qualquer CPF com 11 dígitos autentica o usuário `medico`.

## Fluxo clínico

1. Recepção cadastra o paciente (nome + CPF) e emite senha / agenda o dia.
2. Enfermagem registra sinais vitais e a cor Manchester (escolha do profissional nesta V1).
3. Médico vê a fila por prioridade, preenche o checklist, diagnóstico e prescrição, e finaliza.

## LGPD e segurança (V1)

- Autenticação JWT + papéis (RBAC).
- Senhas com hash do ASP.NET Identity (não há “AES em todo o banco”).
- Auditoria (`AuditLogs`) em cadastro, senha, triagem e encerramento de consulta.
- HTTPS, backup e controle de acesso de infraestrutura devem complementar o sistema em produção.

## Fora desta versão

Farmácia avançada, laboratórios privados, telemedicina e OAuth real do Gov.br. Offline-first completo fica como evolução; a V1 avisa perda de rede.

## Testes

```bash
dotnet test
```
