using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectSaborLatino.Migrations
{
    /// <inheritdoc />
    public partial class GaleriaTituloEn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TituloEn",
                table: "ImagenesGaleria",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TituloEn",
                table: "ImagenesGaleria");
        }
    }
}
