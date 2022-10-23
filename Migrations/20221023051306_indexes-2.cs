using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace moneytree.Migrations
{
    public partial class indexes2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Ticks_TickEpoch",
                table: "Ticks",
                column: "TickEpoch");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Ticks_TickEpoch",
                table: "Ticks");
        }
    }
}
