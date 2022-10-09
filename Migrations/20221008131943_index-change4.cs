using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace moneytree.Migrations
{
    public partial class indexchange4 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "VolumeUsd",
                table: "Ticks",
                type: "decimal(65,30)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_Ticks_SymbolId_OpenTime",
                table: "Ticks",
                columns: new[] { "SymbolId", "OpenTime" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Ticks_SymbolId_OpenTime",
                table: "Ticks");

            migrationBuilder.DropColumn(
                name: "VolumeUsd",
                table: "Ticks");
        }
    }
}
