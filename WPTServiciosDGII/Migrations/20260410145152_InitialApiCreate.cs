using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WPTServiciosDGII.Migrations
{
    /// <inheritdoc />
    public partial class InitialApiCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Api_DocumentoRecibido",
                columns: table => new
                {
                    DocumentoRecibidoId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NucleoID = table.Column<int>(type: "int", nullable: true),
                    DocumentoRecibidoTipo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DocumentoRecibidoNCF = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DocumentoRecibidoRncEmisor = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    DocumentoRecibidoRncReceptor = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    DocumentoRecibidoXML = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DocumentoRecibidoTrackId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DocumentoRecibidoEstado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DocumentoRecibidoFecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DocumentoRecibidoMensaje = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Api_DocumentoRecibido", x => x.DocumentoRecibidoId);
                });

            migrationBuilder.CreateTable(
                name: "Api_LogInteraccion",
                columns: table => new
                {
                    LogInteraccionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LogInteraccionFecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LogInteraccionServicio = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LogInteraccionMetodo = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    LogInteraccionEndpoint = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    LogInteraccionIpOrigen = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LogInteraccionRequestBody = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LogInteraccionResponseBody = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LogInteraccionEstado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    LogInteraccionMsRespuesta = table.Column<int>(type: "int", nullable: false),
                    LogInteraccionRnc = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    LogInteraccionTokenId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Api_LogInteraccion", x => x.LogInteraccionId);
                });

            migrationBuilder.CreateTable(
                name: "Api_SemillaGenerada",
                columns: table => new
                {
                    SemillaGeneradaId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SemillaGeneradaValor = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SemillaGeneradaFecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SemillaGeneradaUsada = table.Column<bool>(type: "bit", nullable: false),
                    SemillaGeneradaRnc = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Api_SemillaGenerada", x => x.SemillaGeneradaId);
                });

            migrationBuilder.CreateTable(
                name: "Api_TokenEmitido",
                columns: table => new
                {
                    TokenEmitidoId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TokenEmitidoValor = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    TokenEmitidoRnc = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    TokenEmitidoFechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TokenEmitidoFechaExpiracion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TokenEmitidoActivo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Api_TokenEmitido", x => x.TokenEmitidoId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Api_DocumentoRecibido_DocumentoRecibidoNCF",
                table: "Api_DocumentoRecibido",
                column: "DocumentoRecibidoNCF");

            migrationBuilder.CreateIndex(
                name: "IX_Api_DocumentoRecibido_DocumentoRecibidoTrackId",
                table: "Api_DocumentoRecibido",
                column: "DocumentoRecibidoTrackId");

            migrationBuilder.CreateIndex(
                name: "IX_Api_LogInteraccion_LogInteraccionFecha",
                table: "Api_LogInteraccion",
                column: "LogInteraccionFecha");

            migrationBuilder.CreateIndex(
                name: "IX_Api_LogInteraccion_LogInteraccionServicio",
                table: "Api_LogInteraccion",
                column: "LogInteraccionServicio");

            migrationBuilder.CreateIndex(
                name: "IX_Api_SemillaGenerada_SemillaGeneradaValor",
                table: "Api_SemillaGenerada",
                column: "SemillaGeneradaValor",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Api_TokenEmitido_TokenEmitidoActivo_TokenEmitidoFechaExpiracion",
                table: "Api_TokenEmitido",
                columns: new[] { "TokenEmitidoActivo", "TokenEmitidoFechaExpiracion" });

            migrationBuilder.CreateIndex(
                name: "IX_Api_TokenEmitido_TokenEmitidoValor",
                table: "Api_TokenEmitido",
                column: "TokenEmitidoValor");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Api_DocumentoRecibido");

            migrationBuilder.DropTable(
                name: "Api_LogInteraccion");

            migrationBuilder.DropTable(
                name: "Api_SemillaGenerada");

            migrationBuilder.DropTable(
                name: "Api_TokenEmitido");
        }
    }
}
