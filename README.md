# Refúgio API

## Integrantes

| Nome | RM |
|---|---|
| Anna Clara Russo Luca | 561928 |
| Gabriel Duarte Maciel | 565754 |
| Tiago Guedes da Costa | 564731 |
| Gustavo Tavares da Silva | 562827 |

## Resumo

O **Refúgio** ajuda idosos a enfrentar ondas de calor. Prédios públicos frescos (bibliotecas, CEUs, UBSs, centros comunitários) viram **refúgios climáticos**: o idoso encontra um refúgio aberto no bairro, reserva uma vaga e mostra o comprovante na porta. O gestor cadastra e mantém os refúgios. ODS 3, 11 e 13.

## Papel desta API

É o backend da solução e a única API que o app Mobile consome. Ela:

- autentica idosos e gestores (JWT) e controla o acesso por papel;
- mantém os refúgios (CRUD do gestor) e preenche o endereço pelo CEP (ViaCEP);
- controla as reservas: vagas por dia, uma reserva por idoso por dia, cancelamento e comprovante;
- evita reserva duplicada (`Idempotency-Key`), limita requisições (429) e expõe a saúde da API, do banco e do ViaCEP.

## Tecnologias

| | |
|---|---|
| API | .NET 10, ASP.NET Core Minimal APIs |
| Banco | Oracle 19c (FIAP) com EF Core 10 e migrações; tabelas `T_RFG_*` |
| Segurança | JWT Bearer (papéis IDOSO e GESTOR), senha com PBKDF2, rate limiting nativo |
| Validação e erros | FluentValidation; todo erro em ProblemDetails |
| Documentação | OpenAPI + Swagger UI (`/swagger`) |
| Monitoramento | Health Checks (`/health`: API, banco e ViaCEP) |
| Testes | xUnit: unitários (regras de negócio) e de integração (WebApplicationFactory + Oracle) |
| Deploy | Docker, Caddy (HTTPS automático) e VM Linux no Azure |

Camadas: `Domain` (regras) ← `Application` (casos de uso, DTOs, validação) ← `Infrastructure` (EF Core, JWT, ViaCEP) ← `Api` (endpoints).

## Como fazer funcionar

**Na nuvem (Azure):** a API fica em **https://refugio-api-gs.eastus.cloudapp.azure.com**. A VM fica desligada quando não está em uso:

```bash
az vm start -g rg-refugio-api -n vm-refugio-api        # liga; a API sobe sozinha em ~1 minuto
az vm deallocate -g rg-refugio-api -n vm-refugio-api   # desliga
```

**Local:** precisa do .NET SDK 10 e de acesso ao Oracle.

```bash
dotnet user-secrets set "ConnectionStrings:Refugio" "User Id=SEU_RM;Password=SUA_SENHA;Data Source=oracle.fiap.com.br:1521/ORCL" --project src/Refugio.Api
dotnet user-secrets set "Jwt:Chave" "$(openssl rand -base64 48)" --project src/Refugio.Api
dotnet run --project src/Refugio.Api      # http://localhost:5159/swagger
dotnet test                               # testes unitários e de integração
```

Na primeira execução, a API cria as tabelas e os usuários de teste (senha `Senha@123`): `gestor@refugio.com` (GESTOR), `maria@email.com` e `jose@email.com` (IDOSO).

## Fluxo de usuário (Postman)

No Postman, crie as variáveis `baseUrl` (`https://refugio-api-gs.eastus.cloudapp.azure.com` ou `http://localhost:5159`), `tokenIdoso` e `tokenGestor`. Cada bloco abaixo pode ser colado em **Import → Raw text**. Troque as datas por uma data futura.

### Idoso

**1. Cadastro**: 201, cria sempre IDOSO

```bash
curl -X POST "{{baseUrl}}/auth/registrar" -H "Content-Type: application/json" -d '{"nome":"Ana Lima","email":"ana.lima@email.com","senha":"Senha@123"}'
```

**2. Login**: 200; copie o `token` para `tokenIdoso`

```bash
curl -X POST "{{baseUrl}}/auth/login" -H "Content-Type: application/json" -d '{"email":"maria@email.com","senha":"Senha@123"}'
```

**3. Perfil**

```bash
curl "{{baseUrl}}/api/v1/usuarios/me" -H "Authorization: Bearer {{tokenIdoso}}"
```

**4. Salvar contato de emergência**

```bash
curl -X PUT "{{baseUrl}}/api/v1/usuarios/me" -H "Authorization: Bearer {{tokenIdoso}}" -H "Content-Type: application/json" -d '{"nome":"Maria Souza","bairro":"Penha","telefone":"(11) 98888-0000","contatoEmergenciaNome":"João Souza (filho)","contatoEmergenciaTelefone":"(11) 97777-0000"}'
```

**5. Refúgios abertos no bairro, com vagas da data**

```bash
curl "{{baseUrl}}/api/v1/refugios?bairro=Penha&data=2026-11-27&ativo=true&pagina=1&tamanho=10" -H "Authorization: Bearer {{tokenIdoso}}"
```

**6. Detalhe do refúgio**

```bash
curl "{{baseUrl}}/api/v1/refugios/1?data=2026-11-27" -H "Authorization: Bearer {{tokenIdoso}}"
```

**7. Reservar**: 201; repetir com a mesma `Idempotency-Key` devolve a mesma reserva

```bash
curl -X POST "{{baseUrl}}/api/v1/reservas" -H "Authorization: Bearer {{tokenIdoso}}" -H "Idempotency-Key: 6f1c2a9e-0000-4000-8000-000000000001" -H "Content-Type: application/json" -d '{"refugioId":1,"dataReserva":"2026-11-27"}'
```

**8. Segunda reserva no mesmo dia**: 409

```bash
curl -X POST "{{baseUrl}}/api/v1/reservas" -H "Authorization: Bearer {{tokenIdoso}}" -H "Idempotency-Key: 6f1c2a9e-0000-4000-8000-000000000002" -H "Content-Type: application/json" -d '{"refugioId":2,"dataReserva":"2026-11-27"}'
```

**9. Minhas reservas**

```bash
curl "{{baseUrl}}/api/v1/reservas/minhas" -H "Authorization: Bearer {{tokenIdoso}}"
```

**10. Comprovante** (use o `id` da reserva)

```bash
curl "{{baseUrl}}/api/v1/reservas/1" -H "Authorization: Bearer {{tokenIdoso}}"
```

**11. Cancelar**: 204

```bash
curl -X DELETE "{{baseUrl}}/api/v1/reservas/1" -H "Authorization: Bearer {{tokenIdoso}}"
```

### Gestor

**12. Login**: copie o `token` para `tokenGestor`

```bash
curl -X POST "{{baseUrl}}/auth/login" -H "Content-Type: application/json" -d '{"email":"gestor@refugio.com","senha":"Senha@123"}'
```

**13. Endereço pelo CEP**

```bash
curl "{{baseUrl}}/api/v1/cep/03601000" -H "Authorization: Bearer {{tokenGestor}}"
```

**14. Criar refúgio**: 201 com `Location`

```bash
curl -X POST "{{baseUrl}}/api/v1/refugios" -H "Authorization: Bearer {{tokenGestor}}" -H "Content-Type: application/json" -d '{"nome":"Biblioteca Municipal da Penha","tipo":"BIBLIOTECA","cep":"03601-000","endereco":"Rua Exemplo, 100","bairro":"Penha","capacidade":20,"abre":"08:00","fecha":"20:00","acessivel":true,"ativo":true}'
```

**15. Editar refúgio** (use o `id` criado)

```bash
curl -X PUT "{{baseUrl}}/api/v1/refugios/6" -H "Authorization: Bearer {{tokenGestor}}" -H "Content-Type: application/json" -d '{"nome":"Biblioteca Municipal da Penha","tipo":"BIBLIOTECA","cep":"03601-000","endereco":"Rua Exemplo, 100","bairro":"Penha","capacidade":25,"abre":"08:00","fecha":"21:00","acessivel":true,"ativo":false}'
```

**16. Excluir refúgio**: 204; 409 se houver reservas ativas futuras

```bash
curl -X DELETE "{{baseUrl}}/api/v1/refugios/6" -H "Authorization: Bearer {{tokenGestor}}"
```

### Saúde

```bash
curl "{{baseUrl}}/health"
```

Erros sempre em ProblemDetails (`title`, `detail` e, na validação, `errors` por campo): 400 validação, 401 sem login, 403 papel sem permissão, 404 não encontrado, 409 sem vaga ou conflito, 422 refúgio inativo, 429 excesso de requisições (`Retry-After`).
