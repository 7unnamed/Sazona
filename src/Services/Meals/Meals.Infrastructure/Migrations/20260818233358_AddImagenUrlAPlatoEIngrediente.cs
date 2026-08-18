using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Meals.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddImagenUrlAPlatoEIngrediente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImagenUrl",
                table: "Platos",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImagenUrl",
                table: "Ingredientes",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImagenUrl",
                table: "Platos");

            migrationBuilder.DropColumn(
                name: "ImagenUrl",
                table: "Ingredientes");
        }
    }
}
