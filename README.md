# Risk List Scraper API

API REST para búsqueda automatizada de entidades en listas de alto riesgo mediante web scraping.

## 📋 Descripción

Esta API permite buscar entidades (empresas o personas) en las siguientes bases de datos de alto riesgo:

- **OFAC** (Office of Foreign Assets Control): Lista de sanciones del Tesoro de Estados Unidos
- **World Bank**: Lista de empresas inhabilitadas del Banco Mundial
- **Offshore Leaks Database**: Base de datos de paraísos fiscales del ICIJ

## 🚀 Características

- ✅ Web scraping de múltiples fuentes en paralelo
- ✅ Autenticación mediante API Key
- ✅ Rate limiting (20 llamadas por minuto)
- ✅ Manejo robusto de errores
- ✅ Validación de datos de entrada
- ✅ Documentación Swagger/OpenAPI
- ✅ Logging detallado
- ✅ CORS habilitado

## 🛠️ Tecnologías

- **.NET 8.0** - Framework principal
- **ASP.NET Core** - Web API
- **HtmlAgilityPack** - Web scraping
- **Swagger/OpenAPI** - Documentación
- **Custom Middleware** - Rate limiting y autenticación

## 📦 Requisitos Previos

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) o [Visual Studio Code](https://code.visualstudio.com/)
- [Postman](https://www.postman.com/downloads/) (para pruebas)

## 🔧 Instalación y Configuración

### 1. Clonar el repositorio

```bash
git clone <repository-url>
cd Testttttttinggg
```

### 2. Restaurar dependencias

```bash
cd src/RiskListScraperAPI
dotnet restore
```

### 3. Configurar API Key

Editar el archivo `appsettings.json` o `appsettings.Development.json`:

```json
{
  "ApiKey": "tu-api-key-segura-aqui"
}
```

**IMPORTANTE**: En producción, usa variables de entorno o Azure Key Vault para almacenar el API Key de forma segura.

### 4. Ejecutar la aplicación

```bash
dotnet run
```

La API estará disponible en:
- HTTP: `http://localhost:5000`
- HTTPS: `https://localhost:5001`
- Swagger: `http://localhost:5000/swagger`

## 📚 Uso de la API

### Endpoints Principales

#### 1. Health Check
```http
GET /health
```

#### 2. Obtener Fuentes Disponibles
```http
GET /api/RiskList/sources
Headers:
  X-API-Key: dev-api-key-12345
```

#### 3. Buscar Entidad
```http
POST /api/RiskList/search
Headers:
  X-API-Key: dev-api-key-12345
  Content-Type: application/json

Body:
{
  "entityName": "PDVSA",
  "sources": ["OFAC", "World Bank"]  // Opcional
}
```

### Respuesta Exitosa

```json
{
  "success": true,
  "message": "Search completed successfully. Found 5 total hits.",
  "data": {
    "entityName": "PDVSA",
    "totalHits": 5,
    "results": [
      {
        "source": "OFAC",
        "hitCount": 3,
        "records": [
          {
            "attributes": {
              "Name": "PETROLEOS DE VENEZUELA S.A.",
              "Address": "Caracas, Venezuela",
              "Type": "Entity",
              "Programs": "VENEZUELA",
              "List": "SDN",
              "Score": "100"
            }
          }
        ]
      },
      {
        "source": "World Bank",
        "hitCount": 2,
        "records": [...]
      }
    ],
    "searchTimestamp": "2025-12-31T10:30:00Z"
  }
}
```

### Códigos de Respuesta

- `200 OK` - Búsqueda exitosa
- `400 Bad Request` - Datos inválidos
- `401 Unauthorized` - API Key inválida o faltante
- `429 Too Many Requests` - Límite de rate excedido
- `500 Internal Server Error` - Error del servidor

## 🔐 Autenticación

La API usa autenticación basada en API Key mediante el header `X-API-Key`.

Ejemplo:
```bash
curl -X POST "http://localhost:5000/api/RiskList/search" \
  -H "X-API-Key: dev-api-key-12345" \
  -H "Content-Type: application/json" \
  -d '{"entityName": "PDVSA"}'
```

## ⚡ Rate Limiting

- **Límite**: 20 llamadas por minuto por API Key/IP
- **Headers de respuesta**:
  - `X-RateLimit-Limit`: Límite máximo
  - `X-RateLimit-Remaining`: Llamadas restantes
  - `X-RateLimit-Reset`: Timestamp de reseteo
  - `Retry-After`: Segundos para reintentar (en caso de 429)

## 📮 Colección de Postman

### Importar la Colección

1. Abrir Postman
2. Click en "Import"
3. Seleccionar el archivo `postman/RiskListScraperAPI.postman_collection.json`
4. Importar también `postman/RiskListScraperAPI.postman_environment.json`

### Ejemplos Incluidos

La colección incluye:
- ✅ Health check
- ✅ Obtener fuentes disponibles
- ✅ Búsqueda en todas las fuentes
- ✅ Búsqueda en fuentes específicas
- ✅ Ejemplos de validación (errores 400)
- ✅ Ejemplos de autenticación (errores 401)

## 🐳 Despliegue con Docker

### Crear Dockerfile

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/RiskListScraperAPI/RiskListScraperAPI.csproj", "RiskListScraperAPI/"]
RUN dotnet restore "RiskListScraperAPI/RiskListScraperAPI.csproj"
COPY src/RiskListScraperAPI/. RiskListScraperAPI/
WORKDIR "/src/RiskListScraperAPI"
RUN dotnet build "RiskListScraperAPI.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "RiskListScraperAPI.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RiskListScraperAPI.dll"]
```

### Construir y Ejecutar

```bash
docker build -t risk-list-scraper-api .
docker run -p 5000:80 -e ApiKey="your-secure-key" risk-list-scraper-api
```

## ☁️ Despliegue en Azure

### Opción 1: Azure App Service

```bash
# Login
az login

# Crear grupo de recursos
az group create --name rg-risk-scraper --location eastus

# Crear App Service Plan
az appservice plan create --name plan-risk-scraper --resource-group rg-risk-scraper --sku B1 --is-linux

# Crear Web App
az webapp create --resource-group rg-risk-scraper --plan plan-risk-scraper --name risk-list-scraper-api --runtime "DOTNET|8.0"

# Configurar API Key
az webapp config appsettings set --resource-group rg-risk-scraper --name risk-list-scraper-api --settings ApiKey="your-secure-key"

# Deploy
dotnet publish -c Release
cd bin/Release/net8.0/publish
zip -r deploy.zip .
az webapp deployment source config-zip --resource-group rg-risk-scraper --name risk-list-scraper-api --src deploy.zip
```

### Opción 2: Azure Container Instances

```bash
# Crear Azure Container Registry
az acr create --resource-group rg-risk-scraper --name riskscraperregistry --sku Basic

# Build y push
az acr build --registry riskscraperregistry --image risk-list-scraper-api:v1 .

# Deploy a Container Instance
az container create --resource-group rg-risk-scraper --name risk-scraper-api --image riskscraperregistry.azurecr.io/risk-list-scraper-api:v1 --dns-name-label risk-scraper-api --ports 80 --environment-variables ApiKey="your-secure-key"
```

## ☁️ Despliegue en AWS

### Opción 1: AWS Elastic Beanstalk

```bash
# Instalar EB CLI
pip install awsebcli

# Inicializar
eb init -p "64bit Amazon Linux 2 v2.6.0 running .NET 8" risk-list-scraper

# Crear ambiente
eb create risk-scraper-env

# Configurar variables de entorno
eb setenv ApiKey="your-secure-key"

# Deploy
eb deploy
```

### Opción 2: AWS ECS (Fargate)

```bash
# Crear ECR repository
aws ecr create-repository --repository-name risk-list-scraper-api

# Build y push Docker image
aws ecr get-login-password --region us-east-1 | docker login --username AWS --password-stdin <account-id>.dkr.ecr.us-east-1.amazonaws.com
docker build -t risk-list-scraper-api .
docker tag risk-list-scraper-api:latest <account-id>.dkr.ecr.us-east-1.amazonaws.com/risk-list-scraper-api:latest
docker push <account-id>.dkr.ecr.us-east-1.amazonaws.com/risk-list-scraper-api:latest

# Crear cluster ECS y servicio (usar AWS Console o CloudFormation)
```

## 🧪 Testing

### Ejecutar con curl

```bash
# Health check
curl http://localhost:5000/health

# Búsqueda básica
curl -X POST "http://localhost:5000/api/RiskList/search" \
  -H "X-API-Key: dev-api-key-12345" \
  -H "Content-Type: application/json" \
  -d '{"entityName": "PDVSA"}'

# Búsqueda con fuentes específicas
curl -X POST "http://localhost:5000/api/RiskList/search" \
  -H "X-API-Key: dev-api-key-12345" \
  -H "Content-Type: application/json" \
  -d '{"entityName": "Bank", "sources": ["OFAC", "World Bank"]}'
```

## 📁 Estructura del Proyecto

```
Testttttttinggg/
├── src/
│   └── RiskListScraperAPI/
│       ├── Controllers/
│       │   └── RiskListController.cs
│       ├── Services/
│       │   ├── IScraperService.cs
│       │   ├── OfacScraperService.cs
│       │   ├── WorldBankScraperService.cs
│       │   ├── OffshoreLeaksScraperService.cs
│       │   └── RiskListSearchService.cs
│       ├── Models/
│       │   ├── SearchRequest.cs
│       │   ├── SearchResponse.cs
│       │   ├── ApiResponse.cs
│       │   ├── OfacRecord.cs
│       │   ├── WorldBankRecord.cs
│       │   └── OffshoreLeaksRecord.cs
│       ├── Middleware/
│       │   ├── ApiKeyAuthMiddleware.cs
│       │   ├── RateLimitingMiddleware.cs
│       │   └── GlobalExceptionMiddleware.cs
│       ├── Properties/
│       │   └── launchSettings.json
│       ├── appsettings.json
│       ├── appsettings.Development.json
│       ├── Program.cs
│       └── RiskListScraperAPI.csproj
├── postman/
│   ├── RiskListScraperAPI.postman_collection.json
│   └── RiskListScraperAPI.postman_environment.json
└── README.md
```

## 🔍 Ejemplos de Entidades para Buscar

### Empresas
- `PDVSA` - Petrolera venezolana
- `Rosneft` - Empresa petrolera rusa
- `Bank Melli` - Banco iraní
- `Gazprom` - Empresa de gas rusa

### Personas
- Nombres de funcionarios en listas de sanciones
- Nombres asociados a paraísos fiscales

### Términos Generales
- `Bank`
- `Oil`
- `Trading`

## ⚠️ Consideraciones Importantes

### Legal
- Esta herramienta es para fines educativos y de compliance
- Asegúrate de cumplir con los términos de servicio de las fuentes
- El web scraping debe usarse de manera responsable

### Técnicas
- Las fuentes pueden cambiar su estructura HTML
- Implementa caché si planeas hacer búsquedas frecuentes
- Considera usar APIs oficiales cuando estén disponibles
- Los scrapers pueden necesitar actualizaciones periódicas

### Seguridad
- Nunca expongas tu API Key en el código
- Usa HTTPS en producción
- Implementa logging para auditoría
- Considera agregar autenticación más robusta (JWT, OAuth)

## 🐛 Troubleshooting

### Error: "dotnet: command not found"
```bash
# Instalar .NET 8.0 SDK
# Windows: Descargar desde https://dotnet.microsoft.com/download
# Linux: sudo apt-get install -y dotnet-sdk-8.0
# macOS: brew install --cask dotnet-sdk
```

### Error: "API Key not configured"
```bash
# Asegúrate de tener configurado el ApiKey en appsettings.json
```

### Error 429: Rate limit exceeded
```bash
# Espera el tiempo indicado en el header Retry-After
# O usa diferentes API Keys para distribuir las llamadas
```

## 📝 Licencia

Este proyecto es un ejercicio técnico para evaluación.

## 👤 Autor

Desarrollado como prueba técnica para posición de C# Fullstack Developer.

## 📞 Soporte

Para preguntas o problemas, contactar al equipo de desarrollo.

---

**Nota**: Este README contiene toda la información necesaria para desplegar y usar la API. Para más detalles técnicos, consultar la documentación Swagger en `/swagger`.
