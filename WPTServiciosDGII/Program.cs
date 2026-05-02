using Microsoft.EntityFrameworkCore;
using WPTServiciosDGII.Core.Interfaces;
using WPTServiciosDGII.Data;
using WPTServiciosDGII.Infrastructure.Data;
using WPTServiciosDGII.Infrastructure.External;
using WPTServiciosDGII.Infrastructure.Security;
using WPTServiciosDGII.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Servicios ────────────────────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddXmlSerializerFormatters(); // Habilitar soporte para XML (necesario para DGII)
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title       = "WPT Servicios DGII",
        Version     = "v1",
        Description = "API de recepción, autenticación y aprobación comercial para certificación DGII e-CF. " +
                      "Dominios: cloud.wptsoftwares.net | wptsoftwares.giize.com/WPTexecutor"
    });
});

// ── Entity Framework Core — SQL Server ───────────────────────────────────────
builder.Services.AddDbContext<WptDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("WptDatabase")));

// ── FASE 0: Memory Cache ──────────────────────────────────────────────────────
builder.Services.AddMemoryCache();

// ── FASE 1: Resolución dinámica de BD externa ─────────────────────────────────
builder.Services.AddSingleton<IDbResolver, DynamicDbResolver>();

// ── FASE 2: Repositorio Nucleo externo ────────────────────────────────────────
builder.Services.AddScoped<INucleoRepository, NucleoRepository>();

// ── FASE 3: Cargador de certificados .p12 ────────────────────────────────────
builder.Services.AddTransient<ICertificadoLoader, CertificadoLoader>();

// ── Servicios propios ─────────────────────────────────────────────────────────
builder.Services.AddScoped<ILogInteraccionService, LogInteraccionService>();

// ── CORS (permite ambos dominios + localhost para desarrollo) ─────────────────
var allowedOrigins = builder.Configuration
    .GetSection("AppSettings:AllowedOrigins")
    .Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy("WptCors", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
    // Política abierta para DGII y herramientas frontales locales
    options.AddPolicy("DgiiServers", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// ── Logging estructurado ──────────────────────────────────────────────────────
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();

// ── Aplicar migraciones automáticamente al iniciar ───────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db     = scope.ServiceProvider.GetRequiredService<WptDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        db.Database.Migrate();
        logger.LogInformation("✅ Migraciones aplicadas correctamente.");
    }
    catch (Exception ex)
    {
        logger.LogWarning("⚠️  No se pudo conectar a la BD al iniciar: {Msg}. " +
                          "Verifica la cadena de conexión en appsettings.json. " +
                          "Ejecuta manualmente: dotnet ef database update", ex.Message);
    }
}

// ── Pipeline HTTP ─────────────────────────────────────────────────────────────
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("v1/swagger.json", "WPT Servicios DGII v1");
    c.RoutePrefix = "swagger";
});

// app.UseHttpsRedirection(); // Desactivado para evitar conflictos de puertos en IIS (cloud.wptsoftwares.net)
app.UseCors("DgiiServers");   // Aplicar CORS permisivo para que DGII pueda llamar a los endpoints
app.UseAuthorization();
app.MapControllers();

// ── Health check con estado de configuración ──────────────────────────────────
app.MapGet("/health", (IConfiguration config) =>
{
    var tenants  = config.GetSection("ExternalDbConfig:Tenants").GetChildren().Select(t => t.Key).ToList();
    var certPath = config["CertificadosConfig:RutaBase"];
    return Results.Ok(new
    {
        status   = "healthy",
        time     = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
        domains  = new[] { "https://cloud.wptsoftwares.net", "https://wptsoftwares.giize.com/WPTexecutor" },
        tenants_registrados = tenants,
        ruta_certificados   = certPath
    });
});

app.Run();
