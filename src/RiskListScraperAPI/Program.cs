using RiskListScraperAPI.Middleware;
using RiskListScraperAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Risk List Scraper API",
        Version = "v1",
        Description = "API for searching entities in risk lists (OFAC, World Bank, Offshore Leaks)",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "Risk List Scraper",
            Email = "support@example.com"
        }
    });

    // Add API Key authentication to Swagger
    c.AddSecurityDefinition("ApiKey", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "API Key needed to access the endpoints. X-API-Key: YOUR_API_KEY",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Name = "X-API-Key",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "ApiKeyScheme"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "ApiKey"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Register HttpClient
builder.Services.AddHttpClient();

// Register scraper services
builder.Services.AddScoped<IScraperService, OfacScraperService>();
builder.Services.AddScoped<IScraperService, WorldBankScraperService>();
builder.Services.AddScoped<IScraperService, OffshoreLeaksScraperService>();
builder.Services.AddScoped<RiskListSearchService>();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Risk List Scraper API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseCors("AllowAll");

// Apply rate limiting before authentication
app.UseMiddleware<RateLimitingMiddleware>();

// Apply API Key authentication
app.UseMiddleware<ApiKeyAuthMiddleware>();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
