<#
.SYNOPSIS
  Cria a infraestrutura da Refúgio API no Azure (Azure CLI) e publica a API numa VM Linux com Docker.

.DESCRIPTION
  1. Resource group, VM Ubuntu 24.04 com Docker (cloud-init), IP público com nome DNS e portas 80/443.
  2. Copia o código (git archive) para a VM, grava o .env lá e sobe API + Caddy (HTTPS automático).
  Credenciais NÃO ficam no repositório: são lidas do .env local (ORACLE_USUARIO, ORACLE_SENHA, JWT_CHAVE).

.EXAMPLE
  az login
  az account set --subscription "<nome ou id>"
  ./scripts/provisionar-vm.ps1 -ResourceGroup rg-refugio -Regiao brazilsouth -NomeDns refugio-api-gs
#>
param(
    [string]$ResourceGroup = "rg-refugio",
    [string]$Regiao = "brazilsouth",
    [string]$NomeVm = "vm-refugio-api",
    [string]$Tamanho = "Standard_B2s",
    [Parameter(Mandatory)][string]$NomeDns,
    [string]$ArquivoEnv = ".env"
)
$ErrorActionPreference = "Stop"
$raiz = Split-Path $PSScriptRoot -Parent

# --- Infraestrutura -----------------------------------------------------------
az group create --name $ResourceGroup --location $Regiao --output none

az vm create `
    --resource-group $ResourceGroup `
    --name $NomeVm `
    --image Ubuntu2404 `
    --size $Tamanho `
    --admin-username azureuser `
    --generate-ssh-keys `
    --public-ip-sku Standard `
    --public-ip-address-dns-name $NomeDns `
    --custom-data "$raiz/scripts/vm/cloud-init.yml" `
    --output none

az vm open-port --resource-group $ResourceGroup --name $NomeVm --port 80 --priority 1010 --output none
az vm open-port --resource-group $ResourceGroup --name $NomeVm --port 443 --priority 1020 --output none

$dominio = az vm show --resource-group $ResourceGroup --name $NomeVm --show-details --query fqdns --output tsv
Write-Host "VM criada: $dominio"

# --- Aplicação ------------------------------------------------------------------
$envLocal = Get-Content (Join-Path $raiz $ArquivoEnv) | Where-Object { $_ -match '^(ORACLE_|JWT_CHAVE|SEED_)' }
$envVm = @($envLocal) + "DOMINIO=$dominio"

$pacote = Join-Path ([IO.Path]::GetTempPath()) "refugio-api.tar.gz"
git -C $raiz archive --format=tar.gz --output $pacote HEAD

# Espera o cloud-init terminar de instalar o Docker.
ssh -o StrictHostKeyChecking=accept-new "azureuser@$dominio" "cloud-init status --wait > /dev/null"
scp $pacote "azureuser@${dominio}:/opt/refugio/refugio-api.tar.gz"
($envVm -join "`n") | ssh "azureuser@$dominio" "cat > /opt/refugio/.env.tmp"

ssh "azureuser@$dominio" @'
set -e
cd /opt/refugio
tar -xzf refugio-api.tar.gz && rm refugio-api.tar.gz
mv .env.tmp .env && chmod 600 .env
sudo docker compose -f docker-compose.yml -f scripts/vm/docker-compose.vm.yml up -d --build
'@

Write-Host "API publicada em https://$dominio/swagger"
