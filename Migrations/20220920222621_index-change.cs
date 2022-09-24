using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace moneytree.Migrations
{
    public partial class indexchange : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
        }
    }
}
