#!/bin/bash

# Script para ejecutar la API localmente

echo "🚀 Starting Risk List Scraper API..."

# Verificar que .NET esté instalado
if ! command -v dotnet &> /dev/null; then
    echo "❌ Error: .NET 8.0 SDK no está instalado"
    echo "Por favor instalar desde: https://dotnet.microsoft.com/download/dotnet/8.0"
    exit 1
fi

# Verificar versión de .NET
DOTNET_VERSION=$(dotnet --version)
echo "✅ .NET version: $DOTNET_VERSION"

# Navegar al proyecto
cd src/RiskListScraperAPI

# Restaurar dependencias
echo "📦 Restoring dependencies..."
dotnet restore

# Build
echo "🔨 Building project..."
dotnet build

# Run
echo "▶️  Running API..."
echo "📝 Swagger UI will be available at: http://localhost:5000/swagger"
echo "🏥 Health endpoint: http://localhost:5000/health"
echo "🔑 Default API Key: dev-api-key-12345"
echo ""
dotnet run
