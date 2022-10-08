using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace moneytree.Migrations
{
    public partial class indexchange2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Ticks_OpenTime",
                table: "Ticks");

            migrationBuilder.DropIndex(
                name: "IX_Ticks_SymbolId_OpenTime",
                table: "Ticks");

            migrationBuilder.CreateIndex(
                name: "IX_Ticks_OpenTime_SymbolId",
                table: "Ticks",
                columns: new[] { "OpenTime", "SymbolId" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Ticks_OpenTime_SymbolId",
                table: "Ticks");

            migrationBuilder.CreateIndex(
                name: "IX_Ticks_OpenTime",
                table: "Ticks",
                column: "OpenTime");

            migrationBuilder.CreateIndex(
                name: "IX_Ticks_SymbolId_OpenTime",
                table: "Ticks",
                columns: new[] { "SymbolId", "OpenTime" },
                unique: true);
        }
    }
}
