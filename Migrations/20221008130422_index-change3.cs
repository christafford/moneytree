using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace moneytree.Migrations
{
    public partial class indexchange3 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Ticks_OpenTime_SymbolId",
                table: "Ticks");

            migrationBuilder.CreateIndex(
                name: "IX_Ticks_OpenTime",
                table: "Ticks",
                column: "OpenTime");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Ticks_OpenTime",
                table: "Ticks");

            migrationBuilder.CreateIndex(
                name: "IX_Ticks_OpenTime_SymbolId",
                table: "Ticks",
                columns: new[] { "OpenTime", "SymbolId" },
                unique: true);
        }
    }
}
