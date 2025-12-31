# Script para ejecutar la API localmente en Windows

Write-Host "🚀 Starting Risk List Scraper API..." -ForegroundColor Green

# Verificar que .NET esté instalado
try {
    $dotnetVersion = dotnet --version
    Write-Host "✅ .NET version: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host "❌ Error: .NET 8.0 SDK no está instalado" -ForegroundColor Red
    Write-Host "Por favor instalar desde: https://dotnet.microsoft.com/download/dotnet/8.0" -ForegroundColor Yellow
    exit 1
}

# Navegar al proyecto
Set-Location -Path "src\RiskListScraperAPI"

# Restaurar dependencias
Write-Host "📦 Restoring dependencies..." -ForegroundColor Cyan
dotnet restore

# Build
Write-Host "🔨 Building project..." -ForegroundColor Cyan
dotnet build

# Run
Write-Host "▶️  Running API..." -ForegroundColor Cyan
Write-Host "📝 Swagger UI will be available at: http://localhost:5000/swagger" -ForegroundColor Yellow
Write-Host "🏥 Health endpoint: http://localhost:5000/health" -ForegroundColor Yellow
Write-Host "🔑 Default API Key: dev-api-key-12345" -ForegroundColor Yellow
Write-Host ""
dotnet run
