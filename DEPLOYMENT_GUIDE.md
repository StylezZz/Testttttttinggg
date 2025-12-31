# Guía de Despliegue - Risk List Scraper API

## 📋 Índice
1. [Despliegue Local](#despliegue-local)
2. [Despliegue con Docker](#despliegue-con-docker)
3. [Despliegue en Azure](#despliegue-en-azure)
4. [Despliegue en AWS](#despliegue-en-aws)
5. [Configuración de Producción](#configuración-de-producción)
6. [Monitoreo y Logging](#monitoreo-y-logging)

## 🖥️ Despliegue Local

### Paso 1: Verificar requisitos

```bash
# Verificar versión de .NET
dotnet --version
# Debe ser 8.0.x o superior
```

### Paso 2: Clonar y restaurar

```bash
git clone <repository-url>
cd Testttttttinggg
cd src/RiskListScraperAPI
dotnet restore
```

### Paso 3: Configurar variables

Editar `appsettings.Development.json`:
```json
{
  "ApiKey": "mi-api-key-desarrollo"
}
```

### Paso 4: Ejecutar

```bash
dotnet run
```

Acceder a: `http://localhost:5000/swagger`

---

## 🐳 Despliegue con Docker

### Construcción de la imagen

```bash
# Desde el directorio raíz del proyecto
docker build -t risk-list-scraper-api:1.0 .
```

### Ejecución del contenedor

```bash
docker run -d \
  --name risk-scraper \
  -p 8080:80 \
  -e ApiKey="tu-api-key-produccion" \
  risk-list-scraper-api:1.0
```

### Verificar funcionamiento

```bash
curl http://localhost:8080/health
```

### Docker Compose (opcional)

Crear `docker-compose.yml`:

```yaml
version: '3.8'

services:
  api:
    build: .
    ports:
      - "8080:80"
    environment:
      - ApiKey=tu-api-key-produccion
      - ASPNETCORE_ENVIRONMENT=Production
    restart: unless-stopped
    logging:
      driver: "json-file"
      options:
        max-size: "10m"
        max-file: "3"
```

Ejecutar:
```bash
docker-compose up -d
```

---

## ☁️ Despliegue en Azure

### Opción A: Azure App Service (Recomendado para inicio)

#### 1. Crear recursos con Azure CLI

```bash
# Login
az login

# Variables
RESOURCE_GROUP="rg-risk-scraper"
LOCATION="eastus"
APP_SERVICE_PLAN="plan-risk-scraper"
WEB_APP_NAME="risk-scraper-api-$(date +%s)"

# Crear grupo de recursos
az group create \
  --name $RESOURCE_GROUP \
  --location $LOCATION

# Crear App Service Plan (B1 = $13/mes aprox)
az appservice plan create \
  --name $APP_SERVICE_PLAN \
  --resource-group $RESOURCE_GROUP \
  --sku B1 \
  --is-linux

# Crear Web App
az webapp create \
  --resource-group $RESOURCE_GROUP \
  --plan $APP_SERVICE_PLAN \
  --name $WEB_APP_NAME \
  --runtime "DOTNET|8.0"

# Configurar API Key (IMPORTANTE: usar un valor seguro)
az webapp config appsettings set \
  --resource-group $RESOURCE_GROUP \
  --name $WEB_APP_NAME \
  --settings ApiKey="$(openssl rand -base64 32)"

# Habilitar logs
az webapp log config \
  --resource-group $RESOURCE_GROUP \
  --name $WEB_APP_NAME \
  --application-logging filesystem \
  --level information
```

#### 2. Deploy desde código local

```bash
# Navegar al proyecto
cd src/RiskListScraperAPI

# Publicar
dotnet publish -c Release -o ./publish

# Crear zip
cd publish
zip -r ../deploy.zip .
cd ..

# Deploy
az webapp deployment source config-zip \
  --resource-group $RESOURCE_GROUP \
  --name $WEB_APP_NAME \
  --src deploy.zip

# Obtener URL
echo "API URL: https://$WEB_APP_NAME.azurewebsites.net"
```

#### 3. Verificar despliegue

```bash
curl https://$WEB_APP_NAME.azurewebsites.net/health
```

### Opción B: Azure Container Instances

```bash
# Variables
ACR_NAME="riskscraperregistry"
CONTAINER_NAME="risk-scraper-api"
DNS_LABEL="risk-scraper-api-$(date +%s)"

# Crear Azure Container Registry
az acr create \
  --resource-group $RESOURCE_GROUP \
  --name $ACR_NAME \
  --sku Basic

# Build y push imagen
az acr build \
  --registry $ACR_NAME \
  --image risk-list-scraper-api:v1 \
  --file Dockerfile \
  .

# Habilitar admin
az acr update -n $ACR_NAME --admin-enabled true

# Obtener credenciales
ACR_PASSWORD=$(az acr credential show --name $ACR_NAME --query "passwords[0].value" -o tsv)

# Crear Container Instance
az container create \
  --resource-group $RESOURCE_GROUP \
  --name $CONTAINER_NAME \
  --image $ACR_NAME.azurecr.io/risk-list-scraper-api:v1 \
  --cpu 1 \
  --memory 1.5 \
  --registry-login-server $ACR_NAME.azurecr.io \
  --registry-username $ACR_NAME \
  --registry-password $ACR_PASSWORD \
  --dns-name-label $DNS_LABEL \
  --ports 80 \
  --environment-variables ApiKey="$(openssl rand -base64 32)"

# Obtener URL
echo "API URL: http://$DNS_LABEL.$LOCATION.azurecontainer.io"
```

### Opción C: Azure Kubernetes Service (Para producción escalable)

```bash
# Crear AKS cluster
az aks create \
  --resource-group $RESOURCE_GROUP \
  --name risk-scraper-aks \
  --node-count 2 \
  --node-vm-size Standard_B2s \
  --enable-managed-identity \
  --attach-acr $ACR_NAME

# Conectar a cluster
az aks get-credentials --resource-group $RESOURCE_GROUP --name risk-scraper-aks

# Deploy (crear manifests de Kubernetes - ver sección siguiente)
kubectl apply -f k8s/
```

---

## ☁️ Despliegue en AWS

### Opción A: AWS Elastic Beanstalk

#### 1. Instalar EB CLI

```bash
pip install awsebcli --upgrade
```

#### 2. Inicializar y deploy

```bash
# Desde el directorio raíz
eb init -p "64bit Amazon Linux 2023 v3.0.0 running .NET 8" risk-list-scraper --region us-east-1

# Crear ambiente
eb create risk-scraper-env

# Configurar API Key
eb setenv ApiKey="$(openssl rand -base64 32)"

# Deploy
eb deploy

# Obtener URL
eb status
```

### Opción B: AWS ECS con Fargate

#### 1. Crear ECR y subir imagen

```bash
# Variables
AWS_REGION="us-east-1"
AWS_ACCOUNT_ID=$(aws sts get-caller-identity --query Account --output text)
ECR_REPO="risk-list-scraper-api"

# Crear ECR repository
aws ecr create-repository \
  --repository-name $ECR_REPO \
  --region $AWS_REGION

# Login a ECR
aws ecr get-login-password --region $AWS_REGION | \
  docker login --username AWS --password-stdin \
  $AWS_ACCOUNT_ID.dkr.ecr.$AWS_REGION.amazonaws.com

# Build y push
docker build -t $ECR_REPO .
docker tag $ECR_REPO:latest $AWS_ACCOUNT_ID.dkr.ecr.$AWS_REGION.amazonaws.com/$ECR_REPO:latest
docker push $AWS_ACCOUNT_ID.dkr.ecr.$AWS_REGION.amazonaws.com/$ECR_REPO:latest
```

#### 2. Crear cluster ECS

```bash
# Crear cluster
aws ecs create-cluster --cluster-name risk-scraper-cluster --region $AWS_REGION

# Crear task definition (ver archivo JSON abajo)
aws ecs register-task-definition --cli-input-json file://ecs-task-definition.json
```

**ecs-task-definition.json:**
```json
{
  "family": "risk-scraper-task",
  "networkMode": "awsvpc",
  "requiresCompatibilities": ["FARGATE"],
  "cpu": "256",
  "memory": "512",
  "containerDefinitions": [
    {
      "name": "risk-scraper-api",
      "image": "<AWS_ACCOUNT_ID>.dkr.ecr.<AWS_REGION>.amazonaws.com/risk-list-scraper-api:latest",
      "portMappings": [
        {
          "containerPort": 80,
          "protocol": "tcp"
        }
      ],
      "environment": [
        {
          "name": "ApiKey",
          "value": "your-secure-api-key"
        }
      ],
      "logConfiguration": {
        "logDriver": "awslogs",
        "options": {
          "awslogs-group": "/ecs/risk-scraper",
          "awslogs-region": "us-east-1",
          "awslogs-stream-prefix": "ecs"
        }
      }
    }
  ]
}
```

#### 3. Crear servicio (requiere VPC, subnets, security groups)

```bash
# Este comando requiere configuración previa de VPC
aws ecs create-service \
  --cluster risk-scraper-cluster \
  --service-name risk-scraper-service \
  --task-definition risk-scraper-task \
  --desired-count 1 \
  --launch-type FARGATE \
  --network-configuration "awsvpcConfiguration={subnets=[subnet-xxxxx],securityGroups=[sg-xxxxx],assignPublicIp=ENABLED}"
```

---

## 🔐 Configuración de Producción

### 1. Generar API Key segura

```bash
# Linux/Mac
openssl rand -base64 32

# Windows PowerShell
[Convert]::ToBase64String((1..32 | ForEach-Object { Get-Random -Minimum 0 -Maximum 256 }))
```

### 2. Variables de entorno de producción

```bash
export ApiKey="tu-api-key-segura"
export ASPNETCORE_ENVIRONMENT="Production"
export ASPNETCORE_URLS="http://+:80"
```

### 3. appsettings.Production.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning",
      "RiskListScraperAPI": "Information"
    }
  },
  "AllowedHosts": "*.yourdomain.com",
  "RateLimiting": {
    "MaxRequestsPerMinute": 20,
    "TimeWindowSeconds": 60
  }
}
```

⚠️ **IMPORTANTE**: No incluir `ApiKey` en el archivo de producción. Usar variables de entorno o Azure Key Vault.

---

## 📊 Monitoreo y Logging

### Azure Application Insights

```bash
# Agregar paquete NuGet
dotnet add package Microsoft.ApplicationInsights.AspNetCore

# En Program.cs agregar:
# builder.Services.AddApplicationInsightsTelemetry();
```

### AWS CloudWatch

Los logs de ECS/Fargate se envían automáticamente a CloudWatch.

### Ver logs en producción

**Azure:**
```bash
az webapp log tail --resource-group $RESOURCE_GROUP --name $WEB_APP_NAME
```

**AWS:**
```bash
aws logs tail /ecs/risk-scraper --follow
```

---

## 🧪 Verificación Post-Despliegue

```bash
# Health check
curl https://your-api-url.com/health

# Test con API Key
curl -X POST "https://your-api-url.com/api/RiskList/search" \
  -H "X-API-Key: your-api-key" \
  -H "Content-Type: application/json" \
  -d '{"entityName": "Test"}'

# Verificar rate limiting (ejecutar 21+ veces)
for i in {1..25}; do
  echo "Request $i"
  curl -X POST "https://your-api-url.com/api/RiskList/search" \
    -H "X-API-Key: your-api-key" \
    -H "Content-Type: application/json" \
    -d '{"entityName": "Test"}' \
    -w "\nStatus: %{http_code}\n"
done
```

---

## 🔄 Actualización de la aplicación

### Azure

```bash
# Publicar nueva versión
dotnet publish -c Release -o ./publish
cd publish && zip -r ../deploy.zip . && cd ..

# Deploy
az webapp deployment source config-zip \
  --resource-group $RESOURCE_GROUP \
  --name $WEB_APP_NAME \
  --src deploy.zip
```

### AWS ECS

```bash
# Build y push nueva imagen
docker build -t $ECR_REPO:v2 .
docker tag $ECR_REPO:v2 $AWS_ACCOUNT_ID.dkr.ecr.$AWS_REGION.amazonaws.com/$ECR_REPO:v2
docker push $AWS_ACCOUNT_ID.dkr.ecr.$AWS_REGION.amazonaws.com/$ECR_REPO:v2

# Actualizar servicio
aws ecs update-service \
  --cluster risk-scraper-cluster \
  --service risk-scraper-service \
  --force-new-deployment
```

---

## 💰 Estimación de Costos

### Azure
- **App Service B1**: ~$13/mes
- **Container Instances**: ~$15/mes (1vCPU, 1.5GB RAM)
- **AKS**: ~$75/mes (2 nodes B2s)

### AWS
- **Elastic Beanstalk (t3.micro)**: ~$10/mes
- **ECS Fargate (0.25 vCPU, 0.5GB)**: ~$12/mes
- **ALB**: ~$20/mes (si se usa)

---

## 🆘 Troubleshooting

### Error: "Could not find a part of the path"
- Verificar que todas las rutas en Dockerfile sean correctas
- Verificar estructura de carpetas

### Error 500 en producción
- Revisar logs: `az webapp log tail` o CloudWatch
- Verificar que ApiKey esté configurada
- Verificar connectivity a internet para web scraping

### Rate limiting no funciona
- Verificar que el middleware esté registrado en Program.cs
- Verificar el orden de middlewares (debe ir antes de Authentication)

---

## 📚 Referencias

- [.NET 8 Deployment](https://docs.microsoft.com/en-us/dotnet/core/deploying/)
- [Azure App Service](https://docs.microsoft.com/en-us/azure/app-service/)
- [AWS Elastic Beanstalk .NET](https://docs.aws.amazon.com/elasticbeanstalk/latest/dg/dotnet-core-tutorial.html)
- [Docker .NET](https://docs.microsoft.com/en-us/dotnet/core/docker/introduction)

---

**Última actualización**: 2025-12-31
