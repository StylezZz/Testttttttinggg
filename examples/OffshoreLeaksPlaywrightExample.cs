/*
 * Ejemplo de Web Scraping con Playwright para ICIJ Offshore Leaks
 *
 * Este programa demuestra cómo usar Microsoft Playwright para hacer scraping
 * de una página que carga datos dinámicamente con JavaScript.
 *
 * URL objetivo: https://offshoreleaks.icij.org/search?q=Lima
 *
 * Datos a extraer:
 * - Entity (nombre de la entidad)
 * - Jurisdiction (jurisdicción)
 * - Linked To (vinculado a)
 * - Data From (fuente de datos)
 */

using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OffshoreLeaksScraperExample
{
    public class OffshoreLeaksRecord
    {
        public string Entity { get; set; } = string.Empty;
        public string Jurisdiction { get; set; } = string.Empty;
        public string LinkedTo { get; set; } = string.Empty;
        public string DataFrom { get; set; } = string.Empty;

        public override string ToString()
        {
            return $"Entity: {Entity}\n" +
                   $"Jurisdiction: {Jurisdiction}\n" +
                   $"Linked To: {LinkedTo}\n" +
                   $"Data From: {DataFrom}\n" +
                   new string('-', 50);
        }
    }

    public class OffshoreLeaksScraper
    {
        private const string BASE_URL = "https://offshoreleaks.icij.org";

        /// <summary>
        /// Realiza scraping de la página de Offshore Leaks usando Playwright
        /// </summary>
        /// <param name="searchQuery">Término de búsqueda (ej: "Lima")</param>
        /// <returns>Lista de registros encontrados</returns>
        public async Task<List<OffshoreLeaksRecord>> ScrapeAsync(string searchQuery)
        {
            var results = new List<OffshoreLeaksRecord>();

            // IMPORTANTE: Inicializar Playwright
            // Esto requiere que los navegadores estén instalados con: playwright install
            using var playwright = await Playwright.CreateAsync();

            Console.WriteLine("Iniciando navegador Chromium en modo headless...");

            // Lanzar el navegador en modo headless (sin ventana visible)
            await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true, // Cambiar a false para ver el navegador en acción
                SlowMo = 50 // Opcional: ralentizar las acciones para debugging
            });

            // Crear una nueva página (pestaña)
            var page = await browser.NewPageAsync();

            try
            {
                var searchUrl = $"{BASE_URL}/search?q={Uri.EscapeDataString(searchQuery)}";
                Console.WriteLine($"Navegando a: {searchUrl}");

                // Navegar a la URL y esperar a que la red esté inactiva
                // WaitUntil.NetworkIdle significa que esperará hasta que no haya más de 2 conexiones de red
                // durante al menos 500ms
                await page.GotoAsync(searchUrl, new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.NetworkIdle,
                    Timeout = 60000 // 60 segundos de timeout
                });

                Console.WriteLine("Página cargada. Esperando a que aparezcan los resultados...");

                // Esperar a que los resultados de búsqueda aparezcan
                // Usamos múltiples selectores porque la estructura puede variar
                try
                {
                    await page.WaitForSelectorAsync(
                        ".search-result, .result-entity, [class*='result'], [class*='SearchResult']",
                        new PageWaitForSelectorOptions
                        {
                            Timeout = 30000 // 30 segundos
                        }
                    );

                    Console.WriteLine("Resultados encontrados. Extrayendo datos...");
                }
                catch (TimeoutException)
                {
                    Console.WriteLine("No se encontraron resultados o timeout esperando resultados.");
                    return results;
                }

                // Buscar todos los elementos de resultado
                var resultElements = await page.QuerySelectorAllAsync(
                    ".search-result, .result-entity, [class*='SearchResult']"
                );

                Console.WriteLine($"Encontrados {resultElements.Count} elementos de resultado");

                // Iterar sobre cada resultado y extraer los datos
                foreach (var element in resultElements)
                {
                    try
                    {
                        var record = new OffshoreLeaksRecord();

                        // Extraer Entity (nombre de la entidad)
                        // Buscamos en múltiples posibles selectores
                        var entityElement = await element.QuerySelectorAsync(
                            "h3, h4, .entity-name, [class*='entityName'], [class*='EntityName']"
                        );
                        if (entityElement != null)
                        {
                            record.Entity = (await entityElement.InnerTextAsync()).Trim();
                        }

                        // Extraer Jurisdiction
                        var jurisdictionElement = await element.QuerySelectorAsync(
                            "[class*='jurisdiction'], dt:has-text('Jurisdiction') + dd, .country"
                        );
                        if (jurisdictionElement != null)
                        {
                            record.Jurisdiction = (await jurisdictionElement.InnerTextAsync()).Trim();
                        }

                        // Extraer Linked To
                        var linkedToElement = await element.QuerySelectorAsync(
                            "[class*='linked'], dt:has-text('Linked') + dd, [class*='connection']"
                        );
                        if (linkedToElement != null)
                        {
                            record.LinkedTo = (await linkedToElement.InnerTextAsync()).Trim();
                        }

                        // Extraer Data From
                        var dataFromElement = await element.QuerySelectorAsync(
                            "[class*='source'], dt:has-text('Data') + dd, [class*='dataset']"
                        );
                        if (dataFromElement != null)
                        {
                            record.DataFrom = (await dataFromElement.InnerTextAsync()).Trim();
                        }

                        // Solo agregar si al menos el Entity tiene valor
                        if (!string.IsNullOrWhiteSpace(record.Entity))
                        {
                            results.Add(record);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error extrayendo datos de un elemento: {ex.Message}");
                        continue;
                    }
                }

                Console.WriteLine($"Scraping completado. Total de registros: {results.Count}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error durante el scraping: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
            finally
            {
                // Cerrar la página
                await page.CloseAsync();
            }

            return results;
        }
    }

    // Programa principal
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== Offshore Leaks Scraper con Playwright ===\n");

            // Obtener el término de búsqueda desde argumentos o usar "Lima" por defecto
            string searchTerm = args.Length > 0 ? args[0] : "Lima";

            Console.WriteLine($"Buscando: {searchTerm}\n");

            var scraper = new OffshoreLeaksScraper();

            try
            {
                var results = await scraper.ScrapeAsync(searchTerm);

                if (results.Count > 0)
                {
                    Console.WriteLine($"\n=== Resultados ({results.Count}) ===\n");

                    foreach (var record in results)
                    {
                        Console.WriteLine(record);
                    }
                }
                else
                {
                    Console.WriteLine("\nNo se encontraron resultados.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nError: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }

            Console.WriteLine("\nPresiona cualquier tecla para salir...");
            Console.ReadKey();
        }
    }
}

/*
 * NOTAS IMPORTANTES:
 *
 * 1. INSTALACIÓN DE PLAYWRIGHT:
 *    Antes de ejecutar este programa, debes instalar los navegadores de Playwright:
 *
 *    dotnet add package Microsoft.Playwright
 *    dotnet build
 *    playwright install
 *
 * 2. ¿POR QUÉ PLAYWRIGHT Y NO HttpClient?
 *    - La página de Offshore Leaks carga datos dinámicamente con JavaScript
 *    - HttpClient solo descarga el HTML inicial, sin ejecutar JavaScript
 *    - Playwright lanza un navegador real que ejecuta JavaScript y renderiza el DOM completo
 *
 * 3. MODOS DE EJECUCIÓN:
 *    - Headless = true: El navegador se ejecuta sin ventana visible (más rápido, ideal para producción)
 *    - Headless = false: Puedes ver el navegador en acción (útil para debugging)
 *
 * 4. SELECTORES CSS:
 *    Los selectores usados ([class*='result'], etc.) son genéricos porque la estructura
 *    exacta de la página puede cambiar. En producción, deberías inspeccionar la página
 *    real con DevTools y ajustar los selectores según sea necesario.
 *
 * 5. TIMEOUTS:
 *    - GotoAsync timeout: 60 segundos (tiempo máximo para cargar la página)
 *    - WaitForSelectorAsync timeout: 30 segundos (tiempo máximo para que aparezcan resultados)
 *
 *    Ajusta estos valores según la velocidad de tu conexión y la carga del servidor.
 *
 * 6. MANEJO DE ERRORES:
 *    El código incluye múltiples try-catch para manejar:
 *    - Timeouts de carga
 *    - Elementos no encontrados
 *    - Errores de parsing
 *
 *    Esto asegura que el programa no se caiga si la estructura de la página cambia.
 *
 * 7. RENDIMIENTO:
 *    Playwright es más lento que una API REST porque:
 *    - Tiene que lanzar un navegador completo
 *    - Ejecutar JavaScript
 *    - Renderizar el DOM
 *
 *    Para mejorar el rendimiento:
 *    - Reutiliza la instancia del navegador si haces múltiples búsquedas
 *    - Usa browser contexts para ejecutar múltiples búsquedas en paralelo
 *    - Considera usar cache para resultados frecuentes
 */
