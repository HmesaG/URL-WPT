using Microsoft.EntityFrameworkCore;
using WPTServiciosDGII.Models;

namespace WPTServiciosDGII.Data;

public class WptDbContext : DbContext
{
    public WptDbContext(DbContextOptions<WptDbContext> options) : base(options) { }

    public DbSet<SemillaGenerada>   SemillasGeneradas   { get; set; }
    public DbSet<TokenEmitido>      TokensEmitidos      { get; set; }
    public DbSet<DocumentoRecibido> DocumentosRecibidos { get; set; }
    public DbSet<LogInteraccion>    LogInteracciones    { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Índices para consultas frecuentes
        modelBuilder.Entity<SemillaGenerada>()
            .HasIndex(s => s.SemillaGeneradaValor).IsUnique();

        modelBuilder.Entity<TokenEmitido>()
            .HasIndex(t => t.TokenEmitidoValor);

        modelBuilder.Entity<TokenEmitido>()
            .HasIndex(t => new { t.TokenEmitidoActivo, t.TokenEmitidoFechaExpiracion });

        modelBuilder.Entity<DocumentoRecibido>()
            .HasIndex(d => d.DocumentoRecibidoTrackId);

        modelBuilder.Entity<DocumentoRecibido>()
            .HasIndex(d => d.DocumentoRecibidoNCF);

        modelBuilder.Entity<LogInteraccion>()
            .HasIndex(l => l.LogInteraccionFecha);

        modelBuilder.Entity<LogInteraccion>()
            .HasIndex(l => l.LogInteraccionServicio);
    }
}
