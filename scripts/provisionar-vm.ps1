<#
.SYNOPSIS
  Cria a infraestrutura da Refúgio API no Azure (Azure CLI) e publica a API numa VM Linux com Docker.

.DESCRIPTION
  1. Resource group, VM Ubuntu 24.04 com Docker (cloud-init), IP público estático com nome DNS.
  2. Rede: 80/443 abertas para a internet; SSH (22) só para o IP de quem roda o script.
  3. Copia o código (git archive do commit atual) para a VM, grava o .env lá (permissão 600)
     e sobe API + Caddy, que emite o certificado HTTPS da Let's Encrypt sozinho.
  Credenciais NÃO ficam no repositório: são lidas do .env local (ORACLE_*, SEED_*). A chave JWT
  da VM é gerada aqui, diferente da de desenvolvimento.

  Assinaturas de estudante restringem região e tamanho. Na assinatura do grupo funcionou
  eastus + Standard_D2as_v4 (brazilsouth e a família B estavam bloqueadas).

.EXAMPLE
  az login --use-device-code
  az account set --subscription "<nome ou id>"
  ./scripts/provisionar-vm.ps1 -NomeDns refugio-api-gs
  bash ./scripts/validar-endpoints.sh https://refugio-api-gs.eastus.cloudapp.azure.com
  az vm deallocate -g rg-refugio-api -n vm-refugio-api     # desliga (para a cobrança de computação)
  az vm start -g rg-refugio-api -n vm-refugio-api          # religa (a API sobe sozinha)
#>
param(
    [string]$ResourceGroup = "rg-refugio-api",
    [string]$Regiao = "eastus",
    [string]$NomeVm = "vm-refugio-api",
    [string]$Tamanho = "Standard_D2as_v4",
    [Parameter(Mandatory)][string]$NomeDns,
    [string]$ArquivoEnv = ".env"
)
$ErrorActionPreference = "Stop"
$raiz = Split-Path $PSScriptRoot -Parent

# --- Infraestrutura -----------------------------------------------------------
az group create --name $ResourceGroup --location $Regiao --tags projeto=refugio --output none

az vm create `
    --resource-group $ResourceGroup --name $NomeVm --location $Regiao `
    --image Ubuntu2404 --size $Tamanho --admin-username azureuser `
    --ssh-key-values "$HOME/.ssh/id_rsa.pub" `
    --public-ip-sku Standard --public-ip-address-dns-name $NomeDns `
    --nsg-rule SSH --custom-data "$raiz/scripts/vm/cloud-init.yml" `
    --tags projeto=refugio --output none

$nsg = az network nsg list -g $ResourceGroup --query "[0].name" -o tsv
$meuIp = Invoke-RestMethod https://api.ipify.org
az network nsg rule update -g $ResourceGroup --nsg-name $nsg -n default-allow-ssh `
    --source-address-prefixes "$meuIp/32" --output none
az network nsg rule create -g $ResourceGroup --nsg-name $nsg -n permitir-http-https --priority 1010 `
    --direction Inbound --access Allow --protocol Tcp --destination-port-ranges 80 443 `
    --source-address-prefixes Internet --output none

$dominio = az vm show -g $ResourceGroup -n $NomeVm --show-details --query fqdns -o tsv
$vm = "azureuser@$dominio"
Write-Host "VM criada: $dominio (SSH liberado só para $meuIp)"

# --- Aplicação ------------------------------------------------------------------
$local = Get-Content (Join-Path $raiz $ArquivoEnv)
$bytes = New-Object byte[] 48
[System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
$envVm = @($local | Where-Object { $_ -match '^(ORACLE_|SEED_)' }) +
    "JWT_CHAVE=$([Convert]::ToBase64String($bytes))" + "DOMINIO=$dominio"

# Arquivos temporários em UTF-8 sem BOM e com LF (o Docker Compose lê o .env literalmente).
$tmp = Join-Path ([IO.Path]::GetTempPath()) "refugio-deploy"
New-Item -ItemType Directory -Force $tmp | Out-Null
[IO.File]::WriteAllText("$tmp/.env", ($envVm -join "`n") + "`n", (New-Object Text.UTF8Encoding $false))
git -C $raiz archive --format=tar.gz --output "$tmp/refugio-api.tar.gz" HEAD

ssh -o StrictHostKeyChecking=accept-new $vm "cloud-init status --wait > /dev/null"
scp "$tmp/refugio-api.tar.gz" "$tmp/.env" "${vm}:/opt/refugio/"
Remove-Item "$tmp/.env"

ssh $vm "cd /opt/refugio && tar -xzf refugio-api.tar.gz && rm refugio-api.tar.gz && chmod 600 .env && sudo docker compose -f docker-compose.yml -f scripts/vm/docker-compose.vm.yml up -d --build"

Write-Host "API publicada em https://$dominio/swagger"
