#!/usr/bin/env bash
# Valida todos os endpoints da Refúgio API pela URL pública. Só faz requisições HTTP.
B="${1:-https://refugio-api-gs.eastus.cloudapp.azure.com}"
OK=0; FAIL=0
H='content-type: application/json'

chk() {
  if [ "$3" = "$2" ]; then OK=$((OK+1)); printf "  ✔ %-62s %s\n" "$1" "$3"
  else FAIL=$((FAIL+1)); printf "  ✘ %-62s esperado %s, veio %s\n" "$1" "$2" "$3"; fi
}
st() { curl -s -o /dev/null -w "%{http_code}" --max-time 20 "$@"; }
js() { curl -s --max-time 20 "$@"; }
chave() { echo "k-$RANDOM$RANDOM-$(date +%s%N)"; }
token() { js -X POST "$B/auth/login" -H "$H" -d "{\"email\":\"$1\",\"senha\":\"Senha@123\"}" | sed -E 's/.*"token":"([^"]+)".*/\1/'; }
campo_id() { grep -o '"id":[0-9]*' | head -1 | cut -d: -f2; }
cabecalho() { grep -i "^$1:" | awk '{print $2}' | tr -d '\r'; }

D1=$(date -d "+40 days" +%F)
D2=$(date -d "+41 days" +%F)

echo "Saúde e documentação"
chk "GET /health" 200 "$(st "$B/health")"
chk "GET /health/live" 200 "$(st "$B/health/live")"
chk "GET /swagger/index.html" 200 "$(st "$B/swagger/index.html")"
chk "GET /openapi/v1.json" 200 "$(st "$B/openapi/v1.json")"

echo "Autenticação"
EMAIL="deploy-$RANDOM@teste.com"
chk "POST /auth/registrar (novo)" 201 "$(st -X POST "$B/auth/registrar" -H "$H" -d "{\"nome\":\"Teste Deploy\",\"email\":\"$EMAIL\",\"senha\":\"Senha@123\"}")"
chk "POST /auth/registrar (e-mail repetido)" 409 "$(st -X POST "$B/auth/registrar" -H "$H" -d "{\"nome\":\"X\",\"email\":\"$EMAIL\",\"senha\":\"Senha@123\"}")"
chk "POST /auth/registrar (senha fraca)" 400 "$(st -X POST "$B/auth/registrar" -H "$H" -d '{"nome":"X","email":"y@y.com","senha":"123"}')"
chk "POST /auth/login (válido)" 200 "$(st -X POST "$B/auth/login" -H "$H" -d '{"email":"maria@email.com","senha":"Senha@123"}')"
chk "POST /auth/login (senha errada)" 401 "$(st -X POST "$B/auth/login" -H "$H" -d '{"email":"maria@email.com","senha":"Errada123"}')"
AM="Authorization: Bearer $(token maria@email.com)"
AJ="Authorization: Bearer $(token jose@email.com)"
AG="Authorization: Bearer $(token gestor@refugio.com)"
AN="Authorization: Bearer $(token "$EMAIL")"

echo "Refúgios"
chk "GET /refugios sem token" 401 "$(st "$B/api/v1/refugios")"
chk "GET /refugios?bairro=Penha" 200 "$(st -H "$AM" "$B/api/v1/refugios?bairro=Penha&pagina=1&tamanho=10")"
chk "GET /refugios?ativo=true" 200 "$(st -H "$AM" "$B/api/v1/refugios?ativo=true")"
chk "GET /refugios?data=15-11-2026 (malformada)" 400 "$(st -H "$AM" "$B/api/v1/refugios?data=15-11-2026")"
chk "GET /refugios?tamanho=500" 400 "$(st -H "$AM" "$B/api/v1/refugios?tamanho=500")"
CORPO='{"nome":"Refugio Teste Deploy","tipo":"CENTRO_COMUNITARIO","cep":"03601000","endereco":"Rua Exemplo, 100","bairro":"Penha","capacidade":1,"abre":"08:00","fecha":"20:00","acessivel":true,"ativo":true}'
CORPO_CAP0='{"nome":"X","tipo":"BIBLIOTECA","cep":"03601000","endereco":"R","bairro":"Penha","capacidade":0,"abre":"08:00","fecha":"20:00","acessivel":true,"ativo":true}'
CORPO_INATIVO='{"nome":"Refugio Inativo Deploy","tipo":"OUTRO","cep":"03601000","endereco":"Rua 2","bairro":"Penha","capacidade":30,"abre":"08:00","fecha":"20:00","acessivel":true,"ativo":false}'
CORPO_PUT='{"nome":"Refugio Teste Deploy 2","tipo":"CENTRO_COMUNITARIO","cep":"03601-000","endereco":"Rua Exemplo, 200","bairro":"Penha","capacidade":1,"abre":"09:00","fecha":"19:30","acessivel":true,"ativo":true}'
chk "POST /refugios (idoso → 403)" 403 "$(st -X POST "$B/api/v1/refugios" -H "$AM" -H "$H" -d "$CORPO")"
chk "POST /refugios (capacidade 0 → 400)" 400 "$(st -X POST "$B/api/v1/refugios" -H "$AG" -H "$H" -d "$CORPO_CAP0")"
RESP=$(curl -s -i --max-time 20 -X POST "$B/api/v1/refugios" -H "$AG" -H "$H" -d "$CORPO")
chk "POST /refugios (gestor → 201)" 201 "$(echo "$RESP" | head -1 | awk '{print $2}')"
RID=$(echo "$RESP" | campo_id)
chk "  header Location" "/api/v1/refugios/$RID" "$(echo "$RESP" | cabecalho location)"
chk "GET /refugios/{id}" 200 "$(st -H "$AM" "$B/api/v1/refugios/$RID")"
chk "GET /refugios/999999" 404 "$(st -H "$AM" "$B/api/v1/refugios/999999")"
chk "PUT /refugios/{id}" 200 "$(st -X PUT "$B/api/v1/refugios/$RID" -H "$AG" -H "$H" -d "$CORPO_PUT")"
RID2=$(js -X POST "$B/api/v1/refugios" -H "$AG" -H "$H" -d "$CORPO_INATIVO" | campo_id)
chk "  refúgio inativo aparece com 0 vagas" '"vagasDisponiveis":0' "$(js -H "$AM" "$B/api/v1/refugios/$RID2" | grep -o '"vagasDisponiveis":[0-9]*')"

echo "CEP (ViaCEP real)"
chk "GET /cep/01001000" 200 "$(st -H "$AG" "$B/api/v1/cep/01001000")"
chk "GET /cep/99999999" 404 "$(st -H "$AG" "$B/api/v1/cep/99999999")"
chk "GET /cep/123" 400 "$(st -H "$AG" "$B/api/v1/cep/123")"

echo "Reservas"
KEY=$(chave)
R=$(curl -s -i --max-time 20 -X POST "$B/api/v1/reservas" -H "$AN" -H "$H" -H "Idempotency-Key: $KEY" -d "{\"refugioId\":$RID,\"dataReserva\":\"$D1\"}")
chk "POST /reservas (201)" 201 "$(echo "$R" | head -1 | awk '{print $2}')"
RES=$(echo "$R" | campo_id)
R2=$(curl -s -i --max-time 20 -X POST "$B/api/v1/reservas" -H "$AN" -H "$H" -H "Idempotency-Key: $KEY" -d "{\"refugioId\":$RID,\"dataReserva\":\"$D1\"}")
chk "  mesma chave → mesma reserva" "$RES" "$(echo "$R2" | campo_id)"
chk "  header Idempotent-Replayed" "true" "$(echo "$R2" | cabecalho idempotent-replayed)"
chk "POST /reservas mesma chave, outra data (422)" 422 "$(st -X POST "$B/api/v1/reservas" -H "$AN" -H "$H" -H "Idempotency-Key: $KEY" -d "{\"refugioId\":$RID,\"dataReserva\":\"$D2\"}")"
chk "POST /reservas sem vaga (409)" 409 "$(st -X POST "$B/api/v1/reservas" -H "$AJ" -H "$H" -H "Idempotency-Key: $(chave)" -d "{\"refugioId\":$RID,\"dataReserva\":\"$D1\"}")"
chk "POST /reservas 2ª no mesmo dia (409)" 409 "$(st -X POST "$B/api/v1/reservas" -H "$AN" -H "$H" -H "Idempotency-Key: $(chave)" -d "{\"refugioId\":1,\"dataReserva\":\"$D1\"}")"
chk "POST /reservas refúgio inativo (422)" 422 "$(st -X POST "$B/api/v1/reservas" -H "$AN" -H "$H" -H "Idempotency-Key: $(chave)" -d "{\"refugioId\":$RID2,\"dataReserva\":\"$D2\"}")"
chk "POST /reservas refúgio inexistente (404)" 404 "$(st -X POST "$B/api/v1/reservas" -H "$AN" -H "$H" -H "Idempotency-Key: $(chave)" -d "{\"refugioId\":999999,\"dataReserva\":\"$D2\"}")"
chk "POST /reservas sem Idempotency-Key (400)" 400 "$(st -X POST "$B/api/v1/reservas" -H "$AN" -H "$H" -d "{\"refugioId\":1,\"dataReserva\":\"$D2\"}")"
chk "POST /reservas data passada (400)" 400 "$(st -X POST "$B/api/v1/reservas" -H "$AN" -H "$H" -H "Idempotency-Key: $(chave)" -d '{"refugioId":1,"dataReserva":"2020-01-01"}')"
chk "POST /reservas pelo gestor (403)" 403 "$(st -X POST "$B/api/v1/reservas" -H "$AG" -H "$H" -H "Idempotency-Key: $(chave)" -d "{\"refugioId\":1,\"dataReserva\":\"$D2\"}")"
chk "GET /reservas/minhas" 200 "$(st -H "$AN" "$B/api/v1/reservas/minhas")"
chk "GET /reservas/{id} (dono)" 200 "$(st -H "$AN" "$B/api/v1/reservas/$RES")"
chk "GET /reservas/{id} (outro idoso → 403)" 403 "$(st -H "$AJ" "$B/api/v1/reservas/$RES")"
chk "GET /reservas/999999 (404)" 404 "$(st -H "$AN" "$B/api/v1/reservas/999999")"
chk "DELETE /refugios/{id} com reserva ativa (409)" 409 "$(st -X DELETE -H "$AG" "$B/api/v1/refugios/$RID")"
chk "DELETE /reservas/{id} (204)" 204 "$(st -X DELETE -H "$AN" "$B/api/v1/reservas/$RES")"
chk "  comprovante mostra CANCELADA" '"status":"CANCELADA"' "$(js -H "$AN" "$B/api/v1/reservas/$RES" | grep -o '"status":"[A-Z]*"')"

echo "Usuário"
chk "GET /usuarios/me" 200 "$(st -H "$AN" "$B/api/v1/usuarios/me")"
chk "PUT /usuarios/me" 200 "$(st -X PUT -H "$AN" -H "$H" "$B/api/v1/usuarios/me" -d '{"nome":"Teste Deploy","bairro":"Penha","telefone":"(11) 98888-0000","contatoEmergenciaNome":"Contato","contatoEmergenciaTelefone":"(11) 97777-0000"}')"
chk "PUT /usuarios/me telefone inválido (400)" 400 "$(st -X PUT -H "$AN" -H "$H" "$B/api/v1/usuarios/me" -d '{"nome":"X","telefone":"abc"}')"

echo "Limpeza dos refúgios criados neste teste (via API)"
chk "DELETE /refugios/{id} (204)" 204 "$(st -X DELETE -H "$AG" "$B/api/v1/refugios/$RID")"
chk "DELETE /refugios/{id inativo} (204)" 204 "$(st -X DELETE -H "$AG" "$B/api/v1/refugios/$RID2")"
chk "GET /refugios/{id} após excluir (404)" 404 "$(st -H "$AG" "$B/api/v1/refugios/$RID")"

echo "Rate limit (por último: bloqueia o login deste IP por 1 minuto)"
C=""
for n in $(seq 1 12); do C=$(st -X POST "$B/auth/login" -H "$H" -d '{"email":"x@x.com","senha":"Errada123"}'); done
chk "POST /auth/login após 10 tentativas (429)" 429 "$C"
RA=$(curl -s -i --max-time 20 -X POST "$B/auth/login" -H "$H" -d '{"email":"x@x.com","senha":"Errada123"}' | cabecalho retry-after)
chk "  header Retry-After presente" "sim" "$([ -n "$RA" ] && echo sim || echo nao)"

echo
echo "RESULTADO: $OK ok, $FAIL falhas"
