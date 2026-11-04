using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectSaborLatino.Migrations
{
    /// <inheritdoc />
    public partial class TraduccionesI18nDinamicas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TraduccionesI18n",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HashOrigen = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    TextoOrigen = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    IdiomaDestino = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    TextoTraducido = table.Column<string>(type: "nvarchar(2500)", maxLength: 2500, nullable: false),
                    EsAutomatica = table.Column<bool>(type: "bit", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TraduccionesI18n", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TraduccionesI18n_HashOrigen_IdiomaDestino",
                table: "TraduccionesI18n",
                columns: new[] { "HashOrigen", "IdiomaDestino" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TraduccionesI18n");
        }
    }
}
