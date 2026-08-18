using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Meals.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Platos",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Platos",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Platos",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "Platos",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Platos",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Platos",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "Platos",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Ingredientes",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Ingredientes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Ingredientes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "Ingredientes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Ingredientes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Ingredientes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "Ingredientes",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Platos");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Platos");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Platos");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Platos");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Platos");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Platos");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Platos");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Ingredientes");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Ingredientes");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Ingredientes");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Ingredientes");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Ingredientes");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Ingredientes");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Ingredientes");
        }
    }
}
