using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectSaborLatino.Migrations
{
    /// <inheritdoc />
    public partial class SolicitudWhatsAppManual : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "CorreoContacto",
                table: "Solicitudes",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256);

            migrationBuilder.AddColumn<Guid>(
                name: "ClaveIdempotencia",
                table: "Solicitudes",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HuellaIdempotencia",
                table: "Solicitudes",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RecibidoWhatsAppEn",
                table: "Solicitudes",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecibidoWhatsAppPorUsuarioId",
                table: "Solicitudes",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Solicitudes_ClaveIdempotencia",
                table: "Solicitudes",
                column: "ClaveIdempotencia",
                unique: true,
                filter: "[ClaveIdempotencia] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Solicitudes_ClaveIdempotencia",
                table: "Solicitudes");

            migrationBuilder.DropColumn(
                name: "ClaveIdempotencia",
                table: "Solicitudes");

            migrationBuilder.DropColumn(
                name: "HuellaIdempotencia",
                table: "Solicitudes");

            migrationBuilder.DropColumn(
                name: "RecibidoWhatsAppEn",
                table: "Solicitudes");

            migrationBuilder.DropColumn(
                name: "RecibidoWhatsAppPorUsuarioId",
                table: "Solicitudes");

            migrationBuilder.AlterColumn<string>(
                name: "CorreoContacto",
                table: "Solicitudes",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256,
                oldNullable: true);
        }
    }
}
