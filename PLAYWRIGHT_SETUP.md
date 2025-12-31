# Configuración de Playwright para .NET

## Descripción

Este proyecto utiliza **Microsoft Playwright** para realizar web scraping de páginas que cargan contenido dinámicamente con JavaScript, específicamente para la fuente de datos ICIJ Offshore Leaks.

## Instalación

### 1. Restaurar dependencias de NuGet

```bash
cd src/RiskListScraperAPI
dotnet restore
```

### 2. Instalar los navegadores de Playwright

Después de restaurar los paquetes, es necesario instalar los navegadores que Playwright utilizará. Ejecuta el siguiente comando:

```bash
pwsh bin/Debug/net8.0/playwright.ps1 install
```

O en sistemas Linux/Mac:

```bash
dotnet exec bin/Debug/net8.0/playwright.ps1 install
```

Alternativamente, puedes usar la herramienta global de Playwright:

```bash
# Instalar la herramienta global
dotnet tool install --global Microsoft.Playwright.CLI

# Instalar los navegadores
playwright install
```

### 3. Instalar dependencias del sistema (Linux)

En sistemas Linux, Playwright puede requerir dependencias adicionales del sistema. Ejecuta:

```bash
playwright install-deps
```

## Fuentes de Datos Implementadas

### 1. ICIJ Offshore Leaks (con Playwright)

- **URL**: https://offshoreleaks.icij.org/search?q={query}
- **Tecnología**: Playwright (scraping de DOM renderizado)
- **Datos extraídos**:
  - Entity (nombre de la entidad)
  - Jurisdiction (jurisdicción)
  - Linked To (vinculado a)
  - Data From (fuente de datos)

**Nota**: Esta página carga datos dinámicamente con JavaScript y NO expone una API pública, por lo que es necesario usar Playwright para acceder al DOM renderizado.

### 2. World Bank API

- **URL**: https://apigwext.worldbank.org/dvsvc/v1.0/json/APPLICATION/ADOBE_EXPRNCE_MGR/FIRM/SANCTIONED_FIRM
- **Tecnología**: API REST con autenticación por API Key
- **API Key**: z9duUaFUiEUYSHs97CU38fcZO7ipOPvm
- **Datos extraídos**:
  - Firm Name
  - Address
  - Country
  - Ineligibility From/To Date
  - Grounds
  - Sanction Type

### 3. OFAC (Office of Foreign Assets Control)

- **Tecnología**: HTTP + HtmlAgilityPack

## Ejecución

```bash
cd src/RiskListScraperAPI
dotnet run
```

La API estará disponible en:
- HTTP: http://localhost:5000
- HTTPS: https://localhost:5001
- Swagger UI: http://localhost:5000/swagger

## Ejemplo de Uso

```bash
curl -X POST "http://localhost:5000/api/risklist/search" \
  -H "X-API-Key: YOUR_API_KEY" \
  -H "Content-Type: application/json" \
  -d '{
    "entityName": "Lima",
    "sources": ["World Bank", "Offshore Leaks Database"]
  }'
```

## Notas Técnicas

### Playwright vs HttpClient

- **HttpClient/HtmlAgilityPack**: Solo puede acceder al HTML inicial que el servidor envía. No ejecuta JavaScript.
- **Playwright**: Lanza un navegador real (Chromium, Firefox, WebKit), ejecuta JavaScript, espera a que el contenido se cargue dinámicamente, y luego permite acceder al DOM completamente renderizado.

### Ventajas de Playwright

1. Maneja contenido cargado dinámicamente con JavaScript
2. Puede esperar a que elementos específicos aparezcan
3. Simula comportamiento real de usuario
4. Soporta navegación compleja y SPA (Single Page Applications)
5. Headless mode para ejecución sin interfaz gráfica

### Rendimiento

El scraping con Playwright es más lento que las llamadas API o scraping estático porque:
- Requiere lanzar un navegador
- Espera a que JavaScript se ejecute
- Renderiza el DOM completo

Sin embargo, es la única opción viable para páginas que no exponen API pública y cargan datos dinámicamente.

## Troubleshooting

### Error: "Executable doesn't exist"

Si recibes un error indicando que el ejecutable del navegador no existe, ejecuta:

```bash
playwright install chromium
```

### Error de permisos en Linux

Si tienes problemas de permisos, ejecuta:

```bash
sudo playwright install-deps
```

### Timeout errors

Si las búsquedas tardan demasiado o dan timeout, puedes ajustar los timeouts en `OffshoreLeaksScraperService.cs`:

```csharp
await page.GotoAsync(searchUrl, new PageGotoOptions
{
    WaitUntil = WaitUntilState.NetworkIdle,
    Timeout = 60000 // Aumentar este valor si es necesario
});
```
