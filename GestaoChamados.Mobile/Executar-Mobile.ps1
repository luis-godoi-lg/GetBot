# Script para executar a aplicação MAUI Windows
# GestaoChamados Mobile - Versão 1.0

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Gestão de Chamados - Mobile MAUI" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Diretório do projeto
$projectDir = Split-Path -Parent $MyInvocation.MyCommand.Path

# Verificar se a API está rodando
Write-Host "📡 Verificando conexão com a API..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "https://localhost:7001/api/health" -Method GET -SkipCertificateCheck -ErrorAction SilentlyContinue -TimeoutSec 2
    Write-Host "✅ API está respondendo!" -ForegroundColor Green
} catch {
    Write-Host "⚠️  API não encontrada em https://localhost:7001" -ForegroundColor Yellow
    Write-Host "    Execute primeiro: cd ..\GestaoChamados.API; dotnet run" -ForegroundColor Gray
}

Write-Host ""
Write-Host "🔨 Compilando aplicação..." -ForegroundColor Cyan
dotnet build -f net9.0-windows10.0.19041.0 -c Debug

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "❌ Erro na compilação!" -ForegroundColor Red
    exit $LASTEXITCODE
}

Write-Host ""
Write-Host "✅ Compilação concluída com sucesso!" -ForegroundColor Green
Write-Host ""

# Caminho do executável
$exePath = "$projectDir\bin\Debug\net9.0-windows10.0.19041.0\win10-x64\GestaoChamados.Mobile.exe"

if (-not (Test-Path $exePath)) {
    Write-Host "❌ Executável não encontrado!" -ForegroundColor Red
    exit 1
}

# Executar a aplicação
Write-Host "🚀 Iniciando aplicação..." -ForegroundColor Green
Write-Host ""
Write-Host "📍 Executável: GestaoChamados.Mobile.exe" -ForegroundColor Cyan
Write-Host "🌐 API Base URL: https://localhost:7001" -ForegroundColor Yellow
Write-Host ""
Write-Host "🔐 Credenciais de teste:" -ForegroundColor Yellow
Write-Host "   • Usuário: usuario@email.com / senha123" -ForegroundColor White
Write-Host "   • Técnico: tecnico@email.com / senha123" -ForegroundColor White
Write-Host "   • Admin: admin@email.com / admin123" -ForegroundColor White
Write-Host ""

Start-Process $exePath

Start-Sleep -Seconds 2

# Verificar se o processo foi iniciado
$process = Get-Process -Name "GestaoChamados.Mobile" -ErrorAction SilentlyContinue

if ($process) {
    Write-Host "✅ Aplicação iniciada com sucesso!" -ForegroundColor Green
    Write-Host "   PID: $($process.Id)" -ForegroundColor Cyan
    $status = if ($process.Responding) { "Respondendo" } else { "Não Respondendo" }
    Write-Host "   Status: $status" -ForegroundColor Cyan
} else {
    Write-Host "⚠️  Processo não encontrado. Verifique se a aplicação abriu." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

