using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace moneytree.Migrations
{
    public partial class indexes1 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Ticks_SymbolId_TickEpoch",
                table: "Ticks",
                columns: new[] { "SymbolId", "TickEpoch" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Ticks_SymbolId_TickEpoch",
                table: "Ticks");
        }
    }
}
