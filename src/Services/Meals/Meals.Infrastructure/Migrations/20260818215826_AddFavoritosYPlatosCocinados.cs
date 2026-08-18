using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Meals.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFavoritosYPlatosCocinados : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Favoritos",
                columns: table => new
                {
                    IdFavorito = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdUsuario = table.Column<int>(type: "integer", nullable: false),
                    IdPlato = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Favoritos", x => x.IdFavorito);
                    table.ForeignKey(
                        name: "FK_Favoritos_Platos_IdPlato",
                        column: x => x.IdPlato,
                        principalTable: "Platos",
                        principalColumn: "IdPlato",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlatosCocinados",
                columns: table => new
                {
                    IdPlatoCocinado = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdUsuario = table.Column<int>(type: "integer", nullable: false),
                    IdPlato = table.Column<int>(type: "integer", nullable: false),
                    FechaCocinado = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlatosCocinados", x => x.IdPlatoCocinado);
                    table.ForeignKey(
                        name: "FK_PlatosCocinados_Platos_IdPlato",
                        column: x => x.IdPlato,
                        principalTable: "Platos",
                        principalColumn: "IdPlato",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Favoritos_IdPlato",
                table: "Favoritos",
                column: "IdPlato");

            migrationBuilder.CreateIndex(
                name: "IX_Favoritos_IdUsuario_IdPlato",
                table: "Favoritos",
                columns: new[] { "IdUsuario", "IdPlato" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlatosCocinados_IdPlato",
                table: "PlatosCocinados",
                column: "IdPlato");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Favoritos");

            migrationBuilder.DropTable(
                name: "PlatosCocinados");
        }
    }
}
