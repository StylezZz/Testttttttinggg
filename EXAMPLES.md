# Ejemplos de Uso - Risk List Scraper API

## 📝 Ejemplos de Solicitudes y Respuestas

### 1. Health Check

**Request:**
```bash
curl http://localhost:5000/health
```

**Response (200 OK):**
```json
{
  "status": "healthy",
  "timestamp": "2025-12-31T10:30:00Z",
  "version": "1.0.0"
}
```

---

### 2. Obtener Fuentes Disponibles

**Request:**
```bash
curl -X GET "http://localhost:5000/api/RiskList/sources" \
  -H "X-API-Key: dev-api-key-12345"
```

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Available sources retrieved successfully",
  "data": [
    "OFAC",
    "World Bank",
    "Offshore Leaks Database"
  ],
  "errors": []
}
```

---

### 3. Búsqueda en Todas las Fuentes

**Request:**
```bash
curl -X POST "http://localhost:5000/api/RiskList/search" \
  -H "X-API-Key: dev-api-key-12345" \
  -H "Content-Type: application/json" \
  -d '{
    "entityName": "PDVSA"
  }'
```

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Search completed successfully. Found 8 total hits.",
  "data": {
    "entityName": "PDVSA",
    "totalHits": 8,
    "results": [
      {
        "source": "OFAC",
        "hitCount": 5,
        "records": [
          {
            "attributes": {
              "Name": "PETROLEOS DE VENEZUELA S.A.",
              "Address": "Avenida Libertador, Caracas, Venezuela",
              "Type": "Entity",
              "Programs": "VENEZUELA",
              "List": "SDN",
              "Score": "100"
            }
          },
          {
            "attributes": {
              "Name": "PDVSA PETROLEO S.A.",
              "Address": "Caracas, Venezuela",
              "Type": "Entity",
              "Programs": "VENEZUELA",
              "List": "SDN",
              "Score": "95"
            }
          }
        ]
      },
      {
        "source": "World Bank",
        "hitCount": 2,
        "records": [
          {
            "attributes": {
              "Firm Name": "PDVSA Services",
              "Address": "123 Main St, Houston, TX",
              "Country": "United States",
              "From Date (Ineligibility Period)": "2019-01-15",
              "To Date (Ineligibility Period)": "2022-01-15",
              "Grounds": "Fraudulent Practice"
            }
          }
        ]
      },
      {
        "source": "Offshore Leaks Database",
        "hitCount": 1,
        "records": [
          {
            "attributes": {
              "Entity": "PDVSA International",
              "Jurisdiction": "Panama",
              "Linked To": "Various Entities",
              "Data From": "Panama Papers"
            }
          }
        ]
      }
    ],
    "searchTimestamp": "2025-12-31T10:30:00Z"
  },
  "errors": []
}
```

---

### 4. Búsqueda en Fuentes Específicas

**Request:**
```bash
curl -X POST "http://localhost:5000/api/RiskList/search" \
  -H "X-API-Key: dev-api-key-12345" \
  -H "Content-Type: application/json" \
  -d '{
    "entityName": "Rosneft",
    "sources": ["OFAC"]
  }'
```

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Search completed successfully. Found 3 total hits.",
  "data": {
    "entityName": "Rosneft",
    "totalHits": 3,
    "results": [
      {
        "source": "OFAC",
        "hitCount": 3,
        "records": [
          {
            "attributes": {
              "Name": "ROSNEFT OIL COMPANY",
              "Address": "Moscow, Russia",
              "Type": "Entity",
              "Programs": "UKRAINE-EO13662",
              "List": "SSI",
              "Score": "100"
            }
          }
        ]
      }
    ],
    "searchTimestamp": "2025-12-31T10:35:00Z"
  },
  "errors": []
}
```

---

### 5. Búsqueda Sin Resultados

**Request:**
```bash
curl -X POST "http://localhost:5000/api/RiskList/search" \
  -H "X-API-Key: dev-api-key-12345" \
  -H "Content-Type: application/json" \
  -d '{
    "entityName": "XYZ123NonExistent"
  }'
```

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Search completed successfully. Found 0 total hits.",
  "data": {
    "entityName": "XYZ123NonExistent",
    "totalHits": 0,
    "results": [
      {
        "source": "OFAC",
        "hitCount": 0,
        "records": []
      },
      {
        "source": "World Bank",
        "hitCount": 0,
        "records": []
      },
      {
        "source": "Offshore Leaks Database",
        "hitCount": 0,
        "records": []
      }
    ],
    "searchTimestamp": "2025-12-31T10:40:00Z"
  },
  "errors": []
}
```

---

## ❌ Ejemplos de Errores

### 1. Error 400 - Nombre de Entidad Vacío

**Request:**
```bash
curl -X POST "http://localhost:5000/api/RiskList/search" \
  -H "X-API-Key: dev-api-key-12345" \
  -H "Content-Type: application/json" \
  -d '{
    "entityName": ""
  }'
```

**Response (400 Bad Request):**
```json
{
  "success": false,
  "message": "Invalid request",
  "data": null,
  "errors": [
    "Entity name must be between 2 and 200 characters"
  ]
}
```

---

### 2. Error 401 - API Key Faltante

**Request:**
```bash
curl -X POST "http://localhost:5000/api/RiskList/search" \
  -H "Content-Type: application/json" \
  -d '{
    "entityName": "Test"
  }'
```

**Response (401 Unauthorized):**
```json
{
  "success": false,
  "message": "API Key is missing. Please provide the X-API-Key header.",
  "errors": [
    "Missing API Key"
  ]
}
```

---

### 3. Error 401 - API Key Inválida

**Request:**
```bash
curl -X POST "http://localhost:5000/api/RiskList/search" \
  -H "X-API-Key: invalid-key" \
  -H "Content-Type: application/json" \
  -d '{
    "entityName": "Test"
  }'
```

**Response (401 Unauthorized):**
```json
{
  "success": false,
  "message": "Invalid API Key provided.",
  "errors": [
    "Unauthorized"
  ]
}
```

---

### 4. Error 429 - Rate Limit Excedido

**Request (después de 20 llamadas en un minuto):**
```bash
curl -X POST "http://localhost:5000/api/RiskList/search" \
  -H "X-API-Key: dev-api-key-12345" \
  -H "Content-Type: application/json" \
  -d '{
    "entityName": "Test"
  }'
```

**Response (429 Too Many Requests):**
```json
{
  "success": false,
  "message": "Rate limit exceeded. Maximum 20 requests per minute allowed.",
  "errors": [
    "Please retry after 45 seconds."
  ]
}
```

**Headers:**
```
X-RateLimit-Limit: 20
X-RateLimit-Remaining: 0
X-RateLimit-Reset: 1735645800
Retry-After: 45
```

---

## 📊 Ejemplos Avanzados

### 1. Búsqueda de Persona

**Request:**
```bash
curl -X POST "http://localhost:5000/api/RiskList/search" \
  -H "X-API-Key: dev-api-key-12345" \
  -H "Content-Type: application/json" \
  -d '{
    "entityName": "Vladimir Putin"
  }'
```

### 2. Búsqueda de Banco

**Request:**
```bash
curl -X POST "http://localhost:5000/api/RiskList/search" \
  -H "X-API-Key: dev-api-key-12345" \
  -H "Content-Type: application/json" \
  -d '{
    "entityName": "Bank Melli"
  }'
```

### 3. Búsqueda con Timeout

**Request con timeout de 10 segundos:**
```bash
curl -X POST "http://localhost:5000/api/RiskList/search" \
  --max-time 10 \
  -H "X-API-Key: dev-api-key-12345" \
  -H "Content-Type: application/json" \
  -d '{
    "entityName": "Complex Entity Name"
  }'
```

---

## 🔄 Script de Pruebas Completo

### Bash Script

```bash
#!/bin/bash

API_URL="http://localhost:5000"
API_KEY="dev-api-key-12345"

echo "=== Testing Risk List Scraper API ==="

# 1. Health Check
echo -e "\n1. Health Check"
curl -s "$API_URL/health" | jq

# 2. Get Sources
echo -e "\n2. Get Available Sources"
curl -s -X GET "$API_URL/api/RiskList/sources" \
  -H "X-API-Key: $API_KEY" | jq

# 3. Search Entity
echo -e "\n3. Search Entity: PDVSA"
curl -s -X POST "$API_URL/api/RiskList/search" \
  -H "X-API-Key: $API_KEY" \
  -H "Content-Type: application/json" \
  -d '{"entityName": "PDVSA"}' | jq

# 4. Test Invalid Request
echo -e "\n4. Test Invalid Request (empty name)"
curl -s -X POST "$API_URL/api/RiskList/search" \
  -H "X-API-Key: $API_KEY" \
  -H "Content-Type: application/json" \
  -d '{"entityName": ""}' | jq

# 5. Test Invalid API Key
echo -e "\n5. Test Invalid API Key"
curl -s -X POST "$API_URL/api/RiskList/search" \
  -H "X-API-Key: invalid-key" \
  -H "Content-Type: application/json" \
  -d '{"entityName": "Test"}' | jq

echo -e "\n=== Tests Completed ==="
```

### PowerShell Script

```powershell
$ApiUrl = "http://localhost:5000"
$ApiKey = "dev-api-key-12345"

Write-Host "=== Testing Risk List Scraper API ===" -ForegroundColor Green

# 1. Health Check
Write-Host "`n1. Health Check" -ForegroundColor Yellow
Invoke-RestMethod -Uri "$ApiUrl/health" -Method Get | ConvertTo-Json

# 2. Get Sources
Write-Host "`n2. Get Available Sources" -ForegroundColor Yellow
Invoke-RestMethod -Uri "$ApiUrl/api/RiskList/sources" `
  -Method Get `
  -Headers @{"X-API-Key" = $ApiKey} | ConvertTo-Json

# 3. Search Entity
Write-Host "`n3. Search Entity: PDVSA" -ForegroundColor Yellow
$body = @{entityName = "PDVSA"} | ConvertTo-Json
Invoke-RestMethod -Uri "$ApiUrl/api/RiskList/search" `
  -Method Post `
  -Headers @{"X-API-Key" = $ApiKey; "Content-Type" = "application/json"} `
  -Body $body | ConvertTo-Json -Depth 10

Write-Host "`n=== Tests Completed ===" -ForegroundColor Green
```

---

## 📈 Prueba de Rate Limiting

```bash
#!/bin/bash

API_URL="http://localhost:5000/api/RiskList/search"
API_KEY="dev-api-key-12345"

echo "Testing Rate Limiting (20 requests per minute)"

for i in {1..25}; do
  echo -e "\nRequest #$i"

  response=$(curl -s -w "\nHTTP_CODE:%{http_code}" \
    -X POST "$API_URL" \
    -H "X-API-Key: $API_KEY" \
    -H "Content-Type: application/json" \
    -d '{"entityName": "Test"}')

  http_code=$(echo "$response" | grep "HTTP_CODE" | cut -d: -f2)

  if [ "$http_code" == "429" ]; then
    echo "Rate limit exceeded at request #$i"
    echo "$response" | grep -v "HTTP_CODE" | jq
    break
  else
    echo "Status: $http_code - OK"
  fi

  sleep 0.5
done
```

---

## 🎯 Entidades de Ejemplo para Probar

### Empresas Conocidas en Listas de Sanciones
- `PDVSA` - Petrolera venezolana
- `Rosneft` - Petrolera rusa
- `Bank Melli` - Banco iraní
- `Gazprom` - Empresa de gas rusa
- `Huawei` - Tecnología china (en algunas listas)

### Búsquedas Genéricas
- `Bank` - Encuentra múltiples bancos
- `Oil` - Empresas petroleras
- `Trading` - Empresas comerciales
- `Corporation` - Corporaciones varias

### Casos Edge
- `""` - String vacío (debe fallar con 400)
- `"A"` - Muy corto (debe fallar con 400)
- `"XYZ123NonExistent"` - Sin resultados (debe retornar 0 hits)

---

**Nota**: Los resultados exactos dependerán de los datos actuales en las fuentes y de la implementación del scraping. Estos ejemplos son ilustrativos del formato de respuesta esperado.
