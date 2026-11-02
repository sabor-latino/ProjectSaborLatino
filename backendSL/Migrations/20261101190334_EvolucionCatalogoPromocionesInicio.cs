using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectSaborLatino.Migrations
{
    /// <inheritdoc />
    public partial class EvolucionCatalogoPromocionesInicio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DescripcionEn",
                table: "Servicios",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NombreEn",
                table: "Servicios",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreguntaEn",
                table: "PreguntasFrecuentes",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RespuestaEn",
                table: "PreguntasFrecuentes",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CoberturaAforo",
                table: "Paquetes",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DescripcionEn",
                table: "Paquetes",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DuracionMontaje",
                table: "Paquetes",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EspacioMinimo",
                table: "Paquetes",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FichaTecnica",
                table: "Paquetes",
                type: "nvarchar(1500)",
                maxLength: 1500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FichaTecnicaEn",
                table: "Paquetes",
                type: "nvarchar(1500)",
                maxLength: 1500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NombreEn",
                table: "Paquetes",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PotenciaSonido",
                table: "Paquetes",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequerimientosElectricos",
                table: "Paquetes",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EspecificacionTecnica",
                table: "Equipos",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EspecificacionTecnicaEn",
                table: "Equipos",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NombreEn",
                table: "Equipos",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PromocionesTemporada",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    NombreEn = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Descripcion = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DescripcionEn = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    EtiquetaTemporada = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    EtiquetaTemporadaEn = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    PorcentajeDescuento = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    CodigoPromocional = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    FechaInicio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaFin = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ImagenUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DestacadaEnInicio = table.Column<bool>(type: "bit", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromocionesTemporada", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PromocionesTemporadaPaquetes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PromocionTemporadaId = table.Column<int>(type: "int", nullable: false),
                    PaqueteId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromocionesTemporadaPaquetes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PromocionesTemporadaPaquetes_Paquetes_PaqueteId",
                        column: x => x.PaqueteId,
                        principalTable: "Paquetes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PromocionesTemporadaPaquetes_PromocionesTemporada_PromocionTemporadaId",
                        column: x => x.PromocionTemporadaId,
                        principalTable: "PromocionesTemporada",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PromocionesTemporadaPaquetes_PaqueteId",
                table: "PromocionesTemporadaPaquetes",
                column: "PaqueteId");

            migrationBuilder.CreateIndex(
                name: "IX_PromocionesTemporadaPaquetes_PromocionTemporadaId_PaqueteId",
                table: "PromocionesTemporadaPaquetes",
                columns: new[] { "PromocionTemporadaId", "PaqueteId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PromocionesTemporadaPaquetes");

            migrationBuilder.DropTable(
                name: "PromocionesTemporada");

            migrationBuilder.DropColumn(
                name: "DescripcionEn",
                table: "Servicios");

            migrationBuilder.DropColumn(
                name: "NombreEn",
                table: "Servicios");

            migrationBuilder.DropColumn(
                name: "PreguntaEn",
                table: "PreguntasFrecuentes");

            migrationBuilder.DropColumn(
                name: "RespuestaEn",
                table: "PreguntasFrecuentes");

            migrationBuilder.DropColumn(
                name: "CoberturaAforo",
                table: "Paquetes");

            migrationBuilder.DropColumn(
                name: "DescripcionEn",
                table: "Paquetes");

            migrationBuilder.DropColumn(
                name: "DuracionMontaje",
                table: "Paquetes");

            migrationBuilder.DropColumn(
                name: "EspacioMinimo",
                table: "Paquetes");

            migrationBuilder.DropColumn(
                name: "FichaTecnica",
                table: "Paquetes");

            migrationBuilder.DropColumn(
                name: "FichaTecnicaEn",
                table: "Paquetes");

            migrationBuilder.DropColumn(
                name: "NombreEn",
                table: "Paquetes");

            migrationBuilder.DropColumn(
                name: "PotenciaSonido",
                table: "Paquetes");

            migrationBuilder.DropColumn(
                name: "RequerimientosElectricos",
                table: "Paquetes");

            migrationBuilder.DropColumn(
                name: "EspecificacionTecnica",
                table: "Equipos");

            migrationBuilder.DropColumn(
                name: "EspecificacionTecnicaEn",
                table: "Equipos");

            migrationBuilder.DropColumn(
                name: "NombreEn",
                table: "Equipos");
        }
    }
}
